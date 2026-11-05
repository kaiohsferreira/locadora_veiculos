namespace AutoVia.Core.Options
{
    /// <summary>
    /// Administrador inicial (secao "Autenticacao:AdminInicial"). Opcional: sem CPF
    /// configurado, nada e criado. A senha so existe na configuracao (variavel de
    /// ambiente ou appsettings.Local.json) e precisa ter 12 caracteres ou mais.
    /// Depois do primeiro uso, remova o bloco da configuracao.
    /// </summary>
    public class AdminInicialOptions
    {
        /// <summary>CPF do administrador (com ou sem mascara).</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Nome do administrador.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Matricula do administrador.</summary>
        public string Matricula { get; set; } = string.Empty;

        /// <summary>Senha inicial, em texto, so ate ser convertida em hash. Nunca registrar.</summary>
        public string Senha { get; set; } = string.Empty;
    }
}
