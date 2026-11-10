namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da caucao no cartao (P7-INT-01, RF-20). Arquivo parcial de ConstantsMessage:
    /// cada modulo grava as suas constantes em arquivo proprio.
    /// Mensagens ao cliente sao FIXAS (sem texto do provedor). Mensagens de log podem ter {0}, {1}...
    /// com valores permitidos (codigo, provedor, HTTP, identificador, referencia enviada).
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Validacao da solicitacao (BusinessException, 409) ---
        public const string ErroCaucaoValorInvalido = "O valor da caução deve ser maior que zero e ter no máximo 2 casas decimais.";
        public const string ErroCaucaoReferenciaInvalida = "A referência da caução deve ter de 1 a 64 caracteres: letras, números, hífen ou sublinhado.";
        public const string ErroCaucaoDescricaoInvalida = "A descrição da caução é obrigatória e deve ter no máximo 140 caracteres.";
        public const string ErroCaucaoCartaoInvalido = "Os dados do cartão são inválidos: informe o token gerado pelo formulário de cartão e a bandeira do cartão.";
        public const string ErroCaucaoPagadorInvalido = "Os dados do pagador são inválidos: informe um e-mail válido e, se informado, um CPF com 11 dígitos.";
        public const string ErroCaucaoIdentificadorObrigatorio = "Informe o identificador da caução no provedor.";
        public const string ErroCaucaoIdentificadorInvalido = "O identificador da caução no provedor não está em um formato válido.";

        // --- Configuracao da integracao PAGAMENTO (IntegrationException, 502) ---
        public const string ErroCaucaoValidadeConfiguracaoInvalida = "A configuração caucao_validade_dias da integração de pagamento não é um número entre 1 e 30.";
        public const string ErroCaucaoProvedorNaoSuportado = "O provedor de caução configurado não é suportado.";
        public const string ErroCaucaoFakeForaDoSandbox = "O provedor de teste (FAKE) só pode ser usado no ambiente SANDBOX.";
        public const string ErroCaucaoChavePublicaInvalida = "A chave pública da integração de pagamento é igual a um valor protegido (como o access_token). Corrija a configuração no banco.";

        // --- Resposta do provedor (mensagens fixas: nunca texto do provedor) ---
        public const string ErroCaucaoCartaoRecusado = "O cartão foi recusado na pré-autorização da caução: {0}";
        public const string MotivoCaucaoSaldoInsuficiente = "limite ou saldo insuficiente.";
        public const string MotivoCaucaoDadosIncorretos = "dados do cartão incorretos.";
        public const string MotivoCaucaoAutorizacaoEmissor = "o banco emissor exige autorização; peça ao cliente para contatar o banco.";
        public const string MotivoCaucaoCartaoDesabilitado = "cartão desabilitado ou bloqueado; peça ao cliente para contatar o banco.";
        public const string MotivoCaucaoRiscoSeguranca = "recusado por segurança; tente outro cartão.";
        public const string MotivoCaucaoOutro = "recusado pelo emissor; tente outro cartão.";
        public const string ErroCaucaoSolicitacaoRecusada = "A solicitação de caução foi recusada pelo provedor de pagamento. Verifique os dados informados e tente novamente.";
        public const string ErroCaucaoNaoEncontrada = "A caução não foi encontrada no provedor.";
        public const string ErroCaucaoCapturaAcimaReservado = "O valor a capturar não pode ser maior que o valor reservado na caução.";
        public const string ErroCaucaoNaoReservada = "A caução não está com a reserva ativa (cancelada, expirada, recusada ou em análise) e não pode ser capturada.";
        public const string ErroCaucaoJaCapturadaOutroValor = "A caução já foi capturada por outro valor.";
        public const string ErroCaucaoCancelarCapturada = "Não é possível cancelar uma caução que já foi capturada.";
        public const string ErroCaucaoReservaNaoSuportada = "Não foi possível concluir a pré-autorização da caução. Acione o suporte antes de tentar novamente.";
        public const string ErroCaucaoCredenciaisRecusadas = "O provedor de pagamento recusou as credenciais configuradas.";
        public const string ErroCaucaoLimiteRequisicoes = "O provedor de pagamento recebeu muitas requisições. Tente novamente em instantes.";
        public const string ErroCaucaoProvedorIndisponivel = "O provedor de pagamento está indisponível no momento.";
        public const string ErroCaucaoRespostaInesperada = "A resposta do provedor de pagamento veio em um formato inesperado.";

        // --- Falhas inesperadas (log + IntegrationException com inner) ---
        public const string ErroCaucaoPreAutorizar = "Não foi possível pré-autorizar a caução.";
        public const string ErroCaucaoConsultar = "Não foi possível consultar a caução.";
        public const string ErroCaucaoCapturar = "Não foi possível capturar a caução.";
        public const string ErroCaucaoCancelar = "Não foi possível cancelar a caução.";
        public const string ErroCaucaoObterConfiguracaoCartao = "Não foi possível obter a configuração do formulário de cartão.";

        // --- Registro tecnico (log, nunca devolvido ao usuario). ---
        /// <summary>Campos: integracao, provedor, HTTP, status do provedor (limpo), detalhe do provedor (limpo), identificador.</summary>
        public const string LogCaucaoRespostaInesperada = "Resposta inesperada do provedor de caução: integração {0}, provedor {1}, HTTP {2}, status do provedor {3}, detalhe do provedor {4}, identificador {5}.";

        /// <summary>Campos: integracao, referencia ENVIADA, valor ENVIADO (F2), identificador do provedor. Unico lugar com a instrucao de estorno.</summary>
        public const string LogCaucaoReservaCapturadaIndevida = "Pré-autorização da caução voltou capturada: integração {0}, referência enviada {1}, valor enviado {2}, identificador do provedor {3}. Estorno manual necessário.";

        /// <summary>Campos: integracao, provedor, HTTP, codigos de causa do provedor (filtrados e truncados).</summary>
        public const string LogCaucaoCausaRecusada = "Solicitação de caução recusada pelo provedor: integração {0}, provedor {1}, HTTP {2}, códigos de causa {3}.";

        /// <summary>Campos: integracao, provedor, estado devolvido, identificador. Estado inesperado numa operacao (captura/cancelamento).</summary>
        public const string LogCaucaoEstadoInesperado = "Estado inesperado devolvido pelo provedor de caução: integração {0}, provedor {1}, estado {2}, identificador {3}.";

        /// <summary>Campos: integracao, referencia enviada. Permite localizar a reserva no provedor por external_reference.</summary>
        public const string LogCaucaoFalhaPreAutorizacao = "Falha na pré-autorização da caução: integração {0}, referência enviada {1}. Consulte o provedor pela referência externa antes de tentar novamente.";
    }
}
