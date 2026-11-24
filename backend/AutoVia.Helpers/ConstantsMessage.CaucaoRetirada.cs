namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da caucao na retirada (P4-BE-04, RF-20; contrato caucao-retirada v1.1). Arquivo parcial de ConstantsMessage.
    /// A mensagem de configuracao nao repete o valor informado: a chave vem do ambiente e o valor pode ser qualquer coisa.
    /// As mensagens so de log levam apenas ids, a referencia CAUCAO-RES-{id}, o identificador do provedor, o estado e o motivo:
    /// nunca token, final do cartao, e-mail, CPF, device id nem chave de idempotencia.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Erro de subida (InvalidOperationException): Pagamentos:ValorCaucao fora do formato ou da faixa aceitos. A API nao sobe.</summary>
        public const string ErroConfiguracaoValorCaucao = "A configuração Pagamentos:ValorCaucao é inválida. Use um número de 0 a 99999999.99, com ponto como separador decimal e no máximo 2 casas (exemplo: 1200.00).";

        /// <summary>409: a pre-autorizacao ficou EM_ANALISE (devolvida ou confirmada); o servidor cancela o id novo e a locacao nao abre.</summary>
        public const string ErroLocacaoCaucaoEmAnalise = "A pré-autorização da caução ficou em análise e não pôde ser usada. A locação não foi iniciada: faça uma nova tentativa.";

        /// <summary>409: a tentativa nao vale mais (CANCELADA, EXPIRADA, RECUSADA devolvida, ESTORNADA ou CAPTURADA); nada e cancelado e a locacao nao abre.</summary>
        public const string ErroLocacaoCaucaoNovaTentativa = "Esta tentativa de pré-autorização da caução não vale mais (foi cancelada, expirou ou foi estornada). A locação não foi iniciada: faça uma nova tentativa.";

        /// <summary>400 do servico (antes do provedor): a caucao e exigida e o pedido veio sem o token ou sem a bandeira do cartao.</summary>
        public const string ErroLocacaoCaucaoCartaoObrigatorio = "Esta retirada exige caução: informe o cartão de crédito do cliente.";

        // --- Registro tecnico (log, nunca devolvido ao usuario) ---

        /// <summary>Campos: reserva, locacao, referencia, identificador do provedor. Sucesso da gravacao conjunta.</summary>
        public const string InfoCaucaoRetiradaGravada = "Caução gravada com a locação: reserva {0}, locação {1}, referência {2}, identificador do provedor {3}.";

        /// <summary>Campos: reserva, referencia, nome do CaucaoStatus (devolvido ou confirmado). So nos estados que nao abrem.</summary>
        public const string LogCaucaoRetiradaNaoAprovada = "A pré-autorização da caução não abriu a locação: reserva {0}, referência {1}, situação {2}.";

        /// <summary>Campos: reserva, referencia. Recusa lancada pelo gateway (BusinessException, sem estado).</summary>
        public const string LogCaucaoRetiradaRecusada = "A pré-autorização da caução foi recusada e a locação não foi iniciada: reserva {0}, referência {1}.";

        /// <summary>Campos: reserva, referencia, identificador do provedor, motivo (VALOR, REFERENCIA ou IDENTIFICADOR). Nada e cancelado.</summary>
        public const string LogCaucaoRetiradaDivergente = "Retorno divergente na pré-autorização da caução: reserva {0}, referência {1}, identificador do provedor {2}, motivo {3}. Nada foi cancelado: confira no provedor.";

        /// <summary>Campos: reserva, referencia, identificador do provedor. Compensacao feita (id novo que nao foi gravado).</summary>
        public const string InfoCaucaoRetiradaCompensada = "Caução cancelada no provedor porque a locação não foi gravada: reserva {0}, referência {1}, identificador do provedor {2}.";

        /// <summary>Campos: reserva, referencia, identificador do provedor. O cancelamento de compensacao falhou: cancelamento manual.</summary>
        public const string ErroCaucaoRetiradaCompensacao = "Não foi possível cancelar no provedor a caução que não foi gravada: reserva {0}, referência {1}, identificador do provedor {2}. Cancele manualmente.";

        /// <summary>Campos: reserva, identificador do provedor. A releitura achou o mesmo id gravado: nada foi cancelado.</summary>
        public const string LogCaucaoRetiradaJaGravada = "A caução já estava gravada por outra abertura e não foi cancelada: reserva {0}, identificador do provedor {1}.";

        /// <summary>Campos: reserva, referencia, identificador do provedor. A releitura antes de cancelar falhou: nada foi cancelado.</summary>
        public const string LogCaucaoRetiradaConferenciaIndisponivel = "Não foi possível conferir se a caução foi gravada; nada foi cancelado: reserva {0}, referência {1}, identificador do provedor {2}. Confira no banco e no provedor.";

        /// <summary>Campos: reserva, referencia, identificador do provedor. A releitura depois de cancelar achou o id gravado ou falhou.</summary>
        public const string LogCaucaoRetiradaConferirDepoisDoCancelamento = "Depois de cancelar a caução no provedor, a releitura achou o identificador gravado numa locação ou não pôde ser feita: reserva {0}, referência {1}, identificador do provedor {2}. Confira a locação e trate manualmente.";

        /// <summary>Campo: reserva. A configuracao do cartao nao pode ser lida no GET da retirada: a caucao sai indisponivel e a leitura continua 200.</summary>
        public const string LogCaucaoRetiradaConfiguracaoIndisponivel = "A configuração da caução não pôde ser lida na retirada: reserva {0}. A tela mostra a caução como indisponível.";
    }
}
