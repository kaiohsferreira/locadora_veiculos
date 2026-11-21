namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Resultado da rotina manual de atraso (contrato pagamento-atraso v1): so a quantidade de pagamentos que passaram a
    /// ATRASADO nesta execucao. Nenhum dado de cobranca sai aqui.
    /// </summary>
    public class ReturnResultadoAtrasosVO
    {
        /// <summary>Quantidade de pagamentos PENDENTE vencidos marcados como ATRASADO nesta execucao.</summary>
        public int QuantidadeAtualizada { get; set; }
    }
}
