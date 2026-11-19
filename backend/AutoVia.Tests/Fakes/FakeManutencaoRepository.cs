using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IManutencaoRepository em memoria. Abertura e encerramento aplicam as MESMAS regras do repositorio real (trava pela
    /// locadora do contexto, RegrasStatusVeiculo.Avaliar, ordem aberta, datas e km), sem banco. A lista de alertas nao e
    /// calculada aqui: o teste semeia ItensAlertas e TotalAlertas (o SQL e provado em ManutencaoConsultasSqlTests).
    /// </summary>
    public class FakeManutencaoRepository : IManutencaoRepository
    {
        private int _proximoId = 1;

        /// <summary>Locadora do contexto da requisicao simulada. Zero nao acha nenhum veiculo nem ordem (filtro fechado).</summary>
        public int IdEmpresaDoContexto { get; set; }

        /// <summary>Veiculos semeados ou gravados (inclusive desativados).</summary>
        public List<Veiculo> Veiculos { get; } = new List<Veiculo>();

        /// <summary>Ordens de manutencao gravadas e semeadas.</summary>
        public List<Manutencao> Manutencoes { get; } = new List<Manutencao>();

        /// <summary>Reservas que a leitura de reservas em aberto enxerga.</summary>
        public List<Reserva> ReservasDoVeiculo { get; } = new List<Reserva>();

        /// <summary>Locacoes que a leitura de locacoes em aberto enxerga.</summary>
        public List<Locacao> LocacoesDoVeiculo { get; } = new List<Locacao>();

        /// <summary>Itens que ListarAlertasRevisaoAsync devolve (o teste semeia; o teto e aplicado no SQL real).</summary>
        public List<VeiculoRevisaoRegistro> ItensAlertas { get; } = new List<VeiculoRevisaoRegistro>();

        /// <summary>Total que ListarAlertasRevisaoAsync devolve, sem o teto.</summary>
        public int TotalAlertas { get; set; }

        /// <summary>Quando nao for null, as operacoes lancam esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoOperar { get; set; }

        /// <summary>Semeia um veiculo sem passar pelo servico. Se o id for zero, gera um.</summary>
        public Veiculo SemearVeiculo(Veiculo veiculo)
        {
            if (veiculo.IdVeiculo == 0)
            {
                veiculo.IdVeiculo = _proximoId;
            }

            if (veiculo.IdVeiculo >= _proximoId)
            {
                _proximoId = veiculo.IdVeiculo + 1;
            }

            Veiculos.Add(veiculo);

            return veiculo;
        }

        /// <summary>Semeia uma ordem sem passar pelo servico. Se o id for zero, gera um.</summary>
        public Manutencao SemearManutencao(Manutencao manutencao)
        {
            if (manutencao.IdManutencao == 0)
            {
                manutencao.IdManutencao = _proximoId;
                _proximoId++;
            }

            Manutencoes.Add(manutencao);

            return manutencao;
        }

        /// <summary>Abre a ordem com as regras do repositorio real. Lanca NotFoundException ou BusinessException.</summary>
        public Task<Manutencao> AbrirAtomicamenteAsync(Manutencao manutencao)
        {
            if (ExcecaoAoOperar != null)
            {
                throw ExcecaoAoOperar;
            }

            Veiculo? veiculo = VeiculoDoContexto(manutencao.IdVeiculo);

            if (veiculo == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
            }

            bool temReservaEmAberto = ReservasDoVeiculo.Any(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null && RegrasStatusVeiculo.StatusReservasEmAberto().Contains(x.Status));
            bool temLocacaoEmAberto = LocacoesDoVeiculo.Any(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null && RegrasStatusVeiculo.StatusLocacoesEmAberto().Contains(x.Status));

            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(veiculo.Status, StatusVeiculo.MANUTENCAO, temReservaEmAberto, temLocacaoEmAberto);

            if (!resultado.Permitida)
            {
                throw new BusinessException(resultado.MensagemRecusa ?? ConstantsMessage.ErroAbrirManutencao);
            }

            if (Manutencoes.Any(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null && x.DataSaida == null))
            {
                throw new BusinessException(ConstantsMessage.ErroManutencaoJaAberta);
            }

            DateTime agora = DateTime.Now;

            manutencao.IdManutencao = 0;
            manutencao.KmRegistrado = veiculo.KmAtual;
            manutencao.DataSaida = null;
            manutencao.Custo = null;
            manutencao.CreatedAt = agora;
            manutencao.UpdatedAt = agora;
            manutencao.DisabledAt = null;
            SemearManutencao(manutencao);

            if (veiculo.Status != StatusVeiculo.MANUTENCAO)
            {
                veiculo.Status = StatusVeiculo.MANUTENCAO;
                veiculo.UpdatedAt = agora;
            }

            manutencao.Veiculo = veiculo;

            return Task.FromResult(manutencao);
        }

        /// <summary>Encerra a ordem com as regras do repositorio real. Lanca NotFound, BusinessException ou ValidationException.</summary>
        public Task<Manutencao> EncerrarAtomicamenteAsync(int idManutencao, DateOnly dataSaida, int kmAtual, decimal? custo)
        {
            if (ExcecaoAoOperar != null)
            {
                throw ExcecaoAoOperar;
            }

            Manutencao? ordem = Manutencoes.FirstOrDefault(x => x.IdManutencao == idManutencao && x.DisabledAt == null);

            if (ordem == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroManutencaoNaoEncontrada);
            }

            Veiculo? veiculo = VeiculoDoContexto(ordem.IdVeiculo);

            if (veiculo == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
            }

            if (ordem.DataSaida != null)
            {
                throw new BusinessException(ConstantsMessage.ErroManutencaoJaEncerrada);
            }

            if (dataSaida < ordem.DataEntrada)
            {
                throw CriarRecusaDeCampo("DataSaida", ConstantsMessage.ErroManutencaoDataSaidaAnterior);
            }

            if (kmAtual < veiculo.KmAtual)
            {
                throw CriarRecusaDeCampo("KmAtual", string.Format(ConstantsMessage.ErroManutencaoKmMenor, veiculo.KmAtual));
            }

            DateTime agora = DateTime.Now;

            ordem.DataSaida = dataSaida;
            ordem.Custo = custo;
            ordem.UpdatedAt = agora;
            veiculo.KmAtual = kmAtual;
            veiculo.UpdatedAt = agora;

            if (veiculo.Status == StatusVeiculo.MANUTENCAO)
            {
                veiculo.Status = StatusVeiculo.DISPONIVEL;
            }

            ordem.Veiculo = veiculo;

            return Task.FromResult(ordem);
        }

        /// <summary>Devolve os alertas semeados e o total semeado.</summary>
        public Task<(List<VeiculoRevisaoRegistro> Itens, int Total)> ListarAlertasRevisaoAsync()
        {
            if (ExcecaoAoOperar != null)
            {
                throw ExcecaoAoOperar;
            }

            return Task.FromResult<(List<VeiculoRevisaoRegistro> Itens, int Total)>((new List<VeiculoRevisaoRegistro>(ItensAlertas), TotalAlertas));
        }

        /// <summary>Monta a excecao de validacao de um campo, com a falha na lista de erros (como o repositorio real).</summary>
        private static FluentValidation.ValidationException CriarRecusaDeCampo(string campo, string mensagem)
        {
            List<FluentValidation.Results.ValidationFailure> falhas = new List<FluentValidation.Results.ValidationFailure> { new FluentValidation.Results.ValidationFailure(campo, mensagem) };

            return new FluentValidation.ValidationException(mensagem, falhas);
        }

        /// <summary>Veiculo ativo da locadora do contexto (zero no contexto = nada).</summary>
        private Veiculo? VeiculoDoContexto(int idVeiculo)
        {
            return Veiculos.FirstOrDefault(x => x.IdVeiculo == idVeiculo
                && x.DisabledAt == null
                && IdEmpresaDoContexto > 0
                && x.IdEmpresa == IdEmpresaDoContexto);
        }
    }
}
