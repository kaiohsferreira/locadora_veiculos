namespace AutoVia.Communication.CustomExceptions
{
    /// <summary>Upload pos-criacao falhou; inclui identificador e rota de retry.</summary>
    public sealed class AvariaUploadException : Exception
    {
        public AvariaUploadException(int idVistoria, int idAvaria, string message, Exception innerException) : base(message, innerException)
        {
            IdVistoria = idVistoria;
            IdAvaria = idAvaria;
        }

        public int IdVistoria { get; }
        public int IdAvaria { get; }
    }
}
