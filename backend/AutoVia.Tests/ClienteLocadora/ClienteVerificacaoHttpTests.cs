using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Api;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Testes HTTP da fila, do detalhe e dos documentos da equipe (P2-BE-05) no pipeline real: 401 sem token, 403 para token de
    /// cliente e para cargo FINANCEIRO, 200 para ATENDENTE, 400 para filtro invalido, 404 para cliente sem vinculo com a
    /// locadora do funcionario (igual ao inexistente). GET /api/clientes/verificacoes cai na fila (rota literal, nao no
    /// GET {id}). Sessao e vinculos sao os do VerificacaoWebFactory.
    /// </summary>
    public class ClienteVerificacaoHttpTests
    {
        private const string MediaProblema = "application/problem+json";
        private const string RotaFila = "fila";
        private const string RotaDetalhe = "detalhe";
        private const string RotaDocumentos = "documentos";
        private const string CaminhoFila = "/api/clientes/verificacoes";
        private const string SufixoDetalhe = "/verificacao";
        private const string SufixoDocumentos = "/verificacao/documentos";
        private const string PropriedadeItens = "itens";
        private const string PropriedadeTotal = "total";
        private const string PropriedadeNome = "nome";
        private const string PropriedadeCpf = "cpf";
        private const string PropriedadeStatus = "status";
        private const string PropriedadeEmail = "email";
        private const string PropriedadeNomeArquivo = "nomeArquivo";
        private const string PropriedadeUrl = "url";
        private const string PropriedadeSenhaHash = "senhaHash";
        private const string PropriedadeGoogleSub = "googleSub";
        private const string StatusPendente = "PENDENTE";
        private const string ChaveDocumento = "clientes/1/documentos/cnh-teste.jpg";
        private const string NomeArquivoCnh = "cnh-teste.jpg";
        private const string TipoConteudoJpeg = "image/jpeg";
        private const string NomeClienteTeste = "Cliente";
        private const string BuscaLonga = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string StatusInvalido = "pendente";
        private const string ParametroStatus = "?status=";
        private const string ParametroBusca = "?busca=";
        private const string ParametroStatusVazio = "?status=";
        private const int QuantidadeEsperadaFilaSemFiltro = 1;
        private const int TamanhoDocumentoTeste = 10;
        private const int QuantidadeEsperadaAssinatura = 1;

        private static string Caminho(VerificacaoWebFactory fabrica, string rota)
        {
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string id = idCliente.ToString(CultureInfo.InvariantCulture);

            return rota switch
            {
                RotaFila => CaminhoFila,
                RotaDetalhe => "/api/clientes/" + id + SufixoDetalhe,
                _ => "/api/clientes/" + id + SufixoDocumentos
            };
        }

        private static HttpRequestMessage Requisicao(string caminho, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        [Theory]
        [InlineData(RotaFila)]
        [InlineData(RotaDetalhe)]
        [InlineData(RotaDocumentos)]
        public async Task SemToken_Devolve401(string rota)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, rota), null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Theory]
        [InlineData(RotaFila)]
        [InlineData(RotaDetalhe)]
        [InlineData(RotaDocumentos)]
        public async Task TokenDeCliente_Devolve403(string rota)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, rota), token));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData(RotaFila)]
        [InlineData(RotaDetalhe)]
        [InlineData(RotaDocumentos)]
        public async Task CargoFinanceiro_Devolve403(string rota)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Financeiro);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, rota), token));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Fila_ComAtendente_Devolve200ComAPaginaESemSegredos()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(CaminhoFila, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            JsonElement raiz = documento.RootElement;
            Assert.Equal(1, raiz.GetProperty(PropriedadeTotal).GetInt32());
            JsonElement item = Assert.Single(raiz.GetProperty(PropriedadeItens).EnumerateArray().ToList());
            Assert.Equal(clienteSemente.Nome, item.GetProperty(PropriedadeNome).GetString());
            Assert.Equal(StaticMethods.FormateCpf(clienteSemente.Cpf), item.GetProperty(PropriedadeCpf).GetString());
            Assert.Equal(StatusPendente, item.GetProperty(PropriedadeStatus).GetString());
            Assert.False(item.TryGetProperty(PropriedadeSenhaHash, out JsonElement _));
            Assert.False(item.TryGetProperty(PropriedadeGoogleSub, out JsonElement _));
        }

        [Theory]
        [InlineData(ParametroStatus + StatusInvalido)]
        [InlineData(ParametroStatus + "1")]
        [InlineData(ParametroBusca + BuscaLonga)]
        public async Task Fila_FiltroInvalido_Devolve400(string query)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(CaminhoFila + query, token));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Detalhe_ComVinculoDaLocadora_Devolve200()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, RotaDetalhe), token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(clienteSemente.Email, documento.RootElement.GetProperty(PropriedadeEmail).GetString());
            Assert.False(documento.RootElement.TryGetProperty(PropriedadeSenhaHash, out JsonElement _));
        }

        [Fact]
        public async Task Detalhe_SemVinculoNaLocadora_Devolve404()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.OutraLocadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, RotaDetalhe), token));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Documentos_SemVinculo_Devolve404SemLerOArmazenamento()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.OutraLocadora.IdEmpresa, StatusCadastroCliente.PENDENTE);
            fabrica.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = clienteSemente.IdCliente, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumento, NomeArquivo = NomeArquivoCnh, TipoConteudo = TipoConteudoJpeg, TamanhoBytes = TamanhoDocumentoTeste });

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, RotaDocumentos), token));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoDocumentos.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Documentos_ComVinculoEDocumento_Devolve200ComLinkTemporario()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);
            fabrica.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = clienteSemente.IdCliente, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumento, NomeArquivo = NomeArquivoCnh, TipoConteudo = TipoConteudoJpeg, TamanhoBytes = TamanhoDocumentoTeste });

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, RotaDocumentos), token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            JsonElement item = Assert.Single(documento.RootElement.EnumerateArray().ToList());
            Assert.Equal(NomeArquivoCnh, item.GetProperty(PropriedadeNomeArquivo).GetString());
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty(PropriedadeUrl).GetString()));
            Assert.Equal(QuantidadeEsperadaAssinatura, fabrica.ArmazenamentoDocumentos.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Documentos_SemDocumento_Devolve200ComListaVaziaSemArmazenamento()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, RotaDocumentos), token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Empty(documento.RootElement.EnumerateArray().ToList());
            Assert.Equal(0, fabrica.ArmazenamentoDocumentos.AssinaturaLoteCount);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE, RotaFila)]
        [InlineData(CargoFuncionario.ATENDENTE, RotaDetalhe)]
        [InlineData(CargoFuncionario.ATENDENTE, RotaDocumentos)]
        [InlineData(CargoFuncionario.ADMINISTRADOR, RotaFila)]
        [InlineData(CargoFuncionario.ADMINISTRADOR, RotaDetalhe)]
        [InlineData(CargoFuncionario.ADMINISTRADOR, RotaDocumentos)]
        public async Task CargoDaPolitica_Devolve200(CargoFuncionario cargo, string rota)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, FuncionarioDoCargo(fabrica, cargo));
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, rota), token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA, RotaFila)]
        [InlineData(CargoFuncionario.GESTOR_FROTA, RotaDetalhe)]
        [InlineData(CargoFuncionario.GESTOR_FROTA, RotaDocumentos)]
        [InlineData(CargoFuncionario.VENDEDOR, RotaFila)]
        [InlineData(CargoFuncionario.VENDEDOR, RotaDetalhe)]
        [InlineData(CargoFuncionario.VENDEDOR, RotaDocumentos)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, RotaFila)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, RotaDetalhe)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, RotaDocumentos)]
        [InlineData(CargoFuncionario.FINANCEIRO, RotaFila)]
        [InlineData(CargoFuncionario.FINANCEIRO, RotaDetalhe)]
        [InlineData(CargoFuncionario.FINANCEIRO, RotaDocumentos)]
        public async Task CargoForaDaPolitica_Devolve403(CargoFuncionario cargo, string rota)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, FuncionarioDoCargo(fabrica, cargo));
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(Caminho(fabrica, rota), token));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Fila_StatusVazio_NaoFiltraEDevolve200ComTodos()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            Cliente clienteSemente = fabrica.Repositorio.Clientes[0];
            fabrica.Armazenamento.Semear(clienteSemente.IdCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(CaminhoFila + ParametroStatusVazio, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(QuantidadeEsperadaFilaSemFiltro, documento.RootElement.GetProperty(PropriedadeTotal).GetInt32());
        }

        private static Funcionario FuncionarioDoCargo(VerificacaoWebFactory fabrica, CargoFuncionario cargo)
        {
            return cargo switch
            {
                CargoFuncionario.ADMINISTRADOR => fabrica.Administrador,
                CargoFuncionario.ATENDENTE => fabrica.Atendente,
                CargoFuncionario.FINANCEIRO => fabrica.Financeiro,
                CargoFuncionario.GESTOR_FROTA => fabrica.GestorFrota,
                CargoFuncionario.VENDEDOR => fabrica.Vendedor,
                CargoFuncionario.ADMINISTRATIVO => fabrica.Administrativo,
                _ => throw new ArgumentOutOfRangeException(nameof(cargo))
            };
        }

        private static string TokenDeFuncionario(VerificacaoWebFactory fabrica, Funcionario funcionario)
        {
            TokenService tokens = CriarTokenService(fabrica);
            string versaoSessao = tokens.CalcularVersaoSessao(funcionario.SenhaHash!);

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome, funcionario.Cargo, versaoSessao, false, funcionario.IdEmpresa).Token;
        }

        private static string TokenDeCliente(VerificacaoWebFactory fabrica)
        {
            TokenService tokens = CriarTokenService(fabrica);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string versaoSessao = tokens.CalcularVersaoSessao(fabrica.HashSenhaCliente);

            return tokens.GerarToken(PerfilUsuario.CLIENTE, idCliente, NomeClienteTeste, null, versaoSessao, false, null).Token;
        }

        private static TokenService CriarTokenService(VerificacaoWebFactory fabrica)
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
    }
}
