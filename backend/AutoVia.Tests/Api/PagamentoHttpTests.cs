using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.ViewObjects.Pagamento;
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
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP da cobranca (RF-31 e RF-36) com o servidor em memoria, o repositorio trocado por FakePagamentoRepository, a
    /// sessao por FakeSessaoService e o relogio fixo em 19/11/2026. Cobre 401 sem token; 403 para cliente nas rotas da
    /// locadora, para funcionario nas rotas do cliente e para cargos fora de FINANCEIRO e ADMINISTRADOR; 201 sem Location
    /// no lancamento; 200 na baixa e nas listagens; 400 (origem dupla, caucao pela rota, status e aba invalidos); 404; 409
    /// (cobranca ja paga); e a ausencia de id_transacao em todas as respostas. Nenhum PostgreSQL e acessado.
    /// </summary>
    public class PagamentoHttpTests : IDisposable
    {
        private const string RotaPagamentos = "/api/pagamentos";
        private const string RotaMeus = "/api/pagamentos/meus";
        private const string RotaResumo = "/api/pagamentos/meus/resumo";
        private const string MediaProblema = "application/problem+json";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string TipoJson = "application/json";
        private const string NomeIdTransacao = "idTransacao";
        private const string NomeDetail = "detail";
        private const string NomeTitle = "title";
        private const string NomeErrors = "errors";
        private const int IdClienteDoTeste = 7;
        private const int IdClienteAlheio = 8;
        private const int IdLocacaoDoTeste = 1;
        private const int IdPagamentoPendente = 5;
        private const int IdPagamentoPago = 6;
        private const int IdPagamentoInexistente = 999;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdClienteSemCobranca = 10;
        private const int IdPagamentoDoSegundoCliente = 7;

        private static readonly DateTime MomentoFixo = new DateTime(2026, 11, 19, 10, 0, 0);

        private readonly PagamentoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public PagamentoHttpTests()
        {
            _fabrica = new PagamentoWebFactory();
            DefinirCargo(CargoFuncionario.FINANCEIRO);
            SemearCobrancas();
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        private void DefinirCargo(CargoFuncionario cargo)
        {
            _fabrica.Sessao.Situacao = new SessaoSituacaoVO
            {
                Ativo = true,
                IdEmpresa = 1,
                Cargo = cargo,
                EmpresaAtiva = true,
                EmpresaOperadora = false
            };
        }

        private void SemearCobrancas()
        {
            Locacao locacao = new Locacao
            {
                IdLocacao = IdLocacaoDoTeste,
                IdCliente = IdClienteDoTeste,
                Cliente = new Cliente { IdCliente = IdClienteDoTeste, Nome = "Maria Souza", Cpf = "12345678901", Email = "maria@exemplo.com" },
                Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" }
            };
            Locacao locacaoAlheia = new Locacao
            {
                IdLocacao = 2,
                IdCliente = IdClienteAlheio,
                Cliente = new Cliente { IdCliente = IdClienteAlheio, Nome = "Joao Lima", Cpf = "10987654321", Email = "joao@exemplo.com" },
                Veiculo = new Veiculo { Marca = "VW", Modelo = "Polo" }
            };
            _fabrica.Pagamentos.Locacoes.Add(locacao);
            _fabrica.Pagamentos.Locacoes.Add(locacaoAlheia);
            _fabrica.Pagamentos.Vendas.Add(new Venda { IdVenda = 4, IdCliente = IdClienteDoTeste, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Mobi" } });

            _fabrica.Pagamentos.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdPagamentoPendente,
                IdLocacao = IdLocacaoDoTeste,
                Locacao = locacao,
                Tipo = TipoPagamento.DIARIA,
                Status = StatusPagamento.PENDENTE,
                Valor = 395.60m,
                DataVencimento = new DateOnly(2026, 11, 25),
                IdTransacao = "tx-nao-pode-sair",
                CreatedAt = MomentoFixo,
                UpdatedAt = MomentoFixo
            });

            _fabrica.Pagamentos.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdPagamentoPago,
                IdLocacao = IdLocacaoDoTeste,
                Locacao = locacao,
                Tipo = TipoPagamento.DIARIA,
                Status = StatusPagamento.PAGO,
                Valor = 100m,
                DataVencimento = new DateOnly(2026, 11, 10),
                DataPagamento = new DateOnly(2026, 11, 10),
                Forma = "PIX",
                IdTransacao = "tx-nao-pode-sair-2",
                CreatedAt = MomentoFixo,
                UpdatedAt = MomentoFixo
            });

            _fabrica.Pagamentos.Pagamentos.Add(new Pagamento
            {
                IdPagamento = 7,
                IdLocacao = 2,
                Locacao = locacaoAlheia,
                Tipo = TipoPagamento.DIARIA,
                Status = StatusPagamento.PENDENTE,
                Valor = 50m,
                DataVencimento = new DateOnly(2026, 11, 25),
                CreatedAt = MomentoFixo,
                UpdatedAt = MomentoFixo
            });
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo, int idUsuario)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = PagamentoWebFactory.Emissor,
                Audiencia = PagamentoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, idUsuario, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, 1).Token;
        }

        private string TokenFinanceiro()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.FINANCEIRO, IdFuncionarioDoTeste);
        }

        private string TokenDoCliente(int idCliente)
        {
            return GerarToken(PerfilUsuario.CLIENTE, null, idCliente);
        }

        private static LancarPagamentoVO LancamentoValido()
        {
            return new LancarPagamentoVO { IdLocacao = IdLocacaoDoTeste, Tipo = "DIARIA", Valor = 395.60m, DataVencimento = new DateOnly(2026, 11, 25) };
        }

        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token, object? corpo)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            if (corpo != null)
            {
                requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, TipoJson);
            }

            return requisicao;
        }

        private static HttpRequestMessage RequisicaoDeIp(HttpMethod metodo, string rota, string? token, string ip)
        {
            HttpRequestMessage requisicao = Requisicao(metodo, rota, token, null);
            requisicao.Headers.Add(ConstantsValues.CabecalhoIpClientePadrao, ip);

            return requisicao;
        }

        private static string IpDeTeste(int numero)
        {
            return $"10.0.1.{numero}";
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task SemToken_NasRotasDaLocadoraEDoCliente_Devolve401ProblemJson()
        {
            HttpResponseMessage listagem = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaPagamentos, null, null));
            HttpResponseMessage meus = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaMeus, null, null));

            Assert.Equal(HttpStatusCode.Unauthorized, listagem.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, meus.StatusCode);
            Assert.Equal(MediaProblema, listagem.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(listagem);
            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task ClienteEmRotaDaLocadora_Devolve403Problem()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaPagamentos, TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task FuncionarioEmRotaDoCliente_Devolve403Problem()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaMeus, TokenFinanceiro(), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task CargoSemPermissao_Lancar_Devolve403ENaoGrava(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            int antes = _fabrica.Pagamentos.Pagamentos.Count;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaPagamentos, GerarToken(PerfilUsuario.FUNCIONARIO, cargo, IdFuncionarioDoTeste), LancamentoValido()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(antes, _fabrica.Pagamentos.Pagamentos.Count);
        }

        [Theory]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task FinanceiroOuAdministrador_Lancar_Devolve201SemLocationESemIdTransacao(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaPagamentos, GerarToken(PerfilUsuario.FUNCIONARIO, cargo, IdFuncionarioDoTeste), LancamentoValido()));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Null(resposta.Headers.Location);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(StatusPagamento.PENDENTE.ToString(), corpo.GetProperty("status").GetString());
            Assert.False(corpo.TryGetProperty(NomeIdTransacao, out JsonElement _));
        }

        [Fact]
        public async Task Lancar_OrigemDupla_Devolve400ComErrosDeValidacao()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.IdVenda = 4;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaPagamentos, TokenFinanceiro(), entrada));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPagamentoOrigemDupla, corpo.GetProperty(NomeDetail).GetString());
            Assert.True(corpo.TryGetProperty(NomeErrors, out JsonElement _));
        }

        [Fact]
        public async Task Lancar_CaucaoPelaRota_Devolve400()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Tipo = "CAUCAO";

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaPagamentos, TokenFinanceiro(), entrada));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPagamentoCaucaoPorRota, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task Lancar_LocacaoDeOutraLocadoraOuInexistente_Devolve404()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.IdLocacao = 999;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaPagamentos, TokenFinanceiro(), entrada));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Baixar_PendenteDevolve200ComPagoEForma()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPendente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "PIX" }));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(StatusPagamento.PAGO.ToString(), corpo.GetProperty("status").GetString());
            Assert.Equal("PIX", corpo.GetProperty("forma").GetString());
            Assert.False(corpo.TryGetProperty(NomeIdTransacao, out JsonElement _));
        }

        [Fact]
        public async Task Baixar_JaPagaDevolve409()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPago}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "PIX" }));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Baixar_InexistenteDevolve404()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoInexistente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "PIX" }));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task Baixar_FormaInvalidaDevolve400()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPendente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "pix" }));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task Baixar_DinheiroComPixGeradoSemConfirmar_Devolve409ComAMensagemDaRegraP6()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPendente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "DINHEIRO" }));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaComPixGerado, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task Baixar_DinheiroComPixGeradoComConfirmacao_Devolve200()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPendente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "DINHEIRO", ConfirmarBaixaComPixGerado = true }));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task Baixar_FalhaDoProvedorNoCancelamento_Devolve502ComUmSoRegistroNoLog()
        {
            _fabrica.PixGateway.FalharCancelamento = true;
            Pagamento cobrancaComPix = _fabrica.Pagamentos.Pagamentos.Single(x => x.IdPagamento == IdPagamentoPendente);
            cobrancaComPix.PixExpiraEm = MomentoFixo.AddMinutes(15);
            int registrosAntes = _fabrica.Log.Entries.Count;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/{IdPagamentoPendente}/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "DINHEIRO", ConfirmarBaixaComPixGerado = true }));

            Assert.Equal(HttpStatusCode.BadGateway, resposta.StatusCode);
            Assert.Equal(1, _fabrica.Log.Entries.Count - registrosAntes);
        }

        [Fact]
        public async Task Baixar_DinheiroSemPixGerado_Devolve200()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaPagamentos}/7/baixa", TokenFinanceiro(), new BaixarPagamentoVO { Forma = "DINHEIRO" }));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPix_ClienteDono_Devolve200SemIdentificadorDoProvedor()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(IdPagamentoPendente, corpo.GetProperty("idPagamento").GetInt32());
            Assert.Equal("PIX", corpo.GetProperty("forma").GetString());
            Assert.False(string.IsNullOrEmpty(corpo.GetProperty("copiaECola").GetString()));
            Assert.False(string.IsNullOrEmpty(corpo.GetProperty("qrCodeBase64").GetString()));
            Assert.Equal(900, corpo.GetProperty("segundosParaExpirar").GetInt32());
            Assert.False(corpo.TryGetProperty(NomeIdTransacao, out JsonElement _));
            Assert.False(corpo.TryGetProperty("identificadorProvedor", out JsonElement _));
        }

        [Fact]
        public async Task GerarPix_CobrancaDeOutroCliente_Devolve404()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaMeus}/7/pix", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPix_CobrancaPaga_Devolve409()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPago}/pix", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPix_SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", null, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPix_TokenDeFuncionarioNaRotaDoCliente_Devolve403()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", TokenFinanceiro(), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPixDaLocadora_Financeiro_Devolve200ComIdentificadorDoProvedor()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", TokenFinanceiro(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal("ID-PAG-5", corpo.GetProperty("identificadorProvedor").GetString());
        }

        [Fact]
        public async Task GerarPixDaLocadora_TokenDeCliente_Devolve403()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task GerarPixDaLocadora_SextaGeracaoDoMesmoFuncionarioNoMinuto_Devolve429MesmoDeIpsDiferentes()
        {
            string token = TokenFinanceiro();

            for (int chamada = 1; chamada <= ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao; chamada++)
            {
                HttpResponseMessage ok = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", token, IpDeTeste(chamada)));
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            HttpResponseMessage excedido = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", token, IpDeTeste(ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao + 1)));

            Assert.Equal(HttpStatusCode.TooManyRequests, excedido.StatusCode);
            JsonElement corpo = await LerJsonAsync(excedido);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task GerarPixDaLocadora_DoisFuncionariosNoMesmoIp_NaoGastamOTetoUmDoOutro()
        {
            string tokenPrimeiro = TokenFinanceiro();
            string tokenSegundo = GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.FINANCEIRO, IdFuncionarioDoTeste + 1);

            for (int chamada = 1; chamada <= ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao; chamada++)
            {
                HttpResponseMessage primeiro = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", tokenPrimeiro, IpDeTeste(chamada)));
                Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
            }

            HttpResponseMessage segundo = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaPagamentos}/{IdPagamentoPendente}/pix", tokenSegundo, IpDeTeste(1)));

            Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        }

        [Fact]
        public async Task GerarPix_SextaGeracaoDoMesmoClienteNoMinuto_Devolve429MesmoDeIpsDiferentes()
        {
            string token = TokenDoCliente(IdClienteDoTeste);

            for (int chamada = 1; chamada <= ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao; chamada++)
            {
                HttpResponseMessage ok = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", token, IpDeTeste(chamada)));
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            HttpResponseMessage excedido = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", token, IpDeTeste(ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao + 1)));

            Assert.Equal(HttpStatusCode.TooManyRequests, excedido.StatusCode);
            JsonElement corpo = await LerJsonAsync(excedido);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task GerarPix_DoisClientesNoMesmoIp_NaoGastamOTetoUmDoOutro()
        {
            string tokenPrimeiro = TokenDoCliente(IdClienteDoTeste);
            string tokenSegundo = TokenDoCliente(IdClienteAlheio);
            string ipCompartilhado = IpDeTeste(1);

            for (int chamada = 1; chamada <= ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao; chamada++)
            {
                HttpResponseMessage primeiro = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", tokenPrimeiro, ipCompartilhado));
                Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
            }

            HttpResponseMessage segundo = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoDoSegundoCliente}/pix", tokenSegundo, ipCompartilhado));

            Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        }

        [Fact]
        public async Task GerarPix_OnzeChamadasNoMesmoIp_AOnzeitaResponde429PeloLimiteDoIp()
        {
            string ipCompartilhado = IpDeTeste(2);
            string tokenPrimeiro = TokenDoCliente(IdClienteDoTeste);
            string tokenSegundo = TokenDoCliente(IdClienteAlheio);
            string tokenSemCobranca = TokenDoCliente(IdClienteSemCobranca);

            for (int chamada = 1; chamada <= ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao; chamada++)
            {
                HttpResponseMessage primeiro = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", tokenPrimeiro, ipCompartilhado));
                Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);

                HttpResponseMessage segundo = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoDoSegundoCliente}/pix", tokenSegundo, ipCompartilhado));
                Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
            }

            HttpResponseMessage excedido = await _cliente.SendAsync(RequisicaoDeIp(HttpMethod.Post, $"{RotaMeus}/{IdPagamentoPendente}/pix", tokenSemCobranca, ipCompartilhado));

            Assert.Equal(HttpStatusCode.TooManyRequests, excedido.StatusCode);
            JsonElement corpo = await LerJsonAsync(excedido);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task Listar_DaLocadora_Devolve200ComAPaginaSemIdTransacao()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaPagamentos}?status=PENDENTE", TokenFinanceiro(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(2, corpo.GetProperty("total").GetInt32());
            Assert.False(corpo.GetProperty("itens")[0].TryGetProperty(NomeIdTransacao, out JsonElement _));
        }

        [Fact]
        public async Task Listar_StatusInvalido_Devolve400()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaPagamentos}?status=pago", TokenFinanceiro(), null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task Meus_DoClienteDono_Devolve200SoComAsDele()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaMeus, TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            List<int> ids = corpo.GetProperty("itens").EnumerateArray().Select(x => x.GetProperty("idPagamento").GetInt32()).OrderBy(x => x).ToList();
            Assert.Equal(new List<int> { IdPagamentoPendente, IdPagamentoPago }, ids);
            Assert.DoesNotContain(7, ids);
            foreach (JsonElement item in corpo.GetProperty("itens").EnumerateArray())
            {
                Assert.False(item.TryGetProperty(NomeIdTransacao, out JsonElement _));
            }
        }

        [Fact]
        public async Task Meus_OutroCliente_NaoVeAsCobrancasDoPrimeiro()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaMeus, TokenDoCliente(IdClienteAlheio), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(1, corpo.GetProperty("total").GetInt32());
            Assert.Equal(7, corpo.GetProperty("itens")[0].GetProperty("idPagamento").GetInt32());
        }

        [Fact]
        public async Task Meus_AbaInvalida_Devolve400()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaMeus}?aba=pendentes", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Resumo_DoCliente_Devolve200ComOsTotaisEAsContagens()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaResumo, TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(2026, corpo.GetProperty("anoExercicio").GetInt32());
            Assert.Equal(1, corpo.GetProperty("quantidadePendentes").GetInt32());
            Assert.Equal(StaticMethods.FormateToMonetary(395.60m), corpo.GetProperty("totalPendenteStr").GetString());
            Assert.Equal(1, corpo.GetProperty("quantidadeQuitadasAno").GetInt32());
            Assert.False(corpo.TryGetProperty(NomeIdTransacao, out JsonElement _));
        }

        [Fact]
        public async Task Resumo_PeriodoInvalido_Devolve400()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaResumo}?periodo=MES", TokenDoCliente(IdClienteDoTeste), null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }
    }

    /// <summary>
    /// Servidor de teste da cobranca: configuracao em memoria (chave JWT gerada a cada teste), repositorio de cobrancas trocado
    /// por FakePagamentoRepository, sessao por FakeSessaoService e relogio fixo em 19/11/2026. Nao acessa o PostgreSQL.
    /// </summary>
    public class PagamentoWebFactory : WebApplicationFactory<Program>
    {
        public const string Emissor = "AutoVia.Api";
        public const string Audiencia = "AutoVia.Web";

        public PagamentoWebFactory()
        {
            ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Pagamentos = new FakePagamentoRepository();
            PixGateway = new FakePixGateway { Agora = new DateTime(2026, 11, 19, 10, 0, 0) };
            Log = new FakeLogService();
        }

        /// <summary>Log em memoria do servidor de teste: os testes contam os registros de uma requisicao.</summary>
        public FakeLogService Log { get; }

        /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada instancia).</summary>
        public string ChaveJwt { get; }

        /// <summary>Repositorio de cobrancas em memoria; os testes semeiam locacoes, vendas e cobrancas por aqui.</summary>
        public FakePagamentoRepository Pagamentos { get; }

        /// <summary>Gateway PIX de teste (sem provedor e sem rede); o servidor usa este no lugar do gateway real.</summary>
        public FakePixGateway PixGateway { get; }

        /// <summary>Conferencia de sessao simulada: os testes trocam o cargo do funcionario.</summary>
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
                services.RemoveAll<IPagamentoRepository>();
                services.AddSingleton<IPagamentoRepository>(Pagamentos);
                services.RemoveAll<IRelogio>();
                services.AddSingleton<IRelogio>(new FakeRelogio(new DateTime(2026, 11, 19, 10, 0, 0)));
                services.RemoveAll<ISessaoService>();
                services.AddSingleton<ISessaoService>(Sessao);
                services.RemoveAll<IPixGateway>();
                services.AddSingleton<IPixGateway>(PixGateway);
                services.RemoveAll<ILogService>();
                services.AddSingleton<ILogService>(Log);
            });
        }
    }
}
