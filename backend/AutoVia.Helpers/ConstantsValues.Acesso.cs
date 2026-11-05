namespace AutoVia.Helpers
{
    /// <summary>
    /// Constantes do modulo de acesso (base comum SEAM): nomes das politicas de
    /// autorizacao e tamanho da coluna senha_hash.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Politica de autorizacao das rotas de funcionario (role FUNCIONARIO).</summary>
        public const string PoliticaFuncionario = "Funcionario";

        /// <summary>Politica de autorizacao das rotas de cliente (role CLIENTE).</summary>
        public const string PoliticaCliente = "Cliente";

        /// <summary>Tamanho maximo da coluna senha_hash (VARCHAR(255)). Guarda so o hash PBKDF2, nunca a senha.</summary>
        public const int TamanhoMaximoSenhaHash = 255;
    }
}
