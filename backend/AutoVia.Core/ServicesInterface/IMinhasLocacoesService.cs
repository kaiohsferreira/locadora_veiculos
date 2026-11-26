using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Leitura paginada das locacoes do proprio cliente (tela 27, RF-46).</summary>
    public interface IMinhasLocacoesService
    {
        /// <summary>Lista as locacoes do cliente pedido, na situacao pedida, com a capa do veiculo e a pagina pedida.</summary>
        Task<PaginaVO<ReturnMinhaLocacaoVO>> ListarAsync(int idCliente, FiltroMinhaLocacaoVO filtro);
    }
}
