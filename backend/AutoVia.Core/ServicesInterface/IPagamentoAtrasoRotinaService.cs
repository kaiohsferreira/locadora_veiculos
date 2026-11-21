using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Rotina do sistema (contrato pagamento-atraso v1, P10): marca como ATRASADO os pagamentos vencidos de todas as locadoras.
    /// Injetada so no servico hospedado da rotina; o controller nao a conhece.
    /// </summary>
    public interface IPagamentoAtrasoRotinaService
    {
        /// <summary>Marca como ATRASADO os pagamentos vencidos de todas as locadoras e devolve so o numero de linhas.</summary>
        Task<int> MarcarAtrasosTodasLocadorasAsync();
    }
}
