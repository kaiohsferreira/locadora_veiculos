using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Tests.Fakes;
using System.Security.Cryptography;
using System.Text.Json;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Servico do login com Google (N3, RF-51) com fakes: prova do Google, repositorios de login e de vinculo, log.
    /// Cobre a situacao de cada caso (AUTENTICADO, CONFIRMACAO, CADASTRO, AMBIGUA), o vinculo (409, idempotencia,
    /// senha, e-mail), a validade da sessao (lembrar) e o log sem dado pessoal. Sem rede e sem PostgreSQL.
    /// </summary>
    public class AutenticacaoGoogleServiceTests
    {
        private const string SubGoogle = "google-sub-servico-1";
        private const string SubOutraConta = "google-sub-servico-2";
        private const string EmailGoogle = "maria.servico@exemplo.com";
        private const string NomeGoogle = "Maria Servico";
        private const string CredencialValida = "credencial-servico-valida";
        private const string CredencialOutra = "credencial-servico-outra";
        private const string CpfCliente = "52998224725";
        private const string CpfOutro = "11144477735";
        private const string SenhaCliente = "SenhaCliente#2026";
        private const string NomeCliente = "Cliente Cadastrado";
        private const string EmailOutro = "outro.servico@exemplo.com";
        private const string CpfInexistente = "39053344705";
        private const string MensagemFalhaBanco = "Falha simulada do banco.";
        private const string ChaveDeTeste = "chave-de-teste-do-servico-google-32-bytes-ou-mais";

        private sealed class Cenario
        {
            public Cenario()
            {
                Titulares = new FakeAutenticacaoRepository();
                Vinculos = new FakeGoogleVinculoRepository(Titulares);
                Google = new FakeGoogleIdentityGateway();
                Log = new FakeLogService();
                Hasher = new FakeSenhaHasher();

                JwtOptions opcoes = new JwtOptions
                {
                    Emissor = "AutoVia.Api",
                    Audiencia = "AutoVia.Web",
                    ExpiracaoMinutos = 120,
                    ExpiracaoLembrarDias = 30,
                    Chave = ChaveDeTeste
                };

                AutenticacaoService autenticacao = new AutenticacaoService(Titulares, Hasher, new TokenService(opcoes, Log), Log);
                Servico = new AutenticacaoGoogleService(Google, Vinculos, Titulares, autenticacao, Hasher, Log);

                Google.Aceitar(CredencialValida, Identidade(SubGoogle, EmailGoogle));
                Google.Aceitar(CredencialOutra, Identidade(SubOutraConta, EmailOutro));
            }

            public FakeAutenticacaoRepository Titulares { get; }

            public FakeGoogleVinculoRepository Vinculos { get; }

            public FakeGoogleIdentityGateway Google { get; }

            public FakeLogService Log { get; }

            public FakeSenhaHasher Hasher { get; }

            public AutenticacaoGoogleService Servico { get; }

            /// <summary>Semeia um cliente com senha (hash de teste), CPF e e-mail informados.</summary>
            public Cliente SemearCliente(string cpf, string email, string? googleSub = null, StatusCadastroCliente status = StatusCadastroCliente.APROVADO, string? senha = SenhaCliente)
            {
                Cliente cliente = new Cliente
                {
                    Nome = NomeCliente,
                    Cpf = cpf,
                    Email = email,
                    SenhaHash = senha == null ? null : Hasher.Hash(senha),
                    StatusCadastro = status,
                    GoogleSub = googleSub
                };

                Titulares.AdicionarCliente(cliente);

                return cliente;
            }
        }

        private static GoogleIdentidade Identidade(string sub, string email)
        {
            return new GoogleIdentidade
            {
                Sub = sub,
                Email = email,
                EmailVerificado = true,
                Nome = NomeGoogle
            };
        }

        private static GoogleLoginVO Login(string credencial, bool lembrar = false)
        {
            return new GoogleLoginVO { Credencial = credencial, Lembrar = lembrar };
        }

        private static GoogleVinculoVO Vinculo(string credencial, string cpf, string senha, bool lembrar = false)
        {
            return new GoogleVinculoVO { Credencial = credencial, Cpf = cpf, Senha = senha, Lembrar = lembrar };
        }

        // ------------------------------------------------------------------
        // Login: situacoes
        // ------------------------------------------------------------------

        [Fact]
        public async Task Login_ContaVinculadaComClienteAtivo_EntraDiretoComSessao()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO, retorno.Situacao);
            Assert.NotNull(retorno.Sessao);
            Assert.Equal(cliente.IdCliente, retorno.Sessao!.IdUsuario);
            Assert.Equal(PerfilUsuario.CLIENTE, retorno.Sessao.Perfil);
            Assert.Null(retorno.Nome);
            Assert.Null(retorno.Email);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Theory]
        [InlineData(StatusCadastroCliente.PENDENTE)]
        [InlineData(StatusCadastroCliente.RECUSADO)]
        public async Task Login_ClientePendenteOuRecusado_TambemEntra(StatusCadastroCliente status)
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle, status);

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO, retorno.Situacao);
            Assert.NotNull(retorno.Sessao);
        }

        [Fact]
        public async Task Login_ClienteDesativadoComMesmoEmail_ContaComoInexistente()
        {
            Cenario cenario = new Cenario();
            Cliente desativado = cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);
            desativado.Disable();

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.CADASTRO_NECESSARIO, retorno.Situacao);
            Assert.Null(retorno.Sessao);
        }

        [Fact]
        public async Task Login_SemCadastroComOEmail_DevolveCadastroNecessarioSemCriarNada()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfOutro, EmailOutro);
            int clientesAntes = cenario.Titulares.Clientes.Count;

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.CADASTRO_NECESSARIO, retorno.Situacao);
            Assert.Null(retorno.Sessao);
            Assert.Equal(NomeGoogle, retorno.Nome);
            Assert.Equal(EmailGoogle, retorno.Email);
            Assert.Equal(clientesAntes, cenario.Titulares.Clientes.Count);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Login_UmCadastroComOEmail_ConfirmacaoNecessariaSemVincular()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle);

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.CONFIRMACAO_NECESSARIA, retorno.Situacao);
            Assert.Null(retorno.Sessao);
            Assert.Equal(NomeGoogle, retorno.Nome);
            Assert.Equal(EmailGoogle, retorno.Email);
            Assert.NotEqual(cliente.Nome, retorno.Nome);
            Assert.Null(cliente.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Login_DoisCadastrosComOEmail_ContaAmbiguaSemNomeNemEmail()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);
            cenario.SemearCliente(CpfOutro, EmailGoogle);

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.CONTA_AMBIGUA, retorno.Situacao);
            Assert.Null(retorno.Sessao);
            Assert.Null(retorno.Nome);
            Assert.Null(retorno.Email);
        }

        [Fact]
        public async Task Login_ProvaInvalida_RecusaSemGravarNada()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.LoginAsync(Login("credencial-inventada")));

            Assert.Equal(ConstantsMessage.ErroGoogleCredencialInvalida, excecao.Message);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Login_GoogleIndisponivel_RepassaIntegrationException()
        {
            Cenario cenario = new Cenario();
            cenario.Google.ExcecaoAoValidar = new IntegrationException("GOOGLE", ConstantsMessage.ErroGoogleIndisponivel);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.LoginAsync(Login(CredencialValida)));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task Login_FalhaDoBanco_NaoViraUnauthorizedERegistraNoLog()
        {
            Cenario cenario = new Cenario();
            cenario.Vinculos.ExcecaoAoBuscar = new InvalidOperationException(MensagemFalhaBanco);

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.LoginAsync(Login(CredencialValida)));

            Assert.Contains(cenario.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLoginGoogle));
        }

        /// <summary>
        /// Cancelamento do cliente (1c): o token da requisicao chega ao gateway, a operacao para com OperationCanceledException
        /// (sem embrulhar em IntegrationException) e nada e registrado como ErroLoginGoogle.
        /// </summary>
        [Fact]
        public async Task TokenCancelado_ChegaAoGateway_EInterrompeSemEntrarNoLogDeErro()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cenario.Servico.LoginAsync(Login(CredencialValida), cancelamento.Token));

            Assert.Equal(cancelamento.Token, cenario.Google.UltimoTokenRecebido);
            Assert.DoesNotContain(cenario.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLoginGoogle, StringComparison.Ordinal));
        }

        /// <summary>
        /// Decisao do Kaio (C4): no login direto pelo Google o 'lembrar' e IGNORADO e a sessao e a curta (120 min).
        /// Antes, este teste esperava 30 dias; a prova do Google e reaproveitavel ate o exp, entao so o vinculo
        /// (com CPF e senha provados) pode abrir sessao de 30 dias.
        /// </summary>
        [Fact]
        public async Task Login_LembrarVerdadeiro_SessaoCurtaPorDecisaoC4()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);
            DateTime antes = DateTime.Now;

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida, lembrar: true));

            Assert.NotNull(retorno.Sessao);
            Assert.InRange(retorno.Sessao!.ExpiraEm, antes.AddMinutes(119), antes.AddMinutes(121));
        }

        [Fact]
        public async Task Login_LembrarFalso_SessaoDeCentoEVinteMinutos()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);
            DateTime antes = DateTime.Now;

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida, lembrar: false));

            Assert.NotNull(retorno.Sessao);
            Assert.InRange(retorno.Sessao!.ExpiraEm, antes.AddMinutes(119), antes.AddMinutes(121));
        }

        [Fact]
        public async Task Login_DepoisDeVinculado_MudarOEmailNaoImpedeOLoginPeloSub()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);
            cliente.Email = EmailOutro;

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO, retorno.Situacao);
            Assert.NotNull(retorno.Sessao);
        }

        // ------------------------------------------------------------------
        // Vinculo
        // ------------------------------------------------------------------

        [Fact]
        public async Task Vinculo_CpfESenhaCertos_GravaOSubEEmiteSessao()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle);

            ReturnLoginVO sessao = await cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente));

            Assert.Equal(PerfilUsuario.CLIENTE, sessao.Perfil);
            Assert.Equal(cliente.IdCliente, sessao.IdUsuario);
            Assert.False(string.IsNullOrWhiteSpace(sessao.Token));
            Assert.Equal(SubGoogle, cliente.GoogleSub);
            Assert.Equal(1, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_ComSucesso_SegundoLoginEntraDireto()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle);
            await cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente));

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO, retorno.Situacao);
            Assert.Equal(cliente.IdCliente, retorno.Sessao!.IdUsuario);
        }

        [Fact]
        public async Task Vinculo_CpfMascarado_AceitaOMesmoCliente()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);

            ReturnLoginVO sessao = await cenario.Servico.VincularAsync(Vinculo(CredencialValida, "529.982.247-25", SenhaCliente));

            Assert.Equal(PerfilUsuario.CLIENTE, sessao.Perfil);
        }

        [Fact]
        public async Task Vinculo_RecusasDePrimeiroNivel_DevolvemAMesmaMensagemSemGravar()
        {
            Cenario cenario = new Cenario();
            Cliente semSenha = cenario.SemearCliente(CpfOutro, EmailGoogle, senha: null);
            Cliente emailDiferente = cenario.SemearCliente(CpfCliente, EmailOutro);

            List<UnauthorizedException> recusas = new List<UnauthorizedException>
            {
                await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, "SenhaErrada#2026"))),
                await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfInexistente, SenhaCliente))),
                await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfOutro, SenhaCliente))),
                await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente)))
            };

            foreach (UnauthorizedException recusa in recusas)
            {
                Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, recusa.Message);
            }

            Assert.Null(semSenha.GoogleSub);
            Assert.Null(emailDiferente.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_ProvaInvalida_RecusaSemGravarEAntesDeConferirSenha()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo("credencial-inventada", CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroGoogleCredencialInvalida, excecao.Message);
            Assert.Null(cliente.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_ContaGoogleJaVinculadaAOutroCliente_Devolve409()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfOutro, EmailOutro, SubGoogle);
            Cliente alvo = cenario.SemearCliente(CpfCliente, EmailGoogle);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroGoogleContaJaVinculada, excecao.Message);
            Assert.Null(alvo.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_ClienteJaVinculadoOutraContaGoogle_Devolve409()
        {
            Cenario cenario = new Cenario();
            Cliente alvo = cenario.SemearCliente(CpfCliente, EmailGoogle, SubOutraConta);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroGoogleClienteJaVinculado, excecao.Message);
            Assert.Equal(SubOutraConta, alvo.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_MesmoSubJaGravado_EIdempotenteEEmiteSessao()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearCliente(CpfCliente, EmailGoogle, SubGoogle);

            ReturnLoginVO sessao = await cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente));

            Assert.Equal(cliente.IdCliente, sessao.IdUsuario);
            Assert.Equal(SubGoogle, cliente.GoogleSub);
            Assert.Equal(0, cenario.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task Vinculo_DoisCadastrosComOEmail_Devolve409ComAmbigua()
        {
            Cenario cenario = new Cenario();
            Cliente alvo = cenario.SemearCliente(CpfCliente, EmailGoogle);
            cenario.SemearCliente(CpfOutro, EmailGoogle);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroGoogleContaAmbigua, excecao.Message);
            Assert.Null(alvo.GoogleSub);
        }

        [Fact]
        public async Task Vinculo_LembrarVerdadeiro_SessaoDeTrintaDias()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);
            DateTime antes = DateTime.Now;

            ReturnLoginVO sessao = await cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente, lembrar: true));

            Assert.InRange(sessao.ExpiraEm, antes.AddDays(30).AddMinutes(-1), antes.AddDays(30).AddMinutes(1));
        }

        [Fact]
        public async Task Vinculo_FalhaDoBancoAoGravar_NaoEmiteSessaoERegistraNoLog()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);
            cenario.Vinculos.ExcecaoAoGravar = new InvalidOperationException(MensagemFalhaBanco);

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente)));

            Assert.Contains(cenario.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroVincularGoogle));
        }

        // ------------------------------------------------------------------
        // Configuracao publica
        // ------------------------------------------------------------------

        [Fact]
        public async Task Configuracao_Desabilitada_NaoDevolveClientIdNemModoTeste()
        {
            Cenario cenario = new Cenario();
            cenario.Google.Configuracao = new GoogleConfiguracaoPublica { Habilitado = false, ClientId = "vazaria", ModoTeste = true };

            ReturnGoogleConfiguracaoVO retorno = await cenario.Servico.ObterConfiguracaoAsync();

            Assert.False(retorno.Habilitado);
            Assert.Null(retorno.ClientId);
            Assert.False(retorno.ModoTeste);
        }

        [Fact]
        public async Task Configuracao_Real_DevolveClientIdSemModoTeste()
        {
            Cenario cenario = new Cenario();
            cenario.Google.Configuracao = new GoogleConfiguracaoPublica { Habilitado = true, ClientId = "cliente-real", ModoTeste = false };

            ReturnGoogleConfiguracaoVO retorno = await cenario.Servico.ObterConfiguracaoAsync();

            Assert.True(retorno.Habilitado);
            Assert.Equal("cliente-real", retorno.ClientId);
            Assert.False(retorno.ModoTeste);
        }

        [Fact]
        public async Task Configuracao_Teste_DevolveModoTeste()
        {
            Cenario cenario = new Cenario();
            cenario.Google.Configuracao = new GoogleConfiguracaoPublica { Habilitado = true, ClientId = "cliente-teste", ModoTeste = true };

            ReturnGoogleConfiguracaoVO retorno = await cenario.Servico.ObterConfiguracaoAsync();

            Assert.True(retorno.Habilitado);
            Assert.Equal("cliente-teste", retorno.ClientId);
            Assert.True(retorno.ModoTeste);
        }

        // ------------------------------------------------------------------
        // Log e forma das respostas
        // ------------------------------------------------------------------

        [Fact]
        public async Task Log_NaoRegistraCredencialSubEmailNomeCpfOuSenhaNemNosErros()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);

            await cenario.Servico.LoginAsync(Login(CredencialValida));
            await cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, SenhaCliente));
            await cenario.Servico.LoginAsync(Login(CredencialValida));
            await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialValida, CpfCliente, "SenhaErrada#2026")));
            await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.LoginAsync(Login("credencial-inventada")));
            await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.VincularAsync(Vinculo(CredencialOutra, CpfCliente, SenhaCliente)));

            Assert.NotEmpty(cenario.Log.Entries);
            string[] proibidos = { SubGoogle, SubOutraConta, EmailGoogle, EmailOutro, NomeGoogle, NomeCliente, CpfCliente, SenhaCliente, CredencialValida, CredencialOutra };

            foreach (string dado in proibidos)
            {
                Assert.DoesNotContain(cenario.Log.Entries, entrada => entrada.Contains(dado, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public async Task RespostasSemSessao_TemSoOsCamposDaSituacaoEMaisNenhumDadoDeCadastro()
        {
            Cenario cenario = new Cenario();
            cenario.SemearCliente(CpfCliente, EmailGoogle);

            ReturnGoogleLoginVO retorno = await cenario.Servico.LoginAsync(Login(CredencialValida));

            Assert.Equal(SituacaoLoginGoogle.CONFIRMACAO_NECESSARIA, retorno.Situacao);
            Assert.Null(retorno.Sessao);

            JsonSerializerOptions opcoes = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            using JsonDocument documento = JsonDocument.Parse(JsonSerializer.Serialize(retorno, opcoes));
            List<string> campos = documento.RootElement.EnumerateObject().Select(propriedade => propriedade.Name).ToList();

            Assert.Equal(new List<string> { "situacao", "sessao", "nome", "email" }, campos);
            Assert.DoesNotContain("cpf", campos);
            Assert.DoesNotContain("idCliente", campos);
            Assert.DoesNotContain("telefone", campos);
            Assert.DoesNotContain(cenario.Log.Entries, entrada => entrada.Contains(NomeCliente, StringComparison.Ordinal));
        }
    }
}
