using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP de GET /api/locacoes/minhas (tela 27) com o servidor em memoria, o repositorio de locacoes trocado por
    /// FakeLocacaoRepository, a sessao por FakeSessaoService, o relogio fixo em 26/11/2026 e o armazenamento de teste. Cobre
    /// 401 sem token, 403 para funcionario, cada cliente so com as proprias locacoes, situacao ausente (em andamento), situacao
    /// invalida (400) e capa com falha (200 com erro no item). Nenhum PostgreSQL e acessado.
    /// </summary>
    public class MinhasLocacoesHttpTests : IDisposable
    {
        private const string Rota = "/api/locacoes/minhas";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const int IdClienteDoTeste = 7;
        private const int IdClienteAlheio = 8;
        private const int IdFuncionarioDoTeste = 9;
        private const string NomeTotal = "total";
        private const string NomeItens = "itens";
        private const string NomeIdLocacao = "idLocacao";
        private const string NomeStatus = "status";
        private const string NomeErroFotoCapa = "erroFotoCapa";

        private static readonly DateTime MomentoFixo = new DateTime(2026, 11, 26, 18, 0, 0);

        private readonly MinhasLocacoesWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public MinhasLocacoesHttpTests()
        {
            _fabrica = new MinhasLocacoesWebFactory();
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        private static Empresa CriarEmpresa(int idEmpresa)
        {
            return new Empresa { IdEmpresa = idEmpresa, NomeFantasia = "Locadora " + idEmpresa, Logradouro = "Rua A", Numero = "1", Bairro = "Centro", Cidade = "Campinas", Uf = "SP", Ativo = true };
        }

        private static Locacao CriarLocacao(int idLocacao, int idCliente, StatusLocacao status, bool comFoto)
        {
            Empresa empresa = CriarEmpresa(1);
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "Economico" };
            Veiculo veiculo = new Veiculo { IdVeiculo = idLocacao, IdCategoria = 1, Categoria = categoria, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Mobi", AnoModelo = 2024, IdEmpresa = 1, Empresa = empresa };

            if (comFoto)
            {
                veiculo.Fotos.Add(new VeiculoFoto { IdVeiculoFoto = idLocacao, IdVeiculo = idLocacao, Ordem = 1, ChaveObjeto = "fotos/" + idLocacao });
            }

            return new Locacao
            {
                IdLocacao = idLocacao,
                IdCliente = idCliente,
                IdVeiculo = idLocacao,
                Veiculo = veiculo,
                DataRetirada = MomentoFixo.AddDays(-idLocacao),
                DataDevolucaoPrevista = MomentoFixo.AddDays(3),
                KmSaida = 1000,
                ValorDiaria = 150m,
                Status = status
            };
        }

        private HttpRequestMessage Requisicao(string? token, string consulta)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, Rota + consulta);

            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        private string TokenDoCliente(int idCliente)
        {
            return GerarToken(PerfilUsuario.CLIENTE, null, idCliente);
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE, IdFuncionarioDoTeste);
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo, int idUsuario)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = MinhasLocacoesWebFactory.Emissor,
                Audiencia = MinhasLocacoesWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, idUsuario, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, 1).Token;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        /// <summary>Procura um texto em qualquer valor string do JSON (a resposta de erro traz a mensagem em detail ou em errors).</summary>
        private static bool ContemTexto(JsonElement elemento, string texto)
        {
            switch (elemento.ValueKind)
            {
                case JsonValueKind.String:
                    return elemento.GetString() == texto;
                case JsonValueKind.Array:
                    foreach (JsonElement item in elemento.EnumerateArray())
                    {
                        if (ContemTexto(item, texto))
                        {
                            return true;
                        }
                    }

                    return false;
                case JsonValueKind.Object:
                    foreach (JsonProperty propriedade in elemento.EnumerateObject())
                    {
                        if (ContemTexto(propriedade.Value, texto))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        [Fact]
        public async Task SemToken_Devolve401ComAMensagemDeNaoAutenticado()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(null, string.Empty));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.True(ContemTexto(corpo, ConstantsMessage.ErroNaoAutenticado));
        }

        [Fact]
        public async Task TokenDeFuncionario_Devolve403ComAMensagemDeAcessoNegado()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDeFuncionario(), string.Empty));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.True(ContemTexto(corpo, ConstantsMessage.ErroAcessoNegado));
        }

        [Fact]
        public async Task FalhaInesperada_Devolve500ERegistraNoLogNoServicoENoController()
        {
            _fabrica.Locacoes.FalhaNaListagemMinhas = new InvalidOperationException("falha de teste");

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), string.Empty));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            Assert.True(ContemTexto(corpo, ConstantsMessage.ErroInterno));
            Assert.Equal(2, ContarEntradasDeListagem());
        }

        [Fact]
        public async Task FalhaDeNegocio_Devolve409ENaoRegistraNoLog()
        {
            _fabrica.Locacoes.FalhaNaListagemMinhas = new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), string.Empty));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(0, ContarEntradasDeListagem());
        }

        /// <summary>Quantas entradas do log do servidor de teste trazem a mensagem de falha da listagem.</summary>
        private int ContarEntradasDeListagem()
        {
            return _fabrica.Log.Entries.Count(entrada => entrada.Contains(ConstantsMessage.ErroListarMinhasLocacoes));
        }

        [Fact]
        public async Task DoisClientes_CadaUmVeSoAsProprias()
        {
            _fabrica.Locacoes.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, false));
            _fabrica.Locacoes.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, false));
            _fabrica.Locacoes.Adicionar(CriarLocacao(3, IdClienteAlheio, StatusLocacao.EM_ANDAMENTO, false));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), string.Empty));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(2, corpo.GetProperty(NomeTotal).GetInt32());
            List<int> ids = corpo.GetProperty(NomeItens).EnumerateArray().Select(item => item.GetProperty(NomeIdLocacao).GetInt32()).ToList();
            Assert.DoesNotContain(3, ids);
        }

        [Fact]
        public async Task SituacaoAusente_ValeEmAndamento()
        {
            _fabrica.Locacoes.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, false));
            _fabrica.Locacoes.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.FINALIZADA, false));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), string.Empty));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(1, corpo.GetProperty(NomeTotal).GetInt32());
            Assert.Equal(nameof(StatusLocacao.EM_ANDAMENTO), corpo.GetProperty(NomeItens)[0].GetProperty(NomeStatus).GetString());
        }

        [Fact]
        public async Task SituacaoInvalida_Devolve400ComAMensagemDaSituacao()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), "?situacao=FINALIZADA"));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.True(ContemTexto(corpo, ConstantsMessage.ErroSituacaoLocacaoInvalida));
        }

        [Fact]
        public async Task CapaComFalhaDoArmazenamento_Devolve200ComErroNoItemComFoto()
        {
            _fabrica.Locacoes.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, true));
            _fabrica.Armazenamento.FalharAssinatura = true;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(TokenDoCliente(IdClienteDoTeste), "?situacao=EM_ANDAMENTO"));
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, corpo.GetProperty(NomeItens)[0].GetProperty(NomeErroFotoCapa).GetString());
        }

        /// <summary>Servidor de teste das locacoes do cliente: repositorio, relogio, sessao, armazenamento e log em memoria.</summary>
        public class MinhasLocacoesWebFactory : WebApplicationFactory<Program>
        {
            public const string Emissor = "AutoVia.Api";
            public const string Audiencia = "AutoVia.Web";

            public MinhasLocacoesWebFactory()
            {
                ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                Locacoes = new FakeLocacaoRepository();
                Armazenamento = new FakeArmazenamentoGateway();
                Log = new FakeLogService();
            }

            /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada instancia).</summary>
            public string ChaveJwt { get; }

            /// <summary>Repositorio de locacoes em memoria; os testes semeiam as locacoes por aqui.</summary>
            public FakeLocacaoRepository Locacoes { get; }

            /// <summary>Armazenamento de teste; FalharAssinatura simula a falha da capa.</summary>
            public FakeArmazenamentoGateway Armazenamento { get; }

            /// <summary>Log em memoria do servidor de teste.</summary>
            public FakeLogService Log { get; }

            /// <summary>Sessao simulada (o padrao e um funcionario ativo; os tokens de cliente nao dependem dela).</summary>
            public FakeSessaoService Sessao { get; } = new FakeSessaoService();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((contexto, configuracao) =>
                {
                    Dictionary<string, string?> valores = new Dictionary<string, string?>
                    {
                        { "Jwt:Emissor", Emissor },
                        { "Jwt:Audiencia", Audiencia },
                        { "Jwt:ExpiracaoMinutos", "120" },
                        { "Jwt:Chave", ChaveJwt },
                        { "Autenticacao:LimitePublicoPorMinuto", "1000" },
                        { "Autenticacao:Recuperacao:UrlPublicaFront", "https://front.exemplo.com" },
                        { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                    };

                    configuracao.AddInMemoryCollection(valores);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ILocacaoRepository>();
                    services.AddSingleton<ILocacaoRepository>(Locacoes);
                    services.RemoveAll<IRelogio>();
                    services.AddSingleton<IRelogio>(new FakeRelogio(new DateTime(2026, 11, 26, 18, 0, 0)));
                    services.RemoveAll<ISessaoService>();
                    services.AddSingleton<ISessaoService>(Sessao);
                    services.RemoveAll<IArmazenamentoGateway>();
                    services.AddSingleton<IArmazenamentoGateway>(Armazenamento);
                    services.RemoveAll<ILogService>();
                    services.AddSingleton<ILogService>(Log);
                });
            }
        }
    }
}
