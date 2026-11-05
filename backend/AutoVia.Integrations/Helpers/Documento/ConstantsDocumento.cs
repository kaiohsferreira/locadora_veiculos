namespace AutoVia.Integrations.Helpers.Documento
{
    /// <summary>
    /// Constantes da leitura de documentos (CNH). Chaves de integration_setting,
    /// nome do provedor FAKE, limites da imagem e dados fixos do provedor de teste.
    /// </summary>
    public static class ConstantsDocumento
    {
        // --- Chaves de integration_setting da integracao DOCUMENTO ---
        public const string ChaveProvedor = "provedor";
        public const string ChaveModeloOcr = "modelo_ocr";
        public const string ChaveConfiancaMinima = "confianca_minima";
        public const string ChaveApiKey = "api_key";

        // --- Provedores ---
        public const string ProvedorFake = "FAKE";

        // --- Imagem aceita ---
        public const string TipoJpeg = "image/jpeg";
        public const string TipoPng = "image/png";
        public const int TamanhoMaximoMb = 4;
        public const int TamanhoMaximoBytes = TamanhoMaximoMb * 1024 * 1024;

        /// <summary>Assinatura do JPEG: FF D8 FF.</summary>
        public static readonly byte[] AssinaturaJpeg = new byte[] { 0xFF, 0xD8, 0xFF };

        /// <summary>Assinatura do PNG: 89 50 4E 47.</summary>
        public static readonly byte[] AssinaturaPng = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // --- Campos da CNH ---
        public const int TamanhoNumeroCnh = 11;

        /// <summary>Formatos aceitos para a validade, em InvariantCulture.</summary>
        public static readonly string[] FormatosValidade = new string[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd/MM/yy" };

        // --- Faixa valida da confianca_minima ---
        public const decimal ConfiancaMinimaLimiteInferior = 0m;
        public const decimal ConfiancaMinimaLimiteSuperior = 1m;

        // --- Provedor FAKE: CNH sintetica fixa ---
        public const string FakeNumeroCnh = "12345678901";
        public const string FakeCategoria = "B";
        public const string FakeValidade = "2030-12-31";
        public const decimal FakeConfianca = 0.97m;
        public const decimal FakeConfiancaBaixa = 0.60m;

        /// <summary>Se o nome do arquivo contiver este texto, o FAKE devolve confianca baixa.</summary>
        public const string FakeMarcadorBaixaConfianca = "baixa-confianca";
    }
}
