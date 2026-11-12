using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Email
{
    /// <summary>
    /// Gateway de e-mail transacional. Valida a mensagem (BusinessException, 409), resolve a integracao
    /// EMAIL, le 'provedor', 'remetente_email' e 'remetente_nome', escolhe o IEmailProvider pelo Nome e
    /// repassa a entrega. Falha do provedor vira IntegrationException (502) com a excecao original.
    ///
    /// Log: so provedor, etiqueta e um identificador sem dado pessoal. Nunca destinatario, assunto ou corpo.
    /// </summary>
    public class EmailGateway : IEmailGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IEmailProvider> _providers;
        private readonly ILogService _logService;

        public EmailGateway(IIntegrationResolver integrationResolver, IEnumerable<IEmailProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        /// <summary>Valida e envia a mensagem pelo provedor configurado na integracao EMAIL.</summary>
        public async Task EnviarAsync(EmailMensagem mensagem, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mensagem);

            try
            {
                ValidarMensagem(mensagem);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Email, cancellationToken);
                IEmailProvider provider = SelecionarProvedor(configuracao);
                string remetenteEmail = configuracao.GetRequired(ConstantsEmail.ChaveRemetenteEmail);
                string remetenteNome = configuracao.GetRequired(ConstantsEmail.ChaveRemetenteNome);
                string identificador = Guid.NewGuid().ToString("N");

                await provider.EnviarAsync(configuracao, mensagem, remetenteEmail, remetenteNome, cancellationToken);

                string etiqueta = mensagem.Etiqueta ?? ConstantsEmail.MarcadorAusente;
                _logService.WriteInfo(string.Format(ConstantsMessage.LogEmailEnviado, provider.Nome, etiqueta, identificador), GetType().ToString());
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha do provedor.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailEnviar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Email, ConstantsMessage.ErroEmailEnviar, ex);
            }
        }

        // ------------------------------------------------------------------
        // Validacao da mensagem (antes de qualquer chamada externa)
        // ------------------------------------------------------------------

        /// <summary>Confere os campos da mensagem. Qualquer falha vira BusinessException com a mesma mensagem.</summary>
        private static void ValidarMensagem(EmailMensagem mensagem)
        {
            if (!EmailValido(mensagem.DestinatarioEmail))
            {
                throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
            }

            if (mensagem.DestinatarioNome != null
                && (mensagem.DestinatarioNome.Length > ConstantsEmail.DestinatarioNomeTamanhoMaximo || ContemQuebraDeLinha(mensagem.DestinatarioNome)))
            {
                throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
            }

            if (string.IsNullOrWhiteSpace(mensagem.Assunto)
                || mensagem.Assunto.Length > ConstantsEmail.AssuntoTamanhoMaximo
                || ContemQuebraDeLinha(mensagem.Assunto))
            {
                throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
            }

            if (string.IsNullOrWhiteSpace(mensagem.CorpoTexto))
            {
                throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
            }
        }

        private static bool EmailValido(string? email)
        {
            // Limite de tamanho antes do regex: entrada enorme nao chega ao padrao.
            return !string.IsNullOrWhiteSpace(email)
                && email.Length <= ConstantsEmail.DestinatarioEmailTamanhoMaximo
                && Regex.IsMatch(email, ConstantsEmail.PadraoEmail);
        }

        private static bool ContemQuebraDeLinha(string texto)
        {
            return texto.IndexOfAny(ConstantsEmail.CaracteresQuebraDeLinha) >= 0;
        }

        // ------------------------------------------------------------------
        // Escolha do provedor pela integracao EMAIL
        // ------------------------------------------------------------------

        /// <summary>Escolhe o provedor pela chave 'provedor'. FAKE so funciona em SANDBOX (a trava de Development fica no proprio provedor).</summary>
        private IEmailProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string nomeProvedor = configuracao.GetRequired(ConstantsEmail.ChaveProvedor).Trim();

            IEmailProvider? provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsEmail.ChaveProvedor, ConstantsMessage.ErroEmailProvedorNaoSuportado);
            }

            bool isFake = string.Equals(provider.Nome, ConstantsEmail.ProvedorFake, StringComparison.OrdinalIgnoreCase);

            if (isFake && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsEmail.ChaveProvedor, ConstantsMessage.ErroEmailFakeForaDoDesenvolvimento);
            }

            return provider;
        }
    }
}
