using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoVia.Api.Filters
{
    /// <summary>
    /// Executa o validador FluentValidation de cada argumento da action (se
    /// houver um registrado para o tipo) e devolve 400 com ValidationProblemDetails
    /// antes de chegar ao servico. Assim o controller nao precisa validar nada.
    /// </summary>
    public class FluentValidationActionFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public FluentValidationActionFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (KeyValuePair<string, object?> argument in context.ActionArguments)
            {
                if (argument.Value == null)
                    continue;

                Type validatorType = typeof(IValidator<>).MakeGenericType(argument.Value.GetType());
                IValidator? validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator == null)
                    continue;

                ValidationContext<object> validationContext = new ValidationContext<object>(argument.Value);
                ValidationResult result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (result.IsValid)
                    continue;

                Dictionary<string, string[]> errors = result.Errors
                    .GroupBy(x => ToCamelCase(x.PropertyName))
                    .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).Distinct().ToArray());

                ValidationProblemDetails problem = new ValidationProblemDetails(errors)
                {
                    Title = ConstantsMessage.TituloErroValidacao,
                    Detail = result.Errors[0].ErrorMessage,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                BadRequestObjectResult badRequest = new BadRequestObjectResult(problem);
                badRequest.ContentTypes.Add("application/problem+json");
                context.Result = badRequest;
                return;
            }

            await next();
        }

        private static string ToCamelCase(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
                return propertyName;

            return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }
    }
}
