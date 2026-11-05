using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// Filtro da listagem de clientes (GET /api/clientes): paginacao herdada de
    /// FiltroPaginacaoVO e status de cadastro opcional. Sem status, lista todos.
    /// Validado por ClienteFiltroVOValidator.
    /// </summary>
    public class ClienteFiltroVO : FiltroPaginacaoVO
    {
        /// <summary>Status de cadastro para filtrar (PENDENTE, APROVADO ou RECUSADO). Omitido = todos.</summary>
        public StatusCadastroCliente? Status { get; set; }
    }
}
