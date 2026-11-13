using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Tests.Fakes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Ambiente compartilhado dos testes da recuperacao de senha: o servico (requisicao), o processador (segundo plano)
    /// e o servico de redefinicao, sobre repositorios, fila de e-mail, fila de pedidos, sessao e log em memoria.
    /// Nao tem banco nem rede. ProcessarPendentesAsync roda o processador sobre os pedidos aceitos pela fila, de
    /// forma sincrona para o teste (sem esperas por tempo).
    /// </summary>
    internal sealed class RecuperacaoAmbiente
    {
        public const string UrlFront = "http://localhost:5173";
        public const int ValidadeMinutos = 30;
        public const int LimitePorHora = 3;
        public const int TempoMinimoZero = 0;
        public const int TempoMinimoDeTeste = 200;
        public const int MinutosDentroDaJanela = 59;
        public const int MinutosForaDaJanela = 61;
        public const string CpfCliente = "52998224725";
        public const string CpfCliente_Mascarado = "529.982.247-25";
        public const string CpfOutro = "11144477735";
        public const string CpfFuncionario = "39053344705";
        public const string CpfFuncionarioSemEmail = "12345678909";
        public const string EmailCliente = "maria.recuperacao@exemplo.com";
        public const string EmailFuncionario = "ana.recuperacao@exemplo.com";
        public const string NomeCliente = "Maria Recuperacao";
        public const string NomeFuncionario = "Ana Recuperacao";
        public const string SenhaAntiga = "SenhaAntiga1";
        public const string SenhaNova = "SenhaNova2026";
        public const string TokenInventado = "token-inventado-que-nao-existe";
        public const string MensagemFalhaSimulada = "Falha simulada do banco na consulta.";
        public const string MensagemFalhaGravacao = "Falha simulada do banco na gravacao.";
        public const string PadraoToken = "#token=([A-Za-z0-9_-]+)";

        public RecuperacaoAmbiente(int tempoMinimoMs = TempoMinimoZero)
        {
            Autenticacao = new FakeAutenticacaoRepository();
            Redefinicoes = new FakeRedefinicaoSenhaRepository(Autenticacao);
            Hasher = new FakeSenhaHasher();
            Fila = new FakeEmailFila();
            FilaPedidos = new FakeRecuperacaoSenhaFila();
            Sessao = new SessaoEspiao(Redefinicoes);
            Log = new FakeLogService();
            Opcoes = new RecuperacaoSenhaOptions
            {
                UrlPublicaFront = UrlFront,
                ValidadeMinutos = ValidadeMinutos,
                LimitePorHora = LimitePorHora,
                TempoMinimoRespostaMs = tempoMinimoMs
            };
            Servico = new RecuperacaoSenhaService(Redefinicoes, Hasher, Sessao, FilaPedidos, Opcoes, Log);
            Processador = new RecuperacaoSenhaProcessador(Autenticacao, Redefinicoes, Fila, Opcoes, Log);
        }

        public FakeAutenticacaoRepository Autenticacao { get; }

        public FakeRedefinicaoSenhaRepository Redefinicoes { get; }

        public FakeSenhaHasher Hasher { get; }

        /// <summary>Fila de e-mail (IEmailFila): recebe o que o processador envia.</summary>
        public FakeEmailFila Fila { get; }

        /// <summary>Fila de pedidos (IRecuperacaoSenhaFila): recebe o que o servico da requisicao enfileira.</summary>
        public FakeRecuperacaoSenhaFila FilaPedidos { get; }

        public SessaoEspiao Sessao { get; }

        public FakeLogService Log { get; }

        public RecuperacaoSenhaOptions Opcoes { get; }

        public RecuperacaoSenhaService Servico { get; }

        public RecuperacaoSenhaProcessador Processador { get; }

        /// <summary>Processa, em ordem, todos os pedidos aceitos pela fila e ainda pendentes. Cada um passa pelo processador real.</summary>
        public async Task ProcessarPendentesAsync()
        {
            List<PedidoRecuperacaoSenha> pendentes = new List<PedidoRecuperacaoSenha>(FilaPedidos.Pendentes);

            FilaPedidos.Pendentes.Clear();

            foreach (PedidoRecuperacaoSenha pedido in pendentes)
            {
                await Processador.ProcessarAsync(pedido, CancellationToken.None);
            }
        }

        /// <summary>Solicita pela requisicao (servico) e processa em segundo plano (processador) para o perfil e CPF informados.</summary>
        public async Task SolicitarEProcessarAsync(PerfilUsuario perfil, string cpf)
        {
            await Servico.SolicitarAsync(Solicitacao(perfil, cpf));

            await ProcessarPendentesAsync();
        }

        /// <summary>Solicita e processa, e devolve o token que foi enviado no e-mail (o ultimo da fila de e-mail).</summary>
        public async Task<string> SolicitarEObterTokenAsync(PerfilUsuario perfil, string cpf)
        {
            await SolicitarEProcessarAsync(perfil, cpf);

            EmailMensagem mensagem = Fila.Mensagens[Fila.Mensagens.Count - 1];

            return ExtrairToken(mensagem.CorpoTexto);
        }

        /// <summary>Semeia um cliente ativo com a senha_hash informada (nula = sem acesso).</summary>
        public Cliente AdicionarCliente(string cpf, string? email, string? senhaHash)
        {
            Cliente cliente = new Cliente
            {
                Nome = NomeCliente,
                Cpf = cpf,
                Email = email ?? string.Empty,
                SenhaHash = senhaHash,
                StatusCadastro = StatusCadastroCliente.APROVADO
            };

            Autenticacao.AdicionarCliente(cliente);

            return cliente;
        }

        /// <summary>Semeia um funcionario com o e-mail informado (nulo = sem e-mail) e o status Ativo.</summary>
        public Funcionario AdicionarFuncionario(string cpf, string? email, string? senhaHash, bool ativo)
        {
            Funcionario funcionario = new Funcionario
            {
                Nome = NomeFuncionario,
                Cpf = cpf,
                Matricula = "F-900",
                Cargo = CargoFuncionario.ATENDENTE,
                Email = email,
                Ativo = ativo,
                SenhaHash = senhaHash
            };

            Autenticacao.AdicionarFuncionario(funcionario);

            return funcionario;
        }

        /// <summary>Solicitacao publica do perfil e do CPF informados (com ou sem mascara).</summary>
        public static SolicitarRecuperacaoSenhaVO Solicitacao(PerfilUsuario perfil, string cpf)
        {
            return new SolicitarRecuperacaoSenhaVO { Perfil = perfil, Cpf = cpf };
        }

        /// <summary>Redefinicao com a mesma senha nos dois campos (confirmacao igual).</summary>
        public static RedefinirSenhaVO Redefinicao(string token, string senha)
        {
            return new RedefinirSenhaVO { Token = token, NovaSenha = senha, ConfirmacaoSenha = senha };
        }

        /// <summary>Token enviado no link do e-mail (parte depois de #token=).</summary>
        public static string ExtrairToken(string texto)
        {
            Match correspondencia = Regex.Match(texto, PadraoToken);

            Assert.True(correspondencia.Success, "O link do e-mail deve conter #token=.");

            return correspondencia.Groups[1].Value;
        }

        /// <summary>Hash SHA-256 em hexadecimal minusculo, calculado de forma independente do servico.</summary>
        public static string HashSha256Hex(string texto)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(texto);
            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    /// <summary>ISessaoService que registra as invalidacoes e quantas trocas ja tinham sido concluidas no momento de cada uma.</summary>
    internal sealed class SessaoEspiao : ISessaoService
    {
        private readonly FakeRedefinicaoSenhaRepository _redefinicoes;

        public SessaoEspiao(FakeRedefinicaoSenhaRepository redefinicoes)
        {
            _redefinicoes = redefinicoes;
        }

        public int Invalidacoes { get; private set; }

        public PerfilUsuario? UltimoPerfil { get; private set; }

        public int UltimoIdUsuario { get; private set; }

        public List<int> ConcluidasNoMomentoDaInvalidacao { get; } = new List<int>();

        public Task<bool> SessaoValidaAsync(PerfilUsuario perfil, int idUsuario, string? versaoSessao)
        {
            return Task.FromResult(true);
        }

        public void Invalidar(PerfilUsuario perfil, int idUsuario)
        {
            Invalidacoes++;
            UltimoPerfil = perfil;
            UltimoIdUsuario = idUsuario;
            ConcluidasNoMomentoDaInvalidacao.Add(_redefinicoes.Concluidas);
        }
    }
}
