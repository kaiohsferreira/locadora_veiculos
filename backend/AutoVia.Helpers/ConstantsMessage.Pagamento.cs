namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de pagamento (cobranca, RF-31 e RF-36): validacao do lancamento, da baixa e dos filtros,
    /// recusas de negocio e registros de log. Arquivo parcial da ConstantsMessage.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Pagamento: validacao do lancamento (LancarPagamentoVO) ---
        public const string ErroPagamentoOrigemObrigatoria = "Informe a locação ou a venda do pagamento.";
        public const string ErroPagamentoOrigemDupla = "O pagamento refere-se a uma locação ou a uma venda, nunca às duas.";
        public const string ErroPagamentoTipoObrigatorio = "Informe o tipo do pagamento.";
        public const string ErroPagamentoTipoInvalido = "Tipo de pagamento inválido. Use DIARIA, COBRANCA_EXTRA ou VENDA.";
        public const string ErroPagamentoCaucaoPorRota = "A caução é lançada na retirada do veículo, não por esta rota.";
        public const string ErroPagamentoTipoExigeLocacao = "Pagamento do tipo DIARIA ou COBRANCA_EXTRA exige uma locação.";
        public const string ErroPagamentoTipoExigeVenda = "Pagamento do tipo VENDA exige uma venda.";
        public const string ErroPagamentoValorInvalido = "O valor deve ser maior que zero.";
        public const string ErroPagamentoValorPrecisao = "O valor deve ter no máximo {0} dígitos inteiros e {1} casas decimais.";
        public const string ErroPagamentoVencimentoObrigatorio = "Informe a data de vencimento.";

        // TEXTO NOVO (nao estava no contrato; Kaio aprova antes do commit): caucao chamada direto com venda, sem locacao.
        public const string ErroPagamentoCaucaoExigeLocacao = "Pagamento do tipo CAUCAO exige uma locação.";

        // --- Pagamento: existencia e recusas do repositorio (404 e 409) ---
        // Locacao nao encontrada: reaproveita ConstantsMessage.ErroLocacaoNaoEncontrada (mesmo sentido; texto ja existente).
        // TEXTO NOVO (nao estava no contrato): venda inexistente, sem constante equivalente no projeto.
        public const string ErroPagamentoVendaNaoEncontrada = "Venda não encontrada.";
        public const string ErroPagamentoNaoEncontrado = "Pagamento não encontrado.";
        public const string ErroPagamentoJaPago = "Este pagamento já foi baixado.";
        public const string ErroPagamentoCancelado = "Pagamento cancelado não pode ser baixado.";
        public const string ErroPagamentoBaixaCaucao = "A caução é baixada ou retida na devolução do veículo, não por esta rota.";

        // --- Pagamento: baixa (BaixarPagamentoVO) ---
        public const string ErroPagamentoFormaObrigatoria = "Informe a forma de pagamento.";
        public const string ErroPagamentoFormaInvalida = "Forma de pagamento inválida. Use PIX, CARTAO, BOLETO, DINHEIRO ou TRANSFERENCIA.";

        // --- Pagamento: filtros das listagens ---
        public const string ErroPagamentoStatusInvalido = "Status inválido. Use PENDENTE, PAGO, ATRASADO ou CANCELADO.";
        public const string ErroPagamentoVencimentoIntervalo = "A data inicial do vencimento não pode ser maior que a final.";
        public const string ErroPagamentoAbaInvalida = "Filtro inválido. Use TODOS, PENDENTES ou PAGOS.";
        public const string ErroPagamentoPeriodoInvalido = "Período inválido. Use DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO.";

        // --- Pagamento: falhas (log e mensagem generica) e linhas informativas do log ---
        // TEXTO NOVO (nao estava no contrato): mensagem de falha do lancamento, gravada no log.
        public const string ErroLancarPagamento = "Não foi possível lançar o pagamento.";

        // TEXTO NOVO (nao estava no contrato): mensagem de falha da baixa, gravada no log.
        public const string ErroBaixarPagamento = "Não foi possível dar baixa no pagamento.";

        // TEXTO NOVO (nao estava no contrato): mensagem de falha da listagem da locadora, gravada no log.
        public const string ErroListarPagamentos = "Não foi possível listar os pagamentos.";

        // TEXTO NOVO (nao estava no contrato): mensagem de falha da listagem do cliente, gravada no log.
        public const string ErroListarMeusPagamentos = "Não foi possível listar as cobranças do cliente.";

        // TEXTO NOVO (nao estava no contrato): mensagem de falha do resumo do cliente, gravada no log.
        public const string ErroResumirMeusPagamentos = "Não foi possível resumir as cobranças do cliente.";

        // TEXTO NOVO (nao estava no contrato): mensagem de falha de leitura no repositorio, gravada no log.
        public const string ErroConsultarPagamento = "Não foi possível consultar os pagamentos.";

        // TEXTO NOVO (nao estava no contrato): linha informativa do log por lancamento (id do pagamento e do funcionario).
        public const string InfoPagamentoLancado = "Pagamento {0} lançado pelo funcionário {1}.";

        // TEXTO NOVO (nao estava no contrato): linha informativa do log por baixa (id do pagamento e do funcionario).
        public const string InfoPagamentoBaixado = "Pagamento {0} baixado pelo funcionário {1}.";
    }
}
