using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica GestorFrota: o funcionario precisa ter, na locadora do contexto, o cargo GESTOR_FROTA ou
    /// ADMINISTRADOR (lido do banco na validacao do token). Quem avalia e AutorizacaoGestorFrotaHandler.
    /// </summary>
    public class RequisitoGestorFrota : IAuthorizationRequirement
    {
    }
}
