using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using VinculoClienteLocadora = AutoVia.Core.Entities.PgSql.ClienteLocadora;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Armazenamento em memoria do vinculo cliente x locadora. Fica separado do repositorio para que o teste HTTP
    /// (um repositorio por requisicao, com o contexto da requisicao) enxergue os mesmos vinculos semeados.
    /// </summary>
    public sealed class ArmazenamentoClienteLocadora
    {
        /// <summary>Locadoras semeadas (ativas ou nao).</summary>
        public List<Empresa> Empresas { get; } = new List<Empresa>();

        /// <summary>Todos os vinculos gravados (ativos e desativados), na ordem de gravacao.</summary>
        public List<VinculoClienteLocadora> Vinculos { get; } = new List<VinculoClienteLocadora>();

        /// <summary>Proximo id a gerar (o banco geraria um SERIAL).</summary>
        public int ProximoId { get; set; } = 1;

        /// <summary>Ids de clientes desativados (DisabledAt). Qualquer outro id conta como ativo no fake.</summary>
        public HashSet<int> ClientesDesativados { get; } = new HashSet<int>();

        /// <summary>Clientes semeados com os dados que a fila e o detalhe mostram. Vinculo de cliente fora desta lista nao aparece na fila.</summary>
        public List<Cliente> Clientes { get; } = new List<Cliente>();

        /// <summary>Semeia uma locadora sem passar pelo servico.</summary>
        public void AdicionarEmpresa(Empresa empresa)
        {
            Empresas.Add(empresa);
        }

        /// <summary>Data de solicitacao padrao: fixa e antiga (fora de qualquer janela de 24 horas), para o resultado nao depender do dia da execucao.</summary>
        public static readonly DateTime DataSolicitacaoPadrao = new DateTime(2020, 1, 1, 10, 0, 0);

        /// <summary>Semeia um vinculo (ativo, ou desativado quando desativadoEm for informado), com id gerado. Sem dataSolicitacao, a data e fixa e antiga (DataSolicitacaoPadrao menos o id, em dias).</summary>
        public VinculoClienteLocadora Semear(int idCliente, int idEmpresa, StatusCadastroCliente status, DateTime? desativadoEm = null, string? justificativa = null, DateTime? dataSolicitacao = null)
        {
            DateTime agora = DateTime.Now;

            VinculoClienteLocadora vinculo = new VinculoClienteLocadora
            {
                IdClienteLocadora = ProximoId,
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                Status = status,
                JustificativaRecusa = justificativa,
                DataSolicitacao = dataSolicitacao ?? DataSolicitacaoPadrao.AddDays(-ProximoId),
                CreatedAt = agora,
                UpdatedAt = agora,
                DisabledAt = desativadoEm
            };

            ProximoId++;
            Vinculos.Add(vinculo);

            return vinculo;
        }
    }

    /// <summary>
    /// IClienteLocadoraRepository em memoria, com o mesmo escopo do repositorio real: so enxerga os vinculos da locadora
    /// do contexto (IdEmpresa) OU do cliente do contexto (IdCliente), e nenhum quando o contexto nao tem nenhum dos dois
    /// (fail-closed). Ignora os desativados nas leituras. Gancho AoInserir: roda antes do INSERT, para simular a corrida
    /// de dois pedidos ao mesmo tempo (a linha concorrente ganha id menor).
    /// </summary>
    public class FakeClienteLocadoraRepository : IClienteLocadoraRepository
    {
        private readonly IContextoEmpresa _contexto;
        private readonly ArmazenamentoClienteLocadora _armazenamento;

        public FakeClienteLocadoraRepository(IContextoEmpresa contexto, ArmazenamentoClienteLocadora armazenamento)
        {
            _contexto = contexto;
            _armazenamento = armazenamento;
        }

        /// <summary>Quando nao for null, roda antes de cada INSERT (simula outro pedido gravado primeiro).</summary>
        public Action<ArmazenamentoClienteLocadora>? AoInserir { get; set; }

        /// <summary>Quantas vezes DesativarAsync foi chamado (conferir a regra da corrida).</summary>
        public int ChamadasDeDesativar { get; private set; }

        /// <summary>Quantas vezes a contagem de pedidos do teto foi consultada (zero prova que o teto nao rodou).</summary>
        public int ChamadasDeContagem { get; private set; }

        /// <summary>Copias de todos os vinculos gravados (ativos e desativados), para conferir o que foi gravado.</summary>
        public IReadOnlyList<VinculoClienteLocadora> Armazenados
        {
            get
            {
                return _armazenamento.Vinculos.Select(Copiar).ToList();
            }
        }

        public Task<Empresa?> FindEmpresaAtivaAsync(int idEmpresa)
        {
            Empresa? empresa = _armazenamento.Empresas.FirstOrDefault(x => x.IdEmpresa == idEmpresa && x.DisabledAt == null && x.Ativo);

            return Task.FromResult(empresa);
        }

        public Task<bool> ClienteAtivoAsync(int idCliente)
        {
            bool ativo = !_armazenamento.ClientesDesativados.Contains(idCliente);

            return Task.FromResult(ativo);
        }

        public Task<VinculoClienteLocadora?> FindAtivoMaisAntigoAsync(int idCliente, int idEmpresa)
        {
            VinculoClienteLocadora? vinculo = VisiveisAtivos()
                .Where(x => x.IdCliente == idCliente && x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.IdClienteLocadora)
                .Select(Copiar)
                .FirstOrDefault();

            return Task.FromResult(vinculo);
        }

        public Task<VinculoClienteLocadora> InsertAsync(VinculoClienteLocadora vinculo)
        {
            if (AoInserir != null)
            {
                AoInserir(_armazenamento);
            }

            DateTime agora = DateTime.Now;

            VinculoClienteLocadora novo = new VinculoClienteLocadora
            {
                IdClienteLocadora = _armazenamento.ProximoId,
                IdCliente = vinculo.IdCliente,
                IdEmpresa = vinculo.IdEmpresa,
                Status = StatusCadastroCliente.PENDENTE,
                JustificativaRecusa = null,
                IdFuncionarioVerificador = null,
                DataSolicitacao = vinculo.DataSolicitacao,
                DataVerificacao = null,
                CreatedAt = agora,
                UpdatedAt = agora,
                DisabledAt = null
            };

            _armazenamento.ProximoId++;
            _armazenamento.Vinculos.Add(novo);

            return Task.FromResult(Copiar(novo));
        }

        public Task<bool> DesativarAsync(int idClienteLocadora)
        {
            ChamadasDeDesativar++;

            VinculoClienteLocadora? vinculo = VisiveisAtivos().FirstOrDefault(x => x.IdClienteLocadora == idClienteLocadora);

            if (vinculo == null)
            {
                return Task.FromResult(false);
            }

            vinculo.Disable();

            return Task.FromResult(true);
        }

        public Task<bool> DecidirParAsync(int idCliente, int idEmpresa, StatusCadastroCliente status, string? justificativa, int idFuncionarioVerificador, DateTime dataVerificacao)
        {
            List<VinculoClienteLocadora> ativos = VisiveisAtivos()
                .Where(x => x.IdCliente == idCliente && x.IdEmpresa == idEmpresa)
                .ToList();

            if (ativos.Count == 0)
            {
                return Task.FromResult(false);
            }

            foreach (VinculoClienteLocadora vinculo in ativos)
            {
                vinculo.Status = status;
                vinculo.JustificativaRecusa = status == StatusCadastroCliente.RECUSADO ? justificativa : null;
                vinculo.IdFuncionarioVerificador = idFuncionarioVerificador;
                vinculo.DataVerificacao = dataVerificacao;
                vinculo.UpdatedAt = DateTime.Now;
            }

            return Task.FromResult(true);
        }

        public Task<int> CountMinhasAsync(int idCliente)
        {
            int total = MinhasPorLocadora(idCliente).Count();

            return Task.FromResult(total);
        }

        public Task<int> CountEmpresasSolicitadasDesdeAsync(int idCliente, DateTime desde)
        {
            ChamadasDeContagem++;

            // Ativas e desativadas contam (como a consulta real, sem filtro de DisabledAt), uma vez por locadora.
            int total = VisiveisTodos()
                .Where(x => x.IdCliente == idCliente && x.DataSolicitacao >= desde)
                .Select(x => x.IdEmpresa)
                .Distinct()
                .Count();

            return Task.FromResult(total);
        }

        public Task<List<VinculoClienteLocadora>> FindMinhasAsync(int idCliente, int deslocamento, int quantidade)
        {
            List<VinculoClienteLocadora> pagina = MinhasPorLocadora(idCliente)
                .OrderBy(x => x.DataSolicitacao)
                .ThenBy(x => x.IdClienteLocadora)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(Copiar)
                .ToList();

            return Task.FromResult(pagina);
        }

        public Task<int> CountFilaAsync(StatusCadastroCliente? status, string? busca, string? digitosCpf)
        {
            int total = Fila(status, busca, digitosCpf).Count();

            return Task.FromResult(total);
        }

        public Task<List<VinculoClienteLocadora>> FindFilaAsync(StatusCadastroCliente? status, string? busca, string? digitosCpf, int deslocamento, int quantidade)
        {
            List<VinculoClienteLocadora> pagina = Fila(status, busca, digitosCpf)
                .OrderBy(x => x.DataSolicitacao)
                .ThenBy(x => x.IdCliente)
                .Skip(deslocamento)
                .Take(quantidade)
                .ToList();

            return Task.FromResult(pagina);
        }

        public Task<VinculoClienteLocadora?> FindVinculoComClienteAsync(int idCliente, int idEmpresa)
        {
            VinculoClienteLocadora? vinculo = VisiveisAtivos()
                .Where(x => x.IdCliente == idCliente && x.IdEmpresa == idEmpresa)
                .OrderBy(x => x.IdClienteLocadora)
                .FirstOrDefault();

            if (vinculo == null || ClienteAtivoDoArmazenamento(idCliente) == null)
            {
                return Task.FromResult<VinculoClienteLocadora?>(null);
            }

            return Task.FromResult<VinculoClienteLocadora?>(CopiarComCliente(vinculo));
        }

        /// <summary>
        /// Fila como a consulta real: clientes ativos (cadastrados no armazenamento e nao desativados) com a linha que vale do par
        /// (a ativa de menor id do par cliente x locadora), filtradas por status e busca. Cada item sai com o cliente anexado.
        /// </summary>
        private List<VinculoClienteLocadora> Fila(StatusCadastroCliente? status, string? busca, string? digitosCpf)
        {
            List<VinculoClienteLocadora> ativos = VisiveisAtivos().ToList();
            List<VinculoClienteLocadora> resultado = new List<VinculoClienteLocadora>();

            foreach (VinculoClienteLocadora vinculo in ativos)
            {
                int menorId = ativos
                    .Where(y => y.IdCliente == vinculo.IdCliente && y.IdEmpresa == vinculo.IdEmpresa)
                    .Min(y => y.IdClienteLocadora);

                if (menorId != vinculo.IdClienteLocadora)
                {
                    continue;
                }

                Cliente? cliente = ClienteAtivoDoArmazenamento(vinculo.IdCliente);

                if (cliente == null)
                {
                    continue;
                }

                if (status.HasValue && vinculo.Status != status.Value)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(busca) && !CasaBusca(cliente, busca, digitosCpf))
                {
                    continue;
                }

                resultado.Add(CopiarComCliente(vinculo));
            }

            return resultado;
        }

        /// <summary>Cliente semeado e nao desativado; nulo quando nao esta no armazenamento.</summary>
        private Cliente? ClienteAtivoDoArmazenamento(int idCliente)
        {
            return _armazenamento.Clientes.FirstOrDefault(x => x.IdCliente == idCliente && x.DisabledAt == null && !_armazenamento.ClientesDesativados.Contains(idCliente));
        }

        /// <summary>Busca como a consulta real: nome e e-mail contem o termo sem diferenciar maiusculas; CPF so com digitos no termo.</summary>
        private static bool CasaBusca(Cliente cliente, string busca, string? digitosCpf)
        {
            string termo = busca.ToLowerInvariant();
            bool casaNomeOuEmail = cliente.Nome.ToLowerInvariant().Contains(termo) || cliente.Email.ToLowerInvariant().Contains(termo);
            bool casaCpf = !string.IsNullOrEmpty(digitosCpf) && cliente.Cpf.Contains(digitosCpf);

            return casaNomeOuEmail || casaCpf;
        }

        /// <summary>Copia do vinculo com a locadora e o cliente anexados, como o Include do repositorio real.</summary>
        private VinculoClienteLocadora CopiarComCliente(VinculoClienteLocadora origem)
        {
            VinculoClienteLocadora copia = Copiar(origem);
            copia.Cliente = ClienteAtivoDoArmazenamento(origem.IdCliente)!;

            return copia;
        }

        /// <summary>Vinculos ativos que o contexto enxerga (o filtro "Empresa"): da locadora do contexto OU do cliente do contexto.</summary>
        private IEnumerable<VinculoClienteLocadora> VisiveisAtivos()
        {
            return VisiveisTodos().Where(x => x.DisabledAt == null);
        }

        /// <summary>Vinculos que o contexto enxerga, ativos e desativados (o filtro "Empresa" nao olha DisabledAt).</summary>
        private IEnumerable<VinculoClienteLocadora> VisiveisTodos()
        {
            bool temEmpresa = _contexto.IdEmpresa > 0;
            bool temCliente = _contexto.IdCliente > 0;

            return _armazenamento.Vinculos.Where(x =>
                (temEmpresa && x.IdEmpresa == _contexto.IdEmpresa) || (temCliente && x.IdCliente == _contexto.IdCliente));
        }

        /// <summary>Uma linha por locadora do cliente: a de menor id ativa do par, como a consulta real.</summary>
        private IEnumerable<VinculoClienteLocadora> MinhasPorLocadora(int idCliente)
        {
            List<VinculoClienteLocadora> ativos = VisiveisAtivos().Where(x => x.IdCliente == idCliente).ToList();

            return ativos
                .GroupBy(x => x.IdEmpresa)
                .Select(grupo => grupo.OrderBy(x => x.IdClienteLocadora).First())
                .Select(Copiar)
                .ToList();
        }

        /// <summary>Copia com a locadora anexada (navegacao Empresa), como o Include do repositorio real.</summary>
        private VinculoClienteLocadora Copiar(VinculoClienteLocadora origem)
        {
            Empresa? empresa = _armazenamento.Empresas.FirstOrDefault(x => x.IdEmpresa == origem.IdEmpresa);

            VinculoClienteLocadora copia = new VinculoClienteLocadora
            {
                IdClienteLocadora = origem.IdClienteLocadora,
                IdCliente = origem.IdCliente,
                IdEmpresa = origem.IdEmpresa,
                Status = origem.Status,
                JustificativaRecusa = origem.JustificativaRecusa,
                IdFuncionarioVerificador = origem.IdFuncionarioVerificador,
                DataSolicitacao = origem.DataSolicitacao,
                DataVerificacao = origem.DataVerificacao,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt,
                Empresa = empresa!
            };

            return copia;
        }
    }
}
