using Microsoft.AspNetCore.Authorization;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Requisito da politica AberturaLocacao (decisao 4 de 24/11/2026): com a caucao exigida, o funcionario precisa ter, na
    /// locadora do contexto, o cargo ATENDENTE ou ADMINISTRADOR (lido do banco na validacao do token); sem caucao exigida, qualquer
    /// funcionario. Quem avalia e AutorizacaoAberturaLocacaoHandler.
    /// </summary>
    public class RequisitoAberturaLocacao : IAuthorizationRequirement
    {
    }
}
