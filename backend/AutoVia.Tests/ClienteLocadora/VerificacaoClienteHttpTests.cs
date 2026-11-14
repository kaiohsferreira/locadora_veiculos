using AutoVia.Communication.Enums;
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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VinculoClienteLocadora = AutoVia.Core.Entities.PgSql.ClienteLocadora;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Testes HTTP do PATCH /api/clientes/{id}/verificacao (T-BE-06) no pipeline real: 401 sem token, 403 para token de
    /// cliente e para cargo FINANCEIRO, 400 para decisao ausente, PENDENTE ou recusa sem justificativa, 204 para
    /// ATENDENTE e ADMINISTRADOR (com a justificativa gravada so na recusa), 404 para cliente sem vinculo e 409 para
    /// cadastro ja aprovado. A sessao e a real; o repositorio de vinculos e o fake compartilhado com a fabrica.
    /// </summary>
    public class VerificacaoClienteHttpTests
    {
        private const string MediaProblema = "application/problem+json";
        private const string CpfAdmin = "12345678909";
        private const string CpfAtendente = "86288366757";
        private const string CpfFinanceiro = "71428793860";
        private const string JustificativaTeste = "Documento ilegível";

        [Fact]
        public async Task SemToken_Devolve401()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, null, "APROVADO", null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "APROVADO", null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task CargoFinanceiro_Devolve403()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Financeiro);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "APROVADO", null));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Atendente_Aprova_Devolve204ESemJustificativa()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "APROVADO", null));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            VinculoClienteLocadora linha = Assert.Single(fabrica.Armazenamento.Vinculos);
            Assert.Equal(StatusCadastroCliente.APROVADO, linha.Status);
            Assert.Null(linha.JustificativaRecusa);
            Assert.Equal(fabrica.Atendente.IdFuncionario, linha.IdFuncionarioVerificador);
        }

        [Fact]
        public async Task Administrador_Recusa_Devolve204ComJustificativaInterna()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Administrador);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "RECUSADO", JustificativaTeste));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            VinculoClienteLocadora linha = Assert.Single(fabrica.Armazenamento.Vinculos);
            Assert.Equal(StatusCadastroCliente.RECUSADO, linha.Status);
            Assert.Equal(JustificativaTeste, linha.JustificativaRecusa);
        }

        [Fact]
        public async Task Recusa_SemJustificativa_Devolve400()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "RECUSADO", null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(StatusCadastroCliente.PENDENTE, Assert.Single(fabrica.Armazenamento.Vinculos).Status);
        }

        [Theory]
        [InlineData("PENDENTE")]
        [InlineData(null)]
        public async Task DecisaoPendenteOuAusente_Devolve400(string? decisao)
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, decisao, null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task ClienteDeOutraLocadoraOuSemVinculo_Devolve404()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "APROVADO", null));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Empty(fabrica.Armazenamento.Vinculos);
        }

        /// <summary>Vinculo semeado em OUTRA locadora: o ATENDENTE da operadora recebe 404 e o vinculo nao muda (C5).</summary>
        [Fact]
        public async Task ClienteComVinculoEmOutraLocadora_Devolve404SemAlterarNada()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.OutraLocadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "APROVADO", null));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            VinculoClienteLocadora linha = Assert.Single(fabrica.Armazenamento.Vinculos);
            Assert.Equal(StatusCadastroCliente.PENDENTE, linha.Status);
            Assert.Null(linha.IdFuncionarioVerificador);
        }

        /// <summary>Decisao de valor fora do enum (XYZ) devolve 400 no pipeline real (C5).</summary>
        [Fact]
        public async Task DecisaoDeValorInvalido_Devolve400()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.PENDENTE);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "XYZ", null));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(StatusCadastroCliente.PENDENTE, Assert.Single(fabrica.Armazenamento.Vinculos).Status);
        }

        [Fact]
        public async Task CadastroJaAprovado_Devolve409()
        {
            using VerificacaoWebFactory fabrica = new VerificacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            fabrica.Armazenamento.Semear(idCliente, fabrica.Operadora.IdEmpresa, StatusCadastroCliente.APROVADO);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoPatch(fabrica, token, "RECUSADO", JustificativaTeste));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
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

            return tokens.GerarToken(PerfilUsuario.CLIENTE, idCliente, "Cliente", null, versaoSessao, false, null).Token;
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

        /// <summary>PATCH com o corpo da decisao. Decisao nula vira ausencia do campo (400 pela validacao).</summary>
        private static HttpRequestMessage RequisicaoPatch(VerificacaoWebFactory fabrica, string? token, string? decisao, string? justificativa)
        {
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Patch, "/api/clientes/" + idCliente.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/verificacao");

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            Dictionary<string, string?> corpo = new Dictionary<string, string?>();

            if (decisao != null)
            {
                corpo["decisao"] = decisao;
            }

            if (justificativa != null)
            {
                corpo["justificativa"] = justificativa;
            }

            requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

            return requisicao;
        }
    }

    /// <summary>
    /// Servidor de teste da verificacao: a fabrica base (login e sessao reais) mais o armazenamento de vinculos compartilhado
    /// e um repositorio de vinculos por requisicao (com o contexto da requisicao). Semeia uma locadora operadora, um
    /// ADMINISTRADOR, um ATENDENTE e um FINANCEIRO nela.
    /// </summary>
    public sealed class VerificacaoWebFactory : AutenticacaoWebFactory
    {
        private const string CpfAdminSemente = "12345678909";
        private const string CpfAtendenteSemente = "86288366757";
        private const string CpfFinanceiroSemente = "71428793860";
        private const string SenhaTeste = "SenhaEquipe1";

        public VerificacaoWebFactory()
            : base(1000, null)
        {
            Operadora = Repositorio.Empresas.Single(x => x.Operadora);
            string hash = new SenhaHasher(new FakeLogService()).Hash(SenhaTeste);

            Administrador = Semear("Admin Verificador", CpfAdminSemente, "A-001", CargoFuncionario.ADMINISTRADOR, hash);
            Atendente = Semear("Atendente Verificador", CpfAtendenteSemente, "A-002", CargoFuncionario.ATENDENTE, hash);
            Financeiro = Semear("Financeiro", CpfFinanceiroSemente, "A-003", CargoFuncionario.FINANCEIRO, hash);
            OutraLocadora = new Empresa
            {
                RazaoSocial = "Locadora Outra Ltda",
                NomeFantasia = "Locadora Outra",
                Cnpj = "22222222222222",
                Ativo = true,
                Operadora = false
            };
            Repositorio.AdicionarEmpresa(OutraLocadora);

            Armazenamento = new ArmazenamentoClienteLocadora();
            Armazenamento.AdicionarEmpresa(Operadora);
            Armazenamento.AdicionarEmpresa(OutraLocadora);
        }

        /// <summary>Locadora operadora da semente (a empresa do contexto dos funcionarios).</summary>
        public Empresa Operadora { get; }

        /// <summary>Segunda locadora, sem funcionarios do teste: vinculos semeados nela nao sao visiveis a operadora.</summary>
        public Empresa OutraLocadora { get; }

        /// <summary>Armazenamento dos vinculos, compartilhado por todas as requisicoes do servidor de teste.</summary>
        public ArmazenamentoClienteLocadora Armazenamento { get; }

        public Funcionario Administrador { get; }

        public Funcionario Atendente { get; }

        public Funcionario Financeiro { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClienteLocadoraRepository>();
                services.AddScoped<IClienteLocadoraRepository>(provedor => new FakeClienteLocadoraRepository(provedor.GetRequiredService<IContextoEmpresa>(), Armazenamento));
            });
        }

        private Funcionario Semear(string nome, string cpf, string matricula, CargoFuncionario cargo, string hash)
        {
            Funcionario funcionario = new Funcionario
            {
                Nome = nome,
                Cpf = cpf,
                Matricula = matricula,
                Cargo = cargo,
                Ativo = true,
                IdEmpresa = Operadora.IdEmpresa,
                DataAdmissao = new DateOnly(2024, 1, 10),
                Email = matricula.ToLowerInvariant() + "@locadora.exemplo.com",
                SenhaHash = hash
            };

            Repositorio.AdicionarFuncionario(funcionario);

            return funcionario;
        }
    }
}
