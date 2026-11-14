using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica Administrador: o funcionario precisa ter o cargo ADMINISTRADOR na empresa
    /// do contexto (lido do banco). Quem avalia e AutorizacaoAdministradorHandler.
    /// </summary>
    public class RequisitoAdministrador : IAuthorizationRequirement
    {
    }
}
