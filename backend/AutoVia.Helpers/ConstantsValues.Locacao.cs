namespace AutoVia.Helpers
{
    /// <summary>Valores do modulo de locacao (retirada da reserva, P4-BE-01): separadores do endereco da locadora mostrado na retirada.</summary>
    public static partial class ConstantsValues
    {
        /// <summary>Separador entre o logradouro e o numero do endereco da locadora (ex.: Rua das Flores, 100).</summary>
        public const string SeparadorNumeroEnderecoLocadora = ", ";

        /// <summary>Separador entre o numero e o bairro do endereco da locadora (ex.: 100 - Centro).</summary>
        public const string SeparadorBairroEnderecoLocadora = " - ";

        /// <summary>Separador entre o bairro e a cidade do endereco da locadora (ex.: Centro, Campinas).</summary>
        public const string SeparadorCidadeEnderecoLocadora = ", ";

        /// <summary>Separador entre a cidade e a UF do endereco da locadora (ex.: Campinas - SP).</summary>
        public const string SeparadorUfEnderecoLocadora = " - ";
    }
}
