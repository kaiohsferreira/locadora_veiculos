using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP da visao geral (P11-BE-01 e P11-BE-02) no pipeline real: 401 sem token e 403 para cliente; 200 para o
    /// ATENDENTE sem valores em reais (quantidades presentes, campos em reais nulos); 200 com valores para ADMINISTRADOR e
    /// FINANCEIRO; 400 para periodo fora do enum; TRINTA_DIAS quando o parametro e omitido. O relogio e fixo e o repositorio
    /// do painel e o fake; a sessao e a real.
    /// </summary>
    public class PainelHttpTests
    {
        private const string RotaVisaoGeral = "/api/painel/visao-geral";
        private const string MediaProblema = "application/problem+json";
        private const string PeriodoTrintaDias = "TRINTA_DIAS";
        private const string InicioTrintaDias = "16/10/2026";
        private const string FimHoje = "14/11/2026";
        private const string InicioHoje = "14/11/2026";
        private const string PeriodoInvalido = "SEMANA";
        private const string PropPeriodo = "periodo";
        private const string PropValoresVisiveis = "valoresVisiveis";
        private const string PropReceitaPeriodoStr = "receitaPeriodoStr";
        private const string PropPeriodoInicioStr = "periodoInicioStr";
        private const string PropQuantidadeEmAberto = "quantidadeCobrancasEmAberto";
        private const string PropTotalVeiculos = "totalVeiculos";
        private const string PropReceita = "receitaPeriodo";
        private const string PropAReceber = "aReceber";
        private const string PropEmAtraso = "emAtraso";
        private const string PropVariacao = "variacaoReceitaPercentual";
        private const int QuantidadeEmAberto = 4;
        private const int TotalVeiculosDaLocadora = 5;

        [Fact]
        public async Task SemToken_Devolve401()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral, token));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Atendente_Devolve200SemValoresEmReais()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral + "?periodo=HOJE", token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.False(corpo.GetProperty(PropValoresVisiveis).GetBoolean());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(PropReceita).ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(PropAReceber).ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(PropEmAtraso).ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(PropVariacao).ValueKind);
            Assert.Equal(string.Empty, corpo.GetProperty(PropReceitaPeriodoStr).GetString());
            Assert.Equal(QuantidadeEmAberto, corpo.GetProperty(PropQuantidadeEmAberto).GetInt32());
            Assert.Equal(TotalVeiculosDaLocadora, corpo.GetProperty(PropTotalVeiculos).GetInt32());
            Assert.DoesNotContain("1500", corpo.GetRawText());
        }

        [Fact]
        public async Task Administrador_Devolve200ComValoresEmReais()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Admin);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral + "?periodo=HOJE", token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty(PropValoresVisiveis).GetBoolean());
            Assert.Equal(1500m, corpo.GetProperty(PropReceita).GetDecimal());
            Assert.Equal(800m, corpo.GetProperty(PropAReceber).GetDecimal());
            Assert.Equal(300m, corpo.GetProperty(PropEmAtraso).GetDecimal());
            Assert.Equal(StaticMethods.FormateToMonetary(1500m), corpo.GetProperty(PropReceitaPeriodoStr).GetString());
        }

        [Fact]
        public async Task Financeiro_Devolve200ComValoresEmReais()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Financeiro);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty(PropValoresVisiveis).GetBoolean());
            Assert.Equal(1500m, corpo.GetProperty(PropReceita).GetDecimal());
        }

        [Fact]
        public async Task SemParametro_UsaTrintaDias()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PeriodoTrintaDias, corpo.GetProperty(PropPeriodo).GetString());
            Assert.Equal(InicioTrintaDias, corpo.GetProperty(PropPeriodoInicioStr).GetString());
            Assert.Equal(FimHoje, corpo.GetProperty("periodoFimStr").GetString());
        }

        [Fact]
        public async Task PeriodoHoje_InicioEFimSaoODiaDeHoje()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Atendente);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(RotaVisaoGeral + "?periodo=HOJE", token));

            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(InicioHoje, corpo.GetProperty(PropPeriodoInicioStr).GetString());
            Assert.Equal(FimHoje, corpo.GetProperty("periodoFimStr").GetString());
        }

        [Fact]
        public async Task PeriodoForaDoEnum_Devolve400()
        {
            using PainelWebFactory fabrica = new PainelWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.Admin);

            HttpResponseMessage nomeInvalido = await cliente.SendAsync(Requisicao(RotaVisaoGeral + "?" + PropPeriodo + "=" + PeriodoInvalido, token));
            HttpResponseMessage numeroForaDoEnum = await cliente.SendAsync(Requisicao(RotaVisaoGeral + "?" + PropPeriodo + "=99", token));

            Assert.Equal(HttpStatusCode.BadRequest, nomeInvalido.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, numeroForaDoEnum.StatusCode);
            Assert.Equal(MediaProblema, nomeInvalido.Content.Headers.ContentType?.MediaType);
        }

        private static HttpRequestMessage Requisicao(string rota, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        private static string TokenDeFuncionario(PainelWebFactory fabrica, Funcionario funcionario)
        {
            TokenService tokens = CriarTokenService(fabrica);
            string versaoSessao = tokens.CalcularVersaoSessao(funcionario.SenhaHash!);

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome, funcionario.Cargo, versaoSessao, false, funcionario.IdEmpresa).Token;
        }

        private static string TokenDeCliente(PainelWebFactory fabrica)
        {
            TokenService tokens = CriarTokenService(fabrica);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string versaoSessao = tokens.CalcularVersaoSessao(fabrica.HashSenhaCliente);

            return tokens.GerarToken(PerfilUsuario.CLIENTE, idCliente, "Cliente", null, versaoSessao, false, null).Token;
        }

        private static TokenService CriarTokenService(PainelWebFactory fabrica)
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

    /// <summary>
    /// Servidor de teste do painel: a factory base (login e sessao reais, com fakes de repositorio), tres funcionarios da
    /// locadora operadora (ADMINISTRADOR, FINANCEIRO e ATENDENTE) e o repositorio do painel com numeros fixos. O relogio e
    /// fixo em 14/11/2026, para os periodos terem datas conhecidas.
    /// </summary>
    public sealed class PainelWebFactory : AutenticacaoWebFactory
    {
        /// <summary>Senha dos funcionarios semeados do painel (hash PBKDF2 de teste).</summary>
        public const string SenhaDoPainel = "SenhaPainel1";

        private const string CpfAdmin = "12345678909";
        private const string CpfFinanceiro = "39053344705";
        private const string CpfAtendente = "86288366757";
        private const int QuantidadeEmAberto = 4;

        private static readonly DateTime DataFixa = new DateTime(2026, 11, 14, 10, 30, 0);

        public PainelWebFactory()
            : base(1000, null)
        {
            string hash = new SenhaHasher(new FakeLogService()).Hash(SenhaDoPainel);

            LocadoraA = Repositorio.Empresas.Single(x => x.Operadora);
            Admin = Semear("Admin Painel", CpfAdmin, "P-001", CargoFuncionario.ADMINISTRADOR, hash);
            Financeiro = Semear("Financeiro Painel", CpfFinanceiro, "P-002", CargoFuncionario.FINANCEIRO, hash);
            Atendente = Semear("Atendente Painel", CpfAtendente, "P-003", CargoFuncionario.ATENDENTE, hash);

            DadosPorEmpresa = new Dictionary<int, DadosPainelFake>
            {
                [LocadoraA.IdEmpresa] = DadosDaLocadora()
            };
        }

        /// <summary>Locadora A: a empresa operadora da semente.</summary>
        public Empresa LocadoraA { get; }

        /// <summary>ADMINISTRADOR da locadora A (ve valores).</summary>
        public Funcionario Admin { get; }

        /// <summary>FINANCEIRO da locadora A (ve valores).</summary>
        public Funcionario Financeiro { get; }

        /// <summary>ATENDENTE da locadora A (nao ve valores).</summary>
        public Funcionario Atendente { get; }

        /// <summary>Numeros do painel por locadora, lidos pelo repositorio falso do painel.</summary>
        public Dictionary<int, DadosPainelFake> DadosPorEmpresa { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPainelRepository>();
                services.AddScoped<IPainelRepository>(provedor => new FakePainelRepository(provedor.GetRequiredService<IContextoEmpresa>(), DadosPorEmpresa));
                services.RemoveAll<IRelogio>();
                services.AddSingleton<IRelogio>(new FakeRelogio(DataFixa));
            });
        }

        /// <summary>Numeros fixos: receita 1500, a receber 800 (4 cobrancas), em atraso 300 (2), 5 veiculos em operacao (2 locados), 2 locacoes e 1 devolucao hoje.</summary>
        private static DadosPainelFake DadosDaLocadora()
        {
            return new DadosPainelFake
            {
                ReceitaPorJanela = (inicio, fimExclusivo) => 1500m,
                AReceber = 800m,
                QuantidadeAReceber = QuantidadeEmAberto,
                EmAtraso = 300m,
                QuantidadeEmAtraso = 2,
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 3 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 2 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 4 }
                },
                LocacoesEmAndamento = 2,
                DevolucoesPrevistas = 1
            };
        }

        /// <summary>Grava um funcionario ativo da locadora A no armazenamento do login, com o hash informado.</summary>
        private Funcionario Semear(string nome, string cpf, string matricula, CargoFuncionario cargo, string hash)
        {
            Funcionario funcionario = new Funcionario
            {
                Nome = nome,
                Cpf = cpf,
                Matricula = matricula,
                Cargo = cargo,
                Ativo = true,
                IdEmpresa = LocadoraA.IdEmpresa,
                DataAdmissao = new DateOnly(2024, 1, 10),
                Email = matricula.ToLowerInvariant() + "@locadora.exemplo.com",
                SenhaHash = hash
            };

            Repositorio.AdicionarFuncionario(funcionario);

            return funcionario;
        }
    }
}
