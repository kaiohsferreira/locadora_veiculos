using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Utils
{
    /// <summary>
    /// Modelo de validador FluentValidation do projeto. Cada VO de entrada dos
    /// modulos ganha um validador como este, em Validators/{Modulo}/, com as
    /// mensagens vindas de ConstantsMessage*.
    ///
    /// O FluentValidationActionFilter (AutoVia.Api/Filters) executa o validador
    /// antes da action e devolve 400 com ProblemDetails se houver erro.
    /// </summary>
    public class FiltroPaginacaoVOValidator : AbstractValidator<FiltroPaginacaoVO>
    {
        public FiltroPaginacaoVOValidator()
        {
            RuleFor(x => x.Pagina)
                .GreaterThanOrEqualTo(1)
                .WithMessage(ConstantsMessage.ErroPaginaInvalida);

            RuleFor(x => x.TamanhoPagina)
                .InclusiveBetween(1, ConstantsValues.TamanhoPaginaMaximo)
                .WithMessage(string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo));
        }
    }
}
