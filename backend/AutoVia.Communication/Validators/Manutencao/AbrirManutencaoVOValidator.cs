using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Manutencao
{
    /// <summary>
    /// Validador FluentValidation da abertura de ordem de manutencao (POST /api/manutencoes). Id do veiculo maior que zero;
    /// tipo obrigatorio e, depois do Trim e em comparacao ordinal, igual a PREVENTIVA ou CORRETIVA; oficina e descricao
    /// com teto de tamanho e sem caractere nulo. A data de entrada futura fica no servico (depende do relogio). As
    /// mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class AbrirManutencaoVOValidator : AbstractValidator<AbrirManutencaoVO>
    {
        private const char CaractereNulo = '\0';
        private const string NomeCampoOficina = "Oficina";
        private const string NomeCampoDescricao = "Descricao";

        public AbrirManutencaoVOValidator()
        {
            RuleFor(x => x.IdVeiculo)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroManutencaoVeiculoObrigatorio);

            RuleFor(x => x.Tipo)
                .Must(tipo => !string.IsNullOrWhiteSpace(tipo))
                .WithMessage(ConstantsMessage.ErroManutencaoTipoObrigatorio);

            RuleFor(x => x.Tipo)
                .Must(tipo => NomesDeTipo().Contains(tipo!.Trim(), StringComparer.Ordinal))
                .When(x => !string.IsNullOrWhiteSpace(x.Tipo))
                .WithMessage(ConstantsMessage.ErroManutencaoTipoInvalido);

            RuleFor(x => x.Oficina)
                .MaximumLength(ConstantsValues.TamanhoMaximoOficinaManutencao)
                .WithMessage(string.Format(ConstantsMessage.ErroManutencaoOficinaTamanho, ConstantsValues.TamanhoMaximoOficinaManutencao));

            RuleFor(x => x.Descricao)
                .MaximumLength(ConstantsValues.TamanhoMaximoDescricaoManutencao)
                .WithMessage(string.Format(ConstantsMessage.ErroManutencaoDescricaoTamanho, ConstantsValues.TamanhoMaximoDescricaoManutencao));

            // Caractere NUL nos textos da ordem: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
            RuleFor(x => x.Oficina)
                .Must(oficina => oficina == null || !oficina.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoOficina));

            RuleFor(x => x.Descricao)
                .Must(descricao => descricao == null || !descricao.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoDescricao));
        }

        /// <summary>Nomes dos valores de TipoManutencao, na ordem da declaracao (PREVENTIVA e CORRETIVA).</summary>
        private static string[] NomesDeTipo()
        {
            return Enum.GetNames<TipoManutencao>();
        }
    }
}
