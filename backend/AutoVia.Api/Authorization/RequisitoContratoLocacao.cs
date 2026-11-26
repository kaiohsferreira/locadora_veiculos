using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica ContratoLocacao (contrato-locacao v5): o funcionario precisa ter, na locadora do contexto, o cargo
    /// ATENDENTE ou ADMINISTRADOR (lido do banco na validacao do token). Quem avalia e AutorizacaoContratoLocacaoHandler.
    /// </summary>
    public class RequisitoContratoLocacao : IAuthorizationRequirement
    {
    }
}
