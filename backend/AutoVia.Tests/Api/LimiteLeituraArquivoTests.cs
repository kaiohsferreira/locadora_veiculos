using AutoVia.Api.Controllers;
using AutoVia.Api.Extensions;
using AutoVia.Api.Middleware;
using AutoVia.Api.Requests.Armazenamento;
using AutoVia.Api.Validators;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Limite por IP e teto da rota de leitura de arquivo no pipeline real (controller, limitador, ErrorHandlingMiddleware), em
    /// servidor de teste (TestServer), sem subir a API. O IP de cada requisicao vem de um cabecalho de teste. Prova: o limite
    /// por IP vem antes do teto (IP recusado nao gasta o teto), o 429 agregado tem um registro por minuto, e as outras
    /// politicas continuam sem registro.
    /// </summary>
    public sealed class LimiteLeituraArquivoTests
    {
        private const string CabecalhoIpTeste = "X-Teste-Ip";
        private const string IpA = "198.51.100.10";
        private const string IpB = "198.51.100.20";
        private const string IpC = "198.51.100.30";
        private const int LimiteIpAlto = 1000;
        private const int LimiteIpUm = 1;
        private const int LimiteIpDois = 2;
        private const int TetoDois = 2;
        private const int TetoCinco = 5;
        private const int ChamadasDoIpRecusado = 10;
        private const int ContagemDaPrimeiraRecusaDoTeto = 1;
        private const int IntervaloAposOMinutoSegundos = 61;
        private const string CaminhoPublicaSemPolitica = "/livre";
        private const string CaminhoPublicaComPoliticaDeLogin = "/publica";

        private static readonly string SigValida = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres);
        private static readonly string CaminhoLeitura = "/api/armazenamento/arquivos?chave=veiculos/1/fotos/a.jpg&exp=1700000000&sig=" + SigValida;

        /// <summary>Servico de leitura que devolve um arquivo pequeno para qualquer link (o teste mede o limite, nao a leitura).</summary>
        private sealed class ServicoLeituraSempreOk : IArmazenamentoLeituraService
        {
            public Task<ArquivoParaLeitura> AbrirAsync(string chave, long expiraEmUnix, string assinatura, IReadOnlyCollection<string> etagsDoCliente, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ArquivoParaLeitura(new byte[] { 1, 2, 3 }, ConstantsArmazenamento.MimeJpeg, "x-1", false));
            }
        }

        /// <summary>Restringe o controller da leitura: o teste nao precisa dos outros controllers da API.</summary>
        private sealed class SomenteArmazenamentoProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                feature.Controllers.Clear();
                feature.Controllers.Add(typeof(ArmazenamentoArquivoController).GetTypeInfo());
            }
        }

        /// <summary>ILogService que separa as sobrecargas e guarda o que foi registrado.</summary>
        private sealed class LogCapturado : ILogService
        {
            public List<string> Mensagens { get; } = new List<string>();

            public List<string> Excecoes { get; } = new List<string>();

            public List<string> Informativos { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                Mensagens.Add(message);
            }

            public void Write(System.Exception exception, string friendlyMessage, string source)
            {
                Excecoes.Add(friendlyMessage);
            }

            public void WriteInfo(string message, string source)
            {
                Informativos.Add(message);
            }
        }

        /// <summary>Sobe um host de teste com a rota real, o limitador real e os limites informados (sem banco).</summary>
        private static async Task<WebApplication> IniciarAsync(int limiteIp, int teto, LogCapturado log, FakeTimeProvider relogio)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { ConstantsValues.ChaveLimiteLeituraArquivo, limiteIp.ToString(CultureInfo.InvariantCulture) },
                { ConstantsValues.ChaveLimiteLeituraArquivoGlobal, teto.ToString(CultureInfo.InvariantCulture) }
            });

            builder.Services.AddSingleton<ILogService>(log);
            builder.Services.AddSingleton<TimeProvider>(relogio);
            builder.Services.AddSingleton<IArmazenamentoLeituraService>(new ServicoLeituraSempreOk());
            builder.Services.AddSingleton<IValidator<LerArquivoArmazenadoRequest>, LerArquivoArmazenadoRequestValidator>();
            builder.Services.AddRateLimitingPublico(builder.Configuration);
            builder.Services.AddControllers().ConfigureApplicationPartManager(pm =>
            {
                pm.FeatureProviders.Clear();
                pm.FeatureProviders.Add(new SomenteArmazenamentoProvider());
            });

            WebApplication app = builder.Build();

            app.Use(async (HttpContext contexto, Func<Task> proximo) =>
            {
                if (contexto.Request.Headers.TryGetValue(CabecalhoIpTeste, out StringValues valor)
                    && IPAddress.TryParse(valor.ToString(), out IPAddress? endereco))
                {
                    contexto.Connection.RemoteIpAddress = endereco;
                }

                await proximo();
            });
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseRouting();
            app.UseRateLimiter();
            app.MapControllers();
            app.MapGet(CaminhoPublicaSemPolitica, () => Results.Ok());
            app.MapGet(CaminhoPublicaComPoliticaDeLogin, () => Results.Ok()).RequireRateLimiting(ConstantsValues.PoliticaLimitePublico);

            await app.StartAsync();
            return app;
        }

        private static HttpClient ClienteDe(WebApplication app)
        {
            TestServer servidor = (TestServer)app.Services.GetRequiredService<IServer>();
            return servidor.CreateClient();
        }

        /// <summary>Faz uma requisicao GET com o IP de teste e devolve o status.</summary>
        private static async Task<HttpStatusCode> PedirAsync(HttpClient cliente, string ip, string caminho)
        {
            using HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Get, caminho);
            pedido.Headers.Add(CabecalhoIpTeste, ip);
            using HttpResponseMessage resposta = await cliente.SendAsync(pedido);
            return resposta.StatusCode;
        }

        private static string MensagemPorIp(int contagem)
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraLimitePorIp, contagem);
        }

        [Fact]
        public async Task LimiteDoIp_TerceiraChamadaDoMesmoIpDa429_OutroIpPassa()
        {
            await using WebApplication app = await IniciarAsync(LimiteIpDois, LimiteIpAlto, new LogCapturado(), new FakeTimeProvider(DateTimeOffset.UtcNow));
            HttpClient cliente = ClienteDe(app);

            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpB, CaminhoLeitura));
        }

        [Fact]
        public async Task TetoDaRota_TerceiraChamadaDeQualquerIpDa429()
        {
            await using WebApplication app = await IniciarAsync(LimiteIpAlto, TetoDois, new LogCapturado(), new FakeTimeProvider(DateTimeOffset.UtcNow));
            HttpClient cliente = ClienteDe(app);

            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpB, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));
        }

        [Fact]
        public async Task IpRecusadoNaoGastaOTeto()
        {
            LogCapturado log = new LogCapturado();
            await using WebApplication app = await IniciarAsync(LimiteIpDois, TetoCinco, log, new FakeTimeProvider(DateTimeOffset.UtcNow));
            HttpClient cliente = ClienteDe(app);

            List<HttpStatusCode> doIpRecusado = new List<HttpStatusCode>();
            for (int chamada = 0; chamada < ChamadasDoIpRecusado; chamada++)
            {
                doIpRecusado.Add(await PedirAsync(cliente, IpA, CaminhoLeitura));
            }

            Assert.Equal(LimiteIpDois, doIpRecusado.Count(status => status == HttpStatusCode.OK));
            Assert.Equal(ChamadasDoIpRecusado - LimiteIpDois, doIpRecusado.Count(status => status == HttpStatusCode.TooManyRequests));

            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpB, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpB, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpC, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpC, CaminhoLeitura));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota, ContagemDaPrimeiraRecusaDoTeto), log.Mensagens);
        }

        [Fact]
        public async Task OutrasRotas_NaoSaoAfetadasPeloTetoNemPeloLimiteDaLeitura()
        {
            await using WebApplication app = await IniciarAsync(LimiteIpUm, LimiteIpAlto, new LogCapturado(), new FakeTimeProvider(DateTimeOffset.UtcNow));
            HttpClient cliente = ClienteDe(app);

            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoPublicaSemPolitica));
        }

        [Fact]
        public async Task RecusaDoLimiteDoIp_RegistraUmPorMinutoComAContagem()
        {
            LogCapturado log = new LogCapturado();
            FakeTimeProvider relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
            await using WebApplication app = await IniciarAsync(LimiteIpUm, LimiteIpAlto, log, relogio);
            HttpClient cliente = ClienteDe(app);

            Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));

            relogio.Avancar(TimeSpan.FromSeconds(IntervaloAposOMinutoSegundos));
            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoLeitura));

            Assert.Equal(2, log.Mensagens.Count);
            Assert.Equal(MensagemPorIp(1), log.Mensagens[0]);
            Assert.Equal(MensagemPorIp(2), log.Mensagens[1]);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task PoliticasDeLoginNaoRegistramO429()
        {
            LogCapturado log = new LogCapturado();
            await using WebApplication app = await IniciarAsync(LimiteIpUm, LimiteIpAlto, log, new FakeTimeProvider(DateTimeOffset.UtcNow));
            HttpClient cliente = ClienteDe(app);

            for (int chamada = 0; chamada < ConstantsValues.LimitePublicoPorMinutoPadrao; chamada++)
            {
                Assert.Equal(HttpStatusCode.OK, await PedirAsync(cliente, IpA, CaminhoPublicaComPoliticaDeLogin));
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, await PedirAsync(cliente, IpA, CaminhoPublicaComPoliticaDeLogin));
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }
    }
}
