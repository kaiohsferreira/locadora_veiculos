using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Manutencao
{
    /// <summary>
    /// Validador FluentValidation da lista de ordens de manutencao (GET /api/manutencoes). Paginacao e estouro de pagina vem
    /// do FiltroPaginacaoVOValidator (1 a 100 por pagina). Busca com teto de tamanho (depois do Trim) e sem caractere nulo;
    /// tipo e situacao, quando informados, pelo nome exato depois do Trim; veiculo, quando informado, maior que zero. As
    /// mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class FiltroManutencaoVOValidator : AbstractValidator<FiltroManutencaoVO>
    {
        private const char CaractereNulo = '\0';
        private const string NomeCampoBusca = "Busca";

        public FiltroManutencaoVOValidator()
        {
            // Pagina, tamanho e estouro do deslocamento vem do FiltroPaginacaoVOValidator; nao repetir aqui.
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaManutencao)
                .WithMessage(string.Format(ConstantsMessage.ErroManutencaoBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaManutencao));

            // Caractere NUL na busca: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !busca.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));

            RuleFor(x => x.Tipo)
                .Must(tipo => string.IsNullOrWhiteSpace(tipo) || NomesDeTipo().Contains(tipo.Trim(), StringComparer.Ordinal))
                .WithMessage(ConstantsMessage.ErroManutencaoTipoInvalido);

            RuleFor(x => x.Situacao)
                .Must(situacao => string.IsNullOrWhiteSpace(situacao) || NomesDeSituacao().Contains(situacao.Trim(), StringComparer.Ordinal))
                .WithMessage(ConstantsMessage.ErroManutencaoSituacaoInvalida);

            RuleFor(x => x.IdVeiculo)
                .Must(idVeiculo => !idVeiculo.HasValue || idVeiculo.Value > 0)
                .WithMessage(ConstantsMessage.ErroManutencaoVeiculoFiltroInvalido);
        }

        /// <summary>Nomes dos valores de TipoManutencao, na ordem da declaracao (PREVENTIVA e CORRETIVA).</summary>
        private static string[] NomesDeTipo()
        {
            return Enum.GetNames<TipoManutencao>();
        }

        /// <summary>Valores aceitos na situacao: ABERTA e ENCERRADA.</summary>
        private static string[] NomesDeSituacao()
        {
            return new string[] { ConstantsValues.SituacaoManutencaoAberta, ConstantsValues.SituacaoManutencaoEncerrada };
        }
    }
}
