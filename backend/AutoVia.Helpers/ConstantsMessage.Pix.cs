namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da cobranca PIX (P7-INT-01 e semana 4). Arquivo parcial de ConstantsMessage:
    /// cada modulo grava as suas constantes em arquivo proprio. Sem corpo do provedor em mensagem de usuario
    /// (ErroPixCobrancaRecusada nao tem argumento).
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
        public const string ErroPixFakeForaDoSandbox = "O provedor de teste (FAKE) do PIX só responde em SANDBOX, com ambiente de desenvolvimento e a chave Pagamentos:PermitirProvedorFake ligada.";

        // --- Resposta do provedor ---
        public const string ErroPixCobrancaRecusada = "A cobrança PIX foi recusada pelo provedor.";
        public const string ErroPixCobrancaNaoEncontrada = "A cobrança PIX não foi encontrada no provedor.";
        public const string ErroPixCobrancaNaoAguardaPagamento = "A cobrança PIX não está aguardando pagamento e não pode ser marcada como paga.";
        public const string ErroPixCancelarCobrancaPaga = "Não é possível cancelar uma cobrança PIX que já foi paga.";
        public const string ErroPixCredenciaisRecusadas = "O provedor de pagamento recusou as credenciais configuradas.";
        public const string ErroPixLimiteRequisicoes = "O provedor de pagamento recebeu muitas requisições. Tente novamente em instantes.";
        public const string ErroPixProvedorIndisponivel = "O provedor de pagamento está indisponível no momento.";
        public const string ErroPixRespostaInesperada = "A resposta do provedor de pagamento veio em um formato inesperado.";

        // --- Falhas inesperadas (log + IntegrationException com inner) ---
        public const string ErroPixCriarCobranca = "Não foi possível criar a cobrança PIX.";
        public const string ErroPixConsultarCobranca = "Não foi possível consultar a cobrança PIX.";
        public const string ErroPixCancelarCobranca = "Não foi possível cancelar a cobrança PIX.";

        // --- Geracao e reabertura do PIX da cobranca (semana 4). Usuario (409) e falhas (log) ---
        public const string ErroPixPagamentoJaPago = "Este pagamento já foi pago. Não é preciso gerar um PIX.";
        public const string ErroPixPagamentoCancelado = "Pagamento cancelado não pode receber PIX.";
        public const string ErroPixTipoNaoPagavel = "Esta cobrança não pode ser paga por PIX.";
        public const string ErroPixGerarIndisponivel = "Não foi possível gerar um PIX novo para esta cobrança agora. Tente novamente em instantes.";
        public const string ErroGerarPixPagamento = "Falha ao gerar ou reabrir o PIX da cobrança.";
        public const string ErroGravarPixPagamento = "Falha ao gravar o PIX gerado na cobrança.";

        // --- Informativos e avisos da geracao (log; so o id da cobranca, sem dado pessoal) ---
        public const string InfoPixGerado = "PIX gerado para a cobrança {0}.";
        public const string InfoPixReaberto = "PIX válido reaberto para a cobrança {0}, sem consulta ao provedor.";
        public const string InfoPixDescartadoPorConcorrencia = "Outro PIX válido foi gravado para a cobrança {0} durante a chamada; o gerado nesta chamada foi descartado.";
        public const string LogPixPagoNoProvedor = "O provedor devolveu a cobrança {0} como paga antes da gravação; a baixa é manual.";
        public const string LogPixStatusNaoAguardandoNoProvedor = "O provedor devolveu a cobrança {0} com status {1} ao gerar o PIX.";
        public const string LogPixRespostaDivergente = "A resposta do provedor para a cobrança {0} traz {1} diferente do pedido; nada foi gravado.";
        public const string LogPixRecusadaPeloProvedor = "O provedor recusou a cobrança PIX: HTTP {0}.";

        // --- Registro tecnico (log, nunca devolvido ao usuario). Campos: integracao, provedor, HTTP, status do provedor, identificador. ---
        public const string LogPixRespostaInesperada = "Resposta inesperada do provedor PIX: integração {0}, provedor {1}, HTTP {2}, status do provedor {3}, identificador {4}.";
        public const string ErroCancelarPixDescartado = "Falha ao cancelar no provedor o PIX criado para a cobrança {0}, que a gravação recusou.";
    }
}
