using AutoVia.Communication.Enums;
using System.Globalization;
using System.Text;

namespace AutoVia.Integrations.Helpers.Documento
{
    /// <summary>
    /// Normalizacao pura dos campos lidos pelo OCR da CNH: sem IO e sem excecao.
    /// Valor que nao pode ser normalizado volta null, e o gateway trata o campo
    /// como ausente.
    /// </summary>
    public static class CnhFieldNormalizer
    {
        /// <summary>Mantem somente os digitos ASCII. Devolve null se nao sobrar nenhum.</summary>
        public static string? NormalizarNumero(string? numeroBruto)
        {
            if (string.IsNullOrWhiteSpace(numeroBruto))
                return null;

            StringBuilder digitos = new StringBuilder();
            foreach (char caractere in numeroBruto)
            {
                if (caractere >= '0' && caractere <= '9')
                    digitos.Append(caractere);
            }

            if (digitos.Length == 0)
                return null;

            return digitos.ToString();
        }

        /// <summary>Remove espacos, põe em maiusculas e converte para CategoriaCnh. Null se nao for A, B, AB, C, D ou E.</summary>
        public static CategoriaCnh? NormalizarCategoria(string? categoriaBruta)
        {
            if (string.IsNullOrWhiteSpace(categoriaBruta))
                return null;

            StringBuilder semEspacos = new StringBuilder();
            foreach (char caractere in categoriaBruta)
            {
                if (!char.IsWhiteSpace(caractere))
                    semEspacos.Append(caractere);
            }

            string categoria = semEspacos.ToString().ToUpperInvariant();

            // Confere pelo nome do membro: Enum.Parse aceitaria numeros ("1") e viraria B.
            string[] nomesValidos = Enum.GetNames<CategoriaCnh>();
            if (Array.IndexOf(nomesValidos, categoria) < 0)
                return null;

            return Enum.Parse<CategoriaCnh>(categoria);
        }

        /// <summary>Converte a validade nos formatos dd/MM/yyyy, yyyy-MM-dd ou dd/MM/yy (InvariantCulture). Null se invalida.</summary>
        public static DateOnly? NormalizarValidade(string? validadeBruta)
        {
            if (string.IsNullOrWhiteSpace(validadeBruta))
                return null;

            DateOnly validade;
            bool valida = DateOnly.TryParseExact(
                validadeBruta.Trim(),
                ConstantsDocumento.FormatosValidade,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out validade);

            if (!valida)
                return null;

            return validade;
        }
    }
}
