namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Totais e contagens do resumo das cobrancas do cliente, lidos numa consulta do repositorio (IPagamentoRepository).
    /// Nao e entidade nem VO: e o retorno interno. Os periodos e os predicados chegam prontos do servico.
    /// </summary>
    public sealed record ResumoPagamentosClienteRegistro(int QuantidadePendentes, decimal TotalPendente, int QuantidadeQuitadasAno, decimal TotalLiquidadoAno, int QuantidadePagosNoPeriodo);
}
