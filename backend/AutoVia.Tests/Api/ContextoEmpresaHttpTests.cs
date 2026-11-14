using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP do contexto de empresa (multi-locadora, P10-BE-04) no pipeline real: a empresa, o cargo e a
    /// marca de operadora vem do banco (fake) pela validacao do token; as politicas Administrador e Operadora
    /// respondem 200/403 corretamente; categoria exige a operadora (POST e PUT); cliente e escopado pela locadora
    /// (GET lista e GET por id). Cada teste cria a sua factory, sem estado compartilhado.
    /// </summary>
    public class ContextoEmpresaHttpTests
    {
        private const string RotaContexto = "/api/teste-contexto/contexto";
        private const string RotaAdministrador = "/api/teste-contexto/administrador";
        private const string RotaOperadora = "/api/teste-contexto/operadora";
        private const string RotaCategorias = "/api/categorias";
        private const string RotaClientes = "/api/clientes";
        private const string NomeCategoriaTeste = "Categoria de teste";
        private const string CnhTeste = "40000000009";
        private const string NomeRaiz = "Cliente";

        [Fact]
        public async Task Contexto_FuncionarioValido_RecebeEmpresaCargoEOperadoraDoBanco()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminOperadora, CargoFuncionario.ATENDENTE, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(fabrica.EmpresaOperadora.IdEmpresa, corpo.GetProperty("idEmpresa").GetInt32());
            Assert.Equal(fabrica.AdminOperadora.IdFuncionario, corpo.GetProperty("idFuncionario").GetInt32());
            Assert.Equal(CargoFuncionario.ADMINISTRADOR.ToString(), corpo.GetProperty("cargo").GetString());
            Assert.True(corpo.GetProperty("empresaOperadora").GetBoolean());
            Assert.Equal(0, corpo.GetProperty("idCliente").GetInt32());
        }

        [Fact]
        public async Task Contexto_FuncionarioDeLocadoraComum_RecebeASuaEmpresaSemOperadora()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminComum, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaComum.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(fabrica.EmpresaComum.IdEmpresa, corpo.GetProperty("idEmpresa").GetInt32());
            Assert.False(corpo.GetProperty("empresaOperadora").GetBoolean());
            Assert.Equal(0, corpo.GetProperty("idCliente").GetInt32());
        }

        [Fact]
        public async Task Contexto_ClienteValido_PreencheIdClienteEDeixaEmpresaZerada()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(fabrica.Repositorio.Clientes[0].IdCliente, corpo.GetProperty("idCliente").GetInt32());
            Assert.Equal(0, corpo.GetProperty("idEmpresa").GetInt32());
            Assert.Equal(0, corpo.GetProperty("idFuncionario").GetInt32());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("cargo").ValueKind);
        }

        [Fact]
        public async Task Contexto_ClienteComClaimEmpForjada_FicaVazio()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeClienteComEmpresaForjada(fabrica, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(0, corpo.GetProperty("idEmpresa").GetInt32());
            Assert.Equal(0, corpo.GetProperty("idFuncionario").GetInt32());
            Assert.Equal(fabrica.Repositorio.Clientes[0].IdCliente, corpo.GetProperty("idCliente").GetInt32());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("cargo").ValueKind);
            Assert.False(corpo.GetProperty("empresaOperadora").GetBoolean());
        }

        [Fact]
        public async Task Politicas_SemToken_Devolvem401NasDuasRotas()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage administrador = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAdministrador, null));
            HttpResponseMessage operadora = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaOperadora, null));

            Assert.Equal(HttpStatusCode.Unauthorized, administrador.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, operadora.StatusCode);
        }

        [Fact]
        public async Task Politicas_AdministradorDaOperadora_PassamNasDuasRotas()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminOperadora, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage administrador = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAdministrador, token));
            HttpResponseMessage operadora = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaOperadora, token));

            Assert.Equal(HttpStatusCode.OK, administrador.StatusCode);
            Assert.Equal(HttpStatusCode.OK, operadora.StatusCode);
        }

        [Fact]
        public async Task Politicas_AdministradorDeLocadoraComum_PassaSoNaDeAdministrador()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminComum, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaComum.IdEmpresa);

            HttpResponseMessage administrador = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAdministrador, token));
            HttpResponseMessage operadora = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaOperadora, token));

            Assert.Equal(HttpStatusCode.OK, administrador.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, operadora.StatusCode);
        }

        [Fact]
        public async Task Politicas_AtendenteDaOperadora_RecusadoNasDuasRotas()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AtendenteOperadora, CargoFuncionario.ATENDENTE, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage administrador = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAdministrador, token));
            HttpResponseMessage operadora = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaOperadora, token));

            Assert.Equal(HttpStatusCode.Forbidden, administrador.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, operadora.StatusCode);
        }

        [Fact]
        public async Task Politicas_ClienteRecusadoNasDuasRotas()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage administrador = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAdministrador, token));
            HttpResponseMessage operadora = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaOperadora, token));

            Assert.Equal(HttpStatusCode.Forbidden, administrador.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, operadora.StatusCode);
        }

        [Fact]
        public async Task Categoria_GetEAnonimo_Devolve200SemToken()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaCategorias, null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task Categoria_PostSemToken_Devolve401()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaCategorias, null, CorpoCategoria()));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Empty(fabrica.Categorias.Armazenados);
        }

        [Fact]
        public async Task Categoria_PostDoAtendenteDaOperadora_Devolve403()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AtendenteOperadora, CargoFuncionario.ATENDENTE, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaCategorias, token, CorpoCategoria()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Empty(fabrica.Categorias.Armazenados);
        }

        [Fact]
        public async Task Categoria_PostDoAdministradorDeLocadoraComum_Devolve403()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminComum, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaComum.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaCategorias, token, CorpoCategoria()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Empty(fabrica.Categorias.Armazenados);
        }

        [Fact]
        public async Task Categoria_PostDoAdministradorDaOperadora_Devolve201ComLocation()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminOperadora, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaCategorias, token, CorpoCategoria()));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.NotNull(resposta.Headers.Location);
            Assert.Single(fabrica.Categorias.Armazenados);
        }

        [Fact]
        public async Task Categoria_PutDoAtendenteDaOperadora_Devolve403()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AtendenteOperadora, CargoFuncionario.ATENDENTE, fabrica.EmpresaOperadora.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaCategorias + "/1", token, CorpoCategoria()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Clientes_FuncionarioDaLocadoraComum_VeSoOsClientesComVinculoNaEmpresaDele()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            Cliente daOperadora = CriarClienteParaEscopo(fabrica, "Operadora", "11122233344", "cliente.operadora@exemplo.com", CnhTeste);
            Cliente daComum = CriarClienteParaEscopo(fabrica, "Comum", "55566677788", "cliente.comum@exemplo.com", "40000000010");
            fabrica.RepositorioClientes.VincularAEmpresa(daOperadora.IdCliente, fabrica.EmpresaOperadora.IdEmpresa);
            fabrica.RepositorioClientes.VincularAEmpresa(daComum.IdCliente, fabrica.EmpresaComum.IdEmpresa);
            string token = TokenDeFuncionario(fabrica, fabrica.AdminComum, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaComum.IdEmpresa);

            HttpResponseMessage lista = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaClientes + "?pagina=1&tamanhoPagina=20", token));
            HttpResponseMessage deOutra = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaClientes + "/" + daOperadora.IdCliente.ToString(CultureInfo.InvariantCulture), token));
            HttpResponseMessage propria = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaClientes + "/" + daComum.IdCliente.ToString(CultureInfo.InvariantCulture), token));

            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            JsonElement corpo = await LerJsonAsync(lista);
            Assert.Equal(1, corpo.GetProperty("total").GetInt32());
            JsonElement itens = corpo.GetProperty("itens");
            Assert.Equal(1, itens.GetArrayLength());
            Assert.Equal(daComum.IdCliente, itens[0].GetProperty("idCliente").GetInt32());
            Assert.Equal(HttpStatusCode.NotFound, deOutra.StatusCode);
            Assert.Equal(HttpStatusCode.OK, propria.StatusCode);
        }

        [Fact]
        public async Task Clientes_TotalEPaginaSaoDoEscopoDaEmpresa()
        {
            using ContextoEmpresaWebFactory fabrica = new ContextoEmpresaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            Cliente primeiro = CriarClienteParaEscopo(fabrica, "Primeiro", "11111111111", "primeiro@exemplo.com", CnhTeste);
            Cliente segundo = CriarClienteParaEscopo(fabrica, "Segundo", "22222222222", "segundo@exemplo.com", "40000000011");
            Cliente deOutra = CriarClienteParaEscopo(fabrica, "De outra", "33333333333", "outra@exemplo.com", "40000000012");
            fabrica.RepositorioClientes.VincularAEmpresa(primeiro.IdCliente, fabrica.EmpresaComum.IdEmpresa);
            fabrica.RepositorioClientes.VincularAEmpresa(segundo.IdCliente, fabrica.EmpresaComum.IdEmpresa);
            fabrica.RepositorioClientes.VincularAEmpresa(deOutra.IdCliente, fabrica.EmpresaOperadora.IdEmpresa);
            string token = TokenDeFuncionario(fabrica, fabrica.AdminComum, CargoFuncionario.ADMINISTRADOR, fabrica.EmpresaComum.IdEmpresa);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaClientes + "?pagina=1&tamanhoPagina=1", token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(2, corpo.GetProperty("total").GetInt32());
            Assert.Equal(1, corpo.GetProperty("itens").GetArrayLength());
        }

        /// <summary>Cria um cliente de teste na factory, ja gravado (id gerado pelo fake).</summary>
        private static Cliente CriarClienteParaEscopo(ContextoEmpresaWebFactory fabrica, string sufixoNome, string cpf, string email, string cnh)
        {
            Cliente cliente = new Cliente
            {
                Nome = NomeRaiz + " " + sufixoNome,
                Cpf = cpf,
                Email = email,
                CnhNumero = cnh,
                StatusCadastro = StatusCadastroCliente.APROVADO
            };

            fabrica.RepositorioClientes.Adicionar(cliente);

            return cliente;
        }

        /// <summary>Token de funcionario assinado pelo servidor de teste. O cargo do token e informativo (o servidor usa o do banco).</summary>
        private static string TokenDeFuncionario(ContextoEmpresaWebFactory fabrica, Funcionario funcionario, CargoFuncionario cargoNoToken, int idEmpresaNoToken)
        {
            TokenService tokens = CriarTokenService(fabrica);
            string versaoSessao = tokens.CalcularVersaoSessao(funcionario.SenhaHash!);

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome, cargoNoToken, versaoSessao, false, idEmpresaNoToken).Token;
        }

        /// <summary>Token de cliente valido, sem claim de empresa.</summary>
        private static string TokenDeCliente(ContextoEmpresaWebFactory fabrica)
        {
            TokenService tokens = CriarTokenService(fabrica);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string versaoSessao = tokens.CalcularVersaoSessao(fabrica.HashSenhaCliente);

            return tokens.GerarToken(PerfilUsuario.CLIENTE, idCliente, NomeRaiz, null, versaoSessao, false, null).Token;
        }

        /// <summary>Token de cliente assinado pelo servidor de teste, com uma claim emp forjada (o servidor deve ignorar).</summary>
        private static string TokenDeClienteComEmpresaForjada(ContextoEmpresaWebFactory fabrica, int idEmpresaForjada)
        {
            TokenService tokens = CriarTokenService(fabrica);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string versaoSessao = tokens.CalcularVersaoSessao(fabrica.HashSenhaCliente);

            Dictionary<string, object> claims = new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, idCliente.ToString(CultureInfo.InvariantCulture) },
                { ConstantsValues.ClaimRole, PerfilUsuario.CLIENTE.ToString() },
                { ConstantsValues.ClaimNome, NomeRaiz },
                { ConstantsValues.ClaimVersaoSessao, versaoSessao },
                { ConstantsValues.ClaimEmpresa, idEmpresaForjada.ToString(CultureInfo.InvariantCulture) },
                { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
            };

            SigningCredentials credenciais = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fabrica.ChaveJwt)),
                SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descritor = new SecurityTokenDescriptor
            {
                Issuer = AutenticacaoWebFactory.Emissor,
                Audience = AutenticacaoWebFactory.Audiencia,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddMinutes(120),
                Claims = claims,
                SigningCredentials = credenciais
            };

            JsonWebTokenHandler handler = new JsonWebTokenHandler();

            return handler.CreateToken(descritor);
        }

        private static TokenService CriarTokenService(ContextoEmpresaWebFactory fabrica)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = AutenticacaoWebFactory.Emissor,
                Audiencia = AutenticacaoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = fabrica.ChaveJwt
            };

            return new TokenService(opcoes, new FakeLogService());
        }

        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        private static HttpRequestMessage RequisicaoComCorpo(HttpMethod metodo, string rota, string? token, object corpo)
        {
            HttpRequestMessage requisicao = Requisicao(metodo, rota, token);
            requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

            return requisicao;
        }

        private static object CorpoCategoria()
        {
            return new
            {
                nome = NomeCategoriaTeste,
                descricao = "Categoria criada no teste",
                valorDiariaBase = 150.00m
            };
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }

    /// <summary>
    /// Servidor de teste do contexto de empresa: a factory base (login e sessao com fakes) mais uma locadora comum
    /// (a operadora e a empresa 1 da semente), funcionarios em cada uma com cargos diferentes, categorias trocadas
    /// por fake e um controller de sondagem que expoe o IContextoEmpresa e as duas politicas. O token de cada
    /// funcionario sai com o id da empresa e o cargo que o teste informa; o servidor confere com o banco.
    /// </summary>
    public sealed class ContextoEmpresaWebFactory : AutenticacaoWebFactory
    {
        private const string CpfAdminOperadora = "12345678909";
        private const string CpfAtendenteOperadora = "86288366757";
        private const string CpfAdminComum = "39053344705";
        private const string CnpjLocadoraComum = "22222222222222";
        private const string NomeLocadoraComum = "Locadora Comum";

        public ContextoEmpresaWebFactory()
            : base(1000, null)
        {
            Empresa comum = new Empresa
            {
                RazaoSocial = NomeLocadoraComum + " Ltda",
                NomeFantasia = NomeLocadoraComum,
                Cnpj = CnpjLocadoraComum,
                Ativo = true,
                Operadora = false
            };

            Repositorio.AdicionarEmpresa(comum);
            EmpresaComum = comum;
            EmpresaOperadora = Repositorio.Empresas.Single(x => x.Operadora);

            AdminOperadora = CriarFuncionario("Admin Operadora", CpfAdminOperadora, CargoFuncionario.ADMINISTRADOR, EmpresaOperadora.IdEmpresa);
            AtendenteOperadora = CriarFuncionario("Atendente Operadora", CpfAtendenteOperadora, CargoFuncionario.ATENDENTE, EmpresaOperadora.IdEmpresa);
            AdminComum = CriarFuncionario("Admin Comum", CpfAdminComum, CargoFuncionario.ADMINISTRADOR, EmpresaComum.IdEmpresa);
        }

        /// <summary>Locadora operadora da plataforma (a empresa inicial da semente).</summary>
        public Empresa EmpresaOperadora { get; }

        /// <summary>Locadora comum, sem o papel de operadora.</summary>
        public Empresa EmpresaComum { get; }

        /// <summary>ADMINISTRADOR da empresa operadora.</summary>
        public Funcionario AdminOperadora { get; }

        /// <summary>ATENDENTE da empresa operadora (cargo sem permissao de administracao).</summary>
        public Funcionario AtendenteOperadora { get; }

        /// <summary>ADMINISTRADOR da locadora comum.</summary>
        public Funcionario AdminComum { get; }

        /// <summary>Repositorio de categorias em memoria, trocado no servidor de teste.</summary>
        public FakeCategoriaRepository Categorias { get; } = new FakeCategoriaRepository();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICategoriaRepository>();
                services.AddSingleton<ICategoriaRepository>(Categorias);
            });
        }

        private Funcionario CriarFuncionario(string nome, string cpf, CargoFuncionario cargo, int idEmpresa)
        {
            Funcionario funcionario = new Funcionario
            {
                Nome = nome,
                Cpf = cpf,
                Matricula = "M-" + cpf,
                Cargo = cargo,
                Ativo = true,
                SenhaHash = HashSenhaFuncionario,
                IdEmpresa = idEmpresa
            };

            Repositorio.AdicionarFuncionario(funcionario);

            return funcionario;
        }
    }

    /// <summary>
    /// Controller de sondagem SO para os testes do contexto de empresa: mostra o IContextoEmpresa preenchido pela
    /// validacao do token e uma rota por politica (administrador e operadora). Nao existe em producao.
    /// </summary>
    [ApiController]
    [Route("api/teste-contexto")]
    public class ProbeContextoEmpresaController : ControllerBase
    {
        private readonly IContextoEmpresa _contextoEmpresa;

        public ProbeContextoEmpresaController(IContextoEmpresa contextoEmpresa)
        {
            _contextoEmpresa = contextoEmpresa;
        }

        [HttpGet("contexto")]
        [Authorize]
        public IActionResult Contexto()
        {
            return Ok(new
            {
                idEmpresa = _contextoEmpresa.IdEmpresa,
                idFuncionario = _contextoEmpresa.IdFuncionario,
                cargo = _contextoEmpresa.Cargo?.ToString(),
                empresaOperadora = _contextoEmpresa.EmpresaOperadora,
                idCliente = _contextoEmpresa.IdCliente
            });
        }

        [HttpGet("administrador")]
        [Authorize(Policy = ConstantsValues.PoliticaAdministrador)]
        public IActionResult Administrador()
        {
            return Ok(new { ok = true });
        }

        [HttpGet("operadora")]
        [Authorize(Policy = ConstantsValues.PoliticaOperadora)]
        public IActionResult Operadora()
        {
            return Ok(new { ok = true });
        }
    }
}
