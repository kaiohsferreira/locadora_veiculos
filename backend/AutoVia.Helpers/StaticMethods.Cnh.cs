namespace AutoVia.Helpers
{
    /// <summary>Mascara da CNH exibida a quem nao verifica cadastro (T-BE-06), no mesmo modelo da mascara do CPF da equipe.</summary>
    public static partial class StaticMethods
    {
        /// <summary>
        /// Mascara o numero da CNH para exibicao: so os ultimos 3 digitos ficam visiveis (********123). O numero
        /// completo nunca sai daqui. Valor que nao tem 11 digitos devolve string vazia, para nao expor nada.
        /// </summary>
        public static string MascararCnh(string? cnh)
        {
            string digitos = OnlyDigits(cnh);

            if (digitos.Length != ConstantsValues.TamanhoCnhNumero)
            {
                return string.Empty;
            }

            int quantidadeOculta = digitos.Length - ConstantsValues.DigitosVisiveisCnhMascarada;
            string ocultos = new string(ConstantsValues.CaractereMascaraCnh, quantidadeOculta);
            string visiveis = digitos.Substring(quantidadeOculta);

            return ocultos + visiveis;
        }
    }
}
