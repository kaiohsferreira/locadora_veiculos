using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes da pasta raiz do armazenamento em disco (pasta temporaria propria, sem variavel de ambiente).</summary>
    public sealed class RaizArmazenamentoDiscoTests : IDisposable
    {
        private const string NomeDaPastaDePublicacao = "publicacao";
        private const string NomeDoRepositorio = "repo";
        private const string NomeDaPastaAutoVia = "AutoVia";
        private const string NomeDaPastaDeDados = "dados";
        private const string NomeDoArquivoAntigo = "antigo.tmp";
        private const string NomeDoArquivoRecente = "recente.tmp";
        private const int IdadeDoTemporarioAntigoMinutos = 120;

        private readonly PastaTemporariaDeTeste _pasta = new PastaTemporariaDeTeste();

        public void Dispose()
        {
            _pasta.Dispose();
        }

        [Fact]
        public void DiretorioAbsoluto_SoCriaAsPastasNaChamadaDeObter()
        {
            string raiz = Path.Combine(_pasta.Caminho, "armazenamento");
            RaizArmazenamentoDisco disco = CriarRaiz(raiz, new FakeLogService());

            Assert.False(Directory.Exists(raiz));
            string arquivos = disco.ObterPastaDosArquivos();

            Assert.Equal(Path.Combine(raiz, ConstantsValues.SubpastaArquivosArmazenamento), arquivos);
            Assert.True(Directory.Exists(arquivos));
            Assert.True(Directory.Exists(Path.Combine(raiz, ConstantsValues.SubpastaTemporariosArmazenamento)));
            Assert.True(Directory.Exists(Path.Combine(raiz, ConstantsValues.SubpastaSegredoArmazenamento)));
        }

        [Fact]
        public void TentarObterSemCriar_SemPasta_DevolveFalsoEFicaSemPastaNenhuma()
        {
            string raiz = Path.Combine(_pasta.Caminho, "armazenamento");
            RaizArmazenamentoDisco disco = CriarRaiz(raiz, new FakeLogService());

            bool existe = disco.TentarObterSemCriar(out string pastaDosArquivos, out string pastaDoSegredo);

            Assert.False(existe);
            Assert.False(Directory.Exists(raiz));
            Assert.Equal(Path.Combine(raiz, ConstantsValues.SubpastaSegredoArmazenamento), pastaDoSegredo);
            Assert.Equal(Path.Combine(raiz, ConstantsValues.SubpastaArquivosArmazenamento), pastaDosArquivos);
        }

        [Fact]
        public void DiretorioVazio_EmContainer_LancaED02_SemCriarNada()
        {
            RaizArmazenamentoDisco disco = CriarRaiz(string.Empty, new FakeLogService(), emContainer: true);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioObrigatorio, ex.Message);
            Assert.False(Directory.Exists(Path.Combine(_pasta.Caminho, NomeDaPastaDeDados)));
        }

        [Fact]
        public void DiretorioVazio_ForaDoWindows_LancaED02()
        {
            RaizArmazenamentoDisco disco = CriarRaiz(string.Empty, new FakeLogService(), windows: false);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.TentarObterSemCriar(out _, out _));

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioObrigatorio, ex.Message);
        }

        [Fact]
        public void DiretorioVazio_NoWindowsForaDeContainer_UsaAPastaPadraoDoUsuario()
        {
            string pastaDeDados = Path.Combine(_pasta.Caminho, NomeDaPastaDeDados);
            RaizArmazenamentoDisco disco = CriarRaiz(string.Empty, new FakeLogService(), pastaDeDados: pastaDeDados);
            string raizEsperada = Path.Combine(pastaDeDados, NomeDaPastaAutoVia, ConstantsValues.PastaArmazenamentoPadrao);

            string arquivos = disco.ObterPastaDosArquivos();

            Assert.Equal(Path.Combine(raizEsperada, ConstantsValues.SubpastaArquivosArmazenamento), arquivos);
            Assert.True(Directory.Exists(arquivos));
        }

        [Fact]
        public void CaminhoRelativo_LancaED04()
        {
            RaizArmazenamentoDisco disco = CriarRaiz(Path.Combine("dados", "relativa"), new FakeLogService());

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido, ex.Message);
        }

        [Fact]
        public void RaizDeDisco_LancaED04()
        {
            string raizDoDisco = Path.GetPathRoot(_pasta.Caminho)!;
            RaizArmazenamentoDisco disco = CriarRaiz(raizDoDisco, new FakeLogService());

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido, ex.Message);
        }

        [Fact]
        public void PastaDentroDaPublicacao_LancaED03()
        {
            string publicacao = Path.Combine(_pasta.Caminho, NomeDaPastaDePublicacao);
            RaizArmazenamentoDisco disco = CriarRaiz(Path.Combine(publicacao, "sub"), new FakeLogService(), pastaDePublicacao: publicacao);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioEmLocalProibido, ex.Message);
        }

        [Fact]
        public void PublicacaoDentroDaPasta_LancaED03()
        {
            string publicacao = Path.Combine(_pasta.Caminho, "armazenamento", "bin");
            RaizArmazenamentoDisco disco = CriarRaiz(Path.Combine(_pasta.Caminho, "armazenamento"), new FakeLogService(), pastaDePublicacao: publicacao);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioEmLocalProibido, ex.Message);
        }

        [Fact]
        public void PastaComRepositorioGitAcima_LancaED03()
        {
            string repositorio = Path.Combine(_pasta.Caminho, NomeDoRepositorio);
            Directory.CreateDirectory(Path.Combine(repositorio, ConstantsValues.NomeEntradaGit));
            RaizArmazenamentoDisco disco = CriarRaiz(Path.Combine(repositorio, "armazenamento"), new FakeLogService());

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioEmLocalProibido, ex.Message);
            Assert.False(Directory.Exists(Path.Combine(repositorio, "armazenamento")));
        }

        [Fact]
        public void TemporariosOrfaos_SaoApagadosNaPrimeiraVezERecentesFicam()
        {
            string raiz = Path.Combine(_pasta.Caminho, "armazenamento");
            string pastaTemporaria = Path.Combine(raiz, ConstantsValues.SubpastaTemporariosArmazenamento);
            Directory.CreateDirectory(pastaTemporaria);
            string antigo = Path.Combine(pastaTemporaria, NomeDoArquivoAntigo);
            string recente = Path.Combine(pastaTemporaria, NomeDoArquivoRecente);
            File.WriteAllText(antigo, string.Empty);
            File.WriteAllText(recente, string.Empty);
            File.SetLastWriteTime(antigo, DateTime.Now.AddMinutes(-IdadeDoTemporarioAntigoMinutos));
            RaizArmazenamentoDisco disco = CriarRaiz(raiz, new FakeLogService());

            disco.ObterPastaTemporaria();

            Assert.False(File.Exists(antigo));
            Assert.True(File.Exists(recente));

            string depoisDaPrimeira = Path.Combine(pastaTemporaria, "outro-antigo.tmp");
            File.WriteAllText(depoisDaPrimeira, string.Empty);
            File.SetLastWriteTime(depoisDaPrimeira, DateTime.Now.AddMinutes(-IdadeDoTemporarioAntigoMinutos));
            disco.ObterPastaTemporaria();

            Assert.True(File.Exists(depoisDaPrimeira));
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("false")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("abc")]
        public void IndicadorDeContainer_VemDaConfiguracao(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();
            if (valor != null)
            {
                valores[ConstantsValues.ChaveExecucaoEmContainer] = valor;
            }

            IConfiguration configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            RaizArmazenamentoDisco disco = new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions()),
                configuracao,
                new FakeLogService());

            // So o valor 'true' (sem diferenciar maiusculas) liga o modo container. Fora do Windows, sem pasta, a falha vale sempre.
            bool emContainerEsperado = string.Equals(valor, bool.TrueString, StringComparison.OrdinalIgnoreCase);
            bool falhaEsperada = emContainerEsperado || !OperatingSystem.IsWindows();
            if (falhaEsperada)
            {
                ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => disco.TentarObterSemCriar(out _, out _));
                Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoDiretorioObrigatorio, ex.Message);
            }
            else
            {
                Assert.Null(Record.Exception(() => disco.TentarObterSemCriar(out _, out _)));
            }
        }

        [Fact]
        public void FalhaDeConfiguracao_NaoFicaGuardadaESeguinteFunciona()
        {
            string raiz = Path.Combine(_pasta.Caminho, "armazenamento");
            ArmazenamentoDiscoOptions opcoes = new ArmazenamentoDiscoOptions { Diretorio = Path.Combine("relativa") };
            RaizArmazenamentoDisco disco = new RaizArmazenamentoDisco(
                Options.Create(opcoes),
                false,
                _pasta.Caminho,
                Path.Combine(_pasta.Caminho, NomeDaPastaDePublicacao),
                new FakeLogService(),
                true);
            Assert.Throws<ArmazenamentoDiscoException>(() => disco.ObterPastaDosArquivos());

            opcoes.Diretorio = raiz;
            string arquivos = disco.ObterPastaDosArquivos();

            Assert.Equal(Path.Combine(raiz, ConstantsValues.SubpastaArquivosArmazenamento), arquivos);
            Assert.True(Directory.Exists(arquivos));
        }

        private RaizArmazenamentoDisco CriarRaiz(
            string diretorio,
            FakeLogService log,
            bool emContainer = false,
            bool windows = true,
            string? pastaDeDados = null,
            string? pastaDePublicacao = null)
        {
            return new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions { Diretorio = diretorio }),
                emContainer,
                pastaDeDados ?? Path.Combine(_pasta.Caminho, NomeDaPastaDeDados),
                pastaDePublicacao ?? Path.Combine(_pasta.Caminho, NomeDaPastaDePublicacao),
                log,
                windows);
        }
    }

    /// <summary>Pasta temporaria propria de um teste, criada direto em Path.GetTempPath() e apagada no fim (sem raiz compartilhada).</summary>
    internal sealed class PastaTemporariaDeTeste : IDisposable
    {
        private const string PrefixoDaPastaDeTeste = "AutoViaTestesArmazenamento_";

        public PastaTemporariaDeTeste()
        {
            Caminho = Path.Combine(Path.GetTempPath(), PrefixoDaPastaDeTeste + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Caminho);
        }

        public string Caminho { get; }

        public void Dispose()
        {
            if (Directory.Exists(Caminho))
            {
                Directory.Delete(Caminho, recursive: true);
            }
        }
    }
}
