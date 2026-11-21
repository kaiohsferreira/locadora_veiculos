using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.PainelFinanceiro
{
    /// <summary>
    /// Lista de titulos do painel financeiro (GET /api/painel/financeiro/titulos): a pagina da aba pedida e os quatro
    /// contadores, cada um com o tamanho da aba correspondente (os contadores nao dependem da aba pedida).
    /// </summary>
    public class ReturnTitulosFinanceirosVO
    {
        /// <summary>Quantidade da aba TODOS.</summary>
        public int QuantidadeTodos { get; set; }

        /// <summary>Quantidade da aba EM_ATRASO.</summary>
        public int QuantidadeEmAtraso { get; set; }

        /// <summary>Quantidade da aba VENCE_HOJE.</summary>
        public int QuantidadeVenceHoje { get; set; }

        /// <summary>Quantidade da aba PAGOS_RECENTES.</summary>
        public int QuantidadePagosRecentes { get; set; }

        /// <summary>Pagina da aba pedida, com o total da aba e o tamanho da pagina.</summary>
        public PaginaVO<ReturnTituloFinanceiroVO> Pagina { get; set; } = new PaginaVO<ReturnTituloFinanceiroVO>();
    }
}
