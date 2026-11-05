using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato da listagem de veiculos (P1-BE-03). Arquivo parcial do IVeiculoService.
    /// </summary>
    public partial interface IVeiculoService
    {
        /// <summary>
        /// Lista os veiculos ativos, paginados e ordenados por placa, com filtros opcionais por categoria, status e placa.
        /// </summary>
        Task<PaginaVO<ReturnVeiculoVO>> GetAllAsync(FiltroVeiculoVO filtro);
    }
}
