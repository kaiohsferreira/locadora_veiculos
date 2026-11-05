namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da leitura de documentos (CNH, RF-09). Arquivo parcial da classe
    /// ConstantsMessage. {0} = valor formatado pelo chamador.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Documento: leitura da CNH (P2-INT-01) ---
        public const string ErroDocumentoImagemObrigatoria = "Envie a imagem da CNH.";
        public const string ErroDocumentoImagemTipo = "A imagem da CNH deve ser JPG ou PNG.";
        public const string ErroDocumentoImagemTamanho = "A imagem da CNH deve ter no máximo {0} MB.";
        public const string ErroDocumentoConfiancaMinimaInvalida = "A configuração confianca_minima da integração de documentos é inválida.";
        public const string ErroDocumentoProvedorNaoSuportado = "O provedor configurado para a leitura de documentos não é suportado.";
        public const string ErroDocumentoFakeForaDoSandbox = "O provedor de teste de documentos só pode ser usado no ambiente SANDBOX.";
        public const string ErroDocumentoImagemIlegivel = "Não foi possível ler a CNH. Envie uma imagem mais nítida.";
        public const string ErroDocumentoLerCnh = "Não foi possível ler a CNH.";
    }
}
