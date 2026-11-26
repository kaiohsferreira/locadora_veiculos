using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Lista das devolucoes pendentes da locadora do funcionario (locacoes em andamento e atrasadas).</summary>
    public interface IDevolucoesPendentesService
    {
        /// <summary>Pagina das devolucoes pendentes, com a flag de atraso e a devolucao prevista efetiva formatada.</summary>
        Task<PaginaVO<ReturnDevolucaoPendenteVO>> ListarAsync(FiltroDevolucaoPendenteVO filtro);
    }
}
