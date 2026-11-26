using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Api;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Contratos
{
    /// <summary>
    /// Servidor de teste do contrato: a aplicacao real, com o repositorio do contrato, a vistoria, as cobrancas, o relogio e o
    /// log trocados por fakes (sem PostgreSQL). A sessao vem do FakeSessaoService do VeiculoWebFactory (cargo e locadora).
    /// </summary>
    public class ContratoWebFactory : VeiculoWebFactory
    {
        public static readonly DateTime Agora = new DateTime(2026, 11, 26, 15, 0, 0);
        public const int IdLocacaoDoTeste = 10;
        public const int IdFuncionarioDoTeste = 20;

        public ContratoWebFactory()
        {
            Contratos = new FakeContratoLocacaoRepository(() => Sessao.Situacao.IdEmpresa ?? 0);
            Vistorias = new FakeVistoriaRepository();
            Cobrancas = new FakeCobrancasExtrasRepository(ContextoEmpresaDeTeste.Criar(1, CargoFuncionario.ATENDENTE));
            Log = new FakeLogService();

            Contratos.Locacoes.Add(CriarLocacao(IdLocacaoDoTeste, 1));
            Vistorias.Vistorias.Add(new Vistoria
            {
                IdVistoria = 1, IdLocacao = IdLocacaoDoTeste, IdFuncionario = IdFuncionarioDoTeste, Tipo = TipoVistoria.ENTRADA,
                DataHora = new DateTime(2026, 11, 20, 10, 0, 0), KmRegistrado = 100, NivelCombustivel = 0.75m
            });
            Contratos.Funcionarios.Add(new Funcionario { IdFuncionario = IdFuncionarioDoTeste, Nome = "Ana Souza", Ativo = true, IdEmpresa = 1 });
            Cobrancas.Configuracoes.Add(new ConfiguracaoCobrancaExtra
            {
                IdConfiguracao = 1, IdEmpresa = 1, IdCategoria = 1, Versao = 3, BaseFranquia = ConstantsMessage.CobrancasExtrasPorDiaria,
                FranquiaKm = 200, ValorKmExcedente = 1.5m, ValorTanqueCompleto = 300m, ToleranciaAtrasoMinutos = 30,
                UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora, FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional,
                ValorUnidadeAtraso = 20m, BaseAvarias = "ESTIMADO"
            });
        }

        public FakeContratoLocacaoRepository Contratos { get; }

        public FakeVistoriaRepository Vistorias { get; }

        public FakeCobrancasExtrasRepository Cobrancas { get; }

        public FakeLogService Log { get; }

        /// <summary>Locacao semeada com o veiculo de uma locadora (o filtro de empresa usa esse id).</summary>
        public static Locacao CriarLocacao(int idLocacao, int idEmpresa)
        {
            return new Locacao
            {
                IdLocacao = idLocacao, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = new DateTime(2026, 11, 20, 10, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 22, 0, 0, 0), ValorDiaria = 199.90m, ValorCaucao = 500m, KmSaida = 100,
                Veiculo = new Veiculo
                {
                    IdVeiculo = 1, IdEmpresa = idEmpresa, IdCategoria = 1, Marca = "Fiat", Modelo = "Toro", Placa = "ABC1D23",
                    Chassi = "9BD000000000001", Renavam = "12345678901", AnoFabricacao = 2025, AnoModelo = 2026,
                    Empresa = new Empresa
                    {
                        IdEmpresa = idEmpresa, RazaoSocial = "Locadora Teste Ltda", NomeFantasia = "Locadora Teste", Cnpj = "12345678000199",
                        Telefone = "3133334444", Email = "contato@teste.com", Cep = "30100000", Logradouro = "Rua A", Numero = "10",
                        Bairro = "Centro", Cidade = "Belo Horizonte", Uf = "MG"
                    }
                },
                Cliente = new Cliente { Nome = "Maria Souza", Cpf = "12345678909", CnhNumero = "12345678900", CnhValidade = new DateOnly(2030, 1, 1) }
            };
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IContratoLocacaoRepository>();
                services.AddSingleton<IContratoLocacaoRepository>(Contratos);
                services.RemoveAll<IVistoriaRepository>();
                services.AddSingleton<IVistoriaRepository>(Vistorias);
                services.RemoveAll<ICobrancasExtrasRepository>();
                services.AddSingleton<ICobrancasExtrasRepository>(Cobrancas);
                services.RemoveAll<IRelogio>();
                services.AddSingleton<IRelogio>(new FakeRelogio(Agora));
                services.RemoveAll<ILogService>();
                services.AddSingleton<ILogService>(Log);
            });
        }
    }

    public class ContratoLocacaoHttpTests : IDisposable
    {
        private const string Rota = "/api/locacoes/10/contrato";
        private const string RotaAssinatura = "/api/locacoes/10/contrato/assinatura";
        private const string TipoProblema = "application/problem+json";
        private const string CampoDetail = "detail";
        private readonly ContratoWebFactory _fabrica = new ContratoWebFactory();
        private readonly HttpClient _cliente;

        public ContratoLocacaoHttpTests()
        {
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        /// <summary>Token de funcionario (ou de cliente) gerado com a chave do servidor de teste.</summary>
        public static string TokenDe(VeiculoWebFactory fabrica, PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, ContratoWebFactory.IdFuncionarioDoTeste, "Teste", perfil == PerfilUsuario.FUNCIONARIO ? CargoFuncionario.ATENDENTE : null,
                "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? 1 : null).Token;
        }

        private static string PngBase64()
        {
            return Convert.ToBase64String(ConstantsValues.ContratoAssinaturaCabecalhoPng);
        }

        private static string CorpoValido(string nome = "Maria Souza", bool aceite = true, string? png = null)
        {
            return JsonSerializer.Serialize(new
            {
                nomeAssinante = nome,
                aceiteCondicoesGerais = aceite,
                aceitePoliticaCombustivel = true,
                assinaturaPngBase64 = png ?? PngBase64()
            });
        }

        private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string rota, string? token, string? corpo = null, HttpClient? cliente = null)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);
            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }
            if (corpo != null)
            {
                requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
            }

            return await (cliente ?? _cliente).SendAsync(requisicao);
        }

        private static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        {
            string json = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(json);

            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task SemToken_Get401EPost401()
        {
            using HttpResponseMessage get = await Enviar(HttpMethod.Get, Rota, null);
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, null, CorpoValido());

            Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task OutroCargo_Get403EPost403SemGravar(CargoFuncionario cargo)
        {
            using ContratoWebFactory fabrica = new ContratoWebFactory();
            fabrica.Sessao.Situacao = new SessaoSituacaoVO { Ativo = true, IdEmpresa = 1, Cargo = cargo, EmpresaAtiva = true, EmpresaOperadora = false };
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDe(fabrica);

            using HttpResponseMessage get = await Enviar(HttpMethod.Get, Rota, token, null, cliente);
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido(), cliente);

            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
            Assert.Empty(fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task Cliente_Get403EPost403()
        {
            string token = TokenDe(_fabrica, PerfilUsuario.CLIENTE);

            using HttpResponseMessage get = await Enviar(HttpMethod.Get, Rota, token);
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());

            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        }

        [Fact]
        public async Task Get_PendenteDevolve200SemAssinaturaNemHash()
        {
            string token = TokenDe(_fabrica);

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, Rota, token);
            JsonElement corpo = await LerJson(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(ConstantsValues.ContratoSituacaoPendente, corpo.GetProperty("situacao").GetString());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("hashSha256").ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("assinatura").ValueKind);
            Assert.Equal(JsonValueKind.Object, corpo.GetProperty("documento").ValueKind);
        }

        [Fact]
        public async Task Post201ComLocationEGetConsultavelComAMesmaCopia()
        {
            string token = TokenDe(_fabrica);

            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());
            JsonElement criado = await LerJson(post);

            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            Assert.Equal(Rota, post.Headers.Location?.AbsolutePath);
            Assert.Equal(ConstantsValues.ContratoSituacaoAssinado, criado.GetProperty("situacao").GetString());
            Assert.Equal("Ana Souza", criado.GetProperty("assinatura").GetProperty("nomeFuncionarioColetor").GetString());

            using HttpResponseMessage get = await Enviar(HttpMethod.Get, post.Headers.Location!.PathAndQuery, token);
            JsonElement lido = await LerJson(get);

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(criado.GetProperty("hashSha256").GetString(), lido.GetProperty("hashSha256").GetString());
            Assert.Equal(64, lido.GetProperty("hashSha256").GetString()!.Length);
            Assert.Equal(criado.GetProperty("documento").GetRawText(), lido.GetProperty("documento").GetRawText());
        }

        [Fact]
        public async Task PostDuasVezes_SegundaDa409ComProblemaEJaAssinado()
        {
            string token = TokenDe(_fabrica);
            using HttpResponseMessage primeira = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());
            Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);

            using HttpResponseMessage segunda = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());

            Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
            Assert.Equal(TipoProblema, segunda.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsMessage.ErroContratoJaAssinado, (await LerJson(segunda)).GetProperty(CampoDetail).GetString());
            Assert.Single(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task SemVistoriaDeEntrada_Get409ComRP06ESemGravar()
        {
            _fabrica.Vistorias.Vistorias.Clear();
            string token = TokenDe(_fabrica);

            using HttpResponseMessage get = await Enviar(HttpMethod.Get, Rota, token);
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());

            Assert.Equal(HttpStatusCode.Conflict, get.StatusCode);
            Assert.Equal(TipoProblema, get.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, (await LerJson(get)).GetProperty(CampoDetail).GetString());
            Assert.Equal(HttpStatusCode.Conflict, post.StatusCode);
            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, (await LerJson(post)).GetProperty(CampoDetail).GetString());
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task LocacaoDeOutraLocadora_Get404EPost404()
        {
            _fabrica.Contratos.Locacoes[0].Veiculo.IdEmpresa = 2;
            string token = TokenDe(_fabrica);

            using HttpResponseMessage get = await Enviar(HttpMethod.Get, Rota, token);
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido());

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task PngInvalido_Recusa400ESemGravar()
        {
            string token = TokenDe(_fabrica);
            string corpo = CorpoValido(png: "abc");

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, corpo);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task AceiteCondicoesFalso_Recusa400ComTextoAprovado()
        {
            string token = TokenDe(_fabrica);

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido(aceite: false));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, (await LerJson(resposta)).GetProperty(CampoDetail).GetString());
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task NomeVazio_Recusa400()
        {
            string token = TokenDe(_fabrica);

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido(nome: "   "));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task NomeComCaractereDeControle_Recusa400SemGravar()
        {
            string token = TokenDe(_fabrica);
            string corpo = "{\"nomeAssinante\":\"Maria\\u0000Souza\",\"aceiteCondicoesGerais\":true,\"aceitePoliticaCombustivel\":true,\"assinaturaPngBase64\":\""
                + PngBase64() + "\"}";

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, corpo);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task CorpoVazio_Recusa400ESemGravar()
        {
            string token = TokenDe(_fabrica);

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Empty(_fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task Administrador_Post201ComLocation()
        {
            using ContratoWebFactory fabrica = new ContratoWebFactory();
            fabrica.Sessao.Situacao = new SessaoSituacaoVO
            {
                Ativo = true, IdEmpresa = 1, Cargo = CargoFuncionario.ADMINISTRADOR, EmpresaAtiva = true, EmpresaOperadora = false
            };
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDe(fabrica);

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, CorpoValido(), cliente);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Equal(Rota, resposta.Headers.Location?.AbsolutePath);
            Assert.Single(fabrica.Contratos.Contratos);
        }

        [Fact]
        public async Task Aceite2AusenteComClausula5_Recusa400ComTextoAprovadoSemGravar()
        {
            string token = TokenDe(_fabrica);
            string corpo = JsonSerializer.Serialize(new
            {
                nomeAssinante = "Maria Souza",
                aceiteCondicoesGerais = true,
                assinaturaPngBase64 = PngBase64()
            });

            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, RotaAssinatura, token, corpo);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, (await LerJson(resposta)).GetProperty(CampoDetail).GetString());
            Assert.Empty(_fabrica.Contratos.Contratos);
        }
    }
}
