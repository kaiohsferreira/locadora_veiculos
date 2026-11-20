using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// POST /api/categorias por HTTP (item 1 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026): nome de
    /// categoria ativa repetido responde 409 e valor diario base zero responde 400, ambos em ProblemDetails, com status,
    /// title e detail conferidos. Servidor em memoria (ContextoEmpresaWebFactory, com a categoria trocada por fake); nada
    /// chega ao PostgreSQL.
    /// </summary>
    public class CategoriaHttpTests
    {
        private const string RotaCategorias = "/api/categorias";
        private const string MediaProblema = "application/problem+json";
        private const string PrefixoTipo = "https://httpstatuses.io/";
        private const string NomeJaCadastrado = "SUV";
        private const string NomeComOutraCaixaEEspacos = "  suv ";
        private const string NomeNovo = "Sedan";
        private const string DescricaoDeTeste = "Categoria criada no teste";
        private const string CampoValorDiaria = "valorDiariaBase";
        private const decimal ValorDiariaBaseZero = 0m;
        private const decimal ValorDiariaBaseValido = 150m;
        private const decimal ValorDiariaBaseDaCategoriaSemeada = 200m;

        /// <summary>Nome igual ao de uma categoria ativa (com outra caixa e espacos nas pontas) responde 409 e nao grava.</summary>
        [Fact]
        public async Task Post_NomeDeCategoriaAtivaComOutraCaixaEEspacos_Devolve409ComTitleEDetail()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            fabrica.Categorias.Adicionar(new Categoria { Nome = NomeJaCadastrado, ValorDiariaBase = ValorDiariaBaseDaCategoriaSemeada });
            string token = TokenDeAdministradorDaOperadora(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(token, NomeComOutraCaixaEEspacos, ValorDiariaBaseValido));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "409", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloConflito, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroCategoriaNomeDuplicado, corpo.GetProperty("detail").GetString());
            Assert.Equal(RotaCategorias, corpo.GetProperty("instance").GetString());
            Assert.Single(fabrica.Categorias.Armazenados);
        }

        /// <summary>Valor diario base zero responde 400 com title, detail e a mensagem no campo; nada e gravado.</summary>
        [Fact]
        public async Task Post_ValorDiariaBaseZero_Devolve400ComTitleDetailEErrorsDoCampo()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeAdministradorDaOperadora(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(token, NomeNovo, ValorDiariaBaseZero));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "400", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloErroValidacao, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroCategoriaValorDiaria, corpo.GetProperty("detail").GetString());
            Assert.Equal(RotaCategorias, corpo.GetProperty("instance").GetString());
            Assert.Equal(ConstantsMessage.ErroCategoriaValorDiaria, corpo.GetProperty("errors").GetProperty(CampoValorDiaria)[0].GetString());
            Assert.Empty(fabrica.Categorias.Armazenados);
        }

        /// <summary>Token do ADMINISTRADOR da empresa operadora (o unico cargo que cadastra categoria), assinado pelo servidor de teste.</summary>
        private static string TokenDeAdministradorDaOperadora(ContextoEmpresaWebFactory fabrica)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = AutenticacaoWebFactory.Emissor,
                Audiencia = AutenticacaoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            Funcionario administrador = fabrica.AdminOperadora;
            string versaoSessao = tokens.CalcularVersaoSessao(administrador.SenhaHash!);

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, administrador.IdFuncionario, administrador.Nome, CargoFuncionario.ADMINISTRADOR, versaoSessao, false, fabrica.EmpresaOperadora.IdEmpresa).Token;
        }

        private static HttpRequestMessage RequisicaoComCorpo(string token, string nome, decimal valorDiariaBase)
        {
            object corpo = new
            {
                nome = nome,
                descricao = DescricaoDeTeste,
                valorDiariaBase = valorDiariaBase
            };

            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaCategorias);
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

            return requisicao;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }
}
