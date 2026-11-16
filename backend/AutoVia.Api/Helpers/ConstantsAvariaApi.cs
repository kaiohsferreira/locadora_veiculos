namespace AutoVia.Api.Helpers
{
    /// <summary>Chaves de extensao ProblemDetails para o fluxo de retry de avaria.</summary>
    public static class ConstantsAvariaApi
    {
        public const string IdAvaria = "idAvaria";
        public const string RetryUploadUrl = "retryUploadUrl";
        public const string RetryUploadPath = "/api/vistorias/{0}/avarias/{1}/foto";
    }
}
