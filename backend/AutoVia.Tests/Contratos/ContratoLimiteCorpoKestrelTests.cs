using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Contratos
{
    /// <summary>Servidor do contrato com Kestrel REAL (o TestServer nao aplica o teto de corpo do [RequestSizeLimit]).</summary>
    public sealed class ContratoKestrelWebFactory : ContratoWebFactory
    {
        public ContratoKestrelWebFactory()
        {
            UseKestrel(0);
        }
    }

    /// <summary>
    /// Teto do corpo do POST de assinatura (ConstantsValues.ContratoAssinaturaLimiteCorpoBytes) com Kestrel real: corpo acima do teto
    /// responde 400 em problem+json, com errors.corpo, sem texto do framework e sem log novo (erro do cliente, como o teto das outras rotas).
    /// </summary>
    public sealed class ContratoLimiteCorpoKestrelTests
    {
        private const string RotaAssinatura = "/api/locacoes/10/contrato/assinatura";
        private const string CampoErros = "errors";
        private const string CampoCorpo = "corpo";
        private const string CampoDetail = "detail";
        private const string TextoDoFramework = "Request body";

        [Fact]
        public async Task PostAssinatura_CorpoAcimaDoTetoNoKestrel_Devolve400ComErrosDoCorpoESemLog()
        {
            using ContratoKestrelWebFactory fabrica = new ContratoKestrelWebFactory();
            fabrica.StartServer();
            using HttpClient cliente = fabrica.CreateClient();
            string token = ContratoLocacaoHttpTests.TokenDe(fabrica);
            int registrosAntes = fabrica.Log.Snapshot().Count;
            string corpo = "{\"nomeAssinante\":\"" + new string('A', ConstantsValues.ContratoAssinaturaLimiteCorpoBytes + 1) + "\"}";

            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaAssinatura)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json")
            };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            using HttpResponseMessage resposta = await cliente.SendAsync(requisicao);

            string textoResposta = await resposta.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(TextoDoFramework, textoResposta, StringComparison.OrdinalIgnoreCase);

            using JsonDocument documento = JsonDocument.Parse(textoResposta);
            JsonElement raiz = documento.RootElement;
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, raiz.GetProperty(CampoDetail).GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, CampoCorpo), raiz.GetProperty(CampoErros).GetProperty(CampoCorpo)[0].GetString());
            Assert.Equal(registrosAntes, fabrica.Log.Snapshot().Count);
            Assert.Empty(fabrica.Contratos.Contratos);
        }
    }
}
