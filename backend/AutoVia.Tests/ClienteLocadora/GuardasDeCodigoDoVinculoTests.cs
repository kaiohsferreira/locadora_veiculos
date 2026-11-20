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

        private const string InicioPendenteAtomico = "public async Task<ClienteLocadora> InsertPendenteComTetoAtomicallyAsync(";
        private const string FimPendenteAtomico = "public async Task<int> CountMinhasAsync(";
        private const string ChamadaDaTravaDoCliente = "ConsultaTravaCliente(";
        private const string ChamadaDaReleitura = "ConsultaVinculoAtivoMaisAntigo(";
        private const string ChamadaDaContagem = "ConsultaEmpresasSolicitadasDesde(";
        private const string ChamadaDaRegraDoLimite = "RegrasPedidoAprovacao.AtingiuLimite(";
        private const string ChamadaDoInsert = "SaveChangesAsync(";
        private const string IsolamentoDoPendente = "IsolationLevel.ReadCommitted";
        private const string TravaExplicitaProibida = "FOR UPDATE";
        private const string ConexaoCruaProibida = "GetDbConnection(";

        /// <summary>Corpo de InsertPendenteComTetoAtomicallyAsync: do cabecalho ate o inicio de CountMinhasAsync (o metodo seguinte no arquivo).</summary>
        private static string CorpoDoPendenteAtomico()
        {
            string caminho = Path.Combine(RaizDoBackend(), ArquivoRepositorio.Replace('/', Path.DirectorySeparatorChar));
            string codigo = File.ReadAllText(caminho);

            int inicio = codigo.IndexOf(InicioPendenteAtomico, StringComparison.Ordinal);
            Assert.True(inicio >= 0, "InsertPendenteComTetoAtomicallyAsync nao encontrado no ClienteLocadoraRepository.");

            int fim = codigo.IndexOf(FimPendenteAtomico, inicio, StringComparison.Ordinal);
            Assert.True(fim > inicio, "CountMinhasAsync deveria vir depois de InsertPendenteComTetoAtomicallyAsync.");

            return codigo.Substring(inicio, fim - inicio);
        }

        [Fact]
        public void PendenteAtomico_ChamaAGuardaDeTransacaoEUsaReadCommitted()
        {
            string corpo = CorpoDoPendenteAtomico();

            Assert.Contains(ChamadaDaGuarda, corpo);
            Assert.Contains(IsolamentoDoPendente, corpo);
        }

        /// <summary>
        /// Ordem da corrida: a guarda vem antes da transacao, e a trava do cliente vem antes da releitura, da contagem, da regra do
        /// limite e do INSERT. Mover a contagem para antes da trava reabre a corrida do teto.
        /// </summary>
        [Fact]
        public void PendenteAtomico_OrdemGuardaTravaReleituraContagemRegraEInsert()
        {
            string corpo = CorpoDoPendenteAtomico();

            int posicaoDaGuarda = corpo.IndexOf(ChamadaDaGuarda, StringComparison.Ordinal);
            int posicaoDaTrava = corpo.IndexOf(ChamadaDaTravaDoCliente, StringComparison.Ordinal);
            int posicaoDaReleitura = corpo.IndexOf(ChamadaDaReleitura, StringComparison.Ordinal);
            int posicaoDaContagem = corpo.IndexOf(ChamadaDaContagem, StringComparison.Ordinal);
            int posicaoDaRegra = corpo.IndexOf(ChamadaDaRegraDoLimite, StringComparison.Ordinal);
            int posicaoDoInsert = corpo.IndexOf(ChamadaDoInsert, StringComparison.Ordinal);

            Assert.True(posicaoDaGuarda >= 0 && posicaoDaTrava >= 0 && posicaoDaReleitura >= 0 && posicaoDaContagem >= 0 && posicaoDaRegra >= 0 && posicaoDoInsert >= 0, "Faltam etapas no metodo atomico do teto.");
            Assert.True(posicaoDaGuarda < posicaoDaTrava, "A guarda de transacao deve vir antes da trava do cliente.");
            Assert.True(posicaoDaTrava < posicaoDaReleitura, "A trava do cliente deve vir antes da releitura do par.");
            Assert.True(posicaoDaReleitura < posicaoDaContagem, "A releitura do par deve vir antes da contagem do teto.");
            Assert.True(posicaoDaContagem < posicaoDaRegra, "A contagem deve vir antes da regra do limite.");
            Assert.True(posicaoDaRegra < posicaoDoInsert, "A regra do limite deve vir antes do INSERT do pendente.");
        }

        /// <summary>Trava folha (regra do desenho): dentro do metodo atomico nao ha outra trava explicita nem conexao crua.</summary>
        [Fact]
        public void PendenteAtomico_TravaEhFolhaSemOutraTravaNoCorpo()
        {
            string corpo = CorpoDoPendenteAtomico();

            Assert.DoesNotContain(TravaExplicitaProibida, corpo);
            Assert.DoesNotContain(ConexaoCruaProibida, corpo);
        }
    }
}
