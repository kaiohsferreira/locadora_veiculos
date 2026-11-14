using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Filters
{
    /// <summary>
    /// Liga e desliga o cadastro publico de locadora. E um IAsyncResourceFilter: roda ANTES do model binding e
    /// da validacao FluentValidation. Com Locadoras:CadastroPublicoHabilitado desligada, a rota responde 404
    /// (NotFoundException, ProblemDetails) sem olhar o corpo, assim a chave desligada nao revela nada.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CadastroLocadoraHabilitadoAttribute : Attribute, IAsyncResourceFilter
    {
        /// <summary>Lanca NotFoundException quando o cadastro publico esta desligado; senao segue a requisicao.</summary>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            LocadorasOptions opcoes = context.HttpContext.RequestServices.GetRequiredService<LocadorasOptions>();

            if (!opcoes.CadastroPublicoHabilitado)
            {
                throw new NotFoundException(ConstantsMessage.ErroLocadoraCadastroIndisponivel);
            }

            await next();
        }
    }
}
