using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Interfaces;
using System.Diagnostics;
using static AutoVia.Tests.Autenticacao.RecuperacaoAmbiente;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Servico de recuperacao de senha (N1, RF-49) com fakes, sem banco e sem rede. Cobre o que a REQUISICAO faz:
    /// normalizar o CPF, enfileirar sem consultar o banco nem enviar e-mail, responder sempre igual (fila cheia e
    /// falha incluidas), o piso de tempo e o log sem dado pessoal. Cobre tambem a redefinicao com o link, que
    /// continua neste servico. O processamento do pedido (busca, limite, link e e-mail) esta em
    /// RecuperacaoSenhaProcessadorTests.
    /// </summary>
    public class RecuperacaoSenhaServiceTests
    {
        [Fact]
        public async Task Solicitacao_EnfileiraUmPedidoComCpfNormalizadoEPerfil()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente_Mascarado));

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            PedidoRecuperacaoSenha pedido = Assert.Single(ambiente.FilaPedidos.Pendentes);
            Assert.Equal(CpfCliente, pedido.Cpf);
            Assert.Equal(PerfilUsuario.CLIENTE, pedido.Perfil);
        }

        [Fact]
        public async Task Solicitacao_NaoConsultaOBancoNemEnviaEmail_EmNenhumCaso()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.AdicionarFuncionario(CpfFuncionario, EmailFuncionario, ambiente.Hasher.Hash(SenhaAntiga), true);

            await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.FUNCIONARIO, CpfFuncionario));
            await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfOutro));

            Assert.Equal(0, ambiente.Autenticacao.TotalBuscas);
            Assert.Equal(0, ambiente.Redefinicoes.TotalChamadas);
            Assert.Equal(0, ambiente.Fila.TotalChamadas);
            Assert.Equal(3, ambiente.FilaPedidos.TotalChamadas);
        }

        /// <summary>
        /// Prova estrutural: o construtor do servico nao recebe o repositorio de autenticacao nem a fila de e-mail,
        /// entao a requisicao nao tem como consultar a conta nem enviar e-mail.
        /// </summary>
        [Fact]
        public void Construtor_NaoRecebeORepositorioDeAutenticacaoNemAFilaDeEmail()
        {
            List<Type> tipos = typeof(RecuperacaoSenhaService).GetConstructors()
                .SelectMany(construtor => construtor.GetParameters())
                .Select(parametro => parametro.ParameterType)
                .ToList();

            Assert.DoesNotContain(typeof(IAutenticacaoRepository), tipos);
            Assert.DoesNotContain(typeof(IEmailFila), tipos);
        }

        [Fact]
        public async Task CasosNaoElegiveis_DevolvemAMesmaRespostaSemConsultarOBanco()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfOutro, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            List<ReturnRecuperacaoSolicitadaVO> respostas = new List<ReturnRecuperacaoSolicitadaVO>
            {
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.FUNCIONARIO, CpfFuncionario)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.FUNCIONARIO, CpfFuncionarioSemEmail)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfOutro)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente_Mascarado)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.FUNCIONARIO, CpfCliente)),
                await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfFuncionario))
            };

            Assert.Equal(7, respostas.Count);

            foreach (ReturnRecuperacaoSolicitadaVO resposta in respostas)
            {
                Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, resposta.Mensagem);
            }

            Assert.Equal(0, ambiente.Autenticacao.TotalBuscas);
            Assert.Equal(7, ambiente.FilaPedidos.Pendentes.Count);
        }

        [Fact]
        public async Task FilaCheia_RespostaIgual_LogSemCpf()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.FilaPedidos.Aceitar = false;

            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            Assert.Empty(ambiente.FilaPedidos.Pendentes);
            Assert.Contains(ambiente.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroFilaRecuperacaoCheia));
            Assert.DoesNotContain(ambiente.Log.Entries, entrada => entrada.Contains(CpfCliente, StringComparison.Ordinal));
        }

        [Fact]
        public async Task PisoDeTempo_RespostaEsperaAteOTempoMinimo_MesmoComContaInexistente()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente(TempoMinimoDeTeste);

            Stopwatch cronometro = Stopwatch.StartNew();
            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            cronometro.Stop();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            Assert.Single(ambiente.FilaPedidos.Pendentes);
            Assert.True(cronometro.ElapsedMilliseconds >= TempoMinimoDeTeste, $"A resposta levou {cronometro.ElapsedMilliseconds} ms, abaixo do piso de {TempoMinimoDeTeste} ms.");
        }

        [Fact]
        public async Task PisoDeTempo_CaminhoElegivel_RespostaEsperaAteOTempoMinimo()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente(TempoMinimoDeTeste);
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));

            Stopwatch cronometro = Stopwatch.StartNew();
            ReturnRecuperacaoSolicitadaVO retorno = await ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente));
            cronometro.Stop();

            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, retorno.Mensagem);
            Assert.Single(ambiente.FilaPedidos.Pendentes);
            Assert.True(cronometro.ElapsedMilliseconds >= TempoMinimoDeTeste, $"A resposta levou {cronometro.ElapsedMilliseconds} ms, abaixo do piso de {TempoMinimoDeTeste} ms.");
        }

        /// <summary>Piso tambem quando o pedido sai por excecao: a excecao so chega ao chamador depois da mesma espera.</summary>
        [Fact]
        public async Task PisoDeTempo_CaminhoDeFalha_ExcecaoSoSaiDepoisDoPiso()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente(TempoMinimoDeTeste);
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.FilaPedidos.ExcecaoAoEnfileirar = new InvalidOperationException(MensagemFalhaSimulada);

            Stopwatch cronometro = Stopwatch.StartNew();
            await Assert.ThrowsAsync<InvalidOperationException>(() => ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente)));
            cronometro.Stop();

            Assert.True(cronometro.ElapsedMilliseconds >= TempoMinimoDeTeste, $"A excecao saiu em {cronometro.ElapsedMilliseconds} ms, antes do piso de {TempoMinimoDeTeste} ms.");
        }

        [Fact]
        public async Task FalhaNaFila_RegistraLogSemDadoPessoalERepassaAExcecao()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            ambiente.FilaPedidos.ExcecaoAoEnfileirar = new InvalidOperationException(MensagemFalhaSimulada);

            await Assert.ThrowsAsync<InvalidOperationException>(() => ambiente.Servico.SolicitarAsync(Solicitacao(PerfilUsuario.CLIENTE, CpfCliente)));

            Assert.Contains(ambiente.Log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroSolicitarRecuperacaoSenha));
            Assert.DoesNotContain(ambiente.Log.Entries, entrada => entrada.Contains(CpfCliente, StringComparison.Ordinal));
        }

        [Fact]
        public async Task RedefinirComLinkValido_GravaNovaSenhaDesativaOLinkEDerrubaSessaoDepoisDoCommit()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Cliente cliente = ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);

            await ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova));

            Assert.Equal(ambiente.Hasher.Hash(SenhaNova), cliente.SenhaHash);
            Assert.NotNull(ambiente.Redefinicoes.Redefinicoes[0].DisabledAt);
            Assert.Equal(1, ambiente.Redefinicoes.Concluidas);
            Assert.Equal(1, ambiente.Sessao.Invalidacoes);
            Assert.Equal(PerfilUsuario.CLIENTE, ambiente.Sessao.UltimoPerfil);
            Assert.Equal(cliente.IdCliente, ambiente.Sessao.UltimoIdUsuario);
            Assert.Equal(new List<int> { 1 }, ambiente.Sessao.ConcluidasNoMomentoDaInvalidacao);
        }

        [Fact]
        public async Task LinkExpirado_AoTrintaMinutosMaisUmSegundo_EhRecusadoSemTrocarASenha()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Cliente cliente = ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            string hashAntesDaTentativa = cliente.SenhaHash!;

            RedefinicaoSenha gravada = ambiente.Redefinicoes.Redefinicoes[0];
            gravada.ExpiraEm = DateTime.Now.AddSeconds(-1);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova)));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Equal(hashAntesDaTentativa, cliente.SenhaHash);
            Assert.Equal(0, ambiente.Sessao.Invalidacoes);
        }

        [Fact]
        public async Task ReusoDoLink_EhRecusadoComAMesmaMensagem()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            await ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, "OutraSenha99")));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Equal(1, ambiente.Redefinicoes.Concluidas);
        }

        [Fact]
        public async Task LinkInventado_EhRecusadoComAMesmaMensagem()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            await ambiente.SolicitarEProcessarAsync(PerfilUsuario.CLIENTE, CpfCliente);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(TokenInventado, SenhaNova)));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Equal(0, ambiente.Redefinicoes.Concluidas);
        }

        [Fact]
        public async Task TitularDesativadoDepoisDoPedido_EhRecusadoEnaoTrocaASenha()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Cliente cliente = ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            string hashAntes = cliente.SenhaHash!;

            cliente.Disable();

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova)));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Equal(hashAntes, cliente.SenhaHash);
            Assert.Equal(0, ambiente.Redefinicoes.Concluidas);
        }

        [Fact]
        public async Task ContaSemSenhaHashNoUso_EhRecusadaEAoNaoCriaSenhaOndeNaoHavia()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Cliente cliente = ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);

            cliente.SenhaHash = null;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova)));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Null(cliente.SenhaHash);
            Assert.Equal(0, ambiente.Redefinicoes.Concluidas);
        }

        [Fact]
        public async Task FuncionarioInativoNoUso_EhRecusado()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            Funcionario funcionario = ambiente.AdicionarFuncionario(CpfFuncionario, EmailFuncionario, ambiente.Hasher.Hash(SenhaAntiga), true);
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.FUNCIONARIO, CpfFuncionario);

            funcionario.Ativo = false;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova)));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Equal(0, ambiente.Redefinicoes.Concluidas);
        }

        /// <summary>Falha na gravacao da redefinicao: a sessao da conta NAO e derrubada, porque Invalidar so roda depois do commit.</summary>
        [Fact]
        public async Task FalhaNaGravacaoDaRedefinicao_NaoChamaInvalidar()
        {
            RecuperacaoAmbiente ambiente = new RecuperacaoAmbiente();
            ambiente.AdicionarCliente(CpfCliente, EmailCliente, ambiente.Hasher.Hash(SenhaAntiga));
            string token = await ambiente.SolicitarEObterTokenAsync(PerfilUsuario.CLIENTE, CpfCliente);
            ambiente.Redefinicoes.ExcecaoAoGravar = new InvalidOperationException(MensagemFalhaGravacao);
            int invalidacoesAntes = ambiente.Sessao.Invalidacoes;

            await Assert.ThrowsAsync<InvalidOperationException>(() => ambiente.Servico.RedefinirAsync(Redefinicao(token, SenhaNova)));

            Assert.Equal(invalidacoesAntes, ambiente.Sessao.Invalidacoes);
            Assert.Equal(0, ambiente.Redefinicoes.Concluidas);
        }
    }
}
