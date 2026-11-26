using AutoVia.Api.Controllers;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP do cadastro publico de locadora (P10-BE-01) com o servidor em memoria. Cobre: 404 com a chave
    /// desligada antes de validar o corpo; 201 sem Location e sem token; login do responsavel recem-cadastrado
    /// devolvendo a empresa nova; 400 do validador; 409 na ordem CNPJ, e-mail, CPF; 429 do limite por hora; e a
    /// verificacao publica (existe, nao existe, normalizacao, malformado e no-store). Nenhum PostgreSQL e acessado:
    /// o repositorio de empresas usa o mesmo armazenamento em memoria do login.
    /// </summary>
    public class EmpresaHttpTests : IDisposable
    {
        private const string RotaEmpresas = "/api/empresas";
        private const string RotaCidades = RotaEmpresas + "/cidades";
        private const string RotaVerificacao = "/api/empresas/verificar-cadastro";
        private const string RotaLogin = "/api/auth/login";
        private const string MediaProblema = "application/problem+json";
        private const string CnpjCadastrado = "11.222.333/0001-81";
        private const string CnpjNovo = "11.444.777/0001-61";
        private const string EmailEmpresa = "contato@rotasul.exemplo.com";
        private const string EmailNovo = "contato@outralocadora.exemplo.com";
        private const string CpfResponsavel = "390.533.447-05";
        private const string CpfResponsavelDigitos = "39053344705";
        private const string CpfJaSemeadoNoLogin = "111.444.777-35";
        private const string SenhaValida = "SenhaForte1";

        private static readonly JsonSerializerOptions OpcoesJson = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly EmpresaWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public EmpresaHttpTests()
        {
            _fabrica = new EmpresaWebFactory(habilitado: true);
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        [Fact]
        public async Task Cidades_SemTokenECadastroDesligado_ListaParesDistintosSemDadosPrivados()
        {
            using EmpresaWebFactory fabrica = new EmpresaWebFactory(habilitado: false);
            using HttpClient cliente = fabrica.CreateClient();
            FakeEmpresaRepository empresas = fabrica.RepositorioEmpresas;
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Recife", Uf = "PE" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "  Curitiba  ", Uf = "pr" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Curitiba", Uf = "PR" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Curitiba", Uf = "SC" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Sem local", Uf = "XX" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "  ", Uf = "PR" });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Londrina", Uf = "PR", Ativo = false });
            empresas.AdicionarEmpresa(new Empresa { Cidade = "Maringá", Uf = "PR", DisabledAt = new DateTime(2026, 12, 10) });

            using HttpResponseMessage resposta = await cliente.GetAsync(RotaCidades);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(3, corpo.GetArrayLength());
            Assert.Collection(corpo.EnumerateArray(),
                item => ConferirCidade(item, "Curitiba", "PR"),
                item => ConferirCidade(item, "Curitiba", "SC"),
                item => ConferirCidade(item, "Recife", "PE"));
        }

        [Fact]
        public async Task Cidades_SemLocadoras_DevolveListaVazia()
        {
            using HttpResponseMessage resposta = await _cliente.GetAsync(RotaCidades);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(0, corpo.GetArrayLength());
        }

        private static void ConferirCidade(JsonElement item, string cidade, string uf)
        {
            Assert.Equal(2, item.EnumerateObject().Count());
            Assert.Equal(cidade, item.GetProperty("cidade").GetString());
            Assert.Equal(uf, item.GetProperty("uf").GetString());
        }

        [Fact]
        public async Task ChaveDesligada_PostEVerificacao_Devolvem404AntesDeValidarOCorpo()
        {
            using EmpresaWebFactory desligada = new EmpresaWebFactory(habilitado: false);
            using HttpClient cliente = desligada.CreateClient();

            HttpResponseMessage cadastro = await cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, "{}"));
            HttpResponseMessage verificacao = await cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{}"));

            Assert.Equal(HttpStatusCode.NotFound, cadastro.StatusCode);
            Assert.Equal(MediaProblema, cadastro.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(cadastro);
            Assert.Equal(ConstantsMessage.ErroLocadoraCadastroIndisponivel, corpo.GetProperty("detail").GetString());
            Assert.Equal(HttpStatusCode.NotFound, verificacao.StatusCode);
            Assert.Equal(0, desligada.RepositorioEmpresas.Insercoes);
        }

        [Fact]
        public async Task Post_Valido_Devolve201SemLocationESemToken()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido())));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Null(resposta.Headers.Location);
            string texto = await resposta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(SenhaValida, texto);
            Assert.DoesNotContain("token", texto, StringComparison.OrdinalIgnoreCase);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("idEmpresa").GetInt32() > 0);
            Assert.Equal("ana@rotasul.exemplo.com", corpo.GetProperty("responsavelEmail").GetString());
            Assert.Equal(CnpjCadastrado, corpo.GetProperty("cnpjStr").GetString());
            Assert.Equal("MAT-0001", corpo.GetProperty("matriculaResponsavel").GetString());
            Assert.True(corpo.GetProperty("ativo").GetBoolean());
        }

        /// <summary>
        /// O teto do corpo e conferido por reflexao (o RequestSizeLimit fica no metodo Insert). Um teste HTTP de corpo acima
        /// do teto nao e possivel aqui: o TestServer nao aplica o limite (o Kestrel aplica), e o endpoint de verificacao
        /// tambem so mostra 400 pelo validador, nunca pelo limite.
        /// </summary>
        [Fact]
        public void Insert_TemTetoDeCorpoDoCadastroDeLocadora()
        {
            MethodInfo? metodo = typeof(EmpresaController).GetMethod(nameof(EmpresaController.Insert));

            Assert.NotNull(metodo);
            RequestSizeLimitAttribute? tamanho = metodo.GetCustomAttribute<RequestSizeLimitAttribute>();
            Assert.NotNull(tamanho);
            IRequestSizeLimitMetadata metadadoTamanho = tamanho;
            Assert.Equal<long?>(ConstantsValues.TamanhoMaximoCorpoCadastroLocadora, metadadoTamanho.MaxRequestBodySize);
        }

        [Fact]
        public async Task Post_Valido_LoginDoResponsavelDevolveAEmpresaNova()
        {
            HttpResponseMessage cadastro = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido(cpf: CpfResponsavel))));
            JsonElement criada = await LerJsonAsync(cadastro);
            int idCriado = criada.GetProperty("idEmpresa").GetInt32();

            string corpoLogin = "{\"perfil\":\"FUNCIONARIO\",\"cpf\":\"" + CpfResponsavelDigitos + "\",\"senha\":\"" + SenhaValida + "\",\"lembrar\":false}";
            HttpResponseMessage login = await _cliente.SendAsync(Json(HttpMethod.Post, RotaLogin, corpoLogin));

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement sessao = await LerJsonAsync(login);
            Assert.Equal(idCriado, sessao.GetProperty("idEmpresa").GetInt32());
            Assert.Equal("ADMINISTRADOR", sessao.GetProperty("cargo").GetString());
        }

        [Fact]
        public async Task Post_CorpoInvalido_Devolve400ComMensagemDoValidador()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.RazaoSocial = string.Empty;

            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(vo)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroLocadoraRazaoSocialObrigatoria, corpo.GetProperty("detail").GetString());
            Assert.Equal(0, _fabrica.RepositorioEmpresas.Insercoes);
        }

        [Fact]
        public async Task Post_CnpjJaCadastrado_Devolve409DeCnpj()
        {
            await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido())));

            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido(email: EmailNovo, cpf: "111.444.777-35"))));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroLocadoraCnpjDuplicado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Post_EmailJaCadastrado_Devolve409DeEmail()
        {
            await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido())));

            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido(cnpj: CnpjNovo, cpf: CpfResponsavel))));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroLocadoraEmailDuplicado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Post_CpfDoResponsavelJaCadastrado_Devolve409DeCpf()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido(cnpj: CnpjNovo, email: EmailNovo, cpf: CpfJaSemeadoNoLogin))));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroLocadoraResponsavelCpfDuplicado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Post_AcimaDoLimiteDeCadastrosPorHora_Devolve429()
        {
            using EmpresaWebFactory limitada = new EmpresaWebFactory(habilitado: true, limiteCadastroPorHora: 2);
            using HttpClient cliente = limitada.CreateClient();

            HttpResponseMessage primeira = await cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, "{}"));
            HttpResponseMessage segunda = await cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, "{}"));
            HttpResponseMessage terceira = await cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, "{}"));

            Assert.Equal(HttpStatusCode.BadRequest, primeira.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
            Assert.Equal(MediaProblema, terceira.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(terceira);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Verificacao_CnpjCadastradoComMascara_DevolveExisteEMarcaNoStore()
        {
            await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido())));

            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{\"campo\":\"CNPJ\",\"valor\":\"" + CnpjCadastrado + "\"}"));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.True(resposta.Headers.CacheControl?.NoStore);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("existe").GetBoolean());
            Assert.Single(corpo.EnumerateObject());
        }

        [Fact]
        public async Task Verificacao_CnpjNaoCadastrado_DevolveNaoExiste()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{\"campo\":\"CNPJ\",\"valor\":\"" + CnpjNovo + "\"}"));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.False(corpo.GetProperty("existe").GetBoolean());
        }

        [Fact]
        public async Task Verificacao_EmailEmMaiusculasEComBordas_DevolveExiste()
        {
            await _cliente.SendAsync(Json(HttpMethod.Post, RotaEmpresas, SerializarVO(CriarVOValido())));

            HttpResponseMessage resposta = await _cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{\"campo\":\"EMAIL\",\"valor\":\"  CONTATO@ROTASUL.exemplo.com \"}"));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("existe").GetBoolean());
        }

        [Fact]
        public async Task Verificacao_ValorMalformadoOuCampoAusente_Devolve400()
        {
            HttpResponseMessage cnpjCurto = await _cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{\"campo\":\"CNPJ\",\"valor\":\"123\"}"));
            HttpResponseMessage semCampo = await _cliente.SendAsync(Json(HttpMethod.Post, RotaVerificacao, "{\"valor\":\"11222333000181\"}"));

            Assert.Equal(HttpStatusCode.BadRequest, cnpjCurto.StatusCode);
            Assert.Equal(MediaProblema, cnpjCurto.Content.Headers.ContentType?.MediaType);
            Assert.Equal(HttpStatusCode.BadRequest, semCampo.StatusCode);
        }

        /// <summary>Requisicao com corpo JSON bruto (usada para os corpos invalidos e para as verificacoes).</summary>
        private static HttpRequestMessage Json(HttpMethod metodo, string rota, string json)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return requisicao;
        }

        private static string SerializarVO(CadastroLocadoraVO vo)
        {
            string json = JsonSerializer.Serialize(vo, OpcoesJson);

            return json;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        /// <summary>VO valido do cadastro. Os parametros trocam so o campo que o teste precisa diferente.</summary>
        private static CadastroLocadoraVO CriarVOValido(string cnpj = CnpjCadastrado, string email = EmailEmpresa, string cpf = CpfResponsavel)
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO
            {
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = cnpj,
                Telefone = "(11) 3456-7890",
                Email = email,
                Cep = "01234-567",
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = null,
                Bairro = "Centro",
                Cidade = "São Paulo",
                Uf = "SP",
                ResponsavelNome = "Ana Responsável",
                ResponsavelCpf = cpf,
                ResponsavelEmail = "ana@rotasul.exemplo.com",
                Senha = SenhaValida,
                AceiteTermos = true
            };

            return vo;
        }
    }

    /// <summary>
    /// Servidor de teste do cadastro de locadora: herda a fabrica de autenticacao (funcionarios semeados, login real e
    /// limites altos) e troca o IEmpresaRepository pelo fake que compartilha o armazenamento do login. Liga ou desliga
    /// a chave Locadoras:CadastroPublicoHabilitado e fixa o limite por hora do cadastro.
    /// </summary>
    public sealed class EmpresaWebFactory : AutenticacaoWebFactory
    {
        private readonly bool _habilitado;
        private readonly int _limiteCadastroPorHora;

        public EmpresaWebFactory(bool habilitado, int limiteCadastroPorHora = ConstantsValues.LimiteCadastroEmpresaPorHoraPadrao)
            : base(1000, null)
        {
            _habilitado = habilitado;
            _limiteCadastroPorHora = limiteCadastroPorHora;
            RepositorioEmpresas = new FakeEmpresaRepository(Repositorio);
        }

        /// <summary>Repositorio de empresas em memoria, sobre o mesmo armazenamento do login.</summary>
        public FakeEmpresaRepository RepositorioEmpresas { get; }

        protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
            valores[ConstantsValues.ChaveCadastroPublicoHabilitado] = _habilitado ? "true" : "false";
            valores[ConstantsValues.ChaveLimiteCadastroPorHora] = _limiteCadastroPorHora.ToString(CultureInfo.InvariantCulture);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmpresaRepository>();
                services.AddSingleton<IEmpresaRepository>(RepositorioEmpresas);
            });
        }
    }
}
