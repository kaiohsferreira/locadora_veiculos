using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Frota
{
    /// <summary>
    /// Validador FluentValidation da troca manual de status (PATCH /api/veiculos/{id}/status). Obrigatorio (nulo, vazio ou so
    /// espacos) e, depois do Trim e em comparacao ordinal, igual a um nome de StatusVeiculo. Numero, minusculas e caractere
    /// nulo sao recusados. As mensagens vem de ConstantsMessage.
    /// </summary>
    public class AlterarStatusVeiculoVOValidator : AbstractValidator<AlterarStatusVeiculoVO>
    {
        public AlterarStatusVeiculoVOValidator()
        {
            RuleFor(x => x.Status)
                .Must(status => !string.IsNullOrWhiteSpace(status))
                .WithMessage(ConstantsMessage.ErroVeiculoStatusObrigatorio);

            RuleFor(x => x.Status)
                .Must(status => NomesDeStatus().Contains(status!.Trim(), StringComparer.Ordinal))
                .When(x => !string.IsNullOrWhiteSpace(x.Status))
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, string.Join(", ", NomesDeStatus())));
        }

        /// <summary>Nomes dos seis valores de StatusVeiculo, na ordem da declaracao.</summary>
        private static string[] NomesDeStatus()
        {
            return Enum.GetNames<StatusVeiculo>();
        }
    }
}
