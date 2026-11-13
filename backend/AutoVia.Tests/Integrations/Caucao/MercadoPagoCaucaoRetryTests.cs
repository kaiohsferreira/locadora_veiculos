using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http;

namespace AutoVia.Tests.Integrations.Caucao
{
    /// <summary>
    /// Retry da caucao de ponta a ponta: CaucaoGateway -> MercadoPagoCaucaoProvider -> ComunicacaoService REAL,
    /// com um HttpMessageHandler roteirizado no lugar da rede. Prova que a pre-autorizacao repete com a mesma
    /// chave de idempotencia, que 4xx e 429 nao repetem, e que consulta (GET) e captura/cancelamento (PUT) repetem em 5xx.
    /// </summary>
    public class MercadoPagoCaucaoRetryTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string TokenCartao = "tok_cartao_9876543210";
        private const string Referencia = "CAUCAO-LOC-123";
        private const string BaseUrl = "https://api.mercadopago.com";
        private const string CorpoReservada = "{\"id\":123456789,\"status\":\"authorized\",\"status_detail\":\"pending_capture\",\"transaction_amount\":500,\"external_reference\":\"CAUCAO-LOC-123\",\"date_created\":\"2026-11-10T10:00:00.000-03:00\",\"card\":{\"last_four_digits\":\"4242\"}}";
        private const string CorpoAprovada = "{\"id\":123456789,\"status\":\"approved\",\"transaction_amount\":300,\"external_reference\":\"CAUCAO-LOC-123\",\"date_approved\":\"2026-11-10T11:00:00.000-03:00\"}";
        private const string CorpoCancelada = "{\"id\":123456789,\"status\":\"cancelled\",\"transaction_amount\":500,\"external_reference\":\"CAUCAO-LOC-123\"}";

        // ------------------------------------------------------------------
        // Handler roteirizado: cada chamada HTTP consome o proximo passo e e gravada para conferencia.
        // ------------------------------------------------------------------

        /// <summary>Um passo do roteiro. Status nulo = falha de rede.</summary>
        private sealed record PassoRoteirizado(HttpStatusCode? Status, string Corpo);

        /// <summary>Foto da requisicao real, tirada dentro do handler (metodo, URL, corpo e cabecalhos).</summary>
        private sealed record RequisicaoCapturada(string Metodo, string Url, string Corpo, Dictionary<string, string> Cabecalhos);

        private sealed class HandlerRoteirizado : HttpMessageHandler
        {
            private readonly Queue<PassoRoteirizado> _roteiro = new Queue<PassoRoteirizado>();

            public List<RequisicaoCapturada> Requisicoes { get; } = new List<RequisicaoCapturada>();

            public void Responder(HttpStatusCode status, string corpo = "{}")
            {
                _roteiro.Enqueue(new PassoRoteirizado(status, corpo));
            }

            public void FalharRede()
            {
                _roteiro.Enqueue(new PassoRoteirizado(null, string.Empty));
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string corpoEnviado = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Requisicoes.Add(new RequisicaoCapturada(request.Method.Method, request.RequestUri?.ToString() ?? string.Empty, corpoEnviado, LerCabecalhos(request)));

                PassoRoteirizado passo = _roteiro.Dequeue();

                if (passo.Status == null)
                {
                    throw new HttpRequestException("falha simulada de rede");
                }

                return new HttpResponseMessage(passo.Status.Value)
                {
                    Content = new StringContent(passo.Corpo),
                    RequestMessage = request
                };
            }

            private static Dictionary<string, string> LerCabecalhos(HttpRequestMessage request)
            {
                Dictionary<string, string> cabecalhos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, IEnumerable<string>> cabecalho in request.Headers)
                {
                    cabecalhos[cabecalho.Key] = string.Join(",", cabecalho.Value);
                }

                if (request.Content != null)
                {
                    foreach (KeyValuePair<string, IEnumerable<string>> cabecalho in request.Content.Headers)
                    {
                        cabecalhos[cabecalho.Key] = string.Join(",", cabecalho.Value);
                    }
                }

