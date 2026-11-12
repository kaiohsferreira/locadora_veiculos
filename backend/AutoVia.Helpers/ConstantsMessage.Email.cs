namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da porta de e-mail (BE-3, integracao EMAIL). Arquivo parcial de ConstantsMessage.
    /// Textos do contrato integracao-email v1. Nenhum valor de destinatario, assunto ou corpo vai para mensagem.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Validacao da mensagem (BusinessException, 409) ---
        public const string ErroEmailMensagemInvalida = "A mensagem de e-mail é inválida.";

        // --- Integracao EMAIL (IntegrationException, 502) ---
        public const string ErroEmailProvedorNaoSuportado = "O provedor de e-mail configurado não é suportado.";
        public const string ErroEmailFakeForaDoDesenvolvimento = "O provedor de e-mail FAKE só pode ser usado em desenvolvimento.";
        public const string ErroEmailEnviar = "Não foi possível enviar o e-mail.";

        // --- Fila e caixa de teste (log / IntegrationException) ---
        public const string ErroEmailFilaCheia = "A fila de e-mails está cheia.";
        public const string ErroEmailGravarCaixaFake = "Não foi possível gravar a mensagem de e-mail de teste.";
        public const string ErroEmailProcessarFila = "Não foi possível processar a fila de e-mails.";

        // --- Registro tecnico (log). Campos: provedor, etiqueta, identificador. Nunca dado pessoal. ---
        public const string LogEmailEnviado = "E-mail enviado: provedor {0}, etiqueta {1}, identificador {2}.";
    }
}
