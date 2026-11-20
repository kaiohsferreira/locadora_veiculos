using AutoVia.Communication.ViewObjects.Vistorias;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public interface IVistoriaService
    {
        Task<ReturnVistoriaVO> RegistrarEntradaAsync(int idLocacao, VistoriaEntradaVO entrada, int idFuncionario);
        Task<ReturnVistoriaVO> GetEntradaAsync(int idLocacao);
    }
}
