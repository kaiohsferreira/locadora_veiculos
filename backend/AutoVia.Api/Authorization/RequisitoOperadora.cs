using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica Operadora: o funcionario precisa pertencer a empresa operadora da plataforma
    /// e ter o cargo ADMINISTRADOR. Quem avalia e AutorizacaoOperadoraHandler.
    /// </summary>
    public class RequisitoOperadora : IAuthorizationRequirement
    {
    }
}
