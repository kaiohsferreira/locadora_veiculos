using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes de ponta a ponta da recuperacao de senha com o servidor em memoria (WebApplicationFactory sobre
    /// Program). Troca os repositorios de login e de links, a fila de e-mail e o log por fakes; mantem a fila de
    /// pedidos, o servico hospedado, o processador, o SessaoService e o SenhaHasher reais. Nenhum PostgreSQL e
    /// acessado e nenhum e-mail sai. O processamento roda em segundo plano: os testes esperam por condicao (com
    /// tempo limite curto), nunca por tempo fixo.
    /// </summary>
    public class RecuperacaoSenhaHttpTests : IClassFixture<RecuperacaoSenhaHttpTests.RecuperacaoWebFactory>
    {
        private const string RotaSolicitar = "/api/auth/recuperacao-senha";
        private const string RotaRedefinir = "/api/auth/redefinicao-senha";
        private const string RotaLogin = "/api/auth/login";
        private const string RotaIdentidade = "/api/teste-autorizacao/identidade";
        private const string MediaProblema = "application/problem+json";
        private const string PadraoToken = "#token=([A-Za-z0-9_-]+)";
        private const string SenhaAntiga = "SenhaAntiga1Rec";
        private const string SenhaNova = "SenhaNova2Rec";
        private const string SenhaFraca = "somenteletras";
        private const string TokenInventado = "token-inventado-pela-http";
        private const string MensagemFalhaBusca = "Falha simulada do banco na busca por CPF.";
        private const string CpfLimite = "20000000001";
        private const string CpfInexistente = "20000000002";
        private const string CpfDesativado = "20000000003";
        private const string CpfFuncionarioInativo = "20000000004";
        private const string CpfFuncionarioSemEmail = "20000000005";
        private const string CpfClienteSemSenha = "20000000006";
        private const string CpfClienteSemEmail = "20000000007";
        private const string CpfElegivel = "20000000008";
        private const string CpfElegivelLogin = "20000000009";
        private const string CpfTokenAntigo = "20000000010";
        private const string CpfTokenUsado = "20000000011";
        private const string CpfExpirado = "20000000012";
        private const string CpfTitularInativo = "20000000013";
        private const string CpfFraca = "20000000014";
        private const string CpfFila = "20000000015";
        private const string CpfBuscaLenta = "20000000016";
        private const string CpfBuscaFalha = "20000000017";
        private const string CpfDepoisDaFalha = "20000000018";
        private const string CpfCurto = "123";
        private const string EmailBase = "@exemplo.com";
        private const int QuantidadeLimiteHttp = 3;
        private const int TempoLimiteEsperaMs = 5000;
        private const int IntervaloEsperaMs = 10;
        private const int TempoLimiteRespostaMs = 5000;
        private const long BaseCpfSentinela = 90000000000L;
        private const string MensagemTempoEsgotado = "A condição esperada não aconteceu no tempo limite do teste.";
        private const string MensagemLinkSemToken = "O link do e-mail deve conter #token=.";

        /// <summary>Contador dos pedidos sentinela: cada um tem um CPF novo, para nunca bater no limite por conta.</summary>
        private static int _contadorSentinela;

        private readonly RecuperacaoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public RecuperacaoSenhaHttpTests(RecuperacaoWebFactory fabrica)
        {
            _fabrica = fabrica;
            _cliente = fabrica.CreateClient();
        }

        /// <summary>Requisicao JSON para a rota, com o token Bearer quando informado.</summary>
        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);

            if (!string.IsNullOrWhiteSpace(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

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

        /// <summary>Assinatura da resposta sem valores: status, corpo e nomes de todos os cabecalhos (inclusive os de conteudo).</summary>
        private static async Task<string> AssinaturaDaRespostaAsync(HttpResponseMessage resposta)
        {
            string corpo = await resposta.Content.ReadAsStringAsync();
            List<string> cabecalhos = resposta.Headers.Select(cabecalho => cabecalho.Key)
                .Concat(resposta.Content.Headers.Select(cabecalho => cabecalho.Key))
                .OrderBy(nome => nome, StringComparer.Ordinal)
                .ToList();

            return $"{(int)resposta.StatusCode}|{corpo}|{string.Join(",", cabecalhos)}";
        }

        private static string HashReal(string senha)
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            return hasher.Hash(senha);
        }

        private static string EmailDe(string cpf)
        {
            return "conta." + cpf + EmailBase;
        }

        /// <summary>Espera uma condicao verdadeira, consultando de tempos em tempos, ate o tempo limite do teste.</summary>
        private static async Task AguardarAsync(Func<bool> condicao)
        {
            Stopwatch cronometro = Stopwatch.StartNew();

            while (!condicao())
            {
                if (cronometro.ElapsedMilliseconds > TempoLimiteEsperaMs)
                {
                    throw new TimeoutException(MensagemTempoEsgotado);
                }

                await Task.Delay(IntervaloEsperaMs);
            }
        }

        private Cliente SemearCliente(string cpf, string? email, string? senhaHash)
        {
            Cliente cliente = new Cliente
            {
                Nome = "Cliente Recuperacao " + cpf,
                Cpf = cpf,
                Email = email ?? string.Empty,
                SenhaHash = senhaHash,
                StatusCadastro = StatusCadastroCliente.APROVADO
            };

            _fabrica.Repositorio.AdicionarCliente(cliente);

            return cliente;
        }

        private async Task<HttpResponseMessage> SolicitarAsync(string perfil, string cpf)
        {
            return await _cliente.PostAsync(RotaSolicitar, CorpoJson(new { perfil, cpf }));
        }

        /// <summary>
        /// Espera o processamento do pedido sentinela: um cliente elegivel com CPF novo, solicitado por ultimo. A fila e
        /// processada em ordem, entao quando o e-mail dele chega, todos os pedidos anteriores ja terminaram.
        /// </summary>
        private async Task AguardarPedidosAnterioresAsync()
        {
            int numero = Interlocked.Increment(ref _contadorSentinela);
            string cpf = (BaseCpfSentinela + numero).ToString(CultureInfo.InvariantCulture);
            string email = EmailDe(cpf);
            SemearCliente(cpf, email, HashReal(SenhaAntiga));

            HttpResponseMessage resposta = await SolicitarAsync("CLIENTE", cpf);
            Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);

            await AguardarAsync(() => MensagensPara(email) == 1);
        }

        /// <summary>Token do link enviado para o e-mail (a mensagem mais recente daquele destinatario). Espera o e-mail chegar.</summary>
        private async Task<string> TokenEnviadoParaAsync(string email)
        {
            await AguardarAsync(() => MensagensPara(email) > 0);

            EmailMensagem mensagem = _fabrica.Fila.Snapshot().Last(x => x.DestinatarioEmail == email);
            Match correspondencia = Regex.Match(mensagem.CorpoTexto, PadraoToken);

            Assert.True(correspondencia.Success, MensagemLinkSemToken);

            return correspondencia.Groups[1].Value;
        }

        private int MensagensPara(string email)
        {
            return _fabrica.Fila.Snapshot().Count(x => x.DestinatarioEmail == email);
        }

        private async Task<HttpResponseMessage> RedefinirAsync(string token, string senha, string confirmacao)
        {
            return await _cliente.PostAsync(RotaRedefinir, CorpoJson(new { token, novaSenha = senha, confirmacaoSenha = confirmacao }));
        }

        private async Task<HttpResponseMessage> LoginAsync(string cpf, string senha)
        {
            return await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf, senha, lembrar = false }));
        }

        [Fact]
        public async Task Solicitar_ContaElegivel_Devolve202ComAMensagemNeutraEEnviaOLink()
        {
            string email = EmailDe(CpfElegivel);
            SemearCliente(CpfElegivel, email, HashReal(SenhaAntiga));

            HttpResponseMessage resposta = await SolicitarAsync("CLIENTE", CpfElegivel);

            Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, corpo.GetProperty("mensagem").GetString());
            await AguardarAsync(() => MensagensPara(email) == 1);
            Assert.False(string.IsNullOrWhiteSpace(await TokenEnviadoParaAsync(email)));
        }

        [Fact]
        public async Task Solicitar_CasosNaoElegiveis_DevolvemRespostaIdenticaESemEnvio()
        {
            string emailDesativado = EmailDe(CpfDesativado);
            Cliente desativado = SemearCliente(CpfDesativado, emailDesativado, HashReal(SenhaAntiga));
            desativado.Disable();

            string emailFuncionarioInativo = EmailDe(CpfFuncionarioInativo);
            _fabrica.Repositorio.AdicionarFuncionario(new Funcionario { Nome = "Func Inativo", Cpf = CpfFuncionarioInativo, Matricula = "F-801", Cargo = CargoFuncionario.ATENDENTE, Email = emailFuncionarioInativo, Ativo = false, SenhaHash = HashReal(SenhaAntiga) });

            _fabrica.Repositorio.AdicionarFuncionario(new Funcionario { Nome = "Func Sem Email", Cpf = CpfFuncionarioSemEmail, Matricula = "F-802", Cargo = CargoFuncionario.ATENDENTE, Email = null, Ativo = true, SenhaHash = HashReal(SenhaAntiga) });

            string emailClienteSemSenha = EmailDe(CpfClienteSemSenha);
            SemearCliente(CpfClienteSemSenha, emailClienteSemSenha, null);

            string emailClienteSemEmail = EmailDe(CpfClienteSemEmail);
            SemearCliente(CpfClienteSemEmail, string.Empty, HashReal(SenhaAntiga));

            List<string> assinaturas = new List<string>();

            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfInexistente)));
            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfDesativado)));
            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("FUNCIONARIO", CpfFuncionarioInativo)));
            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("FUNCIONARIO", CpfFuncionarioSemEmail)));
            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfClienteSemSenha)));
            assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfClienteSemEmail)));

            string esperada = assinaturas[0];
            Assert.StartsWith($"{(int)HttpStatusCode.Accepted}|", esperada);

            foreach (string assinatura in assinaturas)
            {
                Assert.Equal(esperada, assinatura);
            }

            await AguardarPedidosAnterioresAsync();

            Assert.Equal(0, MensagensPara(emailDesativado));
            Assert.Equal(0, MensagensPara(emailFuncionarioInativo));
            Assert.Equal(0, MensagensPara(emailClienteSemSenha));
            Assert.Equal(0, MensagensPara(emailClienteSemEmail));
        }

        [Fact]
        public async Task Solicitar_LimitePorConta_QuartaNaHoraNaoEnviaEDevolveAMesmaResposta()
        {
            string email = EmailDe(CpfLimite);
            SemearCliente(CpfLimite, email, HashReal(SenhaAntiga));

            List<string> assinaturas = new List<string>();

            for (int tentativa = 0; tentativa < QuantidadeLimiteHttp + 1; tentativa++)
            {
                assinaturas.Add(await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfLimite)));
            }

            await AguardarPedidosAnterioresAsync();

            Assert.Equal(QuantidadeLimiteHttp, MensagensPara(email));

            foreach (string assinatura in assinaturas)
            {
                Assert.Equal(assinaturas[0], assinatura);
            }
        }

        [Fact]
        public async Task Solicitar_FilaCheia_DevolveAMesmaRespostaESemEnvio()
        {
            string email = EmailDe(CpfFila);
            Cliente cliente = SemearCliente(CpfFila, email, HashReal(SenhaAntiga));
            string esperada = await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfInexistente));

            int chamadasAntes = _fabrica.Fila.TotalChamadas;
            _fabrica.Fila.Aceitar = false;
            try
            {
                string assinatura = await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfFila));

                // O link e gravado antes da chamada ao Enfileirar: quando a fila de e-mail foi consultada, o link ja existe.
                await AguardarAsync(() => _fabrica.Fila.TotalChamadas > chamadasAntes);

                Assert.Equal(esperada, assinatura);
                Assert.Contains(_fabrica.Redefinicoes.Redefinicoes, x => x.IdCliente == cliente.IdCliente);
                Assert.Equal(0, MensagensPara(email));
            }
            finally
            {
                _fabrica.Fila.Aceitar = true;
            }
        }

        [Fact]
        public async Task Solicitar_PerfilOuCpfInvalidos_Devolve400ProblemJson()
        {
            HttpResponseMessage semPerfil = await _cliente.PostAsync(RotaSolicitar, CorpoJson(new { cpf = CpfElegivel }));
            HttpResponseMessage cpfCurto = await SolicitarAsync("CLIENTE", CpfCurto);

            Assert.Equal(HttpStatusCode.BadRequest, semPerfil.StatusCode);
            Assert.Equal(MediaProblema, semPerfil.Content.Headers.ContentType?.MediaType);
            Assert.Equal(HttpStatusCode.BadRequest, cpfCurto.StatusCode);
            Assert.Equal(MediaProblema, cpfCurto.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Solicitar_AcimaDoLimitePorIp_Devolve429ProblemJson()
        {
            using RecuperacaoWebFactoryLimitado fabricaLimitada = new RecuperacaoWebFactoryLimitado();
            HttpClient cliente = fabricaLimitada.CreateClient();

            HttpResponseMessage ultima = null!;
            for (int tentativa = 0; tentativa < RecuperacaoWebFactoryLimitado.LimiteDeTeste + 1; tentativa++)
            {
                ultima = await cliente.PostAsync(RotaSolicitar, CorpoJson(new { perfil = "CLIENTE", cpf = CpfInexistente }));
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);
            Assert.Equal(MediaProblema, ultima.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>
        /// Prova da correcao: com a busca da conta PRESA (o banco "lento" espera um portao que so o teste libera), o pedido
        /// responde 202 neutra sem esperar a busca. A busca comeca e fica presa no processamento em segundo plano.
        /// </summary>
        [Fact]
        public async Task Solicitar_BuscaLenta_RespostaNaoEsperaABusca()
        {
            string email = EmailDe(CpfBuscaLenta);
            SemearCliente(CpfBuscaLenta, email, HashReal(SenhaAntiga));
            TaskCompletionSource portao = new TaskCompletionSource();
            int buscasAntes = _fabrica.Repositorio.TotalBuscas;
            _fabrica.Repositorio.Portao = portao;

            try
            {
                Task<HttpResponseMessage> tarefa = SolicitarAsync("CLIENTE", CpfBuscaLenta);

                HttpResponseMessage resposta = await tarefa.WaitAsync(TimeSpan.FromMilliseconds(TempoLimiteRespostaMs));

                Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
                JsonElement corpo = await LerJsonAsync(resposta);
                Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, corpo.GetProperty("mensagem").GetString());

                await AguardarAsync(() => _fabrica.Repositorio.TotalBuscas > buscasAntes);
                Assert.Equal(0, MensagensPara(email));
            }
            finally
            {
                portao.TrySetResult();
                _fabrica.Repositorio.Portao = null;
            }

            await AguardarAsync(() => MensagensPara(email) == 1);
        }

        /// <summary>
        /// Busca que lanca no processamento: a resposta HTTP continua 202 igual, o erro vai so para o log sem o CPF, e o
        /// pedido seguinte da fila ainda e processado (o leitor nao morre).
        /// </summary>
        [Fact]
        public async Task Solicitar_BuscaQueFalha_RespostaIgualELeitorContinua()
        {
            string emailDepois = EmailDe(CpfDepoisDaFalha);
            SemearCliente(CpfDepoisDaFalha, emailDepois, HashReal(SenhaAntiga));
            string esperada = await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfInexistente));

            _fabrica.Repositorio.ExcecaoAoBuscar = new InvalidOperationException(MensagemFalhaBusca);
            try
            {
                string assinatura = await AssinaturaDaRespostaAsync(await SolicitarAsync("CLIENTE", CpfBuscaFalha));

                Assert.Equal(esperada, assinatura);

                await AguardarAsync(() => _fabrica.Log.Snapshot().Any(entrada => entrada.StartsWith(ConstantsMessage.ErroProcessarPedidoRecuperacaoSenha)));
            }
            finally
            {
                _fabrica.Repositorio.ExcecaoAoBuscar = null;
            }

            Assert.DoesNotContain(_fabrica.Log.Snapshot(), entrada => entrada.Contains(CpfBuscaFalha, StringComparison.Ordinal));

            // O leitor segue vivo: o pedido seguinte da fila (conta elegivel) chega ao e-mail.
            await SolicitarAsync("CLIENTE", CpfDepoisDaFalha);
            await AguardarAsync(() => MensagensPara(emailDepois) == 1);
        }

        [Fact]
        public async Task Redefinir_LinkValido_Devolve204_TrocaASenhaEOLoginAntigoFalha()
        {
            string email = EmailDe(CpfElegivelLogin);
            SemearCliente(CpfElegivelLogin, email, HashReal(SenhaAntiga));

            HttpResponseMessage loginAntes = await LoginAsync(CpfElegivelLogin, SenhaAntiga);
            Assert.Equal(HttpStatusCode.OK, loginAntes.StatusCode);

            await SolicitarAsync("CLIENTE", CpfElegivelLogin);
            string token = await TokenEnviadoParaAsync(email);

            HttpResponseMessage redefinicao = await RedefinirAsync(token, SenhaNova, SenhaNova);

            Assert.Equal(HttpStatusCode.NoContent, redefinicao.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(CpfElegivelLogin, SenhaAntiga)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(CpfElegivelLogin, SenhaNova)).StatusCode);
        }

        [Fact]
        public async Task Redefinir_DerrubaOTokenDeSessaoEmitidoAntes()
        {
            string email = EmailDe(CpfTokenAntigo);
            SemearCliente(CpfTokenAntigo, email, HashReal(SenhaAntiga));

            HttpResponseMessage login = await LoginAsync(CpfTokenAntigo, SenhaAntiga);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            string tokenDeSessao = (await LerJsonAsync(login)).GetProperty("token").GetString()!;

            HttpResponseMessage antes = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, tokenDeSessao));
            Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

            await SolicitarAsync("CLIENTE", CpfTokenAntigo);
            HttpResponseMessage redefinicao = await RedefinirAsync(await TokenEnviadoParaAsync(email), SenhaNova, SenhaNova);
            Assert.Equal(HttpStatusCode.NoContent, redefinicao.StatusCode);

            HttpResponseMessage depois = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, tokenDeSessao));

            Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
            Assert.Equal(MediaProblema, depois.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Redefinir_LinkUsadoDeNovo_Devolve409ComAMesmaMensagem()
        {
            string email = EmailDe(CpfTokenUsado);
            SemearCliente(CpfTokenUsado, email, HashReal(SenhaAntiga));
            await SolicitarAsync("CLIENTE", CpfTokenUsado);
            string token = await TokenEnviadoParaAsync(email);

            await RedefinirAsync(token, SenhaNova, SenhaNova);
            HttpResponseMessage reuso = await RedefinirAsync(token, "OutraSenha2026", "OutraSenha2026");

            Assert.Equal(HttpStatusCode.Conflict, reuso.StatusCode);
            Assert.Equal(MediaProblema, reuso.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(reuso);
            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Redefinir_LinkInventado_Devolve409ComAMesmaMensagem()
        {
            HttpResponseMessage resposta = await RedefinirAsync(TokenInventado, SenhaNova, SenhaNova);

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Redefinir_LinkExpiradoAos30MinutosMaisUmSegundo_Devolve409()
        {
            string email = EmailDe(CpfExpirado);
            Cliente cliente = SemearCliente(CpfExpirado, email, HashReal(SenhaAntiga));
            await SolicitarAsync("CLIENTE", CpfExpirado);
            string token = await TokenEnviadoParaAsync(email);

            RedefinicaoSenha gravada = _fabrica.Redefinicoes.Redefinicoes.Last(x => x.IdCliente == cliente.IdCliente);
            gravada.ExpiraEm = DateTime.Now.AddSeconds(-1);

            HttpResponseMessage resposta = await RedefinirAsync(token, SenhaNova, SenhaNova);

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public async Task Redefinir_SenhaFracaOuConfirmacaoDiferente_Devolve400()
        {
            HttpResponseMessage fraca = await RedefinirAsync(TokenInventado, SenhaFraca, SenhaFraca);
            HttpResponseMessage diferente = await RedefinirAsync(TokenInventado, SenhaNova, "OutraSenha2026");

            Assert.Equal(HttpStatusCode.BadRequest, fraca.StatusCode);
            Assert.Equal(MediaProblema, fraca.Content.Headers.ContentType?.MediaType);
            Assert.Equal(HttpStatusCode.BadRequest, diferente.StatusCode);
            Assert.Equal(MediaProblema, diferente.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Redefinir_TitularDesativadoDepoisDoPedido_Devolve409()
        {
            string email = EmailDe(CpfTitularInativo);
            Cliente cliente = SemearCliente(CpfTitularInativo, email, HashReal(SenhaAntiga));
            await SolicitarAsync("CLIENTE", CpfTitularInativo);
            string token = await TokenEnviadoParaAsync(email);

            cliente.Disable();

            HttpResponseMessage resposta = await RedefinirAsync(token, SenhaNova, SenhaNova);

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        }

        [Fact]
        public void Configuracao_UrlPublicaRelativa_ImpedeASubidaDaApi()
        {
            using RecuperacaoWebFactoryUrlInvalida fabricaInvalida = new RecuperacaoWebFactoryUrlInvalida();

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabricaInvalida.CreateClient());

            Assert.NotNull(excecao);
        }

        /// <summary>
        /// Servidor de teste da recuperacao: repositorio de login, repositorio de links, fila de e-mail e log em memoria.
        /// A fila de pedidos, o servico hospedado e o processador sao os reais. O piso de tempo fica em zero (o piso tem
        /// teste proprio no servico). O SessaoService e o SenhaHasher sao os reais.
        /// </summary>
        public class RecuperacaoWebFactory : WebApplicationFactory<Program>
        {
            public const string Emissor = "AutoVia.Api";
            public const string Audiencia = "AutoVia.Web";
            // Ambiente Testing nao e Development: a URL publica precisa ser https fora de loopback (regra da subida).
            public const string UrlFrontTeste = "https://front.exemplo.com";
            public const int LimiteAlto = 1000;

            private readonly int _limitePublico;
            private readonly string _urlPublicaFront;

            public RecuperacaoWebFactory()
                : this(LimiteAlto, UrlFrontTeste)
            {
            }

            protected RecuperacaoWebFactory(int limitePublico, string urlPublicaFront)
            {
                _limitePublico = limitePublico;
                _urlPublicaFront = urlPublicaFront;
                ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                Repositorio = new FakeAutenticacaoRepository();
                Redefinicoes = new FakeRedefinicaoSenhaRepository(Repositorio);
                Fila = new FakeEmailFila();
                Log = new FakeLogService();
            }

            public string ChaveJwt { get; }

            public FakeAutenticacaoRepository Repositorio { get; }

            public FakeRedefinicaoSenhaRepository Redefinicoes { get; }

            public FakeEmailFila Fila { get; }

            /// <summary>Log em memoria: o processamento em segundo plano registra aqui os erros, sem o CPF.</summary>
            public FakeLogService Log { get; }

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
                        { "Autenticacao:LimitePublicoPorMinuto", _limitePublico.ToString(CultureInfo.InvariantCulture) },
                        { "Autenticacao:Recuperacao:UrlPublicaFront", _urlPublicaFront },
                        { "Autenticacao:Recuperacao:TempoMinimoRespostaMs", "0" },
                        { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                    };

                    configuracao.AddInMemoryCollection(valores);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAutenticacaoRepository>();
                    services.AddSingleton<IAutenticacaoRepository>(Repositorio);
                    services.RemoveAll<IRedefinicaoSenhaRepository>();
                    services.AddSingleton<IRedefinicaoSenhaRepository>(Redefinicoes);
                    services.RemoveAll<IEmailFila>();
                    services.AddSingleton<IEmailFila>(Fila);
                    services.RemoveAll<ILogService>();
                    services.AddSingleton<ILogService>(Log);
                    services.AddControllers().AddApplicationPart(typeof(ProbeAutorizacaoController).Assembly);
                });
            }
        }

        /// <summary>Servidor com limite de 2 requisicoes por minuto por IP, para testar o 429.</summary>
        public sealed class RecuperacaoWebFactoryLimitado : RecuperacaoWebFactory
        {
            public const int LimiteDeTeste = 2;

            public RecuperacaoWebFactoryLimitado()
                : base(LimiteDeTeste, UrlFrontTeste)
            {
            }
        }

        /// <summary>Servidor com a URL publica relativa: a validacao na subida tem de impedir a API de iniciar.</summary>
        public sealed class RecuperacaoWebFactoryUrlInvalida : RecuperacaoWebFactory
        {
            public RecuperacaoWebFactoryUrlInvalida()
                : base(LimiteAlto, "/sem-dominio")
            {
            }
        }
    }
}
