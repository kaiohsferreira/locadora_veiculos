using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Api;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// POST /api/locacoes com a caucao (contrato caucao-retirada v1.1) pelo pipeline HTTP de verdade: politica AberturaLocacao
    /// (cargo do banco), validador, teto por funcionario, servico e middleware de erro. O ICaucaoGateway e o FakeCaucaoGateway,
    /// porque os testes HTTP rodam em ambiente Testing e a trava do FAKE real recusaria; Pagamentos:ValorCaucao vem da
    /// configuracao em memoria (sem ela vale o 0 fixado para os testes). Com valor 0 nada muda (RF-17). Tambem a leitura da
    /// retirada (GET) com os cinco campos da caucao, inclusive com a integracao quebrada.
    /// </summary>
    public class AberturaComCaucaoHttpTests
    {
        private const string RotaAbertura = "/api/locacoes";
        private const string RotaRetirada = "/api/locacoes/retirada/10";
        private const string ValorDaApresentacao = "1200.00";
        private const string EmailCliente = "maria.cliente@exemplo.com";
        private const string CpfCliente = "12345678909";
        private const string CorpoSemCartao = "{\"idReserva\":10}";
        private const string CorpoComCartao = "{\"idReserva\":10,\"caucaoTokenCartao\":\"FAKE-tok-http-0001\",\"caucaoMetodoPagamentoId\":\"visa\"}";
        private const string TipoProblema = "application/problem+json";

        private static string Token(AberturaWebFactory fabrica, PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO, CargoFuncionario cargoNoToken = CargoFuncionario.ADMINISTRADOR)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, 20, "Funcionario Teste", perfil == PerfilUsuario.FUNCIONARIO ? cargoNoToken : null,
                "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? 1 : null).Token;
        }

        private static async Task<HttpResponseMessage> Enviar(HttpClient cliente, string rota, string? token, string? corpo)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(corpo == null ? HttpMethod.Get : HttpMethod.Post, rota);

            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            if (corpo != null)
            {
                requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
            }

            return await cliente.SendAsync(requisicao);
        }

        private static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        {
            string json = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(json);

            return documento.RootElement.Clone();
        }

        private static async Task<string?> Detalhe(HttpResponseMessage resposta)
        {
            JsonElement dados = await LerJson(resposta);

            return dados.GetProperty("detail").GetString();
        }

        // --- Valor 0: RF-17 como hoje ---

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task ValorZero_QualquerCargoAbre201_SemGatewayESemCaucao(CargoFuncionario cargo)
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(null);
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoSemCartao);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(0m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
            Assert.Null(fabrica.Locacoes.UltimaCaucao);
        }

        [Fact]
        public async Task ValorZero_DezPostsSeguidos_NenhumRecebe429()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);
            List<HttpStatusCode> codigos = new List<HttpStatusCode>();

            for (int tentativa = 0; tentativa < 10; tentativa++)
            {
                using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, token, CorpoComCartao);
                codigos.Add(resposta.StatusCode);
            }

            Assert.DoesNotContain(HttpStatusCode.TooManyRequests, codigos);
            Assert.Equal(HttpStatusCode.Created, codigos[0]);
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        /// <summary>v1.1, CS-3: um teto mal escrito ('cinco') nao derruba as rotas de locacao.</summary>
        [Fact]
        public async Task ValorZero_TetoMalConfigurado_GetEPostContinuamFuncionando()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(null, "cinco");
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);

            using HttpResponseMessage leitura = await Enviar(cliente, RotaRetirada, token, null);
            using HttpResponseMessage abertura = await Enviar(cliente, RotaAbertura, token, CorpoSemCartao);

            Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
            Assert.Equal(HttpStatusCode.Created, abertura.StatusCode);
        }

        // --- Valor 1200.00: cargo, cartao, teto e respostas ---

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task ComCaucao_CargoSemPermissao403_SemGatewayNemAbertura(CargoFuncionario cargo)
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsMessage.ErroAcessoNegado, await Detalhe(resposta));
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
            Assert.Equal(0, fabrica.Locacoes.ChamadasStartFromReserva);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task ComCaucao_AtendenteEAdministrador201ComOValor_SemIdDoProvedorTokenNemFinalDoCartao(CargoFuncionario cargo)
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            string corpo = await resposta.Content.ReadAsStringAsync();
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(1200m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.Equal(StaticMethods.FormateToMonetary(1200.00m), dados.GetProperty("valorCaucaoStr").GetString());
            Assert.DoesNotContain(FakeCaucaoGateway.IdentificadorPadrao, corpo);
            Assert.DoesNotContain("idTransacao", corpo);
            Assert.DoesNotContain("FAKE-tok-http-0001", corpo);
            Assert.DoesNotContain(FakeCaucaoGateway.CartaoFinalDeTeste, corpo);
            Assert.Single(fabrica.Gateway.Solicitacoes);
            Pagamento caucao = Assert.Single(Assert.Single(fabrica.Locacoes.LocacoesGravadas()).Pagamentos);
            Assert.Equal(TipoPagamento.CAUCAO, caucao.Tipo);
        }

        [Fact]
        public async Task ComCaucao_CargoLidoDoBanco_TokenDeAtendenteComCargoAtualVendedor403()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Sessao.Situacao.Cargo = CargoFuncionario.VENDEDOR;
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica, PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task ComCaucao_SemToken401_TokenDeCliente403()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage anonimo = await Enviar(cliente, RotaAbertura, null, CorpoComCartao);
            using HttpResponseMessage doCliente = await Enviar(cliente, RotaAbertura, Token(fabrica, PerfilUsuario.CLIENTE), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, doCliente.StatusCode);
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task ComCaucao_SemCamposDoCartao400CartaoObrigatorio_SemGatewayESemLogDaControllerNemDosServicos()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoSemCartao);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoCartaoObrigatorio, await Detalhe(resposta));
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
            Assert.DoesNotContain(fabrica.Log.Snapshot(), (string entrada) => entrada.Contains(ConstantsMessage.ErroSalvarLocacao, StringComparison.Ordinal));
            Assert.DoesNotContain(fabrica.Log.Snapshot(), (string entrada) => entrada.Contains(ConstantsMessage.ErroLocacaoCaucaoCartaoObrigatorio, StringComparison.Ordinal));
        }

        [Fact]
        public async Task ComCaucao_CampoDoCartaoMalformado400CartaoInvalidoComOCampo()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), "{\"idReserva\":10,\"caucaoTokenCartao\":\"4111-1111-1111-1111\",\"caucaoMetodoPagamentoId\":\"visa\"}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            string corpo = await resposta.Content.ReadAsStringAsync();
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(ConstantsMessage.ErroCaucaoCartaoInvalido, dados.GetProperty("detail").GetString());
            Assert.True(dados.GetProperty("errors").TryGetProperty("caucaoTokenCartao", out JsonElement _));
            Assert.DoesNotContain("4111-1111-1111-1111", corpo);
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task ComCaucao_SextoPostDoMesmoFuncionarioNoMesmoMinuto429_SemLogDaControllerNemDosServicos()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);

            for (int tentativa = 0; tentativa < 5; tentativa++)
            {
                using HttpResponseMessage recusadaNoServico = await Enviar(cliente, RotaAbertura, token, CorpoSemCartao);
                Assert.Equal(HttpStatusCode.BadRequest, recusadaNoServico.StatusCode);
            }

            using HttpResponseMessage sexta = await Enviar(cliente, RotaAbertura, token, CorpoComCartao);

            Assert.Equal(HttpStatusCode.TooManyRequests, sexta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, await Detalhe(sexta));
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
            Assert.DoesNotContain(fabrica.Log.Snapshot(), (string entrada) => entrada.Contains(ConstantsMessage.ErroSalvarLocacao, StringComparison.Ordinal));
            Assert.DoesNotContain(fabrica.Log.Snapshot(), (string entrada) => entrada.Contains(ConstantsMessage.ErroLimiteRequisicoes, StringComparison.Ordinal));
        }

        /// <summary>v1.1, CS-3: com a caucao exigida e o teto mal escrito ('cinco'), vale o padrao de 5 por minuto.</summary>
        [Fact]
        public async Task ComCaucao_TetoMalConfigurado_UsaOPadraoDeCinco()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao, "cinco");
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);
            List<HttpStatusCode> codigos = new List<HttpStatusCode>();

            for (int tentativa = 0; tentativa < 6; tentativa++)
            {
                using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, token, CorpoSemCartao);
                codigos.Add(resposta.StatusCode);
            }

            Assert.Equal(5, codigos.Count((HttpStatusCode codigo) => codigo == HttpStatusCode.BadRequest));
            Assert.Equal(HttpStatusCode.TooManyRequests, codigos[5]);
        }

        [Fact]
        public async Task ComCaucao_CartaoRecusado409ComAMensagemDoGateway()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            string mensagem = string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro);
            fabrica.Gateway.ExcecaoPreAutorizacao = new BusinessException(mensagem);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(mensagem, await Detalhe(resposta));
            Assert.Equal(0, fabrica.Locacoes.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task ComCaucao_EmAnalise409_CancelaOIdNovo()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Gateway.ResponderPreAutorizacao = (CaucaoSolicitacao solicitacao) => FakeCaucaoGateway.RetornoNoEstado(FakeCaucaoGateway.IdentificadorPadrao, CaucaoStatus.EM_ANALISE, solicitacao.Valor, solicitacao.ReferenciaExterna);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao);

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoEmAnalise, await Detalhe(resposta));
            Assert.Equal(new List<string> { FakeCaucaoGateway.IdentificadorPadrao }, fabrica.Gateway.Cancelamentos);
            Assert.Equal(0, fabrica.Locacoes.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task ComCaucao_FalhaDoProvedor502_RegistradaPeloCatchGenericoEPeloMiddleware()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Gateway.ExcecaoPreAutorizacao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao);

            Assert.Equal(HttpStatusCode.BadGateway, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroCaucaoProvedorIndisponivel, await Detalhe(resposta));
            Assert.Empty(fabrica.Gateway.Cancelamentos);
            List<string> entradas = fabrica.Log.Snapshot();
            Assert.Contains(ConstantsMessage.ErroSalvarLocacao + " | " + ConstantsMessage.ErroCaucaoProvedorIndisponivel, entradas);
            Assert.Contains(ConstantsMessage.ErroCaucaoProvedorIndisponivel + " | " + ConstantsMessage.ErroCaucaoProvedorIndisponivel, entradas);
        }

        [Fact]
        public async Task ComCaucao_ReservaInexistente404_SemGateway()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), CorpoComCartao.Replace("\"idReserva\":10", "\"idReserva\":999"));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, await Detalhe(resposta));
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        /// <summary>v1.1, CE-1: campos a mais no corpo (valor, locadora, e-mail, CPF) sao ignorados; vale a configuracao e o banco.</summary>
        [Fact]
        public async Task ComCaucao_CorpoComCamposAMais_ValorDaConfiguracaoEPagadorDoBanco()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();
            string corpo = "{\"idReserva\":10,\"valorCaucao\":1,\"idEmpresa\":99,\"email\":\"outro@exemplo.com\",\"cpf\":\"11122233344\","
                + "\"caucaoTokenCartao\":\"FAKE-tok-http-0001\",\"caucaoMetodoPagamentoId\":\"visa\"}";

            using HttpResponseMessage resposta = await Enviar(cliente, RotaAbertura, Token(fabrica), corpo);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            CaucaoSolicitacao solicitacao = Assert.Single(fabrica.Gateway.Solicitacoes);
            Assert.Equal(1200.00m, solicitacao.Valor);
            Assert.Equal(EmailCliente, solicitacao.Pagador.Email);
            Assert.Equal(CpfCliente, solicitacao.Pagador.Documento);
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(1200m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.Equal(40, dados.GetProperty("idVeiculo").GetInt32());
        }

        /// <summary>v1.1, CE-1: a politica condicional so vale na acao do POST; a leitura da retirada continua aberta a qualquer cargo.</summary>
        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        public async Task ComCaucao_GetDaRetiradaContinua200ParaOutrosCargos(CargoFuncionario cargo)
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaRetirada, Token(fabrica), null);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement dados = await LerJson(resposta);
            Assert.True(dados.GetProperty("caucaoExigida").GetBoolean());
        }

        // --- Leitura da retirada (GET) com a caucao: correcao f ---

        /// <summary>Com 1200.00, os cinco campos da caucao em camelCase; o gateway e consultado uma vez e nada dele sai na resposta.</summary>
        [Fact]
        public async Task ComCaucao_GetDaRetirada_CincoCamposEmCamelCase_SemDadoDoProvedor()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaRetirada, Token(fabrica), null);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            string corpo = await resposta.Content.ReadAsStringAsync();
            JsonElement dados = await LerJson(resposta);
            Assert.True(dados.GetProperty("caucaoExigida").GetBoolean());
            Assert.Equal(1200m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.Equal(StaticMethods.FormateToMonetary(1200.00m), dados.GetProperty("valorCaucaoStr").GetString());
            Assert.True(dados.GetProperty("caucaoDisponivel").GetBoolean());
            Assert.True(dados.GetProperty("caucaoModoSimulacao").GetBoolean());
            Assert.Equal(1, fabrica.Gateway.ChamadasConfiguracao);
            Assert.Equal(1, fabrica.Gateway.TotalDeChamadas);
            Assert.DoesNotContain(ConstantsCaucao.ChavePublicaFake, corpo);
            Assert.DoesNotContain("chavePublica", corpo);
            Assert.DoesNotContain("provedor", corpo);
            Assert.DoesNotContain("idTransacao", corpo);
        }

        /// <summary>Com 1200.00 e a integracao quebrada: continua 200, com a caucao exigida e indisponivel, e o registro so com a reserva.</summary>
        [Fact]
        public async Task ComCaucao_GetDaRetiradaComIntegracaoQuebrada_200ComCaucaoIndisponivel()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(ValorDaApresentacao);
            fabrica.Gateway.ExcecaoConfiguracao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorNaoSuportado);
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Enviar(cliente, RotaRetirada, Token(fabrica), null);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement dados = await LerJson(resposta);
            Assert.True(dados.GetProperty("caucaoExigida").GetBoolean());
            Assert.Equal(1200m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.False(dados.GetProperty("caucaoDisponivel").GetBoolean());
            Assert.False(dados.GetProperty("caucaoModoSimulacao").GetBoolean());
            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaConfiguracaoIndisponivel, 10);
            Assert.Contains(fabrica.Log.Snapshot(), (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
        }

        /// <summary>Com o valor 0, a integracao quebrada nao muda nada: a mesma resposta da integracao boa, com os campos neutros e sem consulta.</summary>
        [Fact]
        public async Task ValorZero_GetDaRetiradaIgualComAIntegracaoQuebrada_CamposNeutrosSemConsulta()
        {
            using AberturaWebFactory fabrica = new AberturaWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);

            using HttpResponseMessage comIntegracaoBoa = await Enviar(cliente, RotaRetirada, token, null);
            fabrica.Gateway.ExcecaoConfiguracao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorNaoSuportado);
            using HttpResponseMessage comIntegracaoQuebrada = await Enviar(cliente, RotaRetirada, token, null);

            Assert.Equal(HttpStatusCode.OK, comIntegracaoBoa.StatusCode);
            Assert.Equal(HttpStatusCode.OK, comIntegracaoQuebrada.StatusCode);
            string esperado = await comIntegracaoBoa.Content.ReadAsStringAsync();
            Assert.Equal(esperado, await comIntegracaoQuebrada.Content.ReadAsStringAsync());
            JsonElement dados = await LerJson(comIntegracaoQuebrada);
            Assert.False(dados.GetProperty("caucaoExigida").GetBoolean());
            Assert.Equal(0m, dados.GetProperty("valorCaucao").GetDecimal());
            Assert.Equal(StaticMethods.FormateToMonetary(0m), dados.GetProperty("valorCaucaoStr").GetString());
            Assert.False(dados.GetProperty("caucaoDisponivel").GetBoolean());
            Assert.False(dados.GetProperty("caucaoModoSimulacao").GetBoolean());
            Assert.Equal(0, fabrica.Gateway.TotalDeChamadas);
        }

        /// <summary>
        /// Servidor de teste da abertura com caucao: repositorio de locacao, gateway da caucao, log, documentos e armazenamento
        /// em memoria. O valor da caucao e o teto vem da configuracao em memoria (null = nao informa a chave).
        /// </summary>
        private sealed class AberturaWebFactory : VeiculoWebFactory
        {
            private readonly string? _valorCaucao;
            private readonly string? _limiteCaucao;

            public AberturaWebFactory(string? valorCaucao, string? limiteCaucao = null)
            {
                _valorCaucao = valorCaucao;
                _limiteCaucao = limiteCaucao;

                Cliente cliente = new Cliente
                {
                    IdCliente = 30, Nome = "Maria da Silva", Cpf = CpfCliente, Email = EmailCliente, CnhNumero = "12345678901",
                    CnhCategoria = CategoriaCnh.B, CnhValidade = new DateOnly(2030, 1, 1),
                    StatusCadastro = StatusCadastroCliente.APROVADO, SenhaHash = "segredo-interno"
                };
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 1, IdCliente = 30, IdEmpresa = 1, Status = StatusCadastroCliente.APROVADO });
                Reserva reserva = new Reserva
                {
                    IdReserva = 10, IdCliente = 30, Cliente = cliente, IdVeiculo = 40,
                    DataRetiradaPrevista = new DateTime(2026, 11, 24, 9, 30, 0),
                    DataDevolucaoPrevista = new DateTime(2026, 11, 27, 9, 30, 0),
                    ValorEstimado = 600m, Status = StatusReserva.CONFIRMADA,
                    Veiculo = new Veiculo
                    {
                        IdVeiculo = 40, IdEmpresa = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", KmAtual = 1500, ValorDiaria = 200m,
                        Status = StatusVeiculo.RESERVADO, AnoModelo = 2024, IdCategoria = 3,
                        Categoria = new Categoria { IdCategoria = 3, Nome = "SUV" },
                        Empresa = new Empresa { IdEmpresa = 1, NomeFantasia = "Locadora Teste", Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "Campinas", Uf = "SP" }
                    }
                };
                Locacoes.Adicionar(reserva);
                Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
            }

            public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();

            public FakeCaucaoGateway Gateway { get; } = new FakeCaucaoGateway();

            public FakeLogService Log { get; } = new FakeLogService();

            public FakeClienteDocumentoRepository Documentos { get; } = new FakeClienteDocumentoRepository();

            public FakeArmazenamentoGateway Armazenamento { get; } = new FakeArmazenamentoGateway();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((contexto, configuracao) =>
                {
                    Dictionary<string, string?> valores = new Dictionary<string, string?>();

                    if (_valorCaucao != null)
                    {
                        valores[ConstantsValues.ChaveValorCaucao] = _valorCaucao;
                    }

                    if (_limiteCaucao != null)
                    {
                        valores[ConstantsValues.ChaveLimiteCaucaoPorFuncionario] = _limiteCaucao;
                    }

                    configuracao.AddInMemoryCollection(valores);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ILocacaoRepository>();
                    services.RemoveAll<ICaucaoGateway>();
                    services.RemoveAll<ILogService>();
                    services.RemoveAll<IClienteDocumentoRepository>();
                    services.RemoveAll<IArmazenamentoGateway>();
                    services.AddSingleton<ILocacaoRepository>(Locacoes);
                    services.AddSingleton<ICaucaoGateway>(Gateway);
                    services.AddSingleton<ILogService>(Log);
                    services.AddSingleton<IClienteDocumentoRepository>(Documentos);
                    services.AddSingleton<IArmazenamentoGateway>(Armazenamento);
                });
            }
        }
    }
}
