using AutoVia.Communication.ViewObjects.Pagamento;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Marcacao de atrasos pela rota manual do financeiro (contrato pagamento-atraso v1): so a locadora do contexto. A rotina
    /// do sistema, que varre todas as locadoras, tem interface propria e nao e injetada no controller.
    /// </summary>
    public interface IPagamentoAtrasoService
    {
        /// <summary>Marca como ATRASADO os pagamentos PENDENTE vencidos da locadora do contexto e devolve so a quantidade.</summary>
        Task<ReturnResultadoAtrasosVO> MarcarAtrasosDaLocadoraAsync();
    }
}
