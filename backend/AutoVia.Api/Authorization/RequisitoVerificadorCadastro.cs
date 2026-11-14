using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica VerificadorCadastro: o funcionario precisa ter, na locadora do contexto, o cargo
    /// ATENDENTE ou ADMINISTRADOR (lido do banco na validacao do token). Quem avalia e AutorizacaoVerificadorCadastroHandler.
    /// </summary>
    public class RequisitoVerificadorCadastro : IAuthorizationRequirement
    {
    }
}
