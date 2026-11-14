namespace AutoVia.Helpers
{
    /// <summary>
    /// CNPJ (multi-locadora, P10-BE-01): validacao pelos digitos verificadores e formatacao.
    /// Parte parcial de StaticMethods, junto do CPF. Recusa sequencias de um digito repetido
    /// (inclusive 00000000000000) e aceita com ou sem mascara.
    /// </summary>
    public static partial class StaticMethods
    {
        private const int TamanhoCnpj = 14;

        private static readonly int[] PesosPrimeiroDigitoCnpj = new int[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        private static readonly int[] PesosSegundoDigitoCnpj = new int[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        /// <summary>
        /// Valida o CNPJ pelos dois digitos verificadores (pesos da Receita Federal). Aceita com ou sem mascara;
        /// devolve false para qualquer valor que nao tenha 14 digitos, para sequencias repetidas e para digito errado.
        /// </summary>
        public static bool IsCnpjValido(string? cnpj)
        {
            string digitos = OnlyDigits(cnpj);

            if (digitos.Length != TamanhoCnpj)
                return false;

            bool todosIguais = true;
            for (int indice = 1; indice < digitos.Length; indice++)
            {
                if (digitos[indice] != digitos[0])
                {
                    todosIguais = false;
                    break;
                }
            }

            if (todosIguais)
                return false;

            int primeiroDigito = CalcularDigitoCnpj(digitos, PesosPrimeiroDigitoCnpj);
            int segundoDigito = CalcularDigitoCnpj(digitos, PesosSegundoDigitoCnpj);

            return primeiroDigito == digitos[12] - '0' && segundoDigito == digitos[13] - '0';
        }

        /// <summary>Formata 14 digitos como 00.000.000/0000-00. Sem 14 digitos, devolve o valor recebido (ou vazio).</summary>
        public static string FormateCnpj(string? cnpj)
        {
            string digitos = OnlyDigits(cnpj);

            if (digitos.Length != TamanhoCnpj)
                return cnpj ?? string.Empty;

            return $"{digitos.Substring(0, 2)}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}/{digitos.Substring(8, 4)}-{digitos.Substring(12, 2)}";
        }

        /// <summary>Soma ponderada dos digitos informados pelos pesos e devolve o digito verificador (resto menor que 2 vira 0).</summary>
        private static int CalcularDigitoCnpj(string digitos, int[] pesos)
        {
            int soma = 0;

            for (int indice = 0; indice < pesos.Length; indice++)
            {
                soma += (digitos[indice] - '0') * pesos[indice];
            }

            int resto = soma % 11;

            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
