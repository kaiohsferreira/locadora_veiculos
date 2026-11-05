namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>
    /// Login recusado (credenciais invalidas ou conta sem acesso). O middleware de erro
    /// converte em HTTP 401 com ProblemDetails e header WWW-Authenticate: Bearer. A
    /// mensagem deve vir de ConstantsMessage e nunca detalha qual dado falhou.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}
