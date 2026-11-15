using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Helpers;
using FluentValidation;
using System;
using System.Linq;

namespace AutoVia.Communication.Validators.Reservas
{
    /// <summary>Valida filtros e parâmetros de paginação da consulta de reservas.</summary>
    public class FiltroReservaVOValidator : AbstractValidator<FiltroReservaVO>
    {
        public FiltroReservaVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Pagina)
                .Must((filtro, pagina) => filtro.TamanhoPagina <= 0 || (long)pagina <= ((long)int.MaxValue / filtro.TamanhoPagina) + 1L)
                .WithMessage(ConstantsMessage.ErroPaginaDeslocamentoInvalido);

            RuleFor(x => x.Status)
                .Must(status => status == null || Enum.GetNames<StatusReserva>().Contains(status, StringComparer.Ordinal))
                .WithMessage(string.Format(ConstantsMessage.ErroReservaStatusInvalido, string.Join(", ", Enum.GetNames<StatusReserva>())));
        }
    }
}
