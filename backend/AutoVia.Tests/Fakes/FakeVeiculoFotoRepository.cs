using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de fotos em memoria para os testes de servico. Guarda as fotos e os ids de veiculos ativos e conta as chamadas
    /// de escrita. NAO aplica o filtro "Empresa": o isolamento e provado pelos testes de SQL (VeiculoFotoConsultasSqlTests); aqui
    /// o teste informa o que o banco devolveria. Desativar marca o registro (Disable), como o repositorio real.
    /// </summary>
    public sealed class FakeVeiculoFotoRepository : IVeiculoFotoRepository
    {
        /// <summary>Fotos gravadas (ativas e desativadas), na ordem em que entraram.</summary>
        public List<VeiculoFoto> Fotos { get; } = new List<VeiculoFoto>();

        /// <summary>Ids dos veiculos que existem, nao estao desativados e sao da locadora do contexto.</summary>
        public HashSet<int> VeiculosAtivos { get; } = new HashSet<int>();

        /// <summary>Quantas vezes InsertAsync foi chamado.</summary>
        public int InsertCount { get; private set; }

        /// <summary>Quantas vezes DesativarAsync foi chamado.</summary>
        public int DesativarCount { get; private set; }

        /// <summary>Quando verdadeiro, a proxima chamada de InsertAsync lanca (falha de gravacao; a compensacao precisa remover o objeto).</summary>
        public bool FalharInsert { get; set; }

        /// <summary>Quando verdadeiro, a proxima chamada de FindAtivasAsync lanca (leitura depois do envio que falha).</summary>
        public bool FalharProximaLeitura { get; set; }

        /// <summary>Quando nao nulo, a proxima chamada de FindAtivasAsync devolve esta lista (simula o banco com 15 fotos ativas, por exemplo).</summary>
        public List<VeiculoFoto>? FotosNaProximaLeitura { get; set; }

        public Task<bool> VeiculoAtivoExisteAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VeiculosAtivos.Contains(idVeiculo));
        }

        public Task<int> CountAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            int total = Fotos.Count(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);
            return Task.FromResult(total);
        }

        /// <summary>Quando verdadeiro, a proxima chamada de FindAtivasAsync lanca OperationCanceledException (cancelamento na segunda leitura).</summary>
        public bool CancelarProximaLeitura { get; set; }

        public Task<List<VeiculoFoto>> FindAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
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
