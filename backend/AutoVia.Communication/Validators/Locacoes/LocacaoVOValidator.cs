using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AutoVia.Communication.Validators.Locacoes
{
    /// <summary>
    /// Valida os dados para iniciar uma locacao. Sem nenhum campo do cartao passa como no RF-17. Quando os campos do cartao
    /// vierem, confere so o formato (contrato caucao-retirada v1.1, entrada_do_cartao): token e bandeira andam juntos; o token e
    /// o device id nao podem parecer numero de cartao. Toda falha do cartao responde ErroCaucaoCartaoInvalido, com o campo, e a
    /// mensagem nunca repete o valor enviado. Se a caucao e exigida e o cartao nao veio, quem recusa e o servico.
    /// </summary>
    public class LocacaoVOValidator : AbstractValidator<LocacaoVO>
    {
        public LocacaoVOValidator()
        {
            RuleFor(x => x.IdReserva)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroLocacaoReservaObrigatoria);

            RuleFor(x => x.CaucaoTokenCartao)
                .Must(TokenAusenteOuValido)
                .WithMessage(ConstantsMessage.ErroCaucaoCartaoInvalido);

            RuleFor(x => x.CaucaoMetodoPagamentoId)
                .Must((locacaoVO, bandeira) => BandeiraCombinaComToken(locacaoVO.CaucaoTokenCartao, bandeira))
                .WithMessage(ConstantsMessage.ErroCaucaoCartaoInvalido);

            RuleFor(x => x.CaucaoEmissorId)
                .Must(EmissorAusenteOuValido)
                .WithMessage(ConstantsMessage.ErroCaucaoCartaoInvalido);

            RuleFor(x => x.CaucaoIdentificadorDispositivo)
                .Must(DispositivoAusenteOuValido)
                .WithMessage(ConstantsMessage.ErroCaucaoCartaoInvalido);
        }

        /// <summary>Token ausente passa; quando vier: 8 a 128 caracteres de [A-Za-z0-9_-] e sem parecer numero de cartao.</summary>
        private static bool TokenAusenteOuValido(string? token)
        {
            if (token == null)
            {
                return true;
            }

            bool tamanhoValido = token.Length >= ConstantsValues.CaucaoTokenTamanhoMinimo
                && token.Length <= ConstantsValues.CaucaoTokenTamanhoMaximo;

            if (!tamanhoValido || !Regex.IsMatch(token, ConstantsValues.CaucaoPadraoToken))
            {
                return false;
            }

            bool parecido = RegrasEntradaCaucao.PareceNumeroDeCartao(token);

            return !parecido;
        }

        /// <summary>Bandeira obrigatoria com o token e proibida sem ele; quando vier: 2 a 30 caracteres de [a-z0-9_].</summary>
        private static bool BandeiraCombinaComToken(string? token, string? bandeira)
        {
            if (token == null)
            {
                return bandeira == null;
            }

            if (bandeira == null)
            {
                return false;
            }

            bool tamanhoValido = bandeira.Length >= ConstantsValues.CaucaoMetodoPagamentoTamanhoMinimo
                && bandeira.Length <= ConstantsValues.CaucaoMetodoPagamentoTamanhoMaximo;

            return tamanhoValido && Regex.IsMatch(bandeira, ConstantsValues.CaucaoPadraoMetodoPagamento);
        }

        /// <summary>Emissor ausente passa; quando vier: de 1 a 2147483647.</summary>
        private static bool EmissorAusenteOuValido(long? emissor)
        {
            if (!emissor.HasValue)
            {
                return true;
            }

            bool valido = emissor.Value > 0 && emissor.Value <= ConstantsValues.CaucaoEmissorIdMaximo;

            return valido;
        }

        /// <summary>Device id em branco = ausente; quando vier: [A-Za-z0-9_.:-] de 8 a 128 e sem parecer numero de cartao.</summary>
        private static bool DispositivoAusenteOuValido(string? dispositivo)
        {
            if (string.IsNullOrWhiteSpace(dispositivo))
            {
                return true;
            }

            if (!Regex.IsMatch(dispositivo, ConstantsValues.CaucaoPadraoDispositivo))
            {
                return false;
            }

            bool parecido = RegrasEntradaCaucao.PareceNumeroDeCartao(dispositivo);

            return !parecido;
        }
    }
}
