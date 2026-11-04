namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>
    /// Excecao de negocio da biblioteca de integracoes: integracao inexistente,
    /// inativa, sem chave obrigatoria ou falha ao falar com o provedor.
    /// O middleware de erro converte em HTTP 502 com ProblemDetails.
    ///
    /// A mensagem diz QUAL integracao e QUAL chave, mas nunca o VALOR da chave.
    /// </summary>
    public class IntegrationException : Exception
    {
        public IntegrationException(string codigoIntegracao, string message)
            : base(message)
        {
            CodigoIntegracao = codigoIntegracao;
        }

        public IntegrationException(string codigoIntegracao, string? chave, string message)
            : base(message)
        {
            CodigoIntegracao = codigoIntegracao;
            Chave = chave;
        }

        public IntegrationException(string codigoIntegracao, string message, Exception innerException)
            : base(message, innerException)
        {
            CodigoIntegracao = codigoIntegracao;
        }

        /// <summary>Codigo da integracao (ex.: PAGAMENTO).</summary>
        public string CodigoIntegracao { get; }

        /// <summary>Chave de integration_setting envolvida no erro, quando houver.</summary>
        public string? Chave { get; }
    }
}