                return cabecalhos;
            }
        }

        // ------------------------------------------------------------------
        // Montagem do gateway real (resolver + repositorio falso + provedor Mercado Pago + ComunicacaoService).
        // ------------------------------------------------------------------

        private static Integration CriarIntegracao()
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = AmbienteIntegracao.SANDBOX,
                BaseUrl = BaseUrl,
                TimeoutMs = 30000,
                Ativo = true
            };

            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveProvedor, Valor = ConstantsCaucao.ProvedorMercadoPago, Protegido = false });
            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveAccessToken, Valor = TokenSecreto, Protegido = true });

            return integration;
        }

        private static CaucaoGateway CriarGateway(ComunicacaoService servico, ILogService log)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(CriarIntegracao());

            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);
            MercadoPagoCaucaoProvider provider = new MercadoPagoCaucaoProvider(servico, log);

            return new CaucaoGateway(resolver, new ICaucaoProvider[] { provider }, log);
        }

        private static CaucaoSolicitacao CriarSolicitacao()
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = 500m,
                Descricao = "Caucao da locacao",
                TokenCartao = TokenCartao,
                MetodoPagamentoId = "visa",
                Pagador = new CaucaoPagador { Email = "maria@exemplo.com", Documento = "12345678901" }
            };
        }

        // ------------------------------------------------------------------
        // Pre-autorizacao (POST): repete com a mesma chave; 4xx e 429 nunca repetem.
        // ------------------------------------------------------------------

        [Fact]
        public async Task PreAutorizar_500_500_201_TresRequisicoesComAMesmaChaveDeIdempotencia()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.Created, CorpoReservada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal("123456789", resultado.IdentificadorProvedor);
            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(3, handler.Requisicoes.Count);

            string chaveEsperada = handler.Requisicoes[0].Cabecalhos[ConstantsCaucao.CabecalhoIdempotenciaMercadoPago];

            foreach (RequisicaoCapturada requisicao in handler.Requisicoes)
            {
                Assert.Equal(HttpMethod.Post.Method, requisicao.Metodo);
                Assert.Equal(chaveEsperada, requisicao.Cabecalhos[ConstantsCaucao.CabecalhoIdempotenciaMercadoPago]);
                Assert.Equal(chaveEsperada, requisicao.Cabecalhos[ConstantsIntegration.CabecalhoIdempotencia]);
                Assert.Equal("Bearer " + TokenSecreto, requisicao.Cabecalhos["Authorization"]);
                Assert.Contains("\"capture\":false", requisicao.Corpo);
                Assert.Contains("\"installments\":1", requisicao.Corpo);
            }
        }

        [Fact]
        public async Task PreAutorizar_400_NaoRepeteEDevolveBusinessException()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.BadRequest, "{\"message\":\"dados recusados\"}");
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
            Assert.Single(handler.Requisicoes);
        }

        [Fact]
        public async Task PreAutorizar_500_500_500_DevolveProvedorIndisponivelAposTresTentativas()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoProvedorIndisponivel, excecao.Message);
            Assert.Equal(3, handler.Requisicoes.Count);
        }

        [Fact]
        public async Task PreAutorizar_FalhaDeRedeDepoisCriado_RepeteComAMesmaChave()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.FalharRede();
            handler.Responder(HttpStatusCode.Created, CorpoReservada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(2, handler.Requisicoes.Count);
            Assert.Equal(
                handler.Requisicoes[0].Cabecalhos[ConstantsCaucao.CabecalhoIdempotenciaMercadoPago],
                handler.Requisicoes[1].Cabecalhos[ConstantsCaucao.CabecalhoIdempotenciaMercadoPago]);
        }

        [Fact]
        public async Task PreAutorizar_401_NaoRepete()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.Unauthorized, "{\"message\":\"unauthorized\"}");
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoCredenciaisRecusadas, excecao.Message);
            Assert.Single(handler.Requisicoes);
        }

        [Fact]
        public async Task PreAutorizar_429_NaoRepete()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder((HttpStatusCode)429, "{}");
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoLimiteRequisicoes, excecao.Message);
            Assert.Single(handler.Requisicoes);
        }

        // ------------------------------------------------------------------
        // Consulta (GET), captura (PUT) e cancelamento (PUT): repetem em 5xx.
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConsultarAsync_500_500_200_RepeteOGet()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoReservada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.ConsultarAsync("123456789");

            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(3, handler.Requisicoes.Count);

            foreach (RequisicaoCapturada requisicao in handler.Requisicoes)
            {
                Assert.Equal(HttpMethod.Get.Method, requisicao.Metodo);
            }
        }

        [Fact]
        public async Task CapturarAsync_PutRepeteEmCincoXx()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.OK, CorpoReservada);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoAprovada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.CapturarAsync("123456789", 300m);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(300m, resultado.ValorCapturado);

            // 1 GET (estado antes de capturar) + 3 PUT (captura com 5xx repetida).
            Assert.Equal(4, handler.Requisicoes.Count);
            Assert.Equal(HttpMethod.Get.Method, handler.Requisicoes[0].Metodo);

            List<RequisicaoCapturada> capturas = handler.Requisicoes.Skip(1).ToList();
            Assert.Equal(3, capturas.Count);

            foreach (RequisicaoCapturada requisicao in capturas)
            {
                Assert.Equal(HttpMethod.Put.Method, requisicao.Metodo);
                Assert.Contains("\"transaction_amount\":300.00", requisicao.Corpo);
            }
        }

        // ------------------------------------------------------------------
        // Rodada de correcao: cancelamento em EM_ANALISE, reconciliacao apos 5xx esgotado e segredos nos caminhos de erro.
        // ------------------------------------------------------------------

        private const string CorpoEmAnalise = "{\"id\":123456789,\"status\":\"in_process\",\"transaction_amount\":500,\"external_reference\":\"CAUCAO-LOC-123\",\"date_created\":\"2026-11-10T10:00:00.000-03:00\"}";

        /// <summary>Log local dos testes da caucao: guarda tambem exception.ToString(), que o FakeLogService nao guarda.</summary>
        private sealed class LogCompleto : ILogService
        {
            public List<string> Entradas { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                Entradas.Add(message + " | " + friendlyMessage);
            }

            public void Write(Exception exception, string friendlyMessage, string source)
            {
                Entradas.Add(friendlyMessage + " | " + exception.ToString());
            }

            public void WriteInfo(string message, string source)
            {
                Entradas.Add(message);
            }
        }

        [Fact]
        public async Task CancelarAsync_EmAnalise_GetInProcessEPutCancelled_DevolveCancelada()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.OK, CorpoEmAnalise);
            handler.Responder(HttpStatusCode.OK, CorpoCancelada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.CancelarAsync("123456789");

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(2, handler.Requisicoes.Count);
            Assert.Equal(HttpMethod.Get.Method, handler.Requisicoes[0].Metodo);
            Assert.Equal(HttpMethod.Put.Method, handler.Requisicoes[1].Metodo);
        }

        [Fact]
        public async Task CapturarAsync_PutCincoXxComGetApproved_ReconciliaESucesso()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.OK, CorpoReservada);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoAprovada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.CapturarAsync("123456789", 300m);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(300m, resultado.ValorCapturado);

            // GET (estado) + 3 PUT (5xx esgotado) + GET (reconciliacao).
            Assert.Equal(5, handler.Requisicoes.Count);
            Assert.Equal(HttpMethod.Get.Method, handler.Requisicoes[4].Metodo);
        }

        [Theory]
        [InlineData("consultar")]
        [InlineData("capturar")]
        [InlineData("cancelar")]
        public async Task Segredos_CaminhosDeErro_NaoVazamEmExcecaoNemEmLog(string operacao)
        {
            string corpoEcoado = "{\"message\":\"" + TokenSecreto + " " + TokenCartao + " maria@exemplo.com 12345678901 MARIA@EXEMPLO.COM 123.456.789-01\"}";
            HandlerRoteirizado handler = new HandlerRoteirizado();

            if (operacao == "consultar")
            {
                handler.Responder(HttpStatusCode.BadRequest, corpoEcoado);
            }
            else if (operacao == "capturar")
            {
                handler.Responder(HttpStatusCode.OK, CorpoReservada);
                handler.Responder(HttpStatusCode.Unauthorized, corpoEcoado);
                handler.Responder(HttpStatusCode.Unauthorized, corpoEcoado);
            }
            else
            {
                handler.Responder(HttpStatusCode.OK, CorpoReservada);
                handler.Responder(HttpStatusCode.BadRequest, corpoEcoado);
                handler.Responder(HttpStatusCode.BadRequest, corpoEcoado);
            }

            LogCompleto log = new LogCompleto();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            Exception excecao;

            if (operacao == "consultar")
            {
                excecao = await Assert.ThrowsAnyAsync<Exception>(() => gateway.ConsultarAsync("123456789"));
            }
            else if (operacao == "capturar")
            {
                excecao = await Assert.ThrowsAnyAsync<Exception>(() => gateway.CapturarAsync("123456789", 300m));
            }
            else
            {
                excecao = await Assert.ThrowsAnyAsync<Exception>(() => gateway.CancelarAsync("123456789"));
            }

            string[] segredos = { TokenSecreto, TokenCartao, "maria@exemplo.com", "12345678901", "123.456.789-01" };

            foreach (string segredo in segredos)
            {
                Assert.DoesNotContain(segredo, excecao.ToString(), StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(log.Entradas, entrada => entrada.Contains(segredo, StringComparison.OrdinalIgnoreCase));
            }

            Assert.DoesNotContain(log.Entradas, entrada => entrada.Contains("MARIA@EXEMPLO.COM", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CancelarAsync_PutRepeteEmCincoXx()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.OK, CorpoReservada);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoCancelada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            CaucaoGateway gateway = CriarGateway(servico, log);

            CaucaoResultado resultado = await gateway.CancelarAsync("123456789");

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);

            // 1 GET (estado antes de cancelar) + 3 PUT (cancelamento com 5xx repetido).
            Assert.Equal(4, handler.Requisicoes.Count);
            Assert.Equal(HttpMethod.Get.Method, handler.Requisicoes[0].Metodo);

            List<RequisicaoCapturada> cancelamentos = handler.Requisicoes.Skip(1).ToList();
            Assert.Equal(3, cancelamentos.Count);

            foreach (RequisicaoCapturada requisicao in cancelamentos)
            {
                Assert.Equal(HttpMethod.Put.Method, requisicao.Metodo);
                Assert.Contains("\"status\":\"" + ConstantsCaucao.MercadoPagoStatusCancelado + "\"", requisicao.Corpo);
            }
        }
    }
}
