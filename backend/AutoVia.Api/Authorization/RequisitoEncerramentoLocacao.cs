using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica EncerramentoLocacao (D6): o funcionario precisa ter, na locadora do contexto, o cargo ATENDENTE ou
    /// ADMINISTRADOR (lido do banco na validacao do token). Quem avalia e AutorizacaoEncerramentoLocacaoHandler.
    /// </summary>
    public class RequisitoEncerramentoLocacao : IAuthorizationRequirement
    {
    }
}
