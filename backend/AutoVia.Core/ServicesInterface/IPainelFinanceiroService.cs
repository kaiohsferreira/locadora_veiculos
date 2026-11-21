using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Painel financeiro da locadora (P7-BE-05, RF-35): indicadores e lista de titulos, sempre da locadora do contexto.</summary>
    public interface IPainelFinanceiroService
    {
        /// <summary>Indicadores de cobranca no periodo: recebido, a receber, em atraso e ticket medio.</summary>
        Task<ReturnPainelFinanceiroVO> ObterIndicadoresAsync(FiltroPainelFinanceiroVO filtro);

        /// <summary>Lista de titulos da aba pedida, com a pagina e os quatro contadores.</summary>
        Task<ReturnTitulosFinanceirosVO> ObterTitulosAsync(FiltroTitulosFinanceirosVO filtro);
    }
}
