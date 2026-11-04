namespace AutoVia.Integrations.Communication.Base
{
    /// <summary>
    /// Arquivo enviado em requisicao multipart (foto de vistoria, imagem da CNH,
    /// anexo de sinistro).
    /// </summary>
    public class RequestFile
    {
        /// <summary>Nome do campo do formulario esperado pelo provedor (ex.: "file").</summary>
        public string FieldName { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
