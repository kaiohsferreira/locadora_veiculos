using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ISeguroRepository em memoria, com as mesmas regras do SeguroRepository real: ignora
    /// desativadas em todas as buscas, compara o numero com Trim e sem diferenciar maiusculas,
    /// filtra por veiculo e pela janela de fim da vigencia, ordena por DataFim e id e pagina.
    /// Adicionar() semeia apolices (inclusive desativadas); Veiculo simula o Include.
    /// </summary>
    public class FakeSeguroRepository : ISeguroRepository
    {
        private readonly List<Seguro> _seguros = new List<Seguro>();
        private int _proximoId = 1;

        /// <summary>Quando true, UpdateAsync devolve false sem alterar nada (simula id nao encontrado no banco).</summary>
        public bool ForcarUpdateFalse { get; set; }

        /// <summary>Quando nao for null, FindByIdAsync lanca esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoBuscarPorId { get; set; }

        /// <summary>Ultima apolice gravada por InsertAsync ou UpdateAsync (copia), para conferir a normalizacao.</summary>
        public Seguro? UltimoGravado { get; private set; }

        /// <summary>Semeia uma apolice (ativa ou desativada) sem passar pelo servico. Se o id for 0, gera um.</summary>
        public void Adicionar(Seguro seguro)
        {
            if (seguro.IdSeguro == 0)
            {
                seguro.IdSeguro = _proximoId;
            }

            if (seguro.IdSeguro >= _proximoId)
            {
                _proximoId = seguro.IdSeguro + 1;
            }

            _seguros.Add(Copiar(seguro));
        }

        /// <summary>Lista as apolices ativas filtradas, ordenadas por DataFim e id, na pagina pedida.</summary>
        public Task<List<Seguro>> FindAllAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte, int deslocamento, int quantidade)
        {
            List<Seguro> seguros = Filtrar(idVeiculo, fimVigenciaDe, fimVigenciaAte)
                .OrderBy(x => x.DataFim)
                .ThenBy(x => x.IdSeguro)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(seguros);
        }

        /// <summary>Conta as apolices ativas com os mesmos filtros de FindAllAsync.</summary>
        public Task<int> CountAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte)
        {
            int total = Filtrar(idVeiculo, fimVigenciaDe, fimVigenciaAte).Count();

            return Task.FromResult(total);
        }

        /// <summary>Busca uma apolice ativa pelo id; devolve copia (como AsNoTracking) ou null.</summary>
        public Task<Seguro?> FindByIdAsync(int idSeguro)
        {
            if (ExcecaoAoBuscarPorId != null)
            {
                throw ExcecaoAoBuscarPorId;
            }

            Seguro? encontrado = _seguros.FirstOrDefault(x => x.IdSeguro == idSeguro && x.DisabledAt == null);
            Seguro? copia = null;

            if (encontrado != null)
            {
                copia = Copiar(encontrado);
            }

            return Task.FromResult(copia);
        }

        /// <summary>Verifica se existe apolice ativa com o mesmo numero (Trim, sem diferenciar maiusculas), ignorando o id informado.</summary>
        public Task<bool> ExistsByNumeroApoliceAsync(string numeroApolice, int? idSeguroIgnorado)
        {
            string numeroComparacao = numeroApolice.Trim().ToUpper();

            bool existe = _seguros.Any(x =>
                x.DisabledAt == null &&
                (!idSeguroIgnorado.HasValue || x.IdSeguro != idSeguroIgnorado.Value) &&
                x.NumeroApolice.Trim().ToUpper() == numeroComparacao);

            return Task.FromResult(existe);
        }

        /// <summary>Grava uma nova apolice com id gerado, CreatedAt = UpdatedAt = DateTime.Now e DisabledAt nulo.</summary>
        public Task<Seguro> InsertAsync(Seguro seguro)
        {
            DateTime agora = DateTime.Now;

            seguro.IdSeguro = _proximoId;
            _proximoId++;
            seguro.CreatedAt = agora;
            seguro.UpdatedAt = agora;
            seguro.DisabledAt = null;

            _seguros.Add(Copiar(seguro));
            UltimoGravado = Copiar(seguro);

            return Task.FromResult(seguro);
        }

        /// <summary>Atualiza os dados da apolice ativa; CreatedAt e DisabledAt ficam. False se nao achar.</summary>
        public Task<bool> UpdateAsync(Seguro seguro)
        {
            if (ForcarUpdateFalse)
            {
                return Task.FromResult(false);
            }

            Seguro? salvo = _seguros.FirstOrDefault(x => x.IdSeguro == seguro.IdSeguro && x.DisabledAt == null);

            if (salvo == null)
            {
                return Task.FromResult(false);
            }

            salvo.IdVeiculo = seguro.IdVeiculo;
            salvo.Seguradora = seguro.Seguradora;
            salvo.NumeroApolice = seguro.NumeroApolice;
            salvo.DataInicio = seguro.DataInicio;
            salvo.DataFim = seguro.DataFim;
            salvo.ValorPremio = seguro.ValorPremio;
            salvo.Cobertura = seguro.Cobertura;
            salvo.UpdatedAt = DateTime.Now;
            UltimoGravado = Copiar(salvo);

            return Task.FromResult(true);
        }

        /// <summary>Apolices ativas com os filtros combinados por AND.</summary>
        private IEnumerable<Seguro> Filtrar(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte)
        {
            IEnumerable<Seguro> consulta = _seguros.Where(x => x.DisabledAt == null);

            if (idVeiculo.HasValue)
            {
                consulta = consulta.Where(x => x.IdVeiculo == idVeiculo.Value);
            }

            if (fimVigenciaDe.HasValue)
            {
                consulta = consulta.Where(x => x.DataFim >= fimVigenciaDe.Value);
            }

            if (fimVigenciaAte.HasValue)
            {
                consulta = consulta.Where(x => x.DataFim <= fimVigenciaAte.Value);
            }

            return consulta;
        }

        /// <summary>Copia os campos da apolice, inclusive auditoria e a referencia de Veiculo (simula o Include).</summary>
        private static Seguro Copiar(Seguro origem)
        {
            Seguro copia = new Seguro
            {
                IdSeguro = origem.IdSeguro,
                IdVeiculo = origem.IdVeiculo,
                Seguradora = origem.Seguradora,
                NumeroApolice = origem.NumeroApolice,
                DataInicio = origem.DataInicio,
                DataFim = origem.DataFim,
                ValorPremio = origem.ValorPremio,
                Cobertura = origem.Cobertura,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt,
                Veiculo = origem.Veiculo
            };

            return copia;
        }
    }
}
