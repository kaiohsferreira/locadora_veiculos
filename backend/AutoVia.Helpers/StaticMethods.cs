using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoVia.Helpers
{
    /// <summary>
    /// Metodos utilitarios sem estado. Toda formatacao de data, moeda, CPF e
    /// placa usada pelos Profiles e servicos passa por aqui, para que o formato
    /// seja o mesmo em toda a API.
    /// </summary>
    public static partial class StaticMethods
    {
        private static readonly CultureInfo CulturaPtBr = new CultureInfo(ConstantsValues.CulturaPadrao);

        // ------------------------------------------------------------------
        // Datas
        // ------------------------------------------------------------------

        /// <summary>Formata data e hora como dd/MM/yyyy HH:mm.</summary>
        public static string FormateDateTimeToPTBR(DateTime value)
        {
            return value.ToString(ConstantsValues.FormatoDataHora, CulturaPtBr);
        }

        /// <summary>Formata data e hora anulavel; devolve string vazia quando nula.</summary>
        public static string FormateDateTimeToPTBR(DateTime? value)
        {
            return value.HasValue ? FormateDateTimeToPTBR(value.Value) : string.Empty;
        }

        /// <summary>Formata uma data (coluna DATE) como dd/MM/yyyy.</summary>
        public static string FormateDateToPTBR(DateOnly value)
        {
            return value.ToString(ConstantsValues.FormatoData, CulturaPtBr);
        }

        /// <summary>Formata uma data anulavel; devolve string vazia quando nula.</summary>
        public static string FormateDateToPTBR(DateOnly? value)
        {
            return value.HasValue ? FormateDateToPTBR(value.Value) : string.Empty;
        }

        // ------------------------------------------------------------------
        // Moeda
        // ------------------------------------------------------------------

        /// <summary>Formata um valor em reais (R$ 1.234,56).</summary>
        public static string FormateToMonetary(decimal value)
        {
            return value.ToString("C", CulturaPtBr);
        }

        /// <summary>Formata um valor anulavel em reais; devolve string vazia quando nulo.</summary>
        public static string FormateToMonetary(decimal? value)
        {
            return value.HasValue ? FormateToMonetary(value.Value) : string.Empty;
        }

        // ------------------------------------------------------------------
        // Texto
        // ------------------------------------------------------------------

        /// <summary>Remove tudo que nao for digito ASCII 0 a 9 (mascara de CPF, CEP, telefone). Digitos de outros alfabetos (ex.: arabico-indicos) sao descartados.</summary>
        public static string OnlyDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (character >= '0' && character <= '9')
                    builder.Append(character);
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------------
        // CPF
        // ------------------------------------------------------------------

        /// <summary>
        /// Valida CPF pelos digitos verificadores. Aceita com ou sem mascara.
        /// Rejeita sequencias repetidas (111.111.111-11), que passam no calculo
        /// mas nao sao CPF valido.
        /// </summary>
        public static bool IsCpfValido(string? cpf)
        {
            string digits = OnlyDigits(cpf);

            if (digits.Length != 11)
                return false;

            bool allEqual = true;
            for (int index = 1; index < digits.Length; index++)
            {
                if (digits[index] != digits[0])
                {
                    allEqual = false;
                    break;
                }
            }

            if (allEqual)
                return false;

            int firstVerifier = CalculateCpfVerifier(digits, 9);
            int secondVerifier = CalculateCpfVerifier(digits, 10);

            return firstVerifier == digits[9] - '0' && secondVerifier == digits[10] - '0';
        }

        /// <summary>Formata 11 digitos como 000.000.000-00.</summary>
        public static string FormateCpf(string? cpf)
        {
            string digits = OnlyDigits(cpf);

            if (digits.Length != 11)
                return cpf ?? string.Empty;

            return $"{digits.Substring(0, 3)}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits.Substring(9, 2)}";
        }

        private static int CalculateCpfVerifier(string digits, int length)
        {
            int sum = 0;
            int weight = length + 1;

            for (int index = 0; index < length; index++)
            {
                sum += (digits[index] - '0') * weight;
                weight--;
            }

            int rest = sum % 11;
            return rest < 2 ? 0 : 11 - rest;
        }

        // ------------------------------------------------------------------
        // Placa
        // ------------------------------------------------------------------

        /// <summary>
        /// Valida placa no padrao antigo (ABC1234) ou Mercosul (ABC1D23).
        /// Aceita hifen e minusculas; a coluna placa e CHAR(7), sem hifen.
        /// </summary>
        public static bool IsPlacaValida(string? placa)
        {
            string normalized = NormalizePlaca(placa);
            return PlacaRegex().IsMatch(normalized);
        }

        /// <summary>Normaliza a placa para o formato da coluna: 7 caracteres, maiusculos, sem hifen.</summary>
        public static string NormalizePlaca(string? placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
                return string.Empty;

            return placa.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
        }

        [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
        private static partial Regex PlacaRegex();
    }
}
