using System.IO;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Guarda de codigo-fonte do log no repositorio de manutencao: o metodo que monta os filtros da listagem
    /// (AplicarFiltrosDaListagem) tem try/catch que grava log com a constante de consulta e relanca. Nao abre conexao com banco.
    /// </summary>
    public class ManutencaoRepositoryLogTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string ArquivoRepositorio = "AutoVia.Infrastructure/Repositories/ManutencaoRepository.cs";
        private const string AssinaturaDosFiltros = "private IQueryable<Manutencao> AplicarFiltrosDaListagem(";
        private const string InicioDeDocumentacao = "/// <summary>";
        private const string PadraoDeTry = @"\btry\s*\{";
        private const string CatchGenerico = "catch (Exception ex)";
        private const string LogDaConsulta = "_logService.Write(ex, ConstantsMessage.ErroConsultarManutencao";
        private const string Relancamento = "throw;";

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

        /// <summary>Trecho do arquivo do repositorio desde a assinatura do metodo ate a documentacao do metodo seguinte.</summary>
        private static string CorpoDosFiltros()
        {
            string codigo = File.ReadAllText(Path.Combine(RaizDoBackend(), ArquivoRepositorio));

            int inicio = codigo.IndexOf(AssinaturaDosFiltros, StringComparison.Ordinal);

            Assert.True(inicio >= 0, "O metodo AplicarFiltrosDaListagem nao foi encontrado no repositorio.");

            int fim = codigo.IndexOf(InicioDeDocumentacao, inicio, StringComparison.Ordinal);

            Assert.True(fim > inicio, "O metodo AplicarFiltrosDaListagem nao tem o fim esperado.");

            return codigo.Substring(inicio, fim - inicio);
        }

        [Fact]
        public void AplicarFiltrosDaListagem_TemTryCatchQueGravaLogERelanca()
        {
            string corpo = CorpoDosFiltros();

            Assert.Matches(PadraoDeTry, corpo);
            Assert.Contains(CatchGenerico, corpo);
            Assert.Contains(LogDaConsulta, corpo);
            Assert.Contains(Relancamento, corpo);
        }
    }
}
