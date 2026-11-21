namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Guardas de codigo da rotina de atraso (contrato pagamento-atraso v1, isolamento). O controller so conhece a rota manual
    /// (IPagamentoAtrasoService, MarcarAtrasosDaLocadoraAsync): nao referencia a rotina do sistema nem a atualizacao de todas
    /// as locadoras. A interface da rotina e a atualizacao de todas as locadoras aparecem so nos arquivos listados abaixo.
    /// Lista fechada, por texto, nos arquivos de producao (bin e obj ficam de fora).
    /// </summary>
    public class PagamentoAtrasoGuardasTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string SimboloRotinaSistema = "IPagamentoAtrasoRotinaService";
        private const string SimboloMarcarTodas = "MarcarAtrasosTodasLocadorasAsync";
        private const string SimboloAtualizarTodas = "AtualizarAtrasosTodasLocadorasAsync";
        private const string CaminhoController = "AutoVia.Api/Controllers/PagamentoController.cs";
        private const string SeparadorCaminho = "/";
        private const string PastaObj = "obj";
        private const string PastaBin = "bin";
        private const string ExtensaoCSharp = "*.cs";

        /// <summary>Arquivos de producao que podem citar a rotina do sistema (interface, servico, registro e servico hospedado).</summary>
        private static readonly string[] ArquivosDaRotinaSistema = new string[]
        {
            "AutoVia.Api/Hosting/AtrasoPagamentoHostedService.cs",
            "AutoVia.Api/Extensions/AddServicesStartup.cs",
            "AutoVia.Core/ServicesInterface/IPagamentoAtrasoRotinaService.cs",
            "AutoVia.Infrastructure/Services/PagamentoAtrasoRotinaService.cs"
        };

        /// <summary>Arquivos de producao que podem citar a atualizacao de todas as locadoras (bypass nomeado).</summary>
        private static readonly string[] ArquivosDaAtualizacaoTodas = new string[]
        {
            "AutoVia.Core/RepositoriesInterface/IPagamentoRepository.cs",
            "AutoVia.Infrastructure/Repositories/PagamentoRepository.cs",
            "AutoVia.Infrastructure/Services/PagamentoAtrasoRotinaService.cs"
        };

        /// <summary>Sobe a partir da pasta do teste ate achar o AutoVia.sln (raiz do backend).</summary>
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

        /// <summary>Caminhos relativos (com /) dos arquivos de producao que contem o simbolo.</summary>
        private static List<string> ArquivosQueCitam(string simbolo)
        {
            string raiz = RaizDoBackend();
            List<string> achados = new List<string>();

            foreach (string pasta in new string[] { "AutoVia.Api", "AutoVia.Core", "AutoVia.Communication", "AutoVia.Helpers", "AutoVia.Infrastructure", "AutoVia.Integrations" })
            {
                foreach (string arquivo in Directory.EnumerateFiles(Path.Combine(raiz, pasta), ExtensaoCSharp, SearchOption.AllDirectories))
                {
                    if (arquivo.Contains($"{Path.DirectorySeparatorChar}{PastaObj}{Path.DirectorySeparatorChar}") || arquivo.Contains($"{Path.DirectorySeparatorChar}{PastaBin}{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    if (File.ReadAllText(arquivo).Contains(simbolo, StringComparison.Ordinal))
                    {
                        achados.Add(Path.GetRelativePath(raiz, arquivo).Replace(Path.DirectorySeparatorChar.ToString(), SeparadorCaminho));
                    }
                }
            }

            achados.Sort(StringComparer.Ordinal);

            return achados;
        }

        [Fact]
        public void PagamentoController_NaoReferenciaARotinaDoSistemaNemAAtualizacaoDeTodasLocadoras()
        {
            string conteudo = File.ReadAllText(Path.Combine(RaizDoBackend(), CaminhoController.Replace('/', Path.DirectorySeparatorChar)));

            Assert.DoesNotContain(SimboloRotinaSistema, conteudo);
            Assert.DoesNotContain(SimboloMarcarTodas, conteudo);
            Assert.DoesNotContain(SimboloAtualizarTodas, conteudo);
            Assert.Contains("MarcarAtrasosDaLocadoraAsync", conteudo);
        }

        [Fact]
        public void RotinaDoSistema_SoAparececNosArquivosListados()
        {
            List<string> encontrados = ArquivosQueCitam(SimboloRotinaSistema);
            List<string> esperados = ArquivosDaRotinaSistema.ToList();
            esperados.Sort(StringComparer.Ordinal);

            Assert.Equal(esperados, encontrados);
        }

        [Fact]
        public void AtualizacaoDeTodasLocadoras_SoApareceNosArquivosListados()
        {
            List<string> encontrados = ArquivosQueCitam(SimboloAtualizarTodas);
            List<string> esperados = ArquivosDaAtualizacaoTodas.ToList();
            esperados.Sort(StringComparer.Ordinal);

            Assert.Equal(esperados, encontrados);
        }
    }
}
