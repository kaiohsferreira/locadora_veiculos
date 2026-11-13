using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using static AutoVia.Tests.Autenticacao.RecuperacaoAmbiente;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Processador da recuperacao de senha (N1, RF-49) com fakes, sem banco e sem rede. Cobre o que roda em segundo
    /// plano: busca da conta elegivel, limite de 3 por hora (janela de 59 e 61 minutos), gravacao so do hash do
    /// token, invalidacao dos links anteriores, fila de e-mail cheia, funcionario, log sem dado pessoal nos caminhos
    /// de erro e cancelamento. Os casos de requisicao (resposta, fila de pedidos, piso de tempo) estao em
    /// RecuperacaoSenhaServiceTests.
    /// </summary>
    public class RecuperacaoSenhaProcessadorTests
    {
        /// <summary>Processa um pedido direto no processador, como o servico hospedado faz em cada escopo.</summary>
        private static async Task ProcessarAsync(RecuperacaoAmbiente ambiente, PerfilUsuario? perfil, string cpf)
        {
            await ambiente.Processador.ProcessarAsync(new PedidoRecuperacaoSenha(perfil, cpf), CancellationToken.None);
        }

        /// <summary>
        /// Solicitacao para um caso nao elegivel: a resposta da requisicao e a neutra e, depois do processamento,
        /// nada entra na fila de e-mail nem no banco de links.
        /// </summary>
        private static async Task<ReturnRecuperacaoSolicitadaVO> SolicitarSemEnvioAsync(RecuperacaoAmbiente ambiente, PerfilUsuario perfil, string cpf)
        {
            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(perfil, cpf));

            await ambiente.ProcessarPendentesAsync();

            Assert.Empty(ambiente.Fila.Mensagens);
            Assert.Empty(ambiente.Redefinicoes.Redefinicoes);

            return retorno;
        }

        /// <summary>Grava solicitacoes do titular com CreatedAt recuado (a janela do limite e contada pelo CreatedAt gravado).</summary>
        private static async Task SemearSolicitacoesAntigasAsync(RecuperacaoAmbiente ambiente, Cliente cliente, int quantidade, int minutosAtras)
        {
            for (int indice = 0; indice < quantidade; indice++)
            {
                RedefinicaoSenha nova = new RedefinicaoSenha
                {
                    IdCliente = cliente.IdCliente,
                    TokenHash = HashSha256Hex($"semente-{indice}-{minutosAtras}"),
                    ExpiraEm = DateTime.Now.AddMinutes(ValidadeMinutos)
                };

                RedefinicaoSenha gravada = await ambiente.Redefinicoes.InsertInvalidandoAnterioresAsync(nova);
                gravada.CreatedAt = DateTime.Now.AddMinutes(-minutosAtras);
            }
        }

        [Fact]
        public async Task ContaElegivel_EnfileiraUmaMensagemComLinkETokenCujoHashFoiGravado()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Cliente cliente = ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            DateTime antes = DateTime.Now;

            await ProcessarAsync(ambiente, PerfilUsuario.CLIENTE, CpfCliente);

            DateTime depois = DateTime.Now;
            EmailMensagem mensagem = Assert.Single(ambiente.Fila.Mensagens);
            Assert.Equal(EmailCliente, mensagem.DestinatarioEmail);
            Assert.Equal(ConstantsMessage.EmailRecuperacaoAssunto, mensagem.Assunto);
            Assert.Contains("#token=", mensagem.CorpoTexto);
            Assert.Contains("#token=", mensagem.CorpoHtml);
            Assert.Contains(UrlFront + "/redefinir-senha#token=", mensagem.CorpoTexto);

            string token = ExtrairToken(mensagem.CorpoTexto);
            RedefinicaoSenha gravada = Assert.Single(ambiente.Redefinicoes.Redefinicoes);

            Assert.Equal(HashSha256Hex(token), gravada.TokenHash);
            Assert.NotEqual(token, gravada.TokenHash);
            Assert.Equal(ConstantsValues.TamanhoTokenHash, gravada.TokenHash.Length);
            Assert.Equal(cliente.IdCliente, gravada.IdCliente);
            Assert.Null(gravada.IdFuncionario);
            Assert.Null(gravada.DisabledAt);
            Assert.InRange(gravada.ExpiraEm, antes.AddMinutes(ValidadeMinutos), depois.AddMinutes(ValidadeMinutos));
        }

        [Fact]
        public async Task CasosNaoElegiveis_DevolvemAMesmaRespostaENadaEEnviado()
        {
            List<ReturnRecuperacaoSolicitadaVO> respostas = new List<ReturnRecuperacaoSolicitadaVO>();

            RecuperacaoAmbiente cpfInexistente = new RecuperacaoAmbiente();
            cpfInexistente.AdicionarCliente(CpfOutro, EmailCliente, cpfInexistente.Hasher.Hash(SenhaAntiga));
            respostas.Add(await SolicitarSemEnvioAsync(cpfInexistente, PerfilUsuario.CLIENTE, CpfCliente));

            RecuperacaoAmbiente desativado = new RecuperacaoAmbiente();
            Cliente clienteDesativado = desativado.AdicionarCliente(CpfCliente, EmailCliente, desativado.Hasher.Hash(SenhaAntiga));
            clienteDesativado.Disable();
            respostas.Add(await SolicitarSemEnvioAsync(desativado, PerfilUsuario.CLIENTE, CpfCliente));

            RecuperacaoAmbiente funcionarioInativo = new RecuperacaoAmbiente();
            funcionarioInativo.AdicionarFuncionario(CpfFuncionario, EmailFuncionario, funcionarioInativo.Hasher.Hash(SenhaAntiga), false);
            respostas.Add(await SolicitarSemEnvioAsync(funcionarioInativo, PerfilUsuario.FUNCIONARIO, CpfFuncionario));

            RecuperacaoAmbiente funcionarioSemEmail = new RecuperacaoAmbiente();
            funcionarioSemEmail.AdicionarFuncionario(CpfFuncionarioSemEmail, null, funcionarioSemEmail.Hasher.Hash(SenhaAntiga), true);
            respostas.Add(await SolicitarSemEnvioAsync(funcionarioSemEmail, PerfilUsuario.FUNCIONARIO, CpfFuncionarioSemEmail));

            RecuperacaoAmbiente semSenhaHash = new RecuperacaoAmbiente();
            semSenhaHash.AdicionarCliente(CpfCliente, EmailCliente, null);
            respostas.Add(await SolicitarSemEnvioAsync(semSenhaHash, PerfilUsuario.CLIENTE, CpfCliente));

            RecuperacaoAmbiente clienteSemEmail = new RecuperacaoAmbiente();
            clienteSemEmail.AdicionarCliente(CpfCliente, "   ", clienteSemEmail.Hasher.Hash(SenhaAntiga));
            respostas.Add(await SolicitarSemEnvioAsync(clienteSemEmail, PerfilUsuario.CLIENTE, CpfCliente));

            RecuperacaoAmbiente perfilErrado = new RecuperacaoAmbiente();
            perfilErrado.AdicionarCliente(CpfCliente, EmailCliente, perfilErrado.Hasher.Hash(SenhaAntiga));
            respostas.Add(await SolicitarSemEnvioAsync(perfilErrado, PerfilUsuario.FUNCIONARIO, CpfCliente));

            Assert.Equal(7, respostas.Count);

            foreach (ReturnRecuperacaoSolicitadaVO resposta in respostas)
            {
                Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, resposta.Mensagem);
            }
        }

        /// <summary>Perfil ausente no pedido: nao ha conta a localizar, nada e consultado, gravado ou enviado.</summary>
        [Fact]
        public async Task PerfilAusente_NaoConsultaNemGravaNemEnvia()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            await ProcessarAsync(ambiente, null, CpfCliente);

            Assert.Equal(0, ambiente.Autenticacao.TotalBuscas);
            Assert.Empty(ambiente.Fila.Mensagens);
            Assert.Empty(ambiente.Redefinicoes.Redefinicoes);
        }

        /// <summary>Cancelamento (desligamento) antes de buscar: nao consulta a conta e nao e registrado como erro.</summary>
        [Fact]
        public async Task CancelamentoAntesDeBuscar_NaoConsultaAContaENaoEhRegistradoComoErro()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ambiente.Processador.ProcessarAsync(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfCliente), cancelamento.Token));

            Assert.Equal(0, ambiente.Autenticacao.TotalBuscas);
            Assert.Empty(ambiente.Log.Entries);
        }

        [Fact]
        public async Task FilaCheia_RespostaIgual_NadaEnviadoELogRegistrado()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.Fila.Aceitar = false;

            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            await ambiente.ProcessarPendentesAsync();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            Assert.Empty(ambiente.Fila.Mensagens);
            Assert.Contains(ambiente.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroEnviarEmailRecuperacao));
        }

        [Fact]
        public async Task LimiteDeTresPorHora_QuartaSolicitacaoNaHoraNaoEnvia()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            for (int tentativa = 0; tentativa < LimitePorHora; tentativa++)
            {
                await ambiente.SolicitarEProcessarAsync(PerfilUsuario.CLIENTE, CpfCliente);
            }

            Assert.Equal(LimitePorHora, ambiente.Fila.Mensagens.Count);

            ReturnRecuperacaoSolicitadaVO quarta = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            await ambiente.ProcessarPendentesAsync();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, quarta.Mensagem);
            Assert.Equal(LimitePorHora, ambiente.Fila.Mensagens.Count);
        }

        [Fact]
        public async Task SegundaSolicitacao_InvalidaOPrimeiroLink()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            string primeiroToken = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            string segundoToken = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);

            Assert.Equal(2, ambiente.Redefinicoes.Redefinicoes.Count);
            Assert.NotNull(ambiente.Redefinicoes.Redefinicoes[0].DisabledAt);
            Assert.Null(ambiente.Redefinicoes.Redefinicoes[1].DisabledAt);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(primeiroToken, SenhaNova)));
            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);

            await ambiente.Servico.RedefinirAsync(Redefinicao(segundoToken, SenhaNova));
            Assert.Equal(1, ambiente.Redefinicoes.Concluidas);
        }

        /// <summary>
        /// Janela do limite (1 hora): solicitacoes de 59 min atras ainda contam e batem no limite; as de 61 min atras
        /// ja nao contam e o pedido e enviado.
        /// </summary>
        [Fact]
        public async Task JanelaDoLimite_SolicitacaoDe59MinutosConta_SolicitacaoDe61MinutosNaoConta()
        {
            RecuperacaoAmbiente dentro = new RecuperacaoAmbiente();
            Cliente clienteDentro = dentro.AdicionarCliente(CpfCliente, EmailCliente, dentro.Hasher.Hash(SenhaAntiga));
            await SemearSolicitacoesAntigasAsync(dentro, clienteDentro, LimitePorHora, MinutosDentroDaJanela);

            await dentro.SolicitarEProcessarAsync(PerfilUsuario.CLIENTE, CpfCliente);

            Assert.Empty(dentro.Fila.Mensagens);

            RecuperacaoAmbiente fora = new RecuperacaoAmbiente();
            Cliente clienteFora = fora.AdicionarCliente(CpfCliente, EmailCliente, fora.Hasher.Hash(SenhaAntiga));
            await SemearSolicitacoesAntigasAsync(fora, clienteFora, LimitePorHora, MinutosForaDaJanela);

            await fora.SolicitarEProcessarAsync(PerfilUsuario.CLIENTE, CpfCliente);

            Assert.Single(fora.Fila.Mensagens);
        }

        /// <summary>
        /// Fila de e-mail cheia: o link FICA gravado e CONTA no limite por hora. Nao e enviado, mas gasta uma das tres
        /// solicitacoes da hora; a quarta, mesmo com a fila livre, nao gera link. Comportamento deliberado: a resposta
        /// continua igual e o usuario tenta de novo depois da janela.
        /// </summary>
        [Fact]
        public async Task FilaCheia_LinkFicaGravadoEContaNoLimite_NaoEnviaENaoLiberaNovaTentativa()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.Fila.Aceitar = false;

            for (int tentativa = 0; tentativa < LimitePorHora; tentativa++)
            {
                await ambiente.SolicitarEProcessarAsync(PerfilUsuario.CLIENTE, CpfCliente);
            }

            Assert.Equal(LimitePorHora, ambiente.Redefinicoes.Redefinicoes.Count);
            Assert.Empty(ambiente.Fila.Mensagens);

            ReturnRecuperacaoSolicitadaVO quarta = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            await ambiente.ProcessarPendentesAsync();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, quarta.Mensagem);
            Assert.Equal(LimitePorHora, ambiente.Redefinicoes.Redefinicoes.Count);
        }

        [Fact]
        public async Task FuncionarioElegivel_TambemRecuperaERedefineASenhaDaPropriaConta()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Funcionario funcionario = ambiente.AdicionarFuncionario(CpfFuncionario, EmailFuncionario, ambiente.Hasher.Hash(SenhaAntiga), true);

            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.FUNCIONARIO, CpfFuncionario));
            await ambiente.ProcessarPendentesAsync();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            EmailMensagem mensagem = Assert.Single(ambiente.Fila.Mensagens);
            Assert.Equal(EmailFuncionario, mensagem.DestinatarioEmail);
            RedefinicaoSenha gravada = Assert.Single(ambiente.Redefinicoes.Redefinicoes);
            Assert.Equal(funcionario.IdFuncionario, gravada.IdFuncionario);
            Assert.Null(gravada.IdCliente);

            await ambiente.Servico.RedefinirAsync(Redefinicao(ExtrairToken(mensagem.CorpoTexto), SenhaNova));

            Assert.Equal(ambiente.Hasher.Hash(SenhaNova), funcionario.SenhaHash);
            Assert.Equal(PerfilUsuario.FUNCIONARIO, ambiente.Sessao.UltimoPerfil);
            Assert.Equal(funcionario.IdFuncionario, ambiente.Sessao.UltimoIdUsuario);
        }

        /// <summary>Falha de banco no processamento: o erro sai para quem chama e o log traz so a mensagem generica, sem CPF.</summary>
        [Fact]
        public async Task FalhaDeBanco_RegistraLogSemDadoPessoalERepassaAExcecao()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.Autenticacao.ExcecaoAoBuscar = new InvalidOperationException(MensagemFalhaSimulada);

            await Assert.ThrowsAsync<InvalidOperationException>(() => ProcessarAsync(ambiente, PerfilUsuario.CLIENTE, CpfCliente));

            Assert.Contains(ambiente.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroSolicitarRecuperacaoSenha));
            Assert.DoesNotContain(ambiente.Log.Entries, entrada => entrada.Contains(CpfCliente, StringComparison.Ordinal));
        }

        [Fact]
        public async Task NenhumLog_ContemCpfEmailTokenHashOuLink_NosCaminhosDeSucessoEDeErro()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente_Mascarado));
            await ambiente.ProcessarPendentesAsync();
            string token = ExtrairToken(ambiente.Fila.Mensagens[0].CorpoTexto);
            string hashToken = HashSha256Hex(token);
            string link = ambiente.Fila.Mensagens[0].CorpoTexto;

            await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfOutro));
            await ambiente.ProcessarPendentesAsync();
            await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(TokenInventado, SenhaNova)));
            await ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova));

            ambiente.Autenticacao.ExcecaoAoBuscar = new InvalidOperationException(MensagemFalhaSimulada);
            await Assert.ThrowsAsync<InvalidOperationException>(() => ProcessarAsync(ambiente, PerfilUsuario.CLIENTE, CpfCliente));
            ambiente.Autenticacao.ExcecaoAoBuscar = null;

            string tokenNovo = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            ambiente.Redefinicoes.ExcecaoAoGravar = new InvalidOperationException(MensagemFalhaGravacao);
            await Assert.ThrowsAsync<InvalidOperationException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(tokenNovo, SenhaNova)));
            ambiente.Redefinicoes.ExcecaoAoGravar = null;

            Assert.NotEmpty(ambiente.Log.Entries);

            foreach (string entrada in ambiente.Log.Entries)
            {
                Assert.DoesNotContain(CpfCliente, entrada);
                Assert.DoesNotContain(CpfOutro, entrada);
                Assert.DoesNotContain(EmailCliente, entrada);
                Assert.DoesNotContain(token, entrada);
                Assert.DoesNotContain(tokenNovo, entrada);
                Assert.DoesNotContain(hashToken, entrada);
                Assert.DoesNotContain(link, entrada);
                Assert.DoesNotContain(SenhaNova, entrada);
            }
        }
    }
}
