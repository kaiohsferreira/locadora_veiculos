using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Core.ServicesInterface;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Tests.Fakes;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Decisao do Kaio (C2): redefinir a senha de um CLIENTE apaga o vinculo com o Google (GoogleSub = null) no mesmo
    /// commit da troca. Depois disso o login pelo Google da mesma conta volta a pedir CPF e senha
    /// (CONFIRMACAO_NECESSARIA). Funcionario nao tem vinculo e nao e afetado. Servicos reais sobre fakes; sem banco.
    /// O repositorio real (RedefinicaoSenhaRepository) nao tem provedor de banco de teste: a linha que zera GoogleSub
    /// e conferida por revisao; aqui o fake espelha essa regra.
    /// </summary>
    public class RedefinicaoApagaVinculoGoogleTests
    {
        private const string UrlFront = "https://front.exemplo.com";
        private const int ValidadeMinutos = 30;
        private const int LimitePorHora = 3;
        private const int TempoMinimoZero = 0;
        private const string ChaveDeTeste = "chave-de-teste-redefinicao-vinculo-google-32-bytes";
        private const string Emissor = "AutoVia.Api";
        private const string Audiencia = "AutoVia.Web";
        private const int ExpiracaoMinutosSessao = 120;
        private const int ExpiracaoLembrarDiasSessao = 30;
        private const string CpfCliente = "52998224725";
        private const string CpfFuncionario = "39053344705";
        private const string EmailCliente = "maria.vinculo@exemplo.com";
        private const string EmailFuncionario = "ana.vinculo@exemplo.com";
        private const string NomeCliente = "Maria Vinculo";
        private const string NomeFuncionario = "Ana Vinculo";
        private const string MatriculaFuncionario = "F-901";
        private const string SenhaAntiga = "SenhaAntiga1";
        private const string SenhaNova = "SenhaNova2026";
        private const string SubGoogle = "google-sub-vinculo-c2";
        private const string CredencialGoogle = "credencial-vinculo-c2";
        private const string PadraoToken = "#token=([A-Za-z0-9_-]+)";

        /// <summary>Servicos reais de recuperacao e de login Google sobre os mesmos fakes (mesmo titular em memoria).</summary>
        private sealed class Cenario
        {
            public Cenario()
            {
                Titulares = new FakeAutenticacaoRepository();
                Redefinicoes = new FakeRedefinicaoSenhaRepository(Titulares);
                Hasher = new FakeSenhaHasher();
                Fila = new FakeEmailFila();
                Sessao = new FakeSessaoService();
                Log = new FakeLogService();

                RecuperacaoSenhaOptions opcoes = new RecuperacaoSenhaOptions
                {
                    UrlPublicaFront = UrlFront,
                    ValidadeMinutos = ValidadeMinutos,
                    LimitePorHora = LimitePorHora,
                    TempoMinimoRespostaMs = TempoMinimoZero
                };
                FilaPedidos = new FakeRecuperacaoSenhaFila();
                Recuperacao = new RecuperacaoSenhaService(Redefinicoes, Hasher, Sessao, FilaPedidos, opcoes, Log);
                Processador = new RecuperacaoSenhaProcessador(Titulares, Redefinicoes, Fila, opcoes, Log);

                Google = new FakeGoogleIdentityGateway();
                Vinculos = new FakeGoogleVinculoRepository(Titulares);

                JwtOptions jwt = new JwtOptions
                {
                    Emissor = Emissor,
                    Audiencia = Audiencia,
                    ExpiracaoMinutos = ExpiracaoMinutosSessao,
                    ExpiracaoLembrarDias = ExpiracaoLembrarDiasSessao,
                    Chave = ChaveDeTeste
                };
                AutenticacaoService autenticacao = new AutenticacaoService(Titulares, Hasher, new TokenService(jwt, Log), Log);
                LoginGoogle = new AutenticacaoGoogleService(Google, Vinculos, Titulares, autenticacao, Hasher, Log);

                Google.Aceitar(CredencialGoogle, new GoogleIdentidade
                {
                    Sub = SubGoogle,
                    Email = EmailCliente,
                    EmailVerificado = true,
                    Nome = NomeCliente
                });
            }

            public FakeAutenticacaoRepository Titulares { get; }

            public FakeRedefinicaoSenhaRepository Redefinicoes { get; }

            public FakeSenhaHasher Hasher { get; }

            public FakeEmailFila Fila { get; }

            public FakeSessaoService Sessao { get; }

            public FakeLogService Log { get; }

            public FakeGoogleIdentityGateway Google { get; }

            public FakeGoogleVinculoRepository Vinculos { get; }

            public FakeRecuperacaoSenhaFila FilaPedidos { get; }

            public RecuperacaoSenhaService Recuperacao { get; }

            public RecuperacaoSenhaProcessador Processador { get; }

            public AutenticacaoGoogleService LoginGoogle { get; }

            /// <summary>Processa em ordem os pedidos enfileirados pela requisicao (o processamento real roda em segundo plano).</summary>
            public async Task ProcessarPendentesAsync()
            {
                List<PedidoRecuperacaoSenha> pendentes = new List<PedidoRecuperacaoSenha>(FilaPedidos.Pendentes);

                FilaPedidos.Pendentes.Clear();

                foreach (PedidoRecuperacaoSenha pedido in pendentes)
                {
                    await Processador.ProcessarAsync(pedido, CancellationToken.None);
                }
            }

            /// <summary>Cliente ativo com senha e com o vinculo Google informado.</summary>
            public Cliente SemearClienteVinculado()
            {
                Cliente cliente = new Cliente
                {
                    Nome = NomeCliente,
                    Cpf = CpfCliente,
                    Email = EmailCliente,
                    SenhaHash = Hasher.Hash(SenhaAntiga),
                    StatusCadastro = StatusCadastroCliente.APROVADO,
                    GoogleSub = SubGoogle
                };

                Titulares.AdicionarCliente(cliente);

                return cliente;
            }

            /// <summary>Funcionario ativo com senha (sem vinculo Google: a entidade nao tem essa coluna).</summary>
            public Funcionario SemearFuncionario()
            {
                Funcionario funcionario = new Funcionario
                {
                    Nome = NomeFuncionario,
                    Cpf = CpfFuncionario,
                    Matricula = MatriculaFuncionario,
                    Cargo = CargoFuncionario.ATENDENTE,
                    Email = EmailFuncionario,
                    Ativo = true,
                    SenhaHash = Hasher.Hash(SenhaAntiga)
                };

                Titulares.AdicionarFuncionario(funcionario);

                return funcionario;
            }
        }

        /// <summary>Solicita a recuperacao do perfil e devolve o token do e-mail enfileirado.</summary>
        private static async Task<string> SolicitarEObterTokenAsync(Cenario cenario, PerfilUsuario perfil, string cpf)
        {
            await cenario.Recuperacao.SolicitarAsync(new SolicitarRecuperacaoSenhaVO { Perfil = perfil, Cpf = cpf });
            await cenario.ProcessarPendentesAsync();

            EmailMensagem mensagem = cenario.Fila.Mensagens[cenario.Fila.Mensagens.Count - 1];
            Match correspondencia = Regex.Match(mensagem.CorpoTexto, PadraoToken);

            Assert.True(correspondencia.Success, "O link do e-mail deve conter #token=.");

            return correspondencia.Groups[1].Value;
        }

        [Fact]
        public async Task RedefinicaoDoCliente_ApagaOGoogleSub_E_ProximoLoginGoogleVoltaAPedirCpfESenha()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearClienteVinculado();

            ReturnGoogleLoginVO antes = await cenario.LoginGoogle.LoginAsync(new GoogleLoginVO { Credencial = CredencialGoogle });
            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO, antes.Situacao);

            string token = await SolicitarEObterTokenAsync(cenario, PerfilUsuario.CLIENTE, CpfCliente);
            await cenario.Recuperacao.RedefinirAsync(new RedefinirSenhaVO { Token = token, NovaSenha = SenhaNova, ConfirmacaoSenha = SenhaNova });

            Assert.Null(cliente.GoogleSub);

            ReturnGoogleLoginVO depois = await cenario.LoginGoogle.LoginAsync(new GoogleLoginVO { Credencial = CredencialGoogle });

            Assert.Equal(SituacaoLoginGoogle.CONFIRMACAO_NECESSARIA, depois.Situacao);
            Assert.Null(depois.Sessao);
        }

        [Fact]
        public async Task RedefinicaoDeFuncionario_NaoAlteraOVinculoGoogleDeUmCliente()
        {
            Cenario cenario = new Cenario();
            Cliente cliente = cenario.SemearClienteVinculado();
            cenario.SemearFuncionario();

            string token = await SolicitarEObterTokenAsync(cenario, PerfilUsuario.FUNCIONARIO, CpfFuncionario);
            await cenario.Recuperacao.RedefinirAsync(new RedefinirSenhaVO { Token = token, NovaSenha = SenhaNova, ConfirmacaoSenha = SenhaNova });

            Assert.Equal(SubGoogle, cliente.GoogleSub);
            Assert.Equal(1, cenario.Redefinicoes.Concluidas);
        }
    }
}
