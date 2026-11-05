namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da cobranca PIX (P7-INT-01). Arquivo parcial de ConstantsMessage:
    /// cada modulo grava as suas constantes em arquivo proprio.
    /// {0} = motivo mascarado do provedor (ErroPixCobrancaRecusada).
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Validacao da solicitacao (BusinessException, 409) ---
        public const string ErroPixValorInvalido = "O valor da cobrança PIX deve ser maior que zero e ter no máximo 2 casas decimais.";
        public const string ErroPixReferenciaInvalida = "A referência da cobrança PIX deve ter de 1 a 64 caracteres: letras, números, hífen ou sublinhado.";
        public const string ErroPixDescricaoInvalida = "A descrição da cobrança PIX é obrigatória e deve ter no máximo 140 caracteres.";
        public const string ErroPixPagadorInvalido = "Os dados do pagador são inválidos: informe o nome (até 120 caracteres), um e-mail válido e, se informado, um CPF com 11 dígitos.";
        public const string ErroPixExpiracaoInvalida = "A expiração da cobrança PIX deve ser maior que zero e de no máximo 2592000 segundos (30 dias).";
        public const string ErroPixIdentificadorObrigatorio = "Informe o identificador da cobrança PIX no provedor.";

        // --- Configuracao da integracao PAGAMENTO (IntegrationException, 502) ---
        public const string ErroPixExpiracaoConfiguracaoInvalida = "A configuração pix_expiracao_segundos da integração de pagamento não é um número entre 1 e 2592000.";
        public const string ErroPixProvedorNaoSuportado = "O provedor de cobrança PIX configurado não é suportado.";
        public const string ErroPixFakeForaDoSandbox = "O provedor de teste (FAKE) só pode ser usado no ambiente SANDBOX.";

        // --- Resposta do provedor ---
        public const string ErroPixCobrancaRecusada = "A cobrança PIX foi recusada pelo provedor: {0}";
        public const string ErroPixCobrancaNaoEncontrada = "A cobrança PIX não foi encontrada no provedor.";
        public const string ErroPixCancelarCobrancaPaga = "Não é possível cancelar uma cobrança PIX que já foi paga.";
        public const string ErroPixCredenciaisRecusadas = "O provedor de pagamento recusou as credenciais configuradas.";
        public const string ErroPixLimiteRequisicoes = "O provedor de pagamento recebeu muitas requisições. Tente novamente em instantes.";
        public const string ErroPixProvedorIndisponivel = "O provedor de pagamento está indisponível no momento.";
        public const string ErroPixRespostaInesperada = "A resposta do provedor de pagamento veio em um formato inesperado.";

        // --- Falhas inesperadas (log + IntegrationException com inner) ---
        public const string ErroPixCriarCobranca = "Não foi possível criar a cobrança PIX.";
        public const string ErroPixConsultarCobranca = "Não foi possível consultar a cobrança PIX.";
        public const string ErroPixCancelarCobranca = "Não foi possível cancelar a cobrança PIX.";
    }
}
