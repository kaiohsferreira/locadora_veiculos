using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de fotos em memoria para os testes de servico. Guarda as fotos e os ids de veiculos ativos e conta as chamadas
    /// de cada metodo (TotalChamadas soma todas). NAO aplica o filtro "Empresa": o isolamento e provado pelos testes de SQL
    /// (VeiculoFotoConsultasSqlTests); aqui o teste informa o que o banco devolveria. Desativar marca o registro (Disable), como o
    /// repositorio real.
    /// </summary>
    public sealed class FakeVeiculoFotoRepository : IVeiculoFotoRepository
    {
        /// <summary>Fotos gravadas (ativas e desativadas), na ordem em que entraram.</summary>
        public List<VeiculoFoto> Fotos { get; } = new List<VeiculoFoto>();

        /// <summary>Ids dos veiculos que existem, nao estao desativados e sao da locadora do contexto.</summary>
        public HashSet<int> VeiculosAtivos { get; } = new HashSet<int>();

        /// <summary>Quantas vezes VeiculoAtivoExisteAsync foi chamado.</summary>
        public int VeiculoAtivoExisteCount { get; private set; }

        /// <summary>Quantas vezes CountAtivasAsync foi chamado.</summary>
        public int CountAtivasCount { get; private set; }

        /// <summary>Quantas vezes FindAtivasAsync foi chamado.</summary>
        public int FindAtivasCount { get; private set; }

        /// <summary>Quantas vezes FindAtivaAsync foi chamado.</summary>
        public int FindAtivaCount { get; private set; }

        /// <summary>Quantas vezes InsertAsync foi chamado.</summary>
        public int InsertCount { get; private set; }

        /// <summary>Quantas vezes DesativarAsync foi chamado.</summary>
        public int DesativarCount { get; private set; }

        /// <summary>Quantas vezes ContarAtivasDeVeiculoAtivoAsync foi chamado (primeira ida do envio).</summary>
        public int ContarAtivasCount { get; private set; }

        /// <summary>Quantas vezes ObterResumoAtivasAsync foi chamado (segunda ida do envio).</summary>
        public int ResumoCount { get; private set; }

        /// <summary>Total de chamadas a todos os metodos do repositorio: o envio de sucesso faz exatamente 3.</summary>
        public int TotalChamadas => VeiculoAtivoExisteCount + CountAtivasCount + FindAtivasCount + FindAtivaCount + InsertCount + DesativarCount + ContarAtivasCount + ResumoCount;

        /// <summary>Quando verdadeiro, a proxima chamada de InsertAsync lanca (falha de gravacao; a compensacao precisa remover o objeto).</summary>
        public bool FalharInsert { get; set; }

        /// <summary>Quando verdadeiro, a proxima leitura da segunda ida (ObterResumoAtivasAsync) ou de FindAtivasAsync lanca.</summary>
        public bool FalharProximaLeitura { get; set; }

        /// <summary>
        /// Quando nao nulo, a proxima leitura da segunda ida (ObterResumoAtivasAsync) ou de FindAtivasAsync usa esta lista como as
        /// fotos ativas (simula o banco com 15 fotos ativas, por exemplo).
        /// </summary>
        public List<VeiculoFoto>? FotosNaProximaLeitura { get; set; }

        /// <summary>
        /// Quando verdadeiro, a proxima leitura da segunda ida (ObterResumoAtivasAsync) ou de FindAtivasAsync lanca
        /// OperationCanceledException (cancelamento na segunda leitura).
        /// </summary>
        public bool CancelarProximaLeitura { get; set; }

        public Task<bool> VeiculoAtivoExisteAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            VeiculoAtivoExisteCount++;
            return Task.FromResult(VeiculosAtivos.Contains(idVeiculo));
        }

        public Task<int> CountAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            CountAtivasCount++;
            int total = Fotos.Count(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);
            return Task.FromResult(total);
        }

        /// <summary>
        /// Primeira ida do envio: nulo quando o veiculo nao esta ativo (inexistente, desativado ou de outra locadora); senao, quantas
        /// fotos ativas ele tem. Nao consome as flags de leitura (essas valem para a segunda ida).
        /// </summary>
        public Task<int?> ContarAtivasDeVeiculoAtivoAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            ContarAtivasCount++;
            if (!VeiculosAtivos.Contains(idVeiculo))
            {
                return Task.FromResult<int?>(null);
            }

            int total = Fotos.Count(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);
            return Task.FromResult<int?>(total);
        }

        /// <summary>
        /// Segunda ida do envio: total e maior ordem das fotos ativas. Nao exige que o veiculo esteja ativo (como o real): so as
        /// fotos ativas contam, entao um veiculo desativado no meio do envio ainda mostra as fotos.
        /// Consome as flags de leitura (FalharProximaLeitura, CancelarProximaLeitura e FotosNaProximaLeitura).
        /// </summary>
        public Task<ResumoFotosAtivasVeiculo> ObterResumoAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            ResumoCount++;
            if (FalharProximaLeitura)
            {
                FalharProximaLeitura = false;
                throw new InvalidOperationException("Leitura falhou");
            }

            if (CancelarProximaLeitura)
            {
                CancelarProximaLeitura = false;
                throw new OperationCanceledException();
            }

            List<VeiculoFoto> ativas;
            if (FotosNaProximaLeitura != null)
            {
                ativas = FotosNaProximaLeitura;
                FotosNaProximaLeitura = null;
            }
            else
            {
                ativas = Fotos.Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null).ToList();
            }

            int? maiorOrdem = ativas.Select(foto => (int?)foto.Ordem).Max();
            return Task.FromResult(new ResumoFotosAtivasVeiculo(ativas.Count, maiorOrdem));
        }

        /// <summary>Lista as fotos ativas do veiculo (usada pela listagem). Consome as mesmas flags de leitura da segunda ida.</summary>
        public Task<List<VeiculoFoto>> FindAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            FindAtivasCount++;
            if (FalharProximaLeitura)
            {
                FalharProximaLeitura = false;
                throw new InvalidOperationException("Leitura falhou");
            }

            if (CancelarProximaLeitura)
            {
                CancelarProximaLeitura = false;
                throw new OperationCanceledException();
            }

            if (FotosNaProximaLeitura != null)
            {
                List<VeiculoFoto> forcadas = FotosNaProximaLeitura;
                FotosNaProximaLeitura = null;
                return Task.FromResult(forcadas);
            }

            List<VeiculoFoto> ativas = Fotos
                .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null)
                .OrderBy(x => x.Ordem)
                .ThenBy(x => x.IdVeiculoFoto)
                .ToList();

            return Task.FromResult(ativas);
        }

        public Task<VeiculoFoto?> FindAtivaAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default)
        {
            FindAtivaCount++;
            VeiculoFoto? foto = Fotos.FirstOrDefault(x => x.IdVeiculo == idVeiculo && x.IdVeiculoFoto == idVeiculoFoto && x.DisabledAt == null);
            return Task.FromResult(foto);
        }

        public Task<VeiculoFoto> InsertAsync(VeiculoFoto foto, CancellationToken cancellationToken = default)
        {
            InsertCount++;
            if (FalharInsert) throw new InvalidOperationException("Gravacao falhou");
            foto.IdVeiculoFoto = Fotos.Count + 1;
            DateTime agora = DateTime.Now;
            foto.CreatedAt = agora;
            foto.UpdatedAt = agora;
            foto.DisabledAt = null;
            Fotos.Add(foto);
            return Task.FromResult(foto);
        }

        /// <summary>Quando verdadeiro, DesativarAsync lanca (falha ao gravar a desativacao depois de o objeto ja ter sido removido).</summary>
        public bool FalharDesativar { get; set; }

        public Task<bool> DesativarAsync(int idVeiculo, int idVeiculoFoto, int idFuncionarioRemocao, CancellationToken cancellationToken = default)
        {
            DesativarCount++;
            if (FalharDesativar) throw new InvalidOperationException("Desativacao falhou");
            VeiculoFoto? foto = Fotos.FirstOrDefault(x => x.IdVeiculo == idVeiculo && x.IdVeiculoFoto == idVeiculoFoto && x.DisabledAt == null);
            if (foto == null)
            {
                return Task.FromResult(false);
            }

            foto.Disable();
            foto.IdFuncionarioRemocao = idFuncionarioRemocao;
            return Task.FromResult(true);
        }
    }
}
