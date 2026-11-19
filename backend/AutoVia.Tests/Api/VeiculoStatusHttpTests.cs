using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
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
    /// Testes HTTP da troca manual de status (PATCH /api/veiculos/{id}/status, P1-BE-04) com o servidor em memoria e os
    /// repositorios e a conferencia de sessao trocados por fakes. Cobre 401 sem token; 403 para cliente e para cargos fora
    /// de GESTOR_FROTA e ADMINISTRADOR; 204 sem corpo para os dois cargos permitidos; 400 de texto invalido, numero JSON,
    /// status ausente e corpo vazio; campo a mais ignorado; 404 de veiculo inexistente ou de outra locadora; 409 de cada tipo;
    /// e mesmo status sem mudar UpdatedAt; 415 sem Content-Type JSON; 400 de JSON quebrado; 404 de veiculo desativado;
    /// e 500 com o detail ErroInterno, sem o texto da falha. Nenhum PostgreSQL e acessado.
    /// </summary>
    public class VeiculoStatusHttpTests : IDisposable
    {
        private const string RotaVeiculos = "/api/veiculos";
        private const string MediaProblema = "application/problem+json";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string TipoJson = "application/json";
        private const int IdEmpresaDoTeste = 1;
        private const int IdEmpresaAlheia = 2;
        private const int IdVeiculoInexistente = 999;
        private const string CorpoMANUTENCAO = "{\"status\":\"MANUTENCAO\"}";
        private const string SeisNomes = "DISPONIVEL, RESERVADO, LOCADO, MANUTENCAO, VENDIDO, BAIXADO";
        private static readonly DateTime MomentoFixo = new DateTime(2020, 1, 1, 10, 0, 0);

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public VeiculoStatusHttpTests()
        {
            _fabrica = new VeiculoWebFactory();
            _fabrica.Veiculos.IdEmpresaDoContexto = IdEmpresaDoTeste;
            DefinirCargo(CargoFuncionario.GESTOR_FROTA);
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        [Fact]
        public async Task SemToken_Devolve401ProblemJson()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, null, CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403Problem()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            string tokenCliente = GerarToken(PerfilUsuario.CLIENTE, null);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, tokenCliente, CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task CargoSemPermissao_Devolve403Problem(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, GerarToken(PerfilUsuario.FUNCIONARIO, cargo), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministrador_Devolve204SemCorpoEGrava(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            string corpo = await resposta.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, corpo);
            Assert.Equal(StatusVeiculo.MANUTENCAO, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>
        /// Token diz GESTOR_FROTA, mas o cargo do banco (sessao) e ATENDENTE: a politica usa o cargo do banco, entao 403.
        /// </summary>
        [Fact]
        public async Task CargoRebaixadoNoBanco_ComTokenAntigo_Devolve403()
        {
            DefinirCargo(CargoFuncionario.ATENDENTE);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>
        /// Token diz ATENDENTE, mas o cargo do banco (sessao) e GESTOR_FROTA: a politica usa o cargo do banco, entao 204.
        /// </summary>
        [Fact]
        public async Task CargoPromovidoNoBanco_ComTokenAntigo_Devolve204()
        {
            DefinirCargo(CargoFuncionario.GESTOR_FROTA);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(StatusVeiculo.MANUTENCAO, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>
        /// Falha inesperada no repositorio: a resposta e 500 em ProblemDetails com o detail ErroInterno (padrao do
        /// ErrorHandlingMiddleware). O texto de ErroAlterarStatusVeiculo e a excecao original ficam so no log, fora da resposta.
        /// </summary>
        [Fact]
        public async Task FalhaInesperada_Devolve500ProblemJson()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            _fabrica.Veiculos.ExcecaoAoAlterarStatus = new InvalidOperationException("falha tecnica de teste");

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(ConstantsMessage.ErroInterno, documento.RootElement.GetProperty("detail").GetString());
            Assert.DoesNotContain(ConstantsMessage.ErroAlterarStatusVeiculo, texto);
            Assert.DoesNotContain("falha tecnica de teste", texto);
        }

        /// <summary>
        /// Volta da manutencao (MANUTENCAO para DISPONIVEL) com ordem aberta: 409 com ErroVeiculoStatusComOrdemAberta, e o
        /// status segue MANUTENCAO (decisao 1 de 19/11/2026: quem libera o veiculo e o encerramento da ordem).
        /// </summary>
        [Fact]
        public async Task VoltaDaManutencaoComOrdemAberta_Devolve409ComAMensagemNova()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            _fabrica.Veiculos.ManutencoesDoVeiculo.Add(new Manutencao { IdVeiculo = veiculo.IdVeiculo, DataSaida = null });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":\"DISPONIVEL\"}"));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoStatusComOrdemAberta, corpo.GetProperty("detail").GetString());
            Assert.Equal(StatusVeiculo.MANUTENCAO, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>Volta da manutencao sem ordem aberta: 204 e grava DISPONIVEL.</summary>
        [Fact]
        public async Task VoltaDaManutencaoSemOrdemAberta_Devolve204EGravaDisponivel()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":\"DISPONIVEL\"}"));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Fact]
        public async Task TextoQueNaoEUmNome_Devolve400ComOsSeisNomes()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":\"XYZ\"}"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, SeisNomes), corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task NumeroJsonNoLugarDoTexto_Devolve400Problem()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":3}"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>
        /// Corpo vazio com Content-Type JSON: o binder recusa e a resposta e o 400 do projeto. (Sem Content-Type, o ASP.NET
        /// responde 415 antes da action; isso vale para todas as rotas com corpo e nao e tratado aqui.)
        /// </summary>
        [Fact]
        public async Task CorpoVazio_Devolve400Problem()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), string.Empty));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>
        /// Corpo sem Content-Type JSON (text/plain): o ASP.NET responde 415 antes da action, como em todas as rotas com corpo
        /// do projeto. Registrado no contrato veiculo-status v1 (correcao de 19/11/2026).
        /// </summary>
        [Fact]
        public async Task CorpoSemContentTypeJson_Devolve415()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            HttpRequestMessage requisicao = Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), null);
            requisicao.Content = new StringContent(CorpoMANUTENCAO, Encoding.UTF8, "text/plain");

            HttpResponseMessage resposta = await _cliente.SendAsync(requisicao);

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, resposta.StatusCode);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>JSON quebrado com Content-Type JSON: o binder recusa e a resposta e 400 em application/problem+json.</summary>
        [Fact]
        public async Task JsonQuebrado_Devolve400ProblemJson()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Fact]
        public async Task StatusAusente_Devolve400ComMensagemObrigatoria()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{}"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoStatusObrigatorio, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task CampoAMais_EIgnoradoEDevolve204()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), "{\"status\":\"MANUTENCAO\",\"outro\":1}"));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        }

        [Fact]
        public async Task VeiculoInexistente_Devolve404ProblemJson()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(IdVeiculoInexistente, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task VeiculoDeOutraLocadora_Devolve404SemMudar()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaAlheia);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        /// <summary>Veiculo desativado (DisabledAt preenchido) conta como inexistente: 404 em ProblemDetails, sem mudar o status.</summary>
        [Fact]
        public async Task VeiculoDesativado_Devolve404ProblemJsonSemMudar()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste, null, MomentoFixo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, corpo.GetProperty("detail").GetString());
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Theory]
        [InlineData(StatusVeiculo.DISPONIVEL, "RESERVADO", false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, "LOCADO", false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, "VENDIDO", false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, "MANUTENCAO", false, true, ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto)]
        [InlineData(StatusVeiculo.DISPONIVEL, "MANUTENCAO", true, false, ConstantsMessage.ErroVeiculoStatusComReservaEmAberto)]
        [InlineData(StatusVeiculo.VENDIDO, "MANUTENCAO", false, false, ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado)]
        [InlineData(StatusVeiculo.LOCADO, "DISPONIVEL", false, false, ConstantsMessage.ErroVeiculoStatusOrigemPorFluxo)]
        public async Task RegraRecusa_Devolve409ProblemComAMensagemDoContrato(StatusVeiculo origem, string destino, bool reserva, bool locacao, string mensagem)
        {
            Veiculo veiculo = SemearVeiculo(origem, IdEmpresaDoTeste);

            if (reserva)
            {
                _fabrica.Veiculos.ReservasDoVeiculo.Add(new Reserva { IdVeiculo = veiculo.IdVeiculo, Status = StatusReserva.ATIVA });
            }

            if (locacao)
            {
                _fabrica.Veiculos.LocacoesDoVeiculo.Add(new Locacao { IdVeiculo = veiculo.IdVeiculo, Status = StatusLocacao.EM_ANDAMENTO });
            }

            string corpoJson = $"{{\"status\":\"{destino}\"}}";

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), corpoJson));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(mensagem, corpo.GetProperty("detail").GetString());
            Assert.Equal(origem, _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Fact]
        public async Task MesmoStatusManual_Devolve204SemMudarUpdatedAt()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste, MomentoFixo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(veiculo.IdVeiculo, TokenDeFuncionario(), CorpoMANUTENCAO));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Veiculo depois = _fabrica.Veiculos.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo);
            Assert.Equal(StatusVeiculo.MANUTENCAO, depois.Status);
            Assert.Equal(MomentoFixo, depois.UpdatedAt);
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor,
                Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            string token = tokens.GerarToken(perfil, 1, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, IdEmpresaDoTeste).Token;

            return token;
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.GESTOR_FROTA);
        }

        /// <summary>Troca a situacao que a conferencia de sessao devolve: cargo do funcionario na locadora do teste.</summary>
        private void DefinirCargo(CargoFuncionario cargo)
        {
            _fabrica.Sessao.Situacao = new SessaoSituacaoVO
            {
                Ativo = true,
                IdEmpresa = IdEmpresaDoTeste,
                Cargo = cargo,
                EmpresaAtiva = true,
                EmpresaOperadora = false
            };
        }

        private Veiculo SemearVeiculo(StatusVeiculo status, int idEmpresa, DateTime? atualizadoEm = null, DateTime? desativadoEm = null)
        {
            DateTime momento = atualizadoEm ?? DateTime.Now;
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = _fabrica.CategoriaSuv.IdCategoria,
                Categoria = _fabrica.CategoriaSuv,
                IdEmpresa = idEmpresa,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m,
                Status = status,
                CreatedAt = momento,
                UpdatedAt = momento,
                DisabledAt = desativadoEm
            };

            _fabrica.Veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static HttpRequestMessage Requisicao(int idVeiculo, string? token, string? corpoJson)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Patch, $"{RotaVeiculos}/{idVeiculo}/status");

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            if (corpoJson != null)
            {
                requisicao.Content = new StringContent(corpoJson, Encoding.UTF8, TipoJson);
            }

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
