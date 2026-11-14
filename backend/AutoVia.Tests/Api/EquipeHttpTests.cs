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
    /// Testes HTTP da equipe da locadora (P10-BE-03) com DUAS locadoras no pipeline real: 401 sem token e 403 para
    /// atendente e cliente em todas as rotas; o ADMINISTRADOR da locadora A nao lista, nao consulta, nao edita e nao
    /// desativa funcionario da B (404); listagem e totais so da propria locadora; filtros e paginacao; cadastro com 201 e
    /// Location, senha que nao volta e login do funcionario novo na locadora certa; desativado nao entra; troca de cargo
    /// valendo na requisicao seguinte com o token antigo. A sessao e a real (cache com invalidacao); o repositorio e o fake.
    /// </summary>
    public class EquipeHttpTests
    {
        private const string RotaEquipe = "/api/equipe";
        private const string RotaLogin = "/api/auth/login";
        private const string RotaContextoAdministrador = "/api/teste-contexto/administrador";
        private const string RotaContexto = "/api/teste-contexto/contexto";
        private const string MediaProblema = "application/problem+json";
        private const string SenhaNova = "SenhaNova1";
        private const string CpfNovo = "71428793860";
        private const string CpfNovoMascarado = "714.***.***-60";
        private const string MatriculaRepetida = "A-001";

        [Fact]
        public async Task SemToken_TodasAsRotasDevolvem401()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage listagem = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe, null));
            HttpResponseMessage consulta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "/1", null));
            HttpResponseMessage cadastro = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, null, CorpoCadastro()));
            HttpResponseMessage edicao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaEquipe + "/1", null, CorpoEdicao(CargoFuncionario.ATENDENTE)));
            HttpResponseMessage situacao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/1/situacao", null, new { ativo = false }));

            Assert.Equal(HttpStatusCode.Unauthorized, listagem.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, consulta.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, cadastro.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, edicao.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, situacao.StatusCode);
        }

        [Fact]
        public async Task TokenDeAtendente_TodasAsRotasDevolvem403()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AtendenteA);

            HttpResponseMessage listagem = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe, token));
            HttpResponseMessage consulta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "/" + fabrica.AdminA.IdFuncionario, token));
            HttpResponseMessage cadastro = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro()));
            HttpResponseMessage edicao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario, token, CorpoEdicao(CargoFuncionario.ATENDENTE)));
            HttpResponseMessage situacao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario + "/situacao", token, new { ativo = false }));

            Assert.Equal(HttpStatusCode.Forbidden, listagem.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cadastro.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, situacao.StatusCode);
        }

        [Fact]
        public async Task TokenDeCliente_TodasAsRotasDevolvem403()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeCliente(fabrica);

            HttpResponseMessage listagem = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe, token));
            HttpResponseMessage consulta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "/" + fabrica.AdminA.IdFuncionario, token));
            HttpResponseMessage cadastro = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro()));
            HttpResponseMessage edicao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario, token, CorpoEdicao(CargoFuncionario.ATENDENTE)));
            HttpResponseMessage situacao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario + "/situacao", token, new { ativo = false }));

            Assert.Equal(HttpStatusCode.Forbidden, listagem.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cadastro.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, edicao.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, situacao.StatusCode);
        }

        [Fact]
        public async Task Listagem_DevolveSoAEquipeDaLocadoraETotaisDaLocadora()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?pagina=1&tamanhoPagina=20", token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(4, corpo.GetProperty("total").GetInt32());
            Assert.Equal(4, corpo.GetProperty("itens").GetArrayLength());
            Assert.Equal(3, corpo.GetProperty("totalAtivos").GetInt32());
            Assert.Equal(1, corpo.GetProperty("totalInativos").GetInt32());
            Assert.Equal(1, corpo.GetProperty("totalAdministradores").GetInt32());
            Assert.Equal(4, corpo.GetProperty("totalCadastrados").GetInt32());
            string texto = corpo.GetRawText();
            Assert.DoesNotContain("Locadora B", texto);
            Assert.DoesNotContain(fabrica.VendedorB.Cpf, texto);
        }

        [Fact]
        public async Task Listagem_BuscaCargoEAtivoFiltramSemMudarOsTotais()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            JsonElement porNome = await LerJsonAsync(await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?busca=funcionaria", token)));
            JsonElement porCargo = await LerJsonAsync(await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?cargo=ADMINISTRADOR", token)));
            JsonElement inativos = await LerJsonAsync(await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?ativo=false", token)));
            JsonElement porCpf = await LerJsonAsync(await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?busca=862.883", token)));

            Assert.Equal(1, porNome.GetProperty("total").GetInt32());
            Assert.Equal("Ana Funcionaria", porNome.GetProperty("itens")[0].GetProperty("nome").GetString());
            Assert.Equal(1, porCargo.GetProperty("total").GetInt32());
            Assert.Equal("ADMINISTRADOR", porCargo.GetProperty("itens")[0].GetProperty("cargo").GetString());
            Assert.Equal(1, inativos.GetProperty("total").GetInt32());
            Assert.False(inativos.GetProperty("itens")[0].GetProperty("ativo").GetBoolean());
            Assert.Equal(3, inativos.GetProperty("totalAtivos").GetInt32());
            Assert.Equal(1, porCpf.GetProperty("total").GetInt32());
            Assert.Equal("862.***.***-57", porCpf.GetProperty("itens")[0].GetProperty("cpfMascarado").GetString());
        }

        [Fact]
        public async Task Listagem_PaginaERejeitaParametroInvalido()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            JsonElement segunda = await LerJsonAsync(await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?pagina=2&tamanhoPagina=2", token)));
            HttpResponseMessage invalida = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "?pagina=0", token));

            Assert.Equal(4, segunda.GetProperty("total").GetInt32());
            Assert.Equal(2, segunda.GetProperty("itens").GetArrayLength());
            Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        }

        [Fact]
        public async Task Consulta_DeOutraLocadoraOuDesativado_Devolve404ComAMesmaMensagem()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);
            string rotaDeOutra = RotaEquipe + "/" + fabrica.VendedorB.IdFuncionario;
            string rotaDesativado = RotaEquipe + "/" + fabrica.Excluido.IdFuncionario;

            HttpResponseMessage consulta = await cliente.SendAsync(Requisicao(HttpMethod.Get, rotaDeOutra, token));
            HttpResponseMessage edicao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, rotaDeOutra, token, CorpoEdicao(CargoFuncionario.VENDEDOR)));
            HttpResponseMessage situacao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, rotaDeOutra + "/situacao", token, new { ativo = false }));
            HttpResponseMessage desativado = await cliente.SendAsync(Requisicao(HttpMethod.Get, rotaDesativado, token));

            Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, edicao.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, situacao.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, desativado.StatusCode);
            JsonElement corpo = await LerJsonAsync(consulta);
            Assert.Equal(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado, corpo.GetProperty("detail").GetString());
            Assert.True(fabrica.VendedorB.Ativo);
        }

        [Fact]
        public async Task Consulta_DoProprioFuncionario_TraEhVoceECpfMascaradoSemCpfCompleto()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaEquipe + "/" + fabrica.AdminA.IdFuncionario, token));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("ehVoce").GetBoolean());
            Assert.Equal("123.***.***-09", corpo.GetProperty("cpfMascarado").GetString());
            Assert.Equal("10/01/2024", corpo.GetProperty("dataAdmissaoStr").GetString());
            Assert.Equal("2024-01-10", corpo.GetProperty("dataAdmissao").GetString());
            Assert.DoesNotContain(fabrica.AdminA.Cpf, corpo.GetRawText());
            Assert.DoesNotContain("senha", corpo.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cadastro_Cria201ComLocationNaLocadoraDoAdministradorSemDevolverASenha()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(senha: SenhaNova)));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.NotNull(resposta.Headers.Location);
            Assert.Contains("/api/equipe/", resposta.Headers.Location!.ToString(), StringComparison.Ordinal);
            string texto = await resposta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(SenhaNova, texto, StringComparison.Ordinal);
            Assert.DoesNotContain("senhaInicial", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(CpfNovo, texto, StringComparison.Ordinal);
            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(CpfNovoMascarado, documento.RootElement.GetProperty("cpfMascarado").GetString());
            Assert.False(documento.RootElement.GetProperty("ehVoce").GetBoolean());
            Funcionario gravado = Assert.Single(fabrica.Repositorio.Funcionarios, x => x.Cpf == CpfNovo);
            Assert.Equal(fabrica.LocadoraA.IdEmpresa, gravado.IdEmpresa);
        }

        [Fact]
        public async Task Cadastro_CpfDeOutraLocadora_Conflito409()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(cpf: fabrica.VendedorB.Cpf)));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroEquipeCpfDuplicado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Cadastro_MatriculaRepetidaNaMesmaLocadora_Conflito409()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(matricula: " a-001 ")));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroEquipeMatriculaDuplicada, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Cadastro_MatriculaDeOutraLocadora_EPermitida()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminB);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(matricula: MatriculaRepetida)));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        }

        [Fact]
        public async Task Cadastro_SenhaFraca_Devolve400ComOCampoSenhaInicial()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(senha: "senhafraca")));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("errors").TryGetProperty("senhaInicial", out JsonElement mensagens));
            Assert.Equal(ConstantsMessage.ErroEquipeSenhaInicialFraca, mensagens[0].GetString());
        }

        [Fact]
        public async Task Login_FuncionarioCriadoEntraComASenhaInicialNaLocadoraCerta()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage criado = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Post, RotaEquipe, token, CorpoCadastro(senha: SenhaNova, cargo: "VENDEDOR")));
            HttpResponseMessage login = await cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "FUNCIONARIO", cpf = CpfNovo, senha = SenhaNova }));

            Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(fabrica.LocadoraA.IdEmpresa, corpo.GetProperty("idEmpresa").GetInt32());
            Assert.Equal("VENDEDOR", corpo.GetProperty("cargo").GetString());
        }

        [Fact]
        public async Task Atualizacao_MudancaDeCargoValeNaRequisicaoSeguinteComOTokenAntigo()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string tokenAdmin = TokenDeFuncionario(fabrica, fabrica.AdminA);
            string tokenAntigo = TokenDeFuncionario(fabrica, fabrica.AtendenteA);

            HttpResponseMessage antes = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContextoAdministrador, tokenAntigo));
            HttpResponseMessage edicao = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario, tokenAdmin, CorpoEdicao(CargoFuncionario.ADMINISTRADOR)));
            HttpResponseMessage depois = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContextoAdministrador, tokenAntigo));

            Assert.Equal(HttpStatusCode.Forbidden, antes.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, edicao.StatusCode);
            Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
        }

        [Fact]
        public async Task Atualizacao_DoProprioCargo_Conflito409DaPropriaConta()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Put, RotaEquipe + "/" + fabrica.AdminA.IdFuncionario, token, CorpoEdicao(CargoFuncionario.ATENDENTE)));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroEquipePropriaConta, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Situacao_FuncionarioDesativadoNaoAcessaComTokenAntigoNemPeloLogin()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string tokenAdmin = TokenDeFuncionario(fabrica, fabrica.AdminA);
            string tokenAntigo = TokenDeFuncionario(fabrica, fabrica.AtendenteA);

            HttpResponseMessage loginAntes = await cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "FUNCIONARIO", cpf = fabrica.AtendenteA.Cpf, senha = EquipeWebFactory.SenhaDaEquipe }));
            HttpResponseMessage antes = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, tokenAntigo));
            HttpResponseMessage desativar = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario + "/situacao", tokenAdmin, new { ativo = false }));
            HttpResponseMessage depois = await cliente.SendAsync(Requisicao(HttpMethod.Get, RotaContexto, tokenAntigo));
            HttpResponseMessage loginDepois = await cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "FUNCIONARIO", cpf = fabrica.AtendenteA.Cpf, senha = EquipeWebFactory.SenhaDaEquipe }));

            Assert.Equal(HttpStatusCode.OK, loginAntes.StatusCode);
            Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, desativar.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, loginDepois.StatusCode);
        }

        [Fact]
        public async Task Situacao_DesativarAPropriaConta_Conflito409DaPropriaConta()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/" + fabrica.AdminA.IdFuncionario + "/situacao", token, new { ativo = false }));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroEquipePropriaConta, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Situacao_SemAtivo_Devolve400ComOCampoAtivo()
        {
            using EquipeWebFactory fabrica = new EquipeWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = TokenDeFuncionario(fabrica, fabrica.AdminA);

            HttpResponseMessage resposta = await cliente.SendAsync(RequisicaoComCorpo(HttpMethod.Patch, RotaEquipe + "/" + fabrica.AtendenteA.IdFuncionario + "/situacao", token, new { }));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty("errors").TryGetProperty("ativo", out JsonElement mensagens));
            Assert.Equal(ConstantsMessage.ErroEquipeSituacaoObrigatoria, mensagens[0].GetString());
        }

        /// <summary>
        /// Token de funcionario assinado pelo servidor de teste, com o cargo e a locadora do funcionario no momento em que
        /// o token e emitido. A claim de cargo e informativa: o servidor confere o cargo e a locadora no banco.
        /// </summary>
        private static string TokenDeFuncionario(EquipeWebFactory fabrica, Funcionario funcionario)
        {
            TokenService tokens = CriarTokenService(fabrica);
            string versaoSessao = tokens.CalcularVersaoSessao(funcionario.SenhaHash!);

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome, funcionario.Cargo, versaoSessao, false, funcionario.IdEmpresa).Token;
        }

        /// <summary>Token de cliente valido, sem claim de empresa.</summary>
        private static string TokenDeCliente(EquipeWebFactory fabrica)
        {
            TokenService tokens = CriarTokenService(fabrica);
            int idCliente = fabrica.Repositorio.Clientes[0].IdCliente;
            string versaoSessao = tokens.CalcularVersaoSessao(fabrica.HashSenhaCliente);

            return tokens.GerarToken(PerfilUsuario.CLIENTE, idCliente, "Cliente", null, versaoSessao, false, null).Token;
        }

        private static TokenService CriarTokenService(EquipeWebFactory fabrica)
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

        private static object CorpoCadastro(string cpf = CpfNovo, string matricula = "A-003", string senha = SenhaNova, string cargo = "VENDEDOR")
        {
            return new
            {
                nome = "Novo Funcionario",
                cpf,
                matricula,
                email = "novo@locadora.exemplo.com",
                dataAdmissao = "2024-01-10",
                cargo,
                senhaInicial = senha
            };
        }

        private static object CorpoEdicao(CargoFuncionario cargo)
        {
            return new
            {
                nome = "Funcionario Editado",
                matricula = "EDT-01",
                email = "editado@locadora.exemplo.com",
                dataAdmissao = "2024-01-10",
                cargo = cargo.ToString()
            };
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

        private static StringContent CorpoJson(object corpo)
        {
            return new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }

    /// <summary>
    /// Servidor de teste da equipe: a factory base (login e sessao reais, com fakes de repositorio) mais uma segunda
    /// locadora (B). A locadora A e a operadora da semente. Cada locadora tem um ADMINISTRADOR e outros funcionarios, e
    /// ha um funcionario desativado (DisabledAt) na A. O repositorio de equipe enxerga o mesmo armazenamento do login,
    /// com o escopo do contexto da requisicao, e o repositorio de empresas e o fake compartilhado.
    /// </summary>
    public sealed class EquipeWebFactory : AutenticacaoWebFactory
    {
        /// <summary>Senha dos funcionarios semeados da equipe (hash PBKDF2 de teste).</summary>
        public const string SenhaDaEquipe = "SenhaEquipe1";

        private const string CpfAdminA = "12345678909";
        private const string CpfAtendenteA = "86288366757";
        private const string CpfAdminB = "39053344705";
        private const string CpfVendedorB = "52998224725";
        private const string CpfExcluido = "00000000000";
        private const string CnpjLocadoraB = "22222222222222";

        public EquipeWebFactory()
            : base(1000, null)
        {
            string hash = new SenhaHasher(new FakeLogService()).Hash(SenhaDaEquipe);

            Empresa locadoraB = new Empresa
            {
                RazaoSocial = "Locadora B Ltda",
                NomeFantasia = "Locadora B",
                Cnpj = CnpjLocadoraB,
                Ativo = true,
                Operadora = false
            };

            Repositorio.AdicionarEmpresa(locadoraB);
            LocadoraB = locadoraB;
            LocadoraA = Repositorio.Empresas.Single(x => x.Operadora);
            RepositorioEmpresas = new FakeEmpresaRepository(Repositorio);

            AdminA = Semear("Admin Locadora A", CpfAdminA, "A-001", CargoFuncionario.ADMINISTRADOR, LocadoraA.IdEmpresa, hash);
            AtendenteA = Semear("Atendente Locadora A", CpfAtendenteA, "A-002", CargoFuncionario.ATENDENTE, LocadoraA.IdEmpresa, hash);
            AdminB = Semear("Admin Locadora B", CpfAdminB, "B-001", CargoFuncionario.ADMINISTRADOR, LocadoraB.IdEmpresa, hash);
            VendedorB = Semear("Vendedor Locadora B", CpfVendedorB, "B-002", CargoFuncionario.VENDEDOR, LocadoraB.IdEmpresa, hash);
            Excluido = Semear("Excluido Locadora A", CpfExcluido, "A-009", CargoFuncionario.ATENDENTE, LocadoraA.IdEmpresa, hash, DateTime.Now);
        }

        /// <summary>Locadora A: a empresa operadora da semente (id 1).</summary>
        public Empresa LocadoraA { get; }

        /// <summary>Locadora B: segunda locadora do teste.</summary>
        public Empresa LocadoraB { get; }

        /// <summary>ADMINISTRADOR da locadora A.</summary>
        public Funcionario AdminA { get; }

        /// <summary>ATENDENTE da locadora A (sem permissao de administracao).</summary>
        public Funcionario AtendenteA { get; }

        /// <summary>ADMINISTRADOR da locadora B.</summary>
        public Funcionario AdminB { get; }

        /// <summary>VENDEDOR da locadora B.</summary>
        public Funcionario VendedorB { get; }

        /// <summary>Funcionario da locadora A com DisabledAt preenchido (exclusao logica): deve responder como inexistente.</summary>
        public Funcionario Excluido { get; }

        /// <summary>Repositorio de empresas em memoria, sobre o mesmo armazenamento do login.</summary>
        public FakeEmpresaRepository RepositorioEmpresas { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFuncionarioRepository>();
                services.AddScoped<IFuncionarioRepository>(provedor => new FakeFuncionarioRepository(Repositorio, provedor.GetRequiredService<IContextoEmpresa>()));
                services.RemoveAll<IEmpresaRepository>();
                services.AddSingleton<IEmpresaRepository>(RepositorioEmpresas);
            });
        }

        /// <summary>Grava um funcionario no armazenamento do login, com a senha da equipe (hash informado).</summary>
        private Funcionario Semear(string nome, string cpf, string matricula, CargoFuncionario cargo, int idEmpresa, string hash, DateTime? desativadoEm = null)
        {
            Funcionario funcionario = new Funcionario
            {
                Nome = nome,
                Cpf = cpf,
                Matricula = matricula,
                Cargo = cargo,
                Ativo = true,
                IdEmpresa = idEmpresa,
                DataAdmissao = new DateOnly(2024, 1, 10),
                Email = matricula.ToLowerInvariant() + "@locadora.exemplo.com",
                SenhaHash = hash,
                DisabledAt = desativadoEm
            };

            Repositorio.AdicionarFuncionario(funcionario);

            return funcionario;
        }
    }
}
