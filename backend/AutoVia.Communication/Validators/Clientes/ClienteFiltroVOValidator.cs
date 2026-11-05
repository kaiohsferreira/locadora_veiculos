using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Clientes
{
    /// <summary>
    /// Validador FluentValidation do filtro da listagem de clientes: herda as regras de
    /// paginacao (FiltroPaginacaoVOValidator) e confere o status, quando informado.
    /// </summary>
    public class ClienteFiltroVOValidator : AbstractValidator<ClienteFiltroVO>
    {
        public ClienteFiltroVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage(ConstantsMessage.ErroClienteStatusInvalido);
        }
    }
}
