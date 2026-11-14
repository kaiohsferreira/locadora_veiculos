namespace AutoVia.Helpers
{
    /// <summary>Formatacao do CPF exibido pela equipe da locadora (P10-BE-03).</summary>
    public static partial class StaticMethods
    {
        /// <summary>
        /// Mascara o CPF para exibicao: mantem os 3 primeiros e os 2 ultimos digitos (341.***.***-12). O CPF completo nunca
        /// sai daqui. Valor sem 11 digitos devolve string vazia, para nao expor nada.
        /// </summary>
        public static string MascararCpf(string? cpf)
        {
            string digitos = OnlyDigits(cpf);

            if (digitos.Length != ConstantsValues.TamanhoCpfFuncionario)
            {
                return string.Empty;
            }

            string inicio = digitos.Substring(0, 3);
            string fim = digitos.Substring(digitos.Length - 2, 2);

            return $"{inicio}.{ConstantsValues.MascaraCpfOculta}.{ConstantsValues.MascaraCpfOculta}-{fim}";
        }
    }
}
