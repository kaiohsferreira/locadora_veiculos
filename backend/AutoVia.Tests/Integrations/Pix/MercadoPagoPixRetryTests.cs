using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http;

namespace AutoVia.Tests.Integrations.Pix
{
    /// <summary>
    /// Retry do PIX de ponta a ponta: PixGateway -> MercadoPagoPixProvider -> ComunicacaoService REAL,
    /// com um HttpMessageHandler roteirizado no lugar da rede. Prova que a criacao repete com a mesma
    /// chave de idempotencia, que 4xx nao repete e que consulta (GET) e cancelamento (PUT) repetem em 5xx.
    /// </summary>
    public class MercadoPagoPixRetryTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string Referencia = "PED-2026-0001";
        private const string BaseUrl = "https://api.mercadopago.com";
        private const string CorpoCriacaoValido = "{\"id\":123456789,\"status\":\"pending\",\"status_detail\":\"pending_waiting_transfer\",\"transaction_amount\":10.5,\"external_reference\":\"PED-2026-0001\",\"date_of_expiration\":\"2026-11-05T11:00:00.000-03:00\",\"point_of_interaction\":{\"transaction_data\":{\"qr_code\":\"00020126COPIA-E-COLA\",\"qr_code_base64\":\"iVBORw0KGgo=\",\"ticket_url\":\"https://www.mercadopago.com.br/ticket\"}}}";
        private const string CorpoPendente = "{\"id\":42,\"status\":\"pending\",\"transaction_amount\":10.5,\"external_reference\":\"PED-2026-0001\"}";
        private const string CorpoPaga = "{\"id\":42,\"status\":\"approved\",\"transaction_amount\":10.5,\"date_approved\":\"2026-11-05T10:30:00.000-03:00\"}";
        private const string CorpoCancelada = "{\"id\":42,\"status\":\"cancelled\",\"transaction_amount\":10.5,\"external_reference\":\"PED-2026-0001\"}";

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
                TimeoutMs = 15000,
                Ativo = true
            };

            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveProvedor, Valor = ConstantsPix.ProvedorMercadoPago, Protegido = false });
            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveAccessToken, Valor = TokenSecreto, Protegido = true });

            return integration;
        }

        private static PixGateway CriarGateway(ComunicacaoService servico, FakeLogService log)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(CriarIntegracao());

            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);
            MercadoPagoPixProvider provider = new MercadoPagoPixProvider(servico, log);

            return new PixGateway(resolver, new IPixProvider[] { provider }, log);
        }

        private static PixCobrancaSolicitacao CriarSolicitacao()
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = 10.5m,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador { Nome = "Maria da Silva", Email = "maria@exemplo.com", Documento = "12345678901" }
            };
        }

        // ------------------------------------------------------------------
        // Criacao (POST): repete com a mesma chave de idempotencia; 4xx nunca repete.
        // ------------------------------------------------------------------

        [Fact]
        public async Task CriarCobranca_500_500_201_TresRequisicoesComAMesmaChaveDeIdempotencia()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.Created, CorpoCriacaoValido);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            PixGateway gateway = CriarGateway(servico, log);

            PixCobrancaResultado resultado = await gateway.CriarCobrancaAsync(CriarSolicitacao());

            Assert.Equal("123456789", resultado.IdentificadorProvedor);
            Assert.Equal(PixStatusCobranca.AGUARDANDO_PAGAMENTO, resultado.Status);
            Assert.Equal(3, handler.Requisicoes.Count);

            foreach (RequisicaoCapturada requisicao in handler.Requisicoes)
            {
                Assert.Equal(HttpMethod.Post.Method, requisicao.Metodo);
                Assert.Equal(Referencia, requisicao.Cabecalhos[ConstantsPix.CabecalhoIdempotenciaMercadoPago]);
                Assert.Equal(Referencia, requisicao.Cabecalhos[ConstantsIntegration.CabecalhoIdempotencia]);
                Assert.Equal("Bearer " + TokenSecreto, requisicao.Cabecalhos["Authorization"]);
                Assert.Contains("\"transaction_amount\":10.50", requisicao.Corpo);
            }
        }

        [Fact]
        public async Task CriarCobranca_400_NaoRepeteEDevolveBusinessException()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.BadRequest, "{\"message\":\"dados recusados\"}");
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            PixGateway gateway = CriarGateway(servico, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            string prefixo = ConstantsMessage.ErroPixCobrancaRecusada.Replace("{0}", string.Empty);
            Assert.StartsWith(prefixo, excecao.Message);
            Assert.Single(handler.Requisicoes);
        }

        [Fact]
        public async Task CriarCobranca_500_500_500_DevolveProvedorIndisponivelAposTresTentativas()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            PixGateway gateway = CriarGateway(servico, log);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroPixProvedorIndisponivel, excecao.Message);
            Assert.Equal(3, handler.Requisicoes.Count);
        }

        // ------------------------------------------------------------------
        // Consulta (GET) e cancelamento (PUT): repetem em 5xx.
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConsultarCobranca_500_500_200_RepeteOGet()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoPaga);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            PixGateway gateway = CriarGateway(servico, log);

            PixCobrancaResultado resultado = await gateway.ConsultarCobrancaAsync("42");

            Assert.Equal(PixStatusCobranca.PAGA, resultado.Status);
            Assert.Equal(3, handler.Requisicoes.Count);

            foreach (RequisicaoCapturada requisicao in handler.Requisicoes)
            {
                Assert.Equal(HttpMethod.Get.Method, requisicao.Metodo);
                Assert.Equal("Bearer " + TokenSecreto, requisicao.Cabecalhos["Authorization"]);
            }
        }

        [Fact]
        public async Task CancelarCobranca_PutRepeteEmCincoXx()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.OK, CorpoPendente);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK, CorpoCancelada);
            FakeLogService log = new FakeLogService();
            using ComunicacaoService servico = new ComunicacaoService(log, handler);
            PixGateway gateway = CriarGateway(servico, log);

            PixCobrancaResultado resultado = await gateway.CancelarCobrancaAsync("42");

            Assert.Equal(PixStatusCobranca.CANCELADA, resultado.Status);

            // 1 GET (consulta antes de cancelar) + 3 PUT (cancelamento com 5xx repetido).
            Assert.Equal(4, handler.Requisicoes.Count);
            Assert.Equal(HttpMethod.Get.Method, handler.Requisicoes[0].Metodo);

            List<RequisicaoCapturada> cancelamentos = handler.Requisicoes.Skip(1).ToList();
            Assert.Equal(3, cancelamentos.Count);

            foreach (RequisicaoCapturada requisicao in cancelamentos)
            {
                Assert.Equal(HttpMethod.Put.Method, requisicao.Metodo);
                Assert.Contains("\"status\":\"cancelled\"", requisicao.Corpo);
            }
        }
    }
}
