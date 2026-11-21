using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Indicadores e lista de titulos do painel financeiro (P7-BE-05, RF-35). Todas as consultas rodam no banco com o filtro
    /// global "Empresa" do contexto e DisabledAt nulo; a caucao fica fora de tudo. Sem bypass de isolamento.
    /// </summary>
    public interface IPainelFinanceiroRepository
    {
        /// <summary>Recebido: soma e quantidade dos PAGO com data de pagamento em [de, ate].</summary>
        Task<IndicadorFinanceiroRegistro> ObterIndicadorRecebidoAsync(DateOnly de, DateOnly ate);

        /// <summary>A receber: soma e quantidade dos PENDENTE com vencimento em [de, ate] e ainda nao vencidos.</summary>
        Task<IndicadorFinanceiroRegistro> ObterIndicadorAReceberAsync(DateOnly de, DateOnly ate, DateOnly hoje);

        /// <summary>Em atraso: soma e quantidade dos ATRASADO ou PENDENTE vencido, com vencimento em [de, ate].</summary>
        Task<IndicadorFinanceiroRegistro> ObterIndicadorEmAtrasoAsync(DateOnly de, DateOnly ate, DateOnly hoje);

        /// <summary>Base do ticket medio: soma e quantidade dos PAGO com vencimento em [de, ate].</summary>
        Task<IndicadorFinanceiroRegistro> ObterIndicadorTicketMedioAsync(DateOnly de, DateOnly ate);

        /// <summary>Pagina de uma aba da lista de titulos, com o total da aba (ordem e projecao de cada aba no repositorio).</summary>
        Task<(List<TituloFinanceiroRegistro> Itens, int Total)> ListarTitulosAsync(string aba, DateOnly de, DateOnly ate, DateOnly hoje, int deslocamento, int tamanho);

        /// <summary>Tamanho das quatro abas da lista de titulos.</summary>
        Task<ContadoresTitulosFinanceirosRegistro> ContarTitulosAsync(DateOnly de, DateOnly ate, DateOnly hoje);
    }
}
