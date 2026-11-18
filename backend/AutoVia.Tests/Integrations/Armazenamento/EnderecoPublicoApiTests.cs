using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Integrations.Services.Armazenamento;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes do endereco publico dos links: configuracao vence; derivado da requisicao so em loopback.</summary>
    public sealed class EnderecoPublicoApiTests
    {
        private const string HostLocal = "localhost";
        private const string HostIpv6Local = "[::1]";
        private const string HostIpv4Local = "127.0.0.1";
        private const string HostExterno = "api.exemplo.com";
        private const string EsquemaHttp = "http";
        private const string EsquemaHttps = "https";
        private const int PortaDaApi = 5080;
        private const int PortaPadraoHttps = 443;

        [Theory]
        [InlineData("https://api.exemplo.com/", "https://api.exemplo.com")]
        [InlineData("https://api.exemplo.com", "https://api.exemplo.com")]
        [InlineData("http://localhost:5080", "http://localhost:5080")]
        [InlineData("http://127.0.0.1:5080/", "http://127.0.0.1:5080")]
        public void Configurado_Valido_VenceARequisicaoESaiSemBarraFinal(string configurado, string esperado)
        {
            EnderecoPublicoApi endereco = CriarEndereco(configurado, CriarContexto(HostExterno, PortaPadraoHttps, EsquemaHttps));

            Assert.Equal(esperado, endereco.Obter());
        }

        [Theory]
        [InlineData("http://api.exemplo.com")]
        [InlineData("https://api.exemplo.com/caminho")]
        [InlineData("https://api.exemplo.com/?x=1")]
        [InlineData("ftp://api.exemplo.com")]
        public void Configurado_Invalido_LancaED05(string configurado)
        {
            EnderecoPublicoApi endereco = CriarEndereco(configurado, null);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => endereco.Obter());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida, ex.Message);
        }

        [Theory]
        [InlineData(HostLocal, PortaDaApi, EsquemaHttp, "http://localhost:5080")]
        [InlineData(HostIpv4Local, PortaDaApi, EsquemaHttp, "http://127.0.0.1:5080")]
        [InlineData(HostIpv6Local, PortaDaApi, EsquemaHttp, "http://[::1]:5080")]
        public void Derivado_EmLoopback_UsaAPortaDaRequisicao(string host, int porta, string esquema, string esperado)
        {
            EnderecoPublicoApi endereco = CriarEndereco(string.Empty, CriarContexto(host, porta, esquema));

            Assert.Equal(esperado, endereco.Obter());
        }

        [Fact]
        public void Derivado_ForaDeLoopback_MesmoComHttps_LancaED05()
        {
            EnderecoPublicoApi endereco = CriarEndereco(string.Empty, CriarContexto(HostExterno, PortaPadraoHttps, EsquemaHttps));

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => endereco.Obter());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida, ex.Message);
        }

        [Fact]
        public void SemRequisicaoNemConfiguracao_LancaED05()
        {
            EnderecoPublicoApi endereco = CriarEndereco(string.Empty, null);

            ArmazenamentoDiscoException ex = Assert.Throws<ArmazenamentoDiscoException>(() => endereco.Obter());

            Assert.Equal(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida, ex.Message);
        }

        private static EnderecoPublicoApi CriarEndereco(string urlPublicaApi, HttpContext? contexto)
        {
            HttpContextAccessor acessor = new HttpContextAccessor { HttpContext = contexto };
            return new EnderecoPublicoApi(Options.Create(new ArmazenamentoDiscoOptions { UrlPublicaApi = urlPublicaApi }), acessor);
        }

        private static HttpContext CriarContexto(string host, int porta, string esquema)
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Request.Scheme = esquema;
            contexto.Request.Host = new HostString(host, porta);
            return contexto;
        }
    }
}
