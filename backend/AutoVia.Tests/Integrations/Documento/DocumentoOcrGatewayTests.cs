using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Documento;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Documento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Documento;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Documento
{
    public class DocumentoOcrGatewayTests
    {
        private const string ProvedorRoteiro = "ROTEIRO";
        private const string NumeroSintetico = "12345678901";
        private const string ValidadeSintetica = "2031-05-20";
        private const string ChaveApiKeyTeste = "chave-secreta-teste";

        private static readonly byte[] JpegValido = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        private static readonly byte[] PngValido = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private static Dictionary<string, string> ValoresPadrao()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ConstantsDocumento.ChaveProvedor, ConstantsDocumento.ProvedorFake },
                { ConstantsDocumento.ChaveModeloOcr, "modelo-teste" },
                { ConstantsDocumento.ChaveConfiancaMinima, "0.85" },
                { ConstantsDocumento.ChaveApiKey, ChaveApiKeyTeste }
            };
        }

        private static Dictionary<string, string> ValoresRoteiro()
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores[ConstantsDocumento.ChaveProvedor] = ProvedorRoteiro;
            return valores;
        }

        private static FakeIntegrationRepository CriarRepositorio(Dictionary<string, string> valores, AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX, bool ativo = true)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Documento,
                Nome = "Documento de teste",
                Ambiente = ambiente,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 15000,
                Ativo = ativo
            };

            foreach (KeyValuePair<string, string> item in valores)
            {
                integration.Settings.Add(new IntegrationSetting
                {
                    Chave = item.Key,
                    Valor = item.Value,
                    Protegido = item.Key == ConstantsDocumento.ChaveApiKey
                });
            }

            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integration);
            return repositorio;
        }

        private static DocumentoOcrGateway CriarGateway(FakeIntegrationRepository repositorio, FakeLogService log, params IDocumentoOcrProvider[] providers)
        {
            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);
            return new DocumentoOcrGateway(resolver, providers, log);
        }

        private static CnhImagem CriarImagem(byte[] conteudo, string contentType, string nomeArquivo)
        {
            return new CnhImagem { Conteudo = conteudo, ContentType = contentType, NomeArquivo = nomeArquivo };
        }

        private static CnhImagem ImagemValida()
        {
            return CriarImagem(JpegValido, ConstantsDocumento.TipoJpeg, "cnh-frente.jpg");
        }

        private static CnhOcrBruto CriarBruto(
            decimal confianca,
            string? numero = NumeroSintetico,
            string? categoria = "AB",
            string? validade = ValidadeSintetica)
        {
            return new CnhOcrBruto
            {
                Numero = numero,
                ConfiancaNumero = confianca,
                Categoria = categoria,
                ConfiancaCategoria = confianca,
                Validade = validade,
                ConfiancaValidade = confianca
            };
        }

        // --- Imagem ---

        [Fact]
        public async Task LerCnhAsync_ImagemNula_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            FakeIntegrationRepository repositorio = CriarRepositorio(ValoresPadrao());
            DocumentoOcrGateway gateway = CriarGateway(repositorio, log, new FakeDocumentoOcrProvider(log));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(null!));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemObrigatoria, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_ImagemInvalida_NaoConsultaABase()
        {
            FakeLogService log = new FakeLogService();
            FakeIntegrationRepository repositorio = CriarRepositorio(ValoresPadrao());
            DocumentoOcrGateway gateway = CriarGateway(repositorio, log, new FakeDocumentoOcrProvider(log));

            await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(null!));

            Assert.Equal(0, repositorio.QueryCount);
        }

        [Fact]
        public async Task LerCnhAsync_ConteudoVazio_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            CnhImagem imagem = CriarImagem(Array.Empty<byte>(), ConstantsDocumento.TipoJpeg, "cnh.jpg");

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemObrigatoria, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_NomeArquivoVazio_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            CnhImagem imagem = CriarImagem(JpegValido, ConstantsDocumento.TipoJpeg, "");

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemObrigatoria, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_TipoNaoSuportado_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            CnhImagem imagem = CriarImagem(JpegValido, "image/gif", "cnh.gif");

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemTipo, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_BytesNaoBatemComContentType_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            CnhImagem imagem = CriarImagem(PngValido, ConstantsDocumento.TipoJpeg, "cnh.jpg");

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemTipo, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_MaisDe4Mb_LancaBusinessException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            byte[] grande = new byte[ConstantsDocumento.TamanhoMaximoBytes + 1];
            grande[0] = 0xFF;
            grande[1] = 0xD8;
            grande[2] = 0xFF;
            CnhImagem imagem = CriarImagem(grande, ConstantsDocumento.TipoJpeg, "cnh.jpg");

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(string.Format(ConstantsMessage.ErroDocumentoImagemTamanho, ConstantsDocumento.TamanhoMaximoMb), excecao.Message);
        }

        // --- Configuracao ---

        [Theory]
        [InlineData("abc")]
        [InlineData("1.5")]
        [InlineData("0,85")]
        [InlineData("-0.1")]
        public async Task LerCnhAsync_ConfiancaMinimaInvalida_LancaIntegrationExceptionCitandoAChave(string valor)
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores[ConstantsDocumento.ChaveConfiancaMinima] = valor;
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(valores), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsDocumento.ChaveConfiancaMinima, excecao.Chave);
            Assert.Equal(ConstantsMessage.ErroDocumentoConfiancaMinimaInvalida, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_ProvedorAusente_LancaIntegrationExceptionCitandoAChave()
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores.Remove(ConstantsDocumento.ChaveProvedor);
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(valores), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsIntegration.Documento, excecao.CodigoIntegracao);
            Assert.Equal(ConstantsDocumento.ChaveProvedor, excecao.Chave);
        }

        [Fact]
        public async Task LerCnhAsync_ProvedorDefinirNoBanco_LancaIntegrationExceptionCitandoAChave()
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores[ConstantsDocumento.ChaveProvedor] = ConstantsValues.MarcadorValorNaoDefinido;
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(valores), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsDocumento.ChaveProvedor, excecao.Chave);
        }

        [Fact]
        public async Task LerCnhAsync_ProvedorDesconhecido_LancaIntegrationException()
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores[ConstantsDocumento.ChaveProvedor] = "XPTO";
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(valores), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsMessage.ErroDocumentoProvedorNaoSuportado, excecao.Message);
        }

        [Theory]
        [InlineData(AmbienteIntegracao.PRODUCAO)]
        [InlineData(AmbienteIntegracao.HOMOLOGACAO)]
        public async Task LerCnhAsync_FakeForaDoSandbox_LancaIntegrationException(AmbienteIntegracao ambiente)
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao(), ambiente), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsMessage.ErroDocumentoFakeForaDoSandbox, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_ModeloOcrAusente_LancaIntegrationExceptionCitandoAChave()
        {
            Dictionary<string, string> valores = ValoresPadrao();
            valores.Remove(ConstantsDocumento.ChaveModeloOcr);
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(valores), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsDocumento.ChaveModeloOcr, excecao.Chave);
        }

        [Fact]
        public async Task LerCnhAsync_IntegracaoInativa_PropagaIntegrationException()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao(), ativo: false), log, new FakeDocumentoOcrProvider(log));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsIntegration.Documento, excecao.CodigoIntegracao);
        }

        // --- Provedor ---

        [Fact]
        public async Task LerCnhAsync_ProvedorLancaBusinessException_PropagaSemEmbrulhar()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(
                ProvedorRoteiro,
                null,
                new BusinessException(ConstantsMessage.ErroDocumentoImagemIlegivel));
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.LerCnhAsync(ImagemValida()));

            Assert.Equal(ConstantsMessage.ErroDocumentoImagemIlegivel, excecao.Message);
        }

        [Fact]
        public async Task LerCnhAsync_ExcecaoInesperada_VireIntegrationExceptionESLogaSemDadosDaCnh()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(
                ProvedorRoteiro,
                null,
                new InvalidOperationException("falha interna do provedor"));
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);
            CnhImagem imagem = ImagemValida();

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.LerCnhAsync(imagem));

            Assert.Equal(ConstantsIntegration.Documento, excecao.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroDocumentoLerCnh, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);

            Assert.Single(log.Entries);
            string bytesEmBase64 = Convert.ToBase64String(JpegValido);
            foreach (string entrada in log.Entries)
            {
                Assert.Contains(ConstantsMessage.ErroDocumentoLerCnh, entrada);
                Assert.DoesNotContain(bytesEmBase64, entrada);
                Assert.DoesNotContain(NumeroSintetico, entrada);
                Assert.DoesNotContain(ValidadeSintetica, entrada);
                Assert.DoesNotContain(ChaveApiKeyTeste, entrada);
            }
        }

        [Fact]
        public async Task LerCnhAsync_DoisProvedores_EscolheOPeloNome()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado roteiro = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.99m), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, new FakeDocumentoOcrProvider(log), roteiro);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(ProvedorRoteiro, resultado.Provedor);
            Assert.Equal(0.99m, resultado.ConfiancaGeral);
        }

        // --- Resultado e regra de confianca ---

        [Fact]
        public async Task LerCnhAsync_FakeComImagemComum_ConfiancaAltaNaoExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(NumeroSintetico, resultado.NumeroCnh);
            Assert.Equal<CategoriaCnh?>(CategoriaCnh.B, resultado.Categoria);
            Assert.Equal<DateOnly?>(new DateOnly(2030, 12, 31), resultado.Validade);
            Assert.Equal(0.97m, resultado.ConfiancaGeral);
            Assert.Equal(0.85m, resultado.ConfiancaMinima);
            Assert.False(resultado.RequerConferenciaManual);
            Assert.Equal("modelo-teste", resultado.ModeloOcr);
            Assert.Equal(ConstantsDocumento.ProvedorFake, resultado.Provedor);
        }

        [Fact]
        public async Task LerCnhAsync_FakeComNomeDeBaixaConfianca_ExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresPadrao()), log, new FakeDocumentoOcrProvider(log));
            CnhImagem imagem = CriarImagem(JpegValido, ConstantsDocumento.TipoJpeg, "cnh-baixa-confianca.jpg");

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(imagem);

            Assert.Equal(0.60m, resultado.ConfiancaGeral);
            Assert.True(resultado.RequerConferenciaManual);
            Assert.Equal(NumeroSintetico, resultado.NumeroCnh);
        }

        [Fact]
        public async Task LerCnhAsync_ConfiancaAbaixoDoMinimo_ExigeConferenciaEMantemCampos()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.80m), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(0.80m, resultado.ConfiancaGeral);
            Assert.True(resultado.RequerConferenciaManual);
            Assert.Equal(NumeroSintetico, resultado.NumeroCnh);
            Assert.Equal<CategoriaCnh?>(CategoriaCnh.AB, resultado.Categoria);
            Assert.Equal<DateOnly?>(new DateOnly(2031, 5, 20), resultado.Validade);
        }

        /// <summary>Confianca do provedor fora de 0..1 e limitada antes da media (Math.Clamp), sem virar regra.</summary>
        [Theory]
        [InlineData(1.5, 1.0, false)]
        [InlineData(-0.2, 0.0, true)]
        public async Task LerCnhAsync_ConfiancaForaDeZeroAUm_ELimitadaAntesDaMedia(double valorBruto, double esperado, bool requerConferencia)
        {
            decimal confianca = (decimal)valorBruto;
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(confianca), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal((decimal)esperado, resultado.ConfiancaNumero);
            Assert.Equal((decimal)esperado, resultado.ConfiancaCategoria);
            Assert.Equal((decimal)esperado, resultado.ConfiancaValidade);
            Assert.Equal((decimal)esperado, resultado.ConfiancaGeral);
            Assert.Equal(requerConferencia, resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_ConfiancaIgualAoMinimo_NaoExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.85m), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(0.85m, resultado.ConfiancaGeral);
            Assert.False(resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_CampoAusente_ExigeConferenciaEConfiancaZero()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.97m, numero: null), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Null(resultado.NumeroCnh);
            Assert.Equal(0m, resultado.ConfiancaNumero);
            Assert.Equal(0m, resultado.ConfiancaGeral);
            Assert.True(resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_CategoriaForaDoEnum_VireNullEExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.97m, categoria: "ACC"), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Null(resultado.Categoria);
            Assert.Equal(0m, resultado.ConfiancaCategoria);
            Assert.True(resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_NumeroComDezDigitos_ExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.97m, numero: "1234567890"), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal("1234567890", resultado.NumeroCnh);
            Assert.True(resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_NumeroComPontosEEspacos_ViraSoDigitosENaoExigeConferencia()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, CriarBruto(0.97m, numero: "123.456.789 01"), null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(NumeroSintetico, resultado.NumeroCnh);
            Assert.False(resultado.RequerConferenciaManual);
        }

        [Fact]
        public async Task LerCnhAsync_ConfiancaGeral_E_A_MenorDasTres()
        {
            FakeLogService log = new FakeLogService();
            CnhOcrBruto bruto = new CnhOcrBruto
            {
                Numero = NumeroSintetico,
                ConfiancaNumero = 0.99m,
                Categoria = "C",
                ConfiancaCategoria = 0.90m,
                Validade = ValidadeSintetica,
                ConfiancaValidade = 0.92m
            };
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ProvedorRoteiro, bruto, null);
            DocumentoOcrGateway gateway = CriarGateway(CriarRepositorio(ValoresRoteiro()), log, provedor);

            CnhLeituraResultado resultado = await gateway.LerCnhAsync(ImagemValida());

            Assert.Equal(0.90m, resultado.ConfiancaGeral);
            Assert.False(resultado.RequerConferenciaManual);
        }

        /// <summary>Provedor de teste com resposta roteirizada: devolve o bruto ou falha com a excecao informada.</summary>
        private sealed class ProvedorRoteirizado : IDocumentoOcrProvider
        {
            private readonly CnhOcrBruto? _bruto;
            private readonly Exception? _excecao;

            public ProvedorRoteirizado(string nome, CnhOcrBruto? bruto, Exception? excecao)
            {
                Nome = nome;
                _bruto = bruto;
                _excecao = excecao;
            }

            public string Nome { get; }

            public Task<CnhOcrBruto> AnalisarCnhAsync(ConfiguracaoIntegracao configuracao, CnhImagem imagem, CancellationToken cancellationToken = default)
            {
                if (_excecao != null)
                    return Task.FromException<CnhOcrBruto>(_excecao);

                return Task.FromResult<CnhOcrBruto>(_bruto ?? new CnhOcrBruto());
            }
        }
    }
}
