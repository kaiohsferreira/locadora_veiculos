namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>
    /// Violacao de regra de negocio (ex.: reservar veiculo ja reservado no periodo,
    /// emitir contrato sem vistoria de entrada). O middleware de erro converte em
    /// HTTP 409 com ProblemDetails. A mensagem deve vir de ConstantsMessage*.
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message)
            : base(message)
        {
        }

        public BusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
