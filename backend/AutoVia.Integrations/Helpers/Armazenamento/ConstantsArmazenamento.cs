namespace AutoVia.Integrations.Helpers.Armazenamento
{
    /// <summary>Chaves e valores padrao da integracao ARMAZENAMENTO.</summary>
    public static class ConstantsArmazenamento
    {
        public const string Codigo = "ARMAZENAMENTO";
        public const string ChaveAccessKey = "access_key";
        public const string ChaveSecretKey = "secret_key";
        public const string ChaveBucket = "bucket";
        public const string ChaveRegiao = "regiao";
        public const string ChaveExpiracao = "url_expiracao_segundos";
        public const string ChaveProvedor = "provedor";
        public const string ProvedorS3Compativel = "S3_COMPATIVEL";
        public const string BucketPadrao = "autovia-arquivos";
        public const string RegiaoPadrao = "sa-east-1";
        public const int ExpiracaoPadraoSegundos = 900;
        public const int ExpiracaoMaximaSegundos = 900;
        public const string MimeJpeg = "image/jpeg";
        public const string MimePng = "image/png";
        public const int TamanhoMaximoFotoBytes = 10485760;
        public const int DimensaoMaximaFotoPixels = 8192;
        public const long TotalMaximoPixelsFoto = 16777216;
        public const string ContentDispositionInline = "inline";
    }
}
