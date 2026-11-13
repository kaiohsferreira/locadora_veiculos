using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Integrations.Services.Google
{
    /// <summary>
    /// Gateway de identidade do Google. Resolve a integracao GOOGLE, le 'client_id' e 'provedor', escolhe o
    /// IGoogleIdentityProvider pelo Nome e repassa a validacao. Nao persiste nada.
    ///
    /// FAKE so e escolhido em Development + SANDBOX (a mesma trava do provedor). Assim a configuracao publica
    /// nunca mostra modoTeste fora de desenvolvimento.
    ///
    /// Log: so o nome do provedor e mensagens genericas. Nunca credencial, sub, e-mail ou client_id inteiro.
    /// </summary>
    public class GoogleIdentityGateway : IGoogleIdentityGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IGoogleIdentityProvider> _providers;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogService _logService;

        public GoogleIdentityGateway(IIntegrationResolver integrationResolver, IEnumerable<IGoogleIdentityProvider> providers, IHostEnvironment hostEnvironment, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _hostEnvironment = hostEnvironment;
            _logService = logService;
        }

        /// <summary>Habilitado somente com integracao ativa, client_id real e provedor utilizavel. Falha de integracao = desabilitado.</summary>
        public async Task<GoogleConfiguracaoPublica> ObterConfiguracaoPublicaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Google, cancellationToken);
                string clientId = ObterClientId(configuracao);
                IGoogleIdentityProvider provider = SelecionarProvedor(configuracao);

                return new GoogleConfiguracaoPublica
                {
                    Habilitado = true,
                    ClientId = clientId,
                    ModoTeste = EhFake(provider)
                };
            }
            catch (IntegrationException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConfiguracaoGoogle, GetType().ToString());

                return new GoogleConfiguracaoPublica
                {
                    Habilitado = false,
                    ClientId = null,
                    ModoTeste = false
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha de configuracao.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConfiguracaoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>Valida a prova do Google. Credencial vazia ou longa e recusada antes de qualquer chamada externa.</summary>
        public async Task<GoogleIdentidade> ValidarCredencialAsync(string credencial, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(credencial) || credencial.Length > ConstantsGoogle.TamanhoMaximoCredencial)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroGoogleCredencialInvalida);
                }

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Google, cancellationToken);
                string clientId = ObterClientId(configuracao);
                IGoogleIdentityProvider provider = SelecionarProvedor(configuracao);

                GoogleIdentidade identidade = await provider.ValidarAsync(configuracao, clientId, credencial, cancellationToken);

                _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleIdentidadeValidada, provider.Nome), GetType().ToString());

                return identidade;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha do Google.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGoogleIndisponivel, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Google, ConstantsMessage.ErroGoogleIndisponivel, ex);
            }
        }

        // ------------------------------------------------------------------
        // Configuracao da integracao
        // ------------------------------------------------------------------

        /// <summary>Client ID real da integracao. Ausente ou ainda DEFINIR_NO_BANCO = IntegrationException (nunca "aceito").</summary>
        private static string ObterClientId(ConfiguracaoIntegracao configuracao)
        {
            string? valor = configuracao.GetOptional(ConstantsGoogle.ChaveClientId)?.Trim();

            if (string.IsNullOrWhiteSpace(valor)
                || string.Equals(valor, ConstantsValues.MarcadorValorNaoDefinido, StringComparison.OrdinalIgnoreCase))
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsGoogle.ChaveClientId, ConstantsMessage.ErroGoogleIndisponivel);
            }

            return valor;
        }

        /// <summary>Escolhe o provedor pela chave 'provedor'. FAKE exige Development + SANDBOX.</summary>
        private IGoogleIdentityProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string? nomeProvedor = configuracao.GetOptional(ConstantsGoogle.ChaveProvedor)?.Trim();

            IGoogleIdentityProvider? provider = null;
            if (!string.IsNullOrWhiteSpace(nomeProvedor))
            {
                provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));
            }

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsGoogle.ChaveProvedor, ConstantsMessage.ErroGoogleProvedorNaoSuportado);
            }

            if (EhFake(provider) && !FakePermitido(configuracao))
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsGoogle.ChaveProvedor, ConstantsMessage.ErroGoogleFakeForaDoDesenvolvimento);
            }

            return provider;
        }

        private bool FakePermitido(ConfiguracaoIntegracao configuracao)
        {
            return _hostEnvironment.IsDevelopment() && configuracao.Ambiente == AmbienteIntegracao.SANDBOX;
        }

        private static bool EhFake(IGoogleIdentityProvider provider)
        {
            return string.Equals(provider.Nome, ConstantsGoogle.ProvedorFake, StringComparison.OrdinalIgnoreCase);
        }
    }
}
