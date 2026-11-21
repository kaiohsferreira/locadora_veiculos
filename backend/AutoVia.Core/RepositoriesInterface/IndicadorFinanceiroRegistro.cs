namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Soma e quantidade de um indicador do painel financeiro (calculadas no banco pelo repositorio).</summary>
    public class IndicadorFinanceiroRegistro
    {
        /// <summary>Soma dos valores dos pagamentos do indicador (zero quando nao ha nenhum).</summary>
        public decimal Valor { get; set; }

        /// <summary>Quantidade de pagamentos do indicador.</summary>
        public int Quantidade { get; set; }
    }
}
