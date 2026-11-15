namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>
    /// Teto de negocio atingido (ex.: pedidos de aprovacao de um cliente em 24 horas). Nao depende do IP. O middleware
    /// de erro converte em HTTP 429 com ProblemDetails (titulo de limite de requisicoes). A mensagem deve vir de ConstantsMessage*.
    /// </summary>
    public class LimiteExcedidoException : Exception
    {
        public LimiteExcedidoException(string message)
            : base(message)
        {
        }

        public LimiteExcedidoException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
