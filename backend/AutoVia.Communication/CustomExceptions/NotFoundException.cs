namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>
    /// Recurso inexistente (ex.: veiculo com id que nao existe). O middleware de
    /// erro converte em HTTP 404 com ProblemDetails.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
