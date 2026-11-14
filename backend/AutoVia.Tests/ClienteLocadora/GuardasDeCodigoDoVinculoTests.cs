namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Guarda de codigo-fonte da guarda de transacao (T-BE-06, Q3): le o arquivo do ClienteLocadoraRepository e confere que
    /// o corpo de InsertAsync chama ValidarSemTransacaoAberta com Database.CurrentTransaction. Sem isso, remover a chamada
    /// nao quebraria nenhum teste. ATENCAO: esta guarda so prova que a chamada existe no corpo. Que a transacao real e
    /// recusada (com transacao aberta de verdade no PostgreSQL) so se prova em teste de integracao com banco, que nao existe aqui.
    /// </summary>
    public class GuardasDeCodigoDoVinculoTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string ArquivoRepositorio = "AutoVia.Infrastructure/Repositories/ClienteLocadoraRepository.cs";
        private const string InicioInsert = "public async Task<ClienteLocadora> InsertAsync(";
        private const string FimInsert = "public async Task<bool> DesativarAsync(";
        private const string ChamadaDaGuarda = "ValidarSemTransacaoAberta(_db.Database.CurrentTransaction != null);";

        private static string RaizDoBackend()
        {
            DirectoryInfo? pasta = new DirectoryInfo(AppContext.BaseDirectory);

            while (pasta != null && !File.Exists(Path.Combine(pasta.FullName, NomeSolucao)))
            {
                pasta = pasta.Parent;
            }

            Assert.NotNull(pasta);

            return pasta!.FullName;
        }

        /// <summary>Corpo de InsertAsync: do cabecalho ate o inicio de DesativarAsync (o metodo seguinte no arquivo).</summary>
        private static string CorpoDoInsert()
        {
            string caminho = Path.Combine(RaizDoBackend(), ArquivoRepositorio.Replace('/', Path.DirectorySeparatorChar));
            string codigo = File.ReadAllText(caminho);

            int inicio = codigo.IndexOf(InicioInsert, StringComparison.Ordinal);
            Assert.True(inicio >= 0, "InsertAsync nao encontrado no ClienteLocadoraRepository.");

            int fim = codigo.IndexOf(FimInsert, inicio, StringComparison.Ordinal);
            Assert.True(fim > inicio, "DesativarAsync deveria vir depois de InsertAsync.");

            return codigo.Substring(inicio, fim - inicio);
        }

        [Fact]
        public void InsertAsync_ChamaAGuardaDeTransacaoComACurrentTransaction()
        {
            string corpo = CorpoDoInsert();

            Assert.Contains(ChamadaDaGuarda, corpo);
        }

        /// <summary>A data de solicitacao vem do servico (IRelogio): o repositorio nao a define sozinho.</summary>
        [Fact]
        public void InsertAsync_NaoDefineADataDeSolicitacaoPeloRelogioDoRepositorio()
        {
            string corpo = CorpoDoInsert();

            Assert.DoesNotContain("DataSolicitacao = agora", corpo);
            Assert.DoesNotContain("DataSolicitacao = DateTime.Now", corpo);
        }
    }
}
