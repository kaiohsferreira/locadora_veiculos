using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public partial interface IVeiculoService
    {
        Task<ReturnVeiculoLocacaoVO> GetParaLocacaoAsync(int idVeiculo, FiltroVeiculoLocacaoVO filtro, CancellationToken cancellationToken = default);
        Task<PaginaVO<ReturnVeiculoDisponivelVO>> GetDisponiveisAsync(FiltroVeiculoDisponibilidadeVO filtro);
    }
}
