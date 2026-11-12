using AutoVia.Api.Extensions;
using AutoVia.Core.Options;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Validacao da configuracao da recuperacao de senha na subida (AddRecuperacaoSenha): teto das opcoes (M2) e a regra
    /// da URL publica do front (M3/P3): fora de Development so https e host que nao seja loopback. Sem subir o servidor:
    /// as opcoes sao validadas ao serem lidas (o mesmo validador que o ValidateOnStart usa na subida).
    /// </summary>
    public class RecuperacaoSenhaStartupTests
    {
        private const string UrlDesenvolvimento = "http://localhost:5173";
        private const string UrlPublicaSegura = "https://exemplo.com";
        private const string UrlHttpDominio = "http://exemplo.com";
        private const string UrlLocalhostEmProducao = "http://localhost:5173";
        private const string UrlLoopbackIpv4 = "https://127.0.0.2";
        private const string UrlLoopbackIpv6 = "https://[::1]:5173";
        private const string UrlLocalhostComPontoFinal = "https://localhost./";
        private const string UrlLocalhostComPontoFinalEPorta = "https://localhost.:5173";
        private const string UrlSubdominioLocalhost = "https://app.localhost";
        private const string UrlSubdominioLocalhostProfundo = "https://api.teste.localhost:5173";
        private const string UrlHostComLocalhostNoMeio = "https://meu-localhost.com";
        private const string UrlHostQueTerminaParecidoComLocalhost = "https://app.localhostx.com";
        private const string UrlRelativa = "/sem-dominio";
        private const string ChaveUrl = "Autenticacao:Recuperacao:UrlPublicaFront";
        private const string ChaveValidade = "Autenticacao:Recuperacao:ValidadeMinutos";
        private const string ChaveLimite = "Autenticacao:Recuperacao:LimitePorHora";
        private const string ChaveTempo = "Autenticacao:Recuperacao:TempoMinimoRespostaMs";
        private const string ValidadeValida = "30";
        private const string LimiteValido = "3";
        private const string TempoValido = "400";
        private const string ValidadeAcimaDoTeto = "1441";
        private const string ValidadeNoTeto = "1440";
        private const string LimiteAcimaDoTeto = "101";
        private const string LimiteNoTeto = "100";
        private const string TempoAcimaDoTeto = "5001";
        private const string TempoNoTeto = "5000";

        /// <summary>Valores validos com a URL informada; o teste troca so o que quer provar.</summary>
        private static Dictionary<string, string?> ValoresValidos(string url)
        {
            return new Dictionary<string, string?>
            {
                { ChaveUrl, url },
                { ChaveValidade, ValidadeValida },
                { ChaveLimite, LimiteValido },
                { ChaveTempo, TempoValido }
            };
        }

        /// <summary>Le as opcoes como a API faz na subida: lancar OptionsValidationException se a configuracao for invalida.</summary>
        private static RecuperacaoSenhaOptions CarregarOpcoes(string ambiente, Dictionary<string, string?> valores)
        {
            IConfiguration configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            ServiceCollection services = new ServiceCollection();
            services.AddRecuperacaoSenha(configuracao, new GoogleProvaDeTeste.AmbienteFalso(ambiente));

            using ServiceProvider provedor = services.BuildServiceProvider();

            return provedor.GetRequiredService<IOptions<RecuperacaoSenhaOptions>>().Value;
        }

        [Fact]
        public void Development_HttpLocalhost_Passa()
        {
            RecuperacaoSenhaOptions opcoes = CarregarOpcoes(Environments.Development, ValoresValidos(UrlDesenvolvimento));

            Assert.Equal(UrlDesenvolvimento, opcoes.UrlPublicaFront);
        }

        [Fact]
        public void Production_HttpsDominioPublico_Passa()
        {
            RecuperacaoSenhaOptions opcoes = CarregarOpcoes(Environments.Production, ValoresValidos(UrlPublicaSegura));

            Assert.Equal(UrlPublicaSegura, opcoes.UrlPublicaFront);
        }

        /// <summary>M3/P3: fora de Development, http://localhost nao sobe a API.</summary>
        [Fact]
        public void Production_HttpLocalhost_ImpedeASubida()
        {
            Assert.Throws<OptionsValidationException>(() => CarregarOpcoes(Environments.Production, ValoresValidos(UrlLocalhostEmProducao)));
        }

        /// <summary>M3/P3: fora de Development, so https fora de loopback (http, loopback IPv4/IPv6 e URL relativa recusam).</summary>
        [Theory]
        [InlineData(UrlHttpDominio)]
        [InlineData(UrlLoopbackIpv4)]
        [InlineData(UrlLoopbackIpv6)]
        [InlineData(UrlRelativa)]
        [InlineData(UrlLocalhostComPontoFinal)]
        [InlineData(UrlLocalhostComPontoFinalEPorta)]
        [InlineData(UrlSubdominioLocalhost)]
        [InlineData(UrlSubdominioLocalhostProfundo)]
        public void Production_UrlInsegura_ImpedeASubida(string url)
        {
            Assert.Throws<OptionsValidationException>(() => CarregarOpcoes(Environments.Production, ValoresValidos(url)));
        }

        /// <summary>8c: parecido com localhost, mas que nao e loopback (sem o ponto antes de localhost), passa em producao.</summary>
        [Theory]
        [InlineData(UrlHostComLocalhostNoMeio)]
        [InlineData(UrlHostQueTerminaParecidoComLocalhost)]
        public void Production_HostParecidoComLocalhost_Passa(string url)
        {
            RecuperacaoSenhaOptions opcoes = CarregarOpcoes(Environments.Production, ValoresValidos(url));

            Assert.Equal(url, opcoes.UrlPublicaFront);
        }

        /// <summary>M2: teto das opcoes. Acima do teto a API nao sobe; no teto, sobe.</summary>
        [Theory]
        [InlineData(ChaveTempo, TempoAcimaDoTeto)]
        [InlineData(ChaveValidade, ValidadeAcimaDoTeto)]
        [InlineData(ChaveLimite, LimiteAcimaDoTeto)]
        public void ConfiguracaoAcimaDoTeto_ImpedeASubida(string chave, string valor)
        {
            Dictionary<string, string?> valores = ValoresValidos(UrlDesenvolvimento);
            valores[chave] = valor;

            Assert.Throws<OptionsValidationException>(() => CarregarOpcoes(Environments.Development, valores));
        }

        [Theory]
        [InlineData(ChaveTempo, TempoNoTeto)]
        [InlineData(ChaveValidade, ValidadeNoTeto)]
        [InlineData(ChaveLimite, LimiteNoTeto)]
        public void ConfiguracaoNoTeto_Sobe(string chave, string valor)
        {
            Dictionary<string, string?> valores = ValoresValidos(UrlDesenvolvimento);
            valores[chave] = valor;

            RecuperacaoSenhaOptions opcoes = CarregarOpcoes(Environments.Development, valores);

            Assert.NotNull(opcoes);
        }
    }
}
