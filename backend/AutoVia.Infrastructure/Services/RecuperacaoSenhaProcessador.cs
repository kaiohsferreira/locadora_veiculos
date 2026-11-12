using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Interfaces;
using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Processamento do pedido de recuperacao em segundo plano (N1, RF-49), fora da requisicao. Busca a conta ativa
    /// pelo CPF e perfil; so segue se for elegivel (ativa, funcionario com Ativo, senha_hash e e-mail). Respeita o
    /// limite por hora (com desativados), grava so o SHA-256 do token, invalida os links anteriores num unico
    /// SaveChanges e enfileira o e-mail. Nunca registra CPF, e-mail, token, hash, link ou senha.
    /// </summary>
    public class RecuperacaoSenhaProcessador : IRecuperacaoSenhaProcessador
    {
        private readonly IAutenticacaoRepository _autenticacaoRepository;
        private readonly IRedefinicaoSenhaRepository _redefinicaoSenhaRepository;
        private readonly IEmailFila _emailFila;
        private readonly RecuperacaoSenhaOptions _options;
        private readonly ILogService _logService;

        public RecuperacaoSenhaProcessador(
            IAutenticacaoRepository autenticacaoRepository,
            IRedefinicaoSenhaRepository redefinicaoSenhaRepository,
            IEmailFila emailFila,
            RecuperacaoSenhaOptions options,
            ILogService logService)
        {
            _autenticacaoRepository = autenticacaoRepository;
            _redefinicaoSenhaRepository = redefinicaoSenhaRepository;
            _emailFila = emailFila;
            _options = options;
            _logService = logService;
        }

        /// <summary>
        /// Processa um pedido: localiza a conta elegivel e, se houver vaga no limite, grava o link e enfileira o e-mail.
        /// Conta nao elegivel ou no limite nao grava nada. Falha de banco vai para o log e sai para quem chama.
        /// </summary>
        public async Task ProcessarAsync(PedidoRecuperacaoSenha pedido, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                TitularElegivel? titular = await LocalizarTitularElegivelAsync(pedido.Perfil, pedido.Cpf);

                if (titular != null)
                {
                    await GerarEnviarLinkAsync(titular);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Desligamento da aplicacao: nao e falha do pedido.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSolicitarRecuperacaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca a conta pelo CPF no perfil informado. Nulo quando a conta nao e elegivel: inexistente, desativada, funcionario inativo, sem senha_hash ou sem e-mail.</summary>
        private async Task<TitularElegivel?> LocalizarTitularElegivelAsync(PerfilUsuario? perfil, string cpf)
        {
            if (perfil == PerfilUsuario.CLIENTE)
            {
                Cliente? cliente = await _autenticacaoRepository.FindClienteByCpfAsync(cpf);

                if (cliente == null || string.IsNullOrWhiteSpace(cliente.SenhaHash) || string.IsNullOrWhiteSpace(cliente.Email))
                {
                    return null;
                }

                return TitularElegivel.DeCliente(cliente);
            }

            if (perfil == PerfilUsuario.FUNCIONARIO)
            {
                Funcionario? funcionario = await _autenticacaoRepository.FindFuncionarioByCpfAsync(cpf);

                if (funcionario == null || !funcionario.Ativo || string.IsNullOrWhiteSpace(funcionario.SenhaHash))
                {
                    return null;
                }

                string? emailFuncionario = funcionario.Email;

                if (string.IsNullOrWhiteSpace(emailFuncionario))
                {
                    return null;
                }

                return TitularElegivel.DeFuncionario(funcionario, emailFuncionario);
            }

            return null;
        }

        /// <summary>Confere o limite por conta, grava o novo link (invalidando os anteriores) e enfileira o e-mail. Fila cheia so vai para o log.</summary>
        private async Task GerarEnviarLinkAsync(TitularElegivel titular)
        {
            DateTime inicioDaJanela = DateTime.Now.AddMinutes(-ConstantsValues.JanelaLimiteMinutos);

            int solicitacoes = await _redefinicaoSenhaRepository.CountSolicitacoesDesdeWithDisabledsAsync(titular.IdCliente, titular.IdFuncionario, inicioDaJanela);

            if (solicitacoes >= _options.LimitePorHora)
            {
                return;
            }

            string token = GerarToken();

            RedefinicaoSenha nova = new RedefinicaoSenha
            {
                IdCliente = titular.IdCliente,
                IdFuncionario = titular.IdFuncionario,
                TokenHash = RecuperacaoSenhaService.CalcularHashToken(token),
                ExpiraEm = DateTime.Now.AddMinutes(_options.ValidadeMinutos)
            };

            await _redefinicaoSenhaRepository.InsertInvalidandoAnterioresAsync(nova);

            EmailMensagem mensagem = MontarMensagem(titular, token);

            bool enfileirada = _emailFila.Enfileirar(mensagem);

            if (!enfileirada)
            {
                _logService.Write(ConstantsMessage.ErroEnviarEmailRecuperacao, ConstantsMessage.ErroEnviarEmailRecuperacao, GetType().ToString());
            }
        }

        /// <summary>Monta o e-mail pronto (assunto, texto e HTML). Nome e link saem escapados no HTML; sem senha.</summary>
        private EmailMensagem MontarMensagem(TitularElegivel titular, string token)
        {
            string link = MontarLink(token);
            string nomeHtml = WebUtility.HtmlEncode(titular.Nome);
            string linkHtml = WebUtility.HtmlEncode(link);

            EmailMensagem mensagem = new EmailMensagem
            {
                DestinatarioEmail = titular.Email,
                DestinatarioNome = titular.Nome,
                Assunto = ConstantsMessage.EmailRecuperacaoAssunto,
                CorpoTexto = string.Format(ConstantsMessage.EmailRecuperacaoCorpoTexto, titular.Nome, _options.ValidadeMinutos, link),
                CorpoHtml = string.Format(ConstantsMessage.EmailRecuperacaoCorpoHtml, nomeHtml, _options.ValidadeMinutos, linkHtml),
                Etiqueta = ConstantsValues.EtiquetaEmailRecuperacao
            };

            return mensagem;
        }

        /// <summary>Link de redefinicao: URL publica do front, caminho da tela e o token no fragmento (#token=).</summary>
        private string MontarLink(string token)
        {
            string baseUrl = _options.UrlPublicaFront.TrimEnd(ConstantsValues.CaractereBarra);

            string link = string.Format(
                ConstantsValues.ModeloLinkRedefinicao,
                baseUrl,
                ConstantsValues.CaminhoRedefinirSenhaFront,
                ConstantsValues.ParametroTokenFront,
                token);

            return link;
        }

        /// <summary>Token aleatorio de 32 bytes (RandomNumberGenerator) em base64url, sem preenchimento.</summary>
        private static string GerarToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(ConstantsValues.TamanhoTokenBytes);

            string token = Base64Url.EncodeToString(bytes);

            return token;
        }

        /// <summary>Titular elegivel ja identificado: id do cliente ou do funcionario (conforme o perfil), nome e e-mail da mensagem.</summary>
        private sealed class TitularElegivel
        {
            private TitularElegivel(int? idCliente, int? idFuncionario, string nome, string email)
            {
                IdCliente = idCliente;
                IdFuncionario = idFuncionario;
                Nome = nome;
                Email = email;
            }

            public int? IdCliente { get; }

            public int? IdFuncionario { get; }

            public string Nome { get; }

            public string Email { get; }

            public static TitularElegivel DeCliente(Cliente cliente)
            {
                return new TitularElegivel(cliente.IdCliente, null, cliente.Nome, cliente.Email);
            }

            public static TitularElegivel DeFuncionario(Funcionario funcionario, string email)
            {
                return new TitularElegivel(null, funcionario.IdFuncionario, funcionario.Nome, email);
            }
        }
    }
}
