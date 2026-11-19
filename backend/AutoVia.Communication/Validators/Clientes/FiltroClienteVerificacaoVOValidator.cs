using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System;
using System.Linq;

namespace AutoVia.Communication.Validators.Clientes
{
    /// <summary>
    /// Validador FluentValidation do filtro da fila de verificacao (GET /api/clientes/verificacoes). Inclui a paginacao
    /// (FiltroPaginacaoVOValidator). Status, quando informado, e o nome exato de StatusCadastroCliente. Busca: ate
    /// ConstantsValues.TamanhoMaximoBuscaClienteVerificacao caracteres depois de Trim, sem caractere nulo e sem substituto
    /// UTF-16 isolado (o PostgreSQL recusa os dois e a resposta sairia como 500).
    /// </summary>
    public class FiltroClienteVerificacaoVOValidator : AbstractValidator<FiltroClienteVerificacaoVO>
    {
        private const char CaractereNulo = '\0';
        private const string NomeCampoBusca = "Busca";

        public FiltroClienteVerificacaoVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Status)
                .Must(status => status == null || Enum.GetNames<StatusCadastroCliente>().Contains(status, StringComparer.Ordinal))
                .WithMessage(ConstantsMessage.ErroClienteFilaStatusInvalido);

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaClienteVerificacao)
                .WithMessage(string.Format(ConstantsMessage.ErroClienteFilaBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaClienteVerificacao));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !busca.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !ContemSubstitutoIsolado(busca))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));
        }

        /// <summary>Indica se o texto tem substituto UTF-16 sem par (baixo solto, ou alto sem o baixo que o segue).</summary>
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
