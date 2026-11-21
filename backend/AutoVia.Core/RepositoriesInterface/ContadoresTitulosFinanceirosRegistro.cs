namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Tamanho de cada aba da lista de titulos do painel financeiro (um COUNT por aba no banco).</summary>
    public class ContadoresTitulosFinanceirosRegistro
    {
        public int QuantidadeTodos { get; set; }

        public int QuantidadeEmAtraso { get; set; }

        public int QuantidadeVenceHoje { get; set; }

        public int QuantidadePagosRecentes { get; set; }
    }
}
