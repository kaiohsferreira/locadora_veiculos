using AutoVia.Api.Extensions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Chave do limite por IP (ChaveIpLimite.Normalizar): IPv4, IPv4 mapeado, IPv6 por /64 (mesma chave no mesmo prefixo,
    /// chave diferente em outro prefixo), scope id removido e endereco nulo.
    /// </summary>
    public class ChaveIpLimiteTests
    {
        [Fact]
        public void Nulo_DevolveAChaveDesconhecida()
        {
            Assert.Equal(ConstantsValues.ChaveIpDesconhecido, ChaveIpLimite.Normalizar(null));
        }

        [Fact]
        public void IPv4_DevolveOProprioEndereco()
        {
            Assert.Equal("203.0.113.7", ChaveIpLimite.Normalizar(IPAddress.Parse("203.0.113.7")));
        }

        [Fact]
        public void IPv4MapeadoEmIPv6_DevolveOIPv4()
        {
            Assert.Equal("203.0.113.7", ChaveIpLimite.Normalizar(IPAddress.Parse("::ffff:203.0.113.7")));
        }

        [Fact]
        public void DoisIPv6DoMesmo64_DevolvemAMesmaChave()
        {
            string primeiro = ChaveIpLimite.Normalizar(IPAddress.Parse("2001:db8:1:2::1"));
            string segundo = ChaveIpLimite.Normalizar(IPAddress.Parse("2001:db8:1:2:aaaa:bbbb:cccc:dddd"));

            Assert.Equal("2001:db8:1:2::/64", primeiro);
            Assert.Equal(primeiro, segundo);
        }

        [Fact]
        public void IPv6DeOutro64_DevolveOutraChave()
        {
            string doPrefixoUm = ChaveIpLimite.Normalizar(IPAddress.Parse("2001:db8:1:2::1"));
            string doPrefixoDois = ChaveIpLimite.Normalizar(IPAddress.Parse("2001:db8:1:3::1"));

            Assert.Equal("2001:db8:1:3::/64", doPrefixoDois);
            Assert.NotEqual(doPrefixoUm, doPrefixoDois);
        }

        [Fact]
        public void ScopeIdEhRemovidoDaChave()
        {
            IPAddress comEscopo = IPAddress.Parse("2001:db8:1:2::1%5");

            Assert.Equal("2001:db8:1:2::/64", ChaveIpLimite.Normalizar(comEscopo));
        }
    }

    /// <summary>
    /// Interpretacao do cabecalho de IP pelo ForwardedHeadersMiddleware real (as mesmas opcoes que a API usa), sem servidor.
    /// Cada teste configura o cabecalho explicitamente (nao depende do padrao da API).
    /// </summary>
    public class CabecalhoIpEncaminhadoTests
    {
        private const string CabecalhoRealIp = "X-Real-IP";
        private const string CabecalhoXff = "X-Forwarded-For";
        private const string CabecalhoProto = "X-Forwarded-Proto";

        /// <summary>Roda o middleware com as opcoes do cabecalho configurado e devolve o contexto com o IP e o esquema resolvidos.</summary>
        private static async Task<DefaultHttpContext> ProcessarAsync(string? cabecalhoConfigurado, Dictionary<string, string> cabecalhos)
        {
            ForwardedHeadersOptions opcoes = new ForwardedHeadersOptions();
            AddRateLimitingStartup.AplicarCabecalhosEncaminhados(opcoes, AddRateLimitingStartup.ValidarCabecalhoIpCliente(cabecalhoConfigurado));

            DefaultHttpContext context = new DefaultHttpContext();

            foreach (KeyValuePair<string, string> cabecalho in cabecalhos)
            {
                context.Request.Headers[cabecalho.Key] = cabecalho.Value;
            }

            ForwardedHeadersMiddleware middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(opcoes));
            await middleware.Invoke(context);

            return context;
        }

        private static Dictionary<string, string> Um(string nome, string valor)
        {
            return new Dictionary<string, string> { { nome, valor } };
        }

        [Fact]
        public async Task IpUnico_ViraORemoteIpAddress()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoRealIp, "203.0.113.7"));

            Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
        }

        [Fact]
        public async Task IpComPorta_ViraSoOIp()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoRealIp, "203.0.113.7:8080"));

            Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
        }

        [Fact]
        public async Task IPv6SemColchetes_ViraORemoteIpAddress()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoRealIp, "2001:db8::1"));

            Assert.Equal(IPAddress.Parse("2001:db8::1"), context.Connection.RemoteIpAddress);
        }

        [Fact]
        public async Task IPv6ComColchetesEPorta_ViraSoOIp()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoRealIp, "[2001:db8::1]:443"));

            Assert.Equal(IPAddress.Parse("2001:db8::1"), context.Connection.RemoteIpAddress);
        }

        /// <summary>Com ForwardLimit = 1 o middleware le so o ultimo valor da lista (o acrescentado pela borda).</summary>
        [Fact]
        public async Task ListaDeDoisIps_UsaOUltimoComForwardLimitUm()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoXff, Um(CabecalhoXff, "203.0.113.7, 198.51.100.9"));

            Assert.Equal(IPAddress.Parse("198.51.100.9"), context.Connection.RemoteIpAddress);
        }

        /// <summary>Valor que nao e IP: o middleware nao aplica o endereco (o socket continua valendo).</summary>
        [Fact]
        public async Task ValorQueNaoEIp_NaoAlteraORemoteIpAddress()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoRealIp, "nao-e-um-ip"));

            Assert.Null(context.Connection.RemoteIpAddress);
        }

        /// <summary>Configurado X-Real-IP: um X-Forwarded-For forjado nao e lido.</summary>
        [Fact]
        public async Task ConfiguradoRealIp_XForwardedForForjadoNaoEhLido()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, Um(CabecalhoXff, "198.51.100.9"));

            Assert.Null(context.Connection.RemoteIpAddress);
        }

        /// <summary>Configurado X-Forwarded-For: um X-Real-IP forjado nao e lido.</summary>
        [Fact]
        public async Task ConfiguradoXff_RealIpForjadoNaoEhLido()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoXff, Um(CabecalhoRealIp, "198.51.100.9"));

            Assert.Null(context.Connection.RemoteIpAddress);
        }

        /// <summary>Cabecalho vazio (ou so espacos): nenhum cabecalho de IP e lido, nem o X-Real-IP nem o X-Forwarded-For.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CabecalhoVazio_IgnoraOsDoisCabecalhosDeIp(string cabecalhoConfigurado)
        {
            DefaultHttpContext context = await ProcessarAsync(cabecalhoConfigurado, new Dictionary<string, string>
            {
                { CabecalhoRealIp, "203.0.113.7" },
                { CabecalhoXff, "198.51.100.9" }
            });

            Assert.Null(context.Connection.RemoteIpAddress);
        }

        /// <summary>O X-Forwarded-Proto continua sendo lido mesmo com o cabecalho de IP desligado.</summary>
        [Fact]
        public async Task XForwardedProto_ContinuaAplicadoComCabecalhoVazio()
        {
            DefaultHttpContext context = await ProcessarAsync(string.Empty, Um(CabecalhoProto, "https"));

            Assert.Equal("https", context.Request.Scheme);
        }

        [Fact]
        public async Task XForwardedProto_ContinuaAplicadoComOCabecalhoDeIp()
        {
            DefaultHttpContext context = await ProcessarAsync(CabecalhoRealIp, new Dictionary<string, string>
            {
                { CabecalhoRealIp, "203.0.113.7" },
                { CabecalhoProto, "https" }
            });

            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal(IPAddress.Parse("203.0.113.7"), context.Connection.RemoteIpAddress);
        }

        /// <summary>Sem Rede:CabecalhoIpCliente na configuracao: o padrao e X-Real-IP (constante e cabecalho lido pelas opcoes da API).</summary>
        [Fact]
        public void SemChaveNaConfiguracao_PadraoELerXRealIp()
        {
            IConfiguration configuracaoVazia = new ConfigurationBuilder().Build();

            ForwardedHeadersOptions opcoes = AddRateLimitingStartup.CriarCabecalhosEncaminhados(configuracaoVazia);

            Assert.Equal(CabecalhoRealIp, ConstantsValues.CabecalhoIpClientePadrao);
            Assert.Equal(CabecalhoRealIp, opcoes.ForwardedForHeaderName);
        }
    }

    /// <summary>
    /// Servidor de teste com Rede:CabecalhoIpCliente e, opcionalmente, FORWARDEDHEADERS_ENABLED no valor informado. O
    /// ILogService vira um log em memoria (Log), para conferir o aviso da subida. Limite publico de 2 por minuto.
    /// </summary>
    public sealed class CabecalhoIpWebFactory : AutenticacaoWebFactory
    {
        private readonly string _valorCabecalho;
        private readonly string? _guardaHost;

        public CabecalhoIpWebFactory(string valorCabecalho, string? guardaHost = null)
            : base(2, null)
        {
            _valorCabecalho = valorCabecalho;
            _guardaHost = guardaHost;
        }

        /// <summary>Log em memoria no lugar do ILogService real: recebe o aviso gravado na subida.</summary>
        public FakeLogService Log { get; } = new FakeLogService();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILogService>();
                services.AddSingleton<ILogService>(Log);
            });
        }

        protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
            valores[ConstantsValues.ChaveCabecalhoIpCliente] = _valorCabecalho;

            if (_guardaHost != null)
            {
                valores[ConstantsValues.ChaveForwardedHeadersHabilitado] = _guardaHost;
            }
        }
    }

    /// <summary>
    /// Servidor real (TestServer) com o cabecalho de IP configurado explicitamente. Prova as duas direcoes: configurado
    /// X-Real-IP, o X-Forwarded-For forjado nao cria particao; configurado X-Forwarded-For, o X-Real-IP forjado nao cria
    /// particao e a lista "a, b" vale o ultimo valor. Tambem: IPv6 pela politica real (ChaveIpLimite), cabecalho vazio,
    /// valor invalido, guarda do host, aviso na subida e X-Forwarded-Proto.
    /// </summary>
    public class RedeCabecalhoIpHttpTests
    {
        private const string RotaLogin = "/api/auth/login";
        private const string RotaClientes = "/api/clientes";
        private const string CabecalhoRealIp = "X-Real-IP";
        private const string CabecalhoXff = "X-Forwarded-For";
        private const string CabecalhoProto = "X-Forwarded-Proto";
        private const string CpfNovoCabecalho = "86288366757";
        private const string CnhNovoCabecalho = "20000000002";
        private const string SenhaNovaCabecalho = "Senha1234Http";

        /// <summary>Login anonimo (com corpo invalido: 400, mas passa pelo limite) com os cabecalhos informados.</summary>
        private static HttpRequestMessage Login(params (string Nome, string Valor)[] cabecalhos)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaLogin);
            requisicao.Content = CorpoJson(new { perfil = "CLIENTE", cpf = "123", senha = "" });

            foreach ((string Nome, string Valor) cabecalho in cabecalhos)
            {
                requisicao.Headers.Add(cabecalho.Nome, cabecalho.Valor);
            }

            return requisicao;
        }

        private static StringContent CorpoJson(object corpo)
        {
            return new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
        }

        // --- Direcao (a): configurado X-Real-IP ---

        /// <summary>Com o limite de 2 por minuto: tres pedidos do mesmo IP dao 429 no terceiro; outro IP continua passando.</summary>
        [Fact]
        public async Task ConfiguradoRealIp_SeparaOsLimitesPorIp()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoRealIp);
            HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage primeira = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1")));
            HttpResponseMessage segunda = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1")));
            HttpResponseMessage outroIp = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.2")));

            Assert.NotEqual(HttpStatusCode.TooManyRequests, primeira.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, segunda.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, outroIp.StatusCode);
        }

        /// <summary>Configurado X-Real-IP: um X-Forwarded-For forjado com valores diferentes nao cria particao nova.</summary>
        [Fact]
        public async Task ConfiguradoRealIp_XForwardedForForjadoNaoCriaParticaoNova()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoRealIp);
            HttpClient cliente = fabrica.CreateClient();

            await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1"), (CabecalhoXff, "203.0.113.1")));
            await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1"), (CabecalhoXff, "203.0.113.2")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1"), (CabecalhoXff, "203.0.113.3")));

            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        }

        // --- Direcao (b): configurado X-Forwarded-For (configuracao explicita, nao o padrao) ---

        /// <summary>Configurado X-Forwarded-For: um X-Real-IP forjado com valores diferentes nao cria particao nova.</summary>
        [Fact]
        public async Task ConfiguradoXff_RealIpForjadoNaoCriaParticaoNova()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff);
            HttpClient cliente = fabrica.CreateClient();

            await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1"), (CabecalhoRealIp, "203.0.113.1")));
            await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1"), (CabecalhoRealIp, "203.0.113.2")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1"), (CabecalhoRealIp, "203.0.113.3")));

            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        }

        /// <summary>Configurado X-Forwarded-For com lista "a, b": vale o ultimo valor. Ultimos valores diferentes, sem 429.</summary>
        [Fact]
        public async Task ConfiguradoXff_ListaUsaOUltimoValor()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff);
            HttpClient cliente = fabrica.CreateClient();

            await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1, 203.0.113.1")));
            await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1, 203.0.113.2")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1, 203.0.113.3")));

            Assert.NotEqual(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        }

        /// <summary>Configurado X-Forwarded-For com lista "a, b": o ultimo valor igual, entao a particao e a mesma e o terceiro da 429.</summary>
        [Fact]
        public async Task ConfiguradoXff_ListaComMesmoUltimoValor_DaParticaoIgual()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff);
            HttpClient cliente = fabrica.CreateClient();

            await cliente.SendAsync(Login((CabecalhoXff, "203.0.113.5, 198.51.100.1")));
            await cliente.SendAsync(Login((CabecalhoXff, "203.0.113.6, 198.51.100.1")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoXff, "203.0.113.7, 198.51.100.1")));

            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        }

        // --- IPv6 pela politica real (C4): mesmo /64 esgota, outro /64 passa ---

        /// <summary>Tres enderecos IPv6 do mesmo /64 (formato sem colchetes e sem porta) esgotam o limite; um IPv6 de outro /64 passa.</summary>
        [Fact]
        public async Task Ipv6DoMesmo64_EsgotaOLimiteEOutro64Passa()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff);
            HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage primeira = await cliente.SendAsync(Login((CabecalhoXff, "2001:db8:1:2::1")));
            HttpResponseMessage segunda = await cliente.SendAsync(Login((CabecalhoXff, "2001:db8:1:2::2")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoXff, "2001:db8:1:2::3")));
            HttpResponseMessage outroPrefixo = await cliente.SendAsync(Login((CabecalhoXff, "2001:db8:1:3::1")));

            Assert.NotEqual(HttpStatusCode.TooManyRequests, primeira.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, segunda.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, outroPrefixo.StatusCode);
        }

        // --- Cabecalho vazio, invalido e guarda do host ---

        /// <summary>Cabecalho configurado vazio (ou so espacos): X-Real-IP e X-Forwarded-For sao ignorados, entao todos caem na mesma particao (o socket).</summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CabecalhoVazio_IgnoraOsDoisCabecalhos(string valorConfigurado)
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(valorConfigurado);
            HttpClient cliente = fabrica.CreateClient();

            await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.1"), (CabecalhoXff, "203.0.113.1")));
            await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.2"), (CabecalhoXff, "203.0.113.2")));
            HttpResponseMessage terceira = await cliente.SendAsync(Login((CabecalhoRealIp, "198.51.100.3"), (CabecalhoXff, "203.0.113.3")));

            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
        }

        /// <summary>Cabecalho vazio na subida: o aviso (ILogService.WriteInfo) e registrado, sem o valor de configuracao.</summary>
        [Fact]
        public void CabecalhoVazio_RegistraAvisoNaSubida()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(string.Empty);

            fabrica.CreateClient();

            Assert.Contains(ConstantsMessage.AvisoCabecalhoIpDesligado, fabrica.Log.Snapshot());
        }

        /// <summary>Cabecalho ligado (X-Forwarded-For): nenhum aviso de cabecalho desligado na subida.</summary>
        [Fact]
        public void CabecalhoLigado_NaoRegistraAviso()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff);

            fabrica.CreateClient();

            Assert.DoesNotContain(ConstantsMessage.AvisoCabecalhoIpDesligado, fabrica.Log.Snapshot());
        }

        /// <summary>Valor que nao e nome de cabecalho HTTP valido: a API nao sobe (InvalidOperationException com a mensagem de configuracao).</summary>
        [Fact]
        public void CabecalhoInvalido_ImpedeASubidaDaApi()
        {
            using CabecalhoIpWebFactory fabricaInvalida = new CabecalhoIpWebFactory("X Real IP");

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabricaInvalida.CreateClient());

            Assert.Contains(ConstantsMessage.ErroConfiguracaoCabecalhoIpCliente, excecao.ToString());
        }

        /// <summary>FORWARDEDHEADERS_ENABLED verdadeira (qualquer caixa): a API nao sobe, com a mensagem da guarda.</summary>
        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("TRUE")]
        public void ForwardedHeadersHabilitado_ImpedeASubidaDaApi(string valorGuarda)
        {
            using CabecalhoIpWebFactory fabricaComGuarda = new CabecalhoIpWebFactory(CabecalhoXff, valorGuarda);

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabricaComGuarda.CreateClient());

            Assert.Contains(ConstantsMessage.ErroHostComCabecalhosAutomaticos, excecao.ToString());
        }

        /// <summary>FORWARDEDHEADERS_ENABLED falsa: a API sobe e responde (sem 500 e sem 429 para a primeira requisicao).</summary>
        [Fact]
        public async Task ForwardedHeadersDesabilitado_SobeEResponde()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoXff, "false");
            HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(Login((CabecalhoXff, "198.51.100.1")));

            Assert.NotEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, resposta.StatusCode);
        }

        /// <summary>O X-Forwarded-Proto segue valendo: o Location do 201 sai com https mesmo com o cabecalho de IP configurado.</summary>
        [Fact]
        public async Task XForwardedProto_SegueAplicadoNoLocationDoCadastro()
        {
            using CabecalhoIpWebFactory fabrica = new CabecalhoIpWebFactory(CabecalhoRealIp);
            HttpClient cliente = fabrica.CreateClient();
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaClientes);
            requisicao.Content = CorpoJson(new
            {
                nome = "Maria Teste Cabecalho",
                cpf = CpfNovoCabecalho,
                dataNascimento = new DateOnly(1990, 5, 20),
                email = "maria.cabecalho@exemplo.com",
                telefone = "11987654321",
                cnhNumero = CnhNovoCabecalho,
                cnhCategoria = "B",
                cnhValidade = DateOnly.FromDateTime(DateTime.Now).AddYears(3),
                senha = SenhaNovaCabecalho
            });
            requisicao.Headers.Add(CabecalhoProto, "https");
            requisicao.Headers.Add(CabecalhoRealIp, "198.51.100.1");

            HttpResponseMessage resposta = await cliente.SendAsync(requisicao);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Equal("https", resposta.Headers.Location?.Scheme);
        }
    }
}
