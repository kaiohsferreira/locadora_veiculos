using AutoVia.Communication.ViewObjects.Manutencao;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do servico de manutencao (P8-BE-01, P8-BE-02 e P8-BE-03): abertura e encerramento da ordem e alertas de
    /// revisao. Lanca NotFoundException (404), BusinessException (409) e ValidationException (400); as regras ficam em
    /// ManutencaoService e no repositorio.
    /// </summary>
    public interface IManutencaoService
    {
        /// <summary>Abre uma ordem de manutencao para o veiculo; o veiculo passa a MANUTENCAO. Devolve a ordem criada.</summary>
        Task<ReturnManutencaoVO> AbrirAsync(AbrirManutencaoVO abrirManutencaoVO);

        /// <summary>Encerra a ordem informada, com a data de saida, o km do veiculo e o custo. Devolve a ordem encerrada.</summary>
        Task<ReturnManutencaoVO> EncerrarAsync(int idManutencao, EncerrarManutencaoVO encerrarManutencaoVO);

        /// <summary>Lista os veiculos em alerta de revisao (VENCIDA ou PROXIMA), com o total.</summary>
        Task<ReturnAlertasRevisaoVO> ListarAlertasRevisaoAsync();
    }
}
