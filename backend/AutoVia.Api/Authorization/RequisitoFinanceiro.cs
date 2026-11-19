using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica Financeiro: o funcionario precisa ter, na locadora do contexto, o cargo FINANCEIRO ou
    /// ADMINISTRADOR (IContextoEmpresa.PodeVerValores, o ponto unico do corte de valores). Quem avalia e AutorizacaoFinanceiroHandler.
    /// </summary>
    public class RequisitoFinanceiro : IAuthorizationRequirement
    {
    }
}
