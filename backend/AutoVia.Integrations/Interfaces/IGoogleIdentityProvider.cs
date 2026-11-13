using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Google;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor de identidade do Google: GOOGLE (prova real, JWKS) ou FAKE (teste, so Development + SANDBOX).
    /// Escolhido pelo IGoogleIdentityGateway pela chave 'provedor' da integracao GOOGLE.
    /// </summary>
    public interface IGoogleIdentityProvider
    {
        /// <summary>Nome do provedor (valor da chave 'provedor', ex.: GOOGLE ou FAKE).</summary>
        string Nome { get; }

        /// <summary>
        /// Valida a credencial e devolve a identidade. Recusa com UnauthorizedException; falha de
        /// chaves publicas ou de configuracao com IntegrationException.
        /// </summary>
        public Task<GoogleIdentidade> ValidarAsync(ConfiguracaoIntegracao configuracao, string clientId, string credencial, CancellationToken cancellationToken = default);
    }
}
