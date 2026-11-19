using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Frota
{
    /// <summary>
    /// Validador FluentValidation do filtro da listagem de veiculos (GET /api/veiculos).
    /// Inclui as regras de paginacao (FiltroPaginacaoVOValidator), porque o filtro de acao
    /// resolve o validador pelo tipo exato. Categoria, status e placa so sao validados quando informados.
    /// Busca: ate ConstantsValues.TamanhoMaximoBuscaVeiculo caracteres aparados e sem caractere nulo.
    /// As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class FiltroVeiculoVOValidator : AbstractValidator<FiltroVeiculoVO>
    {
        private const char CaractereNulo = '\0';
        private const string NomeCampoPlaca = "Placa";
        private const string NomeCampoBusca = "Busca";

        public FiltroVeiculoVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.IdCategoria)
                .Must(idCategoria => !idCategoria.HasValue || idCategoria.Value > 0)
                .WithMessage(ConstantsMessage.ErroVeiculoFiltroCategoriaInvalida);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, string.Join(", ", Enum.GetNames<StatusVeiculo>())));

            RuleFor(x => x.Placa)
                .MaximumLength(ConstantsValues.TamanhoMaximoBuscaPlacaVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoFiltroPlacaTamanho, ConstantsValues.TamanhoMaximoBuscaPlacaVeiculo));

            // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
            RuleFor(x => x.Placa)
                .Must(placa => placa == null || !placa.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoPlaca));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoFiltroBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaVeiculo));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !busca.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));

            // Substituto UTF-16 sem par nao e texto valido: recusa aqui, com 400, como o caractere nulo.
            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !ContemSubstitutoIsolado(busca))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));
        }

        /// <summary>
        /// Indica se o texto tem substituto UTF-16 sem par: substituto baixo solto, ou substituto alto sem o baixo que o segue.
        /// Par valido (caractere fora do plano basico) passa.
        /// </summary>
        private static bool ContemSubstitutoIsolado(string texto)
        {
            for (int indice = 0; indice < texto.Length; indice++)
            {
                char caractere = texto[indice];

                if (char.IsLowSurrogate(caractere))
                {
                    return true;
                }

                if (char.IsHighSurrogate(caractere))
                {
                    bool temParBaixo = indice + 1 < texto.Length && char.IsLowSurrogate(texto[indice + 1]);

                    if (!temParBaixo)
                    {
                        return true;
                    }

                    indice++;
                }
            }

            return false;
        }
    }
}
