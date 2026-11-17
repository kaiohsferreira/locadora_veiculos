using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Autorizacao das tres rotas de fotos do veiculo, em arquivo proprio (AutorizacaoRotasTests.cs nao muda): politica Funcionario
    /// na classe, nenhuma acao anonima, sem token = 401 e token de cliente = 403, nas tres rotas.
    /// </summary>
    public sealed class AutorizacaoRotasVeiculoFotoTests
    {
        private const string RotaFotos = "/api/veiculos/1/fotos";
        private const string RotaFoto = "/api/veiculos/1/fotos/2";
        private const string RotaLogin = "/api/auth/login";
        private const string PerfilCliente = "CLIENTE";
        private const string CpfCliente = "52998224725";
        private const string SenhaCliente = "SenhaCliente#2026";
        private const string CampoToken = "token";
        private const string TipoProblema = "application/problem+json";

        [Fact]
        public void VeiculoFotoController_TemPoliticaFuncionarioEAcoesSemAnonimo()
        {
            AuthorizeAttribute? autorizacao = typeof(VeiculoFotoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao.Policy);

            foreach (MethodInfo acao in typeof(VeiculoFotoController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.False(Attribute.IsDefined(acao, typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)));
            }
        }

        [Fact]
        public async Task SemToken_Da401NasTresRotasDeFotos()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage envio = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
            using HttpResponseMessage lista = await cliente.GetAsync(RotaFotos);
            using HttpResponseMessage remocao = await cliente.DeleteAsync(RotaFoto);

            Assert.Equal(HttpStatusCode.Unauthorized, envio.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, lista.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, remocao.StatusCode);
            Assert.Equal(TipoProblema, envio.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task TokenDeCliente_Da403NasTresRotasDeFotos()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarClienteAsync(cliente);

            using HttpRequestMessage envio = new HttpRequestMessage(HttpMethod.Post, RotaFotos) { Content = new ByteArrayContent(Array.Empty<byte>()) };
            envio.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            using HttpRequestMessage lista = new HttpRequestMessage(HttpMethod.Get, RotaFotos);
            lista.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            using HttpRequestMessage remocao = new HttpRequestMessage(HttpMethod.Delete, RotaFoto);
            remocao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);

            using HttpResponseMessage respostaEnvio = await cliente.SendAsync(envio);
            using HttpResponseMessage respostaLista = await cliente.SendAsync(lista);
            using HttpResponseMessage respostaRemocao = await cliente.SendAsync(remocao);

            Assert.Equal(HttpStatusCode.Forbidden, respostaEnvio.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, respostaLista.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, respostaRemocao.StatusCode);
        }

        /// <summary>Login real do cliente de teste: devolve o token do cabecalho Authorization.</summary>
        private static async Task<string> LogarClienteAsync(HttpClient cliente)
        {
            string corpo = JsonSerializer.Serialize(new { perfil = PerfilCliente, cpf = CpfCliente, senha = SenhaCliente });
            using StringContent conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");
            using HttpResponseMessage login = await cliente.PostAsync(RotaLogin, conteudo);
            string textoLogin = await login.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            using JsonDocument documento = JsonDocument.Parse(textoLogin);

            return documento.RootElement.GetProperty(CampoToken).GetString()!;
        }
    }
}
