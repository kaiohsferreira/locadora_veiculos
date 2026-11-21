using AutoVia.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Api.Requests.Avaria
{
    /// <summary>
    /// Conversao unica do valor estimado do formulario da avaria. Le sempre com ponto decimal (CultureInfo.InvariantCulture),
    /// independente da cultura do servidor, e recusa texto ambiguo ("150,50", "1.500,00", "1 500", sinal, expoente).
    /// </summary>
    public static class ConversorValorEstimadoFormulario
    {
        /// <summary>
        /// Converte o texto do formulario. Nulo ou vazio significa sem valor (o campo e opcional). Retorna falso quando o texto
        /// nao segue o formato aceito ou nao cabe em decimal. A faixa de 0 a 99999999.99 nao e conferida aqui: fica no validador.
        /// </summary>
        public static bool TentarConverter(string? texto, out decimal? valor)
        {
            if (string.IsNullOrEmpty(texto))
            {
                valor = null;
                return true;
            }

            if (!Regex.IsMatch(texto, ConstantsValues.PadraoValorEstimadoFormulario))
            {
                valor = null;
                return false;
            }

            bool convertido = decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal numero);
            valor = convertido ? (decimal?)numero : null;
            return convertido;
        }
    }
}
