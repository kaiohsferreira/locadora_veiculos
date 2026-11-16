using System.IO;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Guarda de codigo-fonte do log (D-9) nos dois arquivos de repositorio das leituras publicas de veiculo: todo
    /// catch (Exception ex) grava log com a constante do metodo e relanca; nenhum catch de regra (BusinessException ou
    /// NotFoundException) entra no repositorio. Nao abre conexao com banco.
    /// </summary>
    public class VeiculoPublicoLogTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string ArquivoDisponibilidade = "AutoVia.Infrastructure/Repositories/VeiculoRepository.Disponibilidade.cs";
        private const string ArquivoCatalogo = "AutoVia.Infrastructure/Repositories/VeiculoRepository.Catalogo.cs";
        private const string CatchGenerico = "catch (Exception ex)";
        private const string CatchDeRegraNegocio = "catch (BusinessException";
        private const string CatchDeNaoEncontrado = "catch (NotFoundException";
        private const string LogDaBusca = "_logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade";
        private const string LogDoCatalogo = "_logService.Write(ex, ConstantsMessage.ErroListarCatalogo";
        private const string Relancamento = "throw;";
        private const int MetodosComCatchNoRepositorioDeLeitura = 4;

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

        private static string LerArquivo(string caminhoRelativo)
        {
            return File.ReadAllText(Path.Combine(RaizDoBackend(), caminhoRelativo));
        }

        private static int Ocorrencias(string texto, string trecho)
        {
            return texto.Split(trecho).Length - 1;
        }

        [Fact]
        public void RepositorioDaBusca_TodoCatchGravaLogComAConstanteERelanca()
        {
            string codigo = LerArquivo(ArquivoDisponibilidade);

            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, CatchGenerico));
            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, LogDaBusca));
            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, Relancamento));
            Assert.Equal(0, Ocorrencias(codigo, CatchDeRegraNegocio));
            Assert.Equal(0, Ocorrencias(codigo, CatchDeNaoEncontrado));
        }

        [Fact]
        public void RepositorioDoCatalogo_TodoCatchGravaLogComAConstanteERelanca()
        {
            string codigo = LerArquivo(ArquivoCatalogo);

            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, CatchGenerico));
            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, LogDoCatalogo));
            Assert.Equal(MetodosComCatchNoRepositorioDeLeitura, Ocorrencias(codigo, Relancamento));
            Assert.Equal(0, Ocorrencias(codigo, CatchDeRegraNegocio));
            Assert.Equal(0, Ocorrencias(codigo, CatchDeNaoEncontrado));
        }
    }
}
