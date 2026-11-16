namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da integracao ARMAZENAMENTO (contrato integracao-armazenamento v1). Arquivo parcial de ConstantsMessage.
    /// Nenhum valor de chave, URL assinada ou mensagem do SDK entra em mensagem: {0} de ErroArmazenamentoChaveNaoProtegida
    /// e o NOME da chave (access_key ou secret_key), nunca o valor.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Configuracao e selecao do provedor (IntegrationException, 502) ---
        public const string ErroArmazenamentoProvedorNaoSuportado = "O provedor de armazenamento configurado não é suportado.";
        public const string ErroArmazenamentoExpiracaoInvalida = "A configuração url_expiracao_segundos da integração de armazenamento é inválida.";
        public const string ErroArmazenamentoBaseUrlSemHttps = "A base_url da integração de armazenamento deve usar HTTPS.";
        public const string ErroArmazenamentoChaveNaoProtegida = "A chave {0} da integração de armazenamento deve estar marcada como protegida.";
        public const string ErroArmazenamentoUrlAssinadaInvalida = "O armazenamento gerou uma URL temporária inválida.";

        // --- Falha da operacao (IntegrationException, 502) ---
        public const string ErroArmazenamentoEnviar = "Não foi possível enviar o arquivo para o armazenamento.";
        public const string ErroArmazenamentoObterUrl = "Não foi possível gerar a URL temporária do arquivo.";
        public const string ErroArmazenamentoRemover = "Não foi possível remover o arquivo do armazenamento.";
    }
}
