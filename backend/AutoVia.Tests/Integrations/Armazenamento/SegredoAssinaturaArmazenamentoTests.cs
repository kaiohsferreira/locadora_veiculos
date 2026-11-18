using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes do segredo de assinatura em arquivo: so o envio cria, a leitura nunca cria, tamanho errado recusa.</summary>
    public sealed class SegredoAssinaturaArmazenamentoTests : IDisposable
    {
        private const int TamanhoDeArquivoErrado = 10;
        private const int InstanciasConcorrentes = 8;

        private readonly PastaTemporariaDeTeste _pasta = new PastaTemporariaDeTeste();
        private readonly string _raiz;

        public SegredoAssinaturaArmazenamentoTests()
        {
            _raiz = Path.Combine(_pasta.Caminho, "armazenamento");
        }

        public void Dispose()
        {
            _pasta.Dispose();
        }

        [Fact]
        public void ObterSeExistir_SemPasta_DevolveNuloEFicaSemPastaNenhuma()
        {
            SegredoAssinaturaArmazenamento segredo = CriarSegredo(new FakeLogService());

            Assert.Null(segredo.ObterSeExistir());
            Assert.False(Directory.Exists(_raiz));
        }

        [Fact]
        public void ObterOuCriar_CriaSegredoDe32BytesEOutraInstanciaLeOMesmo()
        {
            FakeLogService log = new FakeLogService();
            byte[] criado = CriarSegredo(log).ObterOuCriar();

            byte[] lidoPorOutraInstancia = CriarSegredo(log).ObterOuCriar();
            byte[]? lidoComObterSeExistir = CriarSegredo(log).ObterSeExistir();

            Assert.Equal(ConstantsValues.TamanhoSegredoAssinaturaBytes, criado.Length);
            Assert.Equal(criado, lidoPorOutraInstancia);
            Assert.Equal(criado, lidoComObterSeExistir);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public void ArquivoDeTamanhoErrado_LancaED06_SemSobrescrever()
        {
            FakeLogService log = new FakeLogService();
            string caminho = Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            byte[] errado = new byte[TamanhoDeArquivoErrado];
            File.WriteAllBytes(caminho, errado);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => CriarSegredo(log).ObterOuCriar());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura, ex.Message);
            Assert.Equal(errado, File.ReadAllBytes(caminho));
        }

        [Fact]
        public async Task ObterOuCriar_CriacoesConcorrentes_TerminamComUmUnicoSegredo()
        {
            FakeLogService log = new FakeLogService();
            RaizArmazenamentoDisco raiz = CriarRaiz(log);

            Task<byte[]>[] tarefas = Enumerable.Range(0, InstanciasConcorrentes)
                .Select(indice => Task.Run(() => new SegredoAssinaturaArmazenamento(raiz, log).ObterOuCriar()))
                .ToArray();
            byte[][] segredos = await Task.WhenAll(tarefas);

            Assert.All(segredos, segredo => Assert.Equal(segredos[0], segredo));
            Assert.Equal(ConstantsValues.TamanhoSegredoAssinaturaBytes, segredos[0].Length);
            Assert.Single(Directory.EnumerateFiles(Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento)));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(_raiz, ConstantsValues.SubpastaTemporariosArmazenamento)));
        }

        private RaizArmazenamentoDisco CriarRaiz(FakeLogService log)
        {
            return new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions { Diretorio = _raiz }),
                false,
                _pasta.Caminho,
                Path.Combine(_pasta.Caminho, "publicacao"),
                log,
                true);
        }

        private SegredoAssinaturaArmazenamento CriarSegredo(FakeLogService log)
        {
            RaizArmazenamentoDisco raiz = new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions { Diretorio = _raiz }),
                false,
                _pasta.Caminho,
                Path.Combine(_pasta.Caminho, "publicacao"),
                log,
                true);
            return new SegredoAssinaturaArmazenamento(raiz, log);
        }
    }
}
