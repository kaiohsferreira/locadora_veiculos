using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Regras da cobranca (RF-31 e RF-36). LancarAsync e publico e aceita tambem o tipo CAUCAO quando chamado direto
    /// (o fluxo da caucao na retirada); a recusa da caucao pela rota de lancamento fica no validador da rota.
    /// </summary>
    public interface IPagamentoService
    {
        /// <summary>Lanca uma cobranca PENDENTE, por locacao ou por venda, com tipo, valor e vencimento. Lanca ValidationException (400), NotFoundException (404).</summary>
        Task<ReturnPagamentoVO> LancarAsync(LancarPagamentoVO entrada);

        /// <summary>Da baixa numa cobranca: vira PAGO com a data de hoje e a forma. Lanca ValidationException (400), NotFoundException (404) e BusinessException (409).</summary>
        Task<ReturnPagamentoVO> BaixarAsync(int idPagamento, BaixarPagamentoVO entrada);

        /// <summary>Lista as cobrancas da locadora do contexto, com status, intervalo de vencimento e paginacao.</summary>
        Task<PaginaVO<ReturnPagamentoVO>> ListarAsync(FiltroPagamentoVO filtro);

        /// <summary>Lista as cobrancas do cliente (so as dele), com aba, periodo, situacao calculada e dias para vencer.</summary>
        Task<PaginaVO<ReturnMeuPagamentoVO>> ListarMeusAsync(int idCliente, FiltroMeusPagamentosVO filtro);

        /// <summary>Resumo das cobrancas do cliente: totais do ano corrente, pendencias e contagens das abas.</summary>
        Task<ReturnResumoMeusPagamentosVO> ResumirMeusAsync(int idCliente, FiltroResumoMeusPagamentosVO filtro);
    }
}
