namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do encerramento da locacao e da caucao na devolucao (P5-BE-04, RF-25 e RP-12; devolucao-encerramento v4 e
    /// caucao-devolucao v4). Arquivo parcial de ConstantsMessage. Textos ao usuario aprovados pelo Kaio em 26/11/2026.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Erros do encerramento (ao usuario) ---
        public const string ErroEncerramentoParametros = "Informe a configuração, a versão e o total confirmado da cobrança para encerrar a locação.";
        public const string ErroDevolucaoSemVistoriaSaida = "Registre a vistoria de saída antes de encerrar a locação (RP-07).";
        public const string ErroDevolucaoLocacaoCancelada = "Esta locação foi cancelada e não pode ser encerrada (RP-07).";
        public const string ErroDevolucaoTotalDivergente = "O total mudou desde a confirmação (RP-07). Recarregue a tela, confira o novo total e tente outra vez.";
        public const string ErroDevolucaoVeiculoEstadoInvalido = "O veículo não está locado, então a locação não pode ser encerrada agora (RP-07).";
        public const string ErroDevolucaoProvedorIndisponivel = "Não foi possível consultar a caução no provedor agora (RP-12). Nada foi encerrado; tente outra vez em instantes.";
        public const string ErroEncerrarLocacao = "Não foi possível encerrar a locação.";
        public const string ErroListarCobrancasLocacao = "Não foi possível consultar as cobranças da locação.";

        // --- Avisos depois de encerrar (com a caucao) ---
        public const string AvisoDevolucaoCaucaoNaoEncontrada = "A caução não foi encontrada no provedor. A locação foi encerrada; trate a caução à mão (RP-12).";
        public const string AvisoDevolucaoCaucaoEmAnalise = "A caução ainda está em análise no provedor. A locação foi encerrada; trate a caução à mão (RP-12).";
        public const string AvisoDevolucaoCaucaoDivergente = "A caução não confere com a cobrança no provedor. A locação foi encerrada; trate a caução à mão (RP-12).";
        public const string AvisoDevolucaoExtrasAcimaDaCaucao = "A caução não cobre as cobranças extras. A caução foi liberada e as extras ficaram para pagamento por PIX.";
        public const string AvisoDevolucaoCaucaoSemGarantia = "A caução não estava mais garantida no provedor. Ela foi baixada e as cobranças extras ficaram para pagamento por PIX.";

        // --- Logs (so texto de log, com ids internos; sem id_transacao, sem token, sem dado pessoal) ---
        public const string LogDevolucaoEncerrada = "Locação {0} encerrada. Veículo {1} com status {2}. Caução {3}. Pagamento diária {4}.";
        public const string LogDevolucaoCaucaoDecidida = "Caução da locação {0} (referência {1}, pagamento {2}): estado {3}, ação {4}, motivo {5}.";
        public const string LogDevolucaoCaucaoReconsulta = "Reconsulta da caução da locação {0} depois de falha na ação no provedor (pagamento {1}).";
        public const string LogDevolucaoCaucaoNaoEncontrada = "Caução da locação {0} não encontrada no provedor (pagamento {1}); o encerramento segue pela tabela C12.";
        public const string LogDevolucaoRetentativa = "Caução do pagamento {0} já estava com status {1} na transação final; não regravada (locação {2}).";
        public const string LogDevolucaoTotalDivergente = "Total confirmado difere do apurado na locação {0}; apuração gravada com total {1}.";
        public const string LogDevolucaoAlvoRetencaoInconsistente = "Alvo da retenção da caução inconsistente na locação {0}: pagamento {1}.";
        public const string LogDevolucaoSemApuracao = "Locação {0} sem apuração de cobranças extras: totais zerados na leitura da devolução.";
        public const string LogDevolucaoTotaisSemCalculo = "Locação {0} sem cálculo de cobranças extras (sem vistoria de saída ou sem política ativa): totais nulos na leitura da devolução.";
    }
}
