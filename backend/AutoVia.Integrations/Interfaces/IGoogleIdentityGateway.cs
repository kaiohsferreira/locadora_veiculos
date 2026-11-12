using AutoVia.Integrations.Communication.Google;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway de identidade do Google (BE-5, N3). Resolve a integracao GOOGLE, le 'provedor' e 'client_id',
    /// escolhe o IGoogleIdentityProvider e devolve a identidade provada. Nao persiste nada.
    /// </summary>
    public interface IGoogleIdentityGateway
    {
        /// <summary>
        /// Configuracao publica do login com Google. Qualquer falha de integracao vira habilitado = false (com log).
        /// </summary>
        public Task<GoogleConfiguracaoPublica> ObterConfiguracaoPublicaAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida a prova do Google e devolve a identidade. Prova invalida lanca UnauthorizedException (401);
        /// integracao, provedor ou chaves publicas com falha lancam IntegrationException (502).
        /// </summary>
        public Task<GoogleIdentidade> ValidarCredencialAsync(string credencial, CancellationToken cancellationToken = default);
    }
}
