using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.ViewObjects.Manutencao;
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
    /// Testes HTTP da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03) com o servidor em memoria, o repositorio de manutencao
    /// trocado por FakeManutencaoRepository, a sessao por FakeSessaoService e o relogio fixo em 01/01/2020. Cobre 401 sem
    /// token; 403 para cliente e para cargos fora de GESTOR_FROTA e ADMINISTRADOR; 201 na abertura; 200 no encerramento;
    /// 400 (tipo, data futura, data de saida anterior, km menor, km ausente); 404; 409 (ordem aberta, reserva); e 200 nos
    /// alertas. Nenhum PostgreSQL e acessado.
    /// </summary>
    public class ManutencaoHttpTests : IDisposable
    {
        private const string RotaManutencoes = "/api/manutencoes";
        private const string RotaAlertas = "/api/manutencoes/alertas";
        private const string RotaResumo = "/api/manutencoes/resumo";
        private const string MediaProblema = "application/problem+json";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string TipoJson = "application/json";
        private const string TipoCorretiva = "CORRETIVA";
        private const string TipoPreventiva = "PREVENTIVA";
        private const string PlacaDoTeste = "ABC1D23";
        private const string ChassiDoTeste = "9BWZZZ377VT004251";
        private const string RenavamDoTeste = "12345678901";
        private const int IdEmpresaDoTeste = 1;
        private const int IdEmpresaAlheia = 2;
        private const int IdVeiculoInexistente = 999;
        private const int KmDoVeiculo = 4500;
        private const string NomeCampoKm = "KmAtual";

        private static readonly DateTime MomentoFixo = new DateTime(2020, 1, 1, 10, 0, 0);

        private readonly ManutencaoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public ManutencaoHttpTests()
        {
            _fabrica = new ManutencaoWebFactory();
            _fabrica.Manutencoes.IdEmpresaDoContexto = IdEmpresaDoTeste;
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

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, null, AberturaValida(veiculo.IdVeiculo)));

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

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, tokenCliente, AberturaValida(veiculo.IdVeiculo)));

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

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, GerarToken(PerfilUsuario.FUNCIONARIO, cargo), AberturaValida(veiculo.IdVeiculo)));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Manutencoes.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministrador_Abrir_Devolve201ComAOrdemEVeiculoEmManutencao(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), AberturaValida(veiculo.IdVeiculo)));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("idManutencao").GetInt32() > 0);
            Assert.Equal(PlacaDoTeste, corpo.GetProperty("placa").GetString());
            Assert.Equal(TipoCorretiva, corpo.GetProperty("tipo").GetString());
            Assert.Equal(KmDoVeiculo, corpo.GetProperty("kmRegistrado").GetInt32());
            Assert.True(corpo.GetProperty("aberta").GetBoolean());
            Assert.Equal(StatusVeiculo.MANUTENCAO, _fabrica.Manutencoes.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Fact]
        public async Task AbrirComTipoInvalido_Devolve400ComAMensagemDoValidador()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            AbrirManutencaoVO vo = AberturaValida(veiculo.IdVeiculo);
            vo.Tipo = "preventiva";

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoTipoInvalido, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task AbrirComDataDeEntradaFutura_Devolve400()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            AbrirManutencaoVO vo = AberturaValida(veiculo.IdVeiculo);
            vo.DataEntrada = DateOnly.FromDateTime(MomentoFixo).AddDays(1);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoDataEntradaFutura, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task AbrirVeiculoDeOutraLocadora_Devolve404()
        {
            Veiculo alheio = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaAlheia);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), AberturaValida(alheio.IdVeiculo)));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task AbrirVeiculoInexistente_Devolve404()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), AberturaValida(IdVeiculoInexistente)));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task AbrirComOrdemJaAberta_Devolve409()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            _fabrica.Manutencoes.SemearManutencao(new Manutencao { IdVeiculo = veiculo.IdVeiculo, Veiculo = veiculo, Tipo = TipoManutencao.CORRETIVA, DataEntrada = DateOnly.FromDateTime(MomentoFixo), KmRegistrado = KmDoVeiculo });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), AberturaValida(veiculo.IdVeiculo)));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoJaAberta, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task AbrirComReservaEmAberto_Devolve409()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            _fabrica.Manutencoes.ReservasDoVeiculo.Add(new Reserva { IdVeiculo = veiculo.IdVeiculo, Status = StatusReserva.CONFIRMADA });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaManutencoes, TokenDeFuncionario(), AberturaValida(veiculo.IdVeiculo)));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoStatusComReservaEmAberto, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Encerrar_OrdemAberta_Devolve200ComAOrdemFechada()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            Manutencao ordem = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{ordem.IdManutencao}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = 5000, Custo = 350.75m }));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.False(corpo.GetProperty("aberta").GetBoolean());
            Assert.Equal(DateOnly.FromDateTime(MomentoFixo).ToString("yyyy-MM-dd"), corpo.GetProperty("dataSaida").GetString());
            Assert.Equal(KmDoVeiculo, corpo.GetProperty("kmRegistrado").GetInt32());
            Assert.Equal(StatusVeiculo.DISPONIVEL, _fabrica.Manutencoes.Veiculos.Single(x => x.IdVeiculo == veiculo.IdVeiculo).Status);
        }

        [Fact]
        public async Task Encerrar_OrdemInexistente_Devolve404()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{IdVeiculoInexistente}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = 5000 }));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoNaoEncontrada, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Encerrar_JaEncerrada_Devolve409()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.DISPONIVEL, IdEmpresaDoTeste);
            Manutencao ordem = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));
            ordem.DataSaida = DateOnly.FromDateTime(MomentoFixo).AddDays(-1);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{ordem.IdManutencao}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = 5000 }));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoJaEncerrada, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Encerrar_DataDeSaidaAnterior_Devolve400()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            Manutencao ordem = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{ordem.IdManutencao}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = 5000, DataSaida = DateOnly.FromDateTime(MomentoFixo).AddDays(-4) }));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoDataSaidaAnterior, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Encerrar_KmMenorQueOAtual_Devolve400ComOKmDoVeiculo()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            Manutencao ordem = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{ordem.IdManutencao}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = KmDoVeiculo - 1 }));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(string.Format(ConstantsMessage.ErroManutencaoKmMenor, KmDoVeiculo), corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Encerrar_SemKm_Devolve400ComAMensagemDoKm()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            Manutencao ordem = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Patch, $"{RotaManutencoes}/{ordem.IdManutencao}/encerrar", TokenDeFuncionario(), new EncerrarManutencaoVO { KmAtual = null }));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoKmObrigatorio, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Alertas_Devolve200ComTotalEItens()
        {
            _fabrica.Manutencoes.ItensAlertas.Add(new VeiculoRevisaoRegistro(1, PlacaDoTeste, "Fiat", "Toro", "SUV", 14600, 4500));
            _fabrica.Manutencoes.TotalAlertas = 1;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAlertas, TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(1, corpo.GetProperty("total").GetInt32());
            Assert.Equal(ConstantsValues.IntervaloRevisaoKm, corpo.GetProperty("intervaloRevisaoKm").GetInt32());
            JsonElement item = corpo.GetProperty("itens")[0];
            Assert.Equal(PlacaDoTeste, item.GetProperty("placa").GetString());
            Assert.Equal(-100, item.GetProperty("faltamKm").GetInt32());
            Assert.Equal(ConstantsValues.SituacaoRevisaoVencida, item.GetProperty("situacao").GetString());
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministrador_Listar_Devolve200ComPaginaETotal(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));
            SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-1));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaManutencoes}?pagina=1&tamanhoPagina=10", TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(2, corpo.GetProperty("total").GetInt32());
            Assert.Equal(1, corpo.GetProperty("pagina").GetInt32());
            Assert.Equal(10, corpo.GetProperty("tamanhoPagina").GetInt32());
            JsonElement itens = corpo.GetProperty("itens");
            Assert.Equal(2, itens.GetArrayLength());
            Assert.Equal(PlacaDoTeste, itens[0].GetProperty("placa").GetString());
            Assert.Equal(TipoCorretiva, itens[0].GetProperty("tipo").GetString());
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task CargoSemPermissao_Listar_Devolve403Problem(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaManutencoes, GerarToken(PerfilUsuario.FUNCIONARIO, cargo), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Listar_SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaManutencoes, null, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Listar_TamanhoDePaginaAcimaDoLimite_Devolve400ComAFaixa()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaManutencoes}?tamanhoPagina=101", TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo), corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Listar_SituacaoInvalida_Devolve400()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaManutencoes}?situacao=XYZ", TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroManutencaoSituacaoInvalida, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Listar_FiltroDeSituacaoEDeVeiculoChegaAoRepositorio()
        {
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));
            Manutencao encerrada = SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-5));
            encerrada.DataSaida = DateOnly.FromDateTime(MomentoFixo).AddDays(-4);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaManutencoes}?situacao=ABERTA&idVeiculo={veiculo.IdVeiculo}", TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(1, corpo.GetProperty("total").GetInt32());
            Assert.True(corpo.GetProperty("itens")[0].GetProperty("aberta").GetBoolean());
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministrador_Resumo_Devolve200ComOMesPadrao(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);
            Veiculo veiculo = SemearVeiculo(StatusVeiculo.MANUTENCAO, IdEmpresaDoTeste);
            SemearOrdem(veiculo, DateOnly.FromDateTime(MomentoFixo).AddDays(-3));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaResumo, TokenDeFuncionario(), null));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(1, corpo.GetProperty("ordensAbertas").GetInt32());
            Assert.Equal("2020-01-01", corpo.GetProperty("periodoInicio").GetString());
            Assert.Equal("2020-01-31", corpo.GetProperty("periodoFim").GetString());
            Assert.Equal("01/01/2020", corpo.GetProperty("periodoInicioStr").GetString());
            Assert.Equal(0m, corpo.GetProperty("custoPeriodo").GetDecimal());
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        public async Task CargoSemPermissao_Resumo_Devolve403Problem(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaResumo, GerarToken(PerfilUsuario.FUNCIONARIO, cargo), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Resumo_SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaResumo, null, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Alertas_SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAlertas, null, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Alertas_AtendenteSemPermissao_Devolve403()
        {
            DefinirCargo(CargoFuncionario.ATENDENTE);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaAlertas, GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE), null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

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

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = ManutencaoWebFactory.Emissor,
                Audiencia = ManutencaoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, 1, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, 1).Token;
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.GESTOR_FROTA);
        }

        private Veiculo SemearVeiculo(StatusVeiculo status, int idEmpresa)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = 1,
                IdEmpresa = idEmpresa,
                Placa = PlacaDoTeste,
                Chassi = ChassiDoTeste,
                Renavam = RenavamDoTeste,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = KmDoVeiculo,
                ValorDiaria = 150m,
                Status = status,
                CreatedAt = MomentoFixo,
                UpdatedAt = MomentoFixo
            };

            _fabrica.Manutencoes.SemearVeiculo(veiculo);

            return veiculo;
        }

        private Manutencao SemearOrdem(Veiculo veiculo, DateOnly dataEntrada)
        {
            Manutencao ordem = new Manutencao
            {
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                Tipo = TipoManutencao.CORRETIVA,
                DataEntrada = dataEntrada,
                KmRegistrado = KmDoVeiculo,
                CreatedAt = MomentoFixo,
                UpdatedAt = MomentoFixo
            };

            return _fabrica.Manutencoes.SemearManutencao(ordem);
        }

        private static AbrirManutencaoVO AberturaValida(int idVeiculo)
        {
            return new AbrirManutencaoVO { IdVeiculo = idVeiculo, Tipo = TipoCorretiva, Oficina = "Oficina Centro", Descricao = "Revisao geral" };
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

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }

    /// <summary>
    /// Servidor de teste da manutencao: configuracao em memoria (chave JWT gerada a cada teste), repositorio de manutencao
    /// trocado por FakeManutencaoRepository, sessao por FakeSessaoService e relogio fixo em 01/01/2020. Nao acessa o PostgreSQL.
    /// </summary>
    public class ManutencaoWebFactory : WebApplicationFactory<Program>
    {
        public const string Emissor = "AutoVia.Api";
        public const string Audiencia = "AutoVia.Web";

        public ManutencaoWebFactory()
        {
            ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Manutencoes = new FakeManutencaoRepository();
        }

        /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada instancia).</summary>
        public string ChaveJwt { get; }

        /// <summary>Repositorio de manutencao em memoria; os testes semeiam veiculos e ordens por aqui.</summary>
        public FakeManutencaoRepository Manutencoes { get; }

        /// <summary>Conferencia de sessao simulada: os testes trocam o cargo do funcionario.</summary>
        public FakeSessaoService Sessao { get; } = new FakeSessaoService();

        /// <summary>Armazenamento de mentira: a capa da lista e dos alertas nao sai do servidor de teste (sem bucket, sem disco).</summary>
        public FakeArmazenamentoGateway Armazenamento { get; } = new FakeArmazenamentoGateway();

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
                services.RemoveAll<IManutencaoRepository>();
                services.AddSingleton<IManutencaoRepository>(Manutencoes);
                services.RemoveAll<IRelogio>();
                services.AddSingleton<IRelogio>(new FakeRelogio(new DateTime(2020, 1, 1, 10, 0, 0)));
                services.RemoveAll<ISessaoService>();
                services.AddSingleton<ISessaoService>(Sessao);
                services.RemoveAll<IArmazenamentoGateway>();
                services.AddSingleton<IArmazenamentoGateway>(Armazenamento);
            });
        }
    }
}
