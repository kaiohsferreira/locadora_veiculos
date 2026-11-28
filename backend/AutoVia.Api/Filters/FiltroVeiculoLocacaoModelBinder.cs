using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Api.Filters
{
    /// <summary>Vincula datas ISO da query pública e distingue um campo ausente de um campo vazio.</summary>
    public sealed class FiltroVeiculoLocacaoModelBinder : IModelBinder
    {
        private const string FormatoDataIso = "yyyy-MM-dd";

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            FiltroVeiculoLocacaoVO filtro = new FiltroVeiculoLocacaoVO
            {
                DataRetirada = LerData(bindingContext, nameof(FiltroVeiculoLocacaoVO.DataRetirada)),
                DataDevolucao = LerData(bindingContext, nameof(FiltroVeiculoLocacaoVO.DataDevolucao))
            };

            bindingContext.Result = ModelBindingResult.Success(filtro);
            return Task.CompletedTask;
        }

        private static DateOnly? LerData(ModelBindingContext bindingContext, string propriedade)
        {
            string campo = JsonNamingPolicy.CamelCase.ConvertName(propriedade);
            if (!bindingContext.HttpContext.Request.Query.TryGetValue(campo, out StringValues valores))
            {
                return null;
            }

            if (valores.Count == 1 && DateOnly.TryParseExact(valores[0], FormatoDataIso,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data))
            {
                return data;
            }

            bindingContext.ModelState.TryAddModelError(campo, ConstantsMessage.ErroRequisicaoInvalida);
            return null;
        }
    }
}
