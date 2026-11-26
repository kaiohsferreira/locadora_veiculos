using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio do contrato em memoria, sem banco. O filtro "Empresa" e simulado pelo id de empresa que o teste informa
    /// (Func), no lugar do IContextoEmpresa: a locacao de outra locadora nao aparece, como no EscopoEmpresa real.
    /// </summary>
    public class FakeContratoLocacaoRepository : IContratoLocacaoRepository
    {
        private readonly Func<int> _idEmpresaDoContexto;

        public FakeContratoLocacaoRepository(Func<int> idEmpresaDoContexto)
        {
            _idEmpresaDoContexto = idEmpresaDoContexto;
        }

        /// <summary>Locacoes semeadas (o veiculo de cada uma diz a locadora).</summary>
        public List<Locacao> Locacoes { get; } = new List<Locacao>();

        /// <summary>Contratos gravados, na ordem.</summary>
        public List<ContratoLocacao> Contratos { get; } = new List<ContratoLocacao>();

        /// <summary>Funcionarios que o Include do repositorio real traria (nome de quem coletou).</summary>
        public List<Funcionario> Funcionarios { get; } = new List<Funcionario>();

        /// <summary>Quantas vezes o INSERT foi chamado (prova de que a leitura nao grava).</summary>
        public int Insercoes { get; private set; }

        /// <summary>Quando definido, o INSERT falha com esta excecao (simula falha de banco).</summary>
        public Exception? FalharAoInserir { get; set; }

        private bool Visivel(Locacao locacao)
        {
            int idEmpresa = _idEmpresaDoContexto();
            bool visivel = idEmpresa > 0 && locacao.Veiculo.IdEmpresa == idEmpresa;

            return visivel;
        }

        public Task<Locacao?> FindLocacaoParaContratoAsync(int idLocacao)
        {
            Locacao? locacao = Locacoes.FirstOrDefault(x => x.IdLocacao == idLocacao && x.DisabledAt == null && Visivel(x));

            return Task.FromResult(locacao);
        }

        public Task<ContratoLocacao?> FindAtivoByLocacaoAsync(int idLocacao)
        {
            Locacao? locacao = Locacoes.FirstOrDefault(x => x.IdLocacao == idLocacao);
            if (locacao == null || !Visivel(locacao))
            {
                return Task.FromResult<ContratoLocacao?>(null);
            }

            ContratoLocacao? contrato = Contratos.FirstOrDefault(x => x.IdLocacao == idLocacao && x.DisabledAt == null);
            if (contrato != null)
            {
                Funcionario? coletor = Funcionarios.FirstOrDefault(x => x.IdFuncionario == contrato.IdFuncionario);
                if (coletor != null)
                {
                    contrato.Funcionario = coletor;
                }
            }

            return Task.FromResult(contrato);
        }

        public Task<ContratoLocacao> InsertAsync(ContratoLocacao contrato)
        {
            if (FalharAoInserir != null)
            {
                throw FalharAoInserir;
            }

            Insercoes++;
            contrato.IdContratoLocacao = Contratos.Count + 1;
            Contratos.Add(contrato);

            return Task.FromResult(contrato);
        }
    }
}
