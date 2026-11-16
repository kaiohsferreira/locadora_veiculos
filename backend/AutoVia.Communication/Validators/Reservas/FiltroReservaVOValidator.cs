using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Helpers;
using FluentValidation;
using System;
using System.Linq;

namespace AutoVia.Communication.Validators.Reservas
{
    /// <summary>Valida filtros e parametros de paginacao da consulta de reservas (situacao, status, busca).</summary>
    public class FiltroReservaVOValidator : AbstractValidator<FiltroReservaVO>
    {
        private const char CaractereNulo = '\0';

        public FiltroReservaVOValidator()
        {
            // Pagina e tamanho (inclusive o estouro de deslocamento) vem do FiltroPaginacaoVOValidator; nao repetir aqui.
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Status)
                .Must(status => status == null || Enum.GetNames<StatusReserva>().Contains(status, StringComparer.Ordinal))
                .WithMessage(string.Format(ConstantsMessage.ErroReservaStatusInvalido, string.Join(", ", Enum.GetNames<StatusReserva>())));

            RuleFor(x => x.Situacao)
                .Must(situacao => situacao == null
                    || situacao == ConstantsValues.SituacaoReservaAtivas
                    || situacao == ConstantsValues.SituacaoReservaHistorico)
                .WithMessage(string.Format(ConstantsMessage.ErroReservaSituacaoInvalida, string.Join(", ", ConstantsValues.SituacaoReservaAtivas, ConstantsValues.SituacaoReservaHistorico)));

            RuleFor(x => x.Situacao)
                .Must((filtro, situacao) => situacao == null || filtro.Status == null)
                .WithMessage(ConstantsMessage.ErroReservaSituacaoComStatus);

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaReserva)
                .WithMessage(string.Format(ConstantsMessage.ErroReservaBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaReserva));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !busca.Contains(CaractereNulo))
                .WithMessage(ConstantsMessage.ErroReservaBuscaInvalida);
        }
    }
}
