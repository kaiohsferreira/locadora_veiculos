using AutoVia.Helpers;
using System.Text;

namespace AutoVia.Communication.Regras
{
    /// <summary>
    /// Regras puras da entrada do cartao da caucao na retirada (contrato caucao-retirada v1.1, CS-2). Usadas pelo
    /// LocacaoVOValidator no token e no device id. Sem acesso a dados e sem excecao.
    /// </summary>
    public static class RegrasEntradaCaucao
    {
        /// <summary>
        /// Verdadeiro quando o texto, sem os separadores '-', '_', '.', ':' e espaco, sobra so com digitos (0 a 9) e tem de 12 a
        /// 19 posicoes: parece um numero de cartao (ex.: '4111111111111111' ou '4111-1111-1111-1111'). Nulo, vazio, so
        /// separadores ou texto com qualquer letra devolvem falso. Motivo: o token entra na chave de idempotencia que gera o id do
        /// FAKE (vai para o banco e para o log); um numero de cartao no token seria achado por forca bruta.
        /// </summary>
        public static bool PareceNumeroDeCartao(string? texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }

            StringBuilder digitos = new StringBuilder(texto.Length);

            foreach (char caractere in texto)
            {
                if (ConstantsValues.CaucaoSeparadoresNumeroCartao.IndexOf(caractere) >= 0)
                {
                    continue;
                }

                bool digitoAscii = caractere >= '0' && caractere <= '9';

                if (!digitoAscii)
                {
                    return false;
                }

                digitos.Append(caractere);
            }

            bool pareceCartao = digitos.Length >= ConstantsValues.CaucaoDigitosCartaoMinimo
                && digitos.Length <= ConstantsValues.CaucaoDigitosCartaoMaximo;

            return pareceCartao;
        }
    }
}
