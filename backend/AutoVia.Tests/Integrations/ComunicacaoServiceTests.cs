using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Services;
using AutoVia.Tests.Fakes;
using System.Net;

namespace AutoVia.Tests.Integrations
{
    /// <summary>
    /// Testa o ComunicacaoService REAL (RestSharp) contra um HttpMessageHandler falso.
    /// Nenhuma chamada sai da maquina: as respostas vem do roteiro de cada teste.
    /// </summary>
    public class ComunicacaoServiceTests
    {
        private const string BaseUrl = "https://api.exemplo.com.br";

        /// <summary>Handler com roteiro: cada chamada consome o proximo passo.</summary>
        private sealed class HandlerRoteirizado : HttpMessageHandler
        {
            private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _roteiro = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();

            public List<HttpRequestMessage> Requisicoes { get; } = new List<HttpRequestMessage>();

            public void Responder(HttpStatusCode status)
            {
                // RequestMessage precisa ser preenchido: o RestSharp le RequestMessage.RequestUri
                // ao montar a resposta; sem ele a chamada vira falha de rede.
                _roteiro.Enqueue((HttpRequestMessage requisicao, CancellationToken cancellationToken) =>
                    Task.FromResult(new HttpResponseMessage(status)
                    {
                        Content = new StringContent("{}"),
                        RequestMessage = requisicao
                    }));
            }

            public void FalharRede()
            {
                _roteiro.Enqueue((HttpRequestMessage requisicao, CancellationToken cancellationToken) =>
                    Task.FromException<HttpResponseMessage>(new HttpRequestException("falha simulada de rede")));
            }

            public void Expirar()
            {
                _roteiro.Enqueue(async (HttpRequestMessage requisicao, CancellationToken cancellationToken) =>
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requisicoes.Add(request);
                Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> passo = _roteiro.Dequeue();
                return passo(request, cancellationToken);
            }
        }

        private static ComunicacaoService CriarServico(FakeLogService logService, HttpMessageHandler handler)
        {
            return new ComunicacaoService(logService, handler);
        }

        private static RequestInfo CriarRequisicao(HttpMethod metodo, string recurso = "/v1/teste", bool allowRetry = true, string? idempotencyKey = null, int timeoutMs = 5000)
        {
            return new RequestInfo
            {
                BaseUrl = BaseUrl,
                Resource = recurso,
                Method = metodo,
                AllowRetry = allowRetry,
                IdempotencyKey = idempotencyKey,
                TimeoutMs = timeoutMs
            };
        }

        [Fact]
        public async Task Get_500_500_200_FazTresTentativasEDevolveSucesso()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.OK);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Get));

            Assert.True(resposta.IsSuccess);
            Assert.Equal(200, resposta.StatusCode);
            Assert.Equal(3, resposta.Attempts);
            Assert.Equal(3, handler.Requisicoes.Count);
        }

        [Fact]
        public async Task Get_400_NaoRepete()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.BadRequest);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Get));

            Assert.False(resposta.IsSuccess);
            Assert.Equal(400, resposta.StatusCode);
            Assert.Equal(1, resposta.Attempts);
            Assert.Single(handler.Requisicoes);
        }

        [Fact]
        public async Task Post_500SemChaveDeIdempotencia_NaoRepete()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Post));

            Assert.Equal(1, resposta.Attempts);
            Assert.Single(handler.Requisicoes);
        }

        [Fact]
        public async Task Post_500_500_201ComChave_FazTresTentativas()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.Created);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Post, idempotencyKey: "PED-2026-0001"));

            Assert.True(resposta.IsSuccess);
            Assert.Equal(201, resposta.StatusCode);
            Assert.Equal(3, resposta.Attempts);
        }

        [Fact]
        public async Task Get_500Tres_VezesFalhaComAttemptsTres()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Get));

            Assert.False(resposta.IsSuccess);
            Assert.Equal(500, resposta.StatusCode);
            Assert.Equal(3, resposta.Attempts);
            Assert.Equal(ConstantsMessage.ErroIntegracaoExterna, resposta.ErrorMessage);
        }

        [Fact]
        public async Task Get_FalhaDeRede_TentaDeNovoEDevolveSucesso()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.FalharRede();
            handler.FalharRede();
            handler.Responder(HttpStatusCode.OK);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Get));

            Assert.True(resposta.IsSuccess);
            Assert.Equal(3, resposta.Attempts);
        }

        [Fact]
        public async Task Get_Timeout_DevolveIsTimeoutSemNovaTentativa()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Expirar();
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            RequestResponse<string> resposta = await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Get, allowRetry: false, timeoutMs: 200));

            Assert.True(resposta.IsTimeout);
            Assert.True(resposta.IsNetworkFailure);
            Assert.False(resposta.IsSuccess);
            Assert.Equal(ConstantsMessageIntegration.ErroComunicacaoTimeout, resposta.ErrorMessage);
            Assert.Equal(1, resposta.Attempts);
        }

        [Fact]
        public async Task Post_ComChave_EnviaCabecalhoDeIdempotencia()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.Created);
            using ComunicacaoService servico = CriarServico(new FakeLogService(), handler);

            await servico.SendAsync<string>(CriarRequisicao(HttpMethod.Post, idempotencyKey: "PED-2026-0001"));

            HttpRequestMessage enviada = Assert.Single(handler.Requisicoes);
            List<string> valores = LerCabecalho(enviada, ConstantsIntegration.CabecalhoIdempotencia);

            Assert.Contains("PED-2026-0001", valores);
        }

        /// <summary>Le um cabecalho da mensagem ou do conteudo (o RestSharp pode colocar em qualquer um dos dois).</summary>
        private static List<string> LerCabecalho(HttpRequestMessage mensagem, string nome)
        {
            List<string> valores = new List<string>();

            if (mensagem.Headers.TryGetValues(nome, out IEnumerable<string>? doRequest))
            {
                valores.AddRange(doRequest);
            }

            if (mensagem.Content != null && mensagem.Content.Headers.TryGetValues(nome, out IEnumerable<string>? doConteudo))
            {
                valores.AddRange(doConteudo);
            }

            return valores;
        }

        [Fact]
        public async Task Get_ValorSensivelNaUrl_NaoApareceNoLog()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            handler.Responder(HttpStatusCode.InternalServerError);
            FakeLogService logService = new FakeLogService();
            using ComunicacaoService servico = CriarServico(logService, handler);
            RequestInfo requisicao = CriarRequisicao(HttpMethod.Get, recurso: "/v1/segredo-ABC-123");
            requisicao.SensitiveValues = new List<string> { "segredo-ABC-123" };

            RequestResponse<string> resposta = await servico.SendAsync<string>(requisicao);

            Assert.False(resposta.IsSuccess);
            Assert.NotEmpty(logService.Entries);
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains("segredo-ABC-123"));
        }

        [Fact]
        public async Task Get_FalhaDeRedeComSegredoNaMensagem_NaoApareceNoLog()
        {
            HandlerRoteirizado handler = new HandlerRoteirizado();
            handler.FalharRede();
            handler.FalharRede();
            handler.FalharRede();
            FakeLogService logService = new FakeLogService();
            using ComunicacaoService servico = CriarServico(logService, handler);
            RequestInfo requisicao = CriarRequisicao(HttpMethod.Get, recurso: "/v1/segredo-ABC-123");
            requisicao.SensitiveValues = new List<string> { "segredo-ABC-123" };

            RequestResponse<string> resposta = await servico.SendAsync<string>(requisicao);

            Assert.Equal(3, resposta.Attempts);
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains("segredo-ABC-123"));
        }
    }
}
