using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>
    /// Filtro da lista de devolucoes pendentes (GET /api/locacoes, query string): status e paginacao. Validado por
    /// FiltroDevolucaoPendenteVOValidator. Status ausente ou vazio vale EM_ANDAMENTO (as em andamento, atrasadas inclusive).
    /// </summary>
    public class FiltroDevolucaoPendenteVO : FiltroPaginacaoVO
    {
        /// <summary>EM_ANDAMENTO (padrao) ou ATRASADA (so as que ja passaram da devolucao prevista efetiva).</summary>
        public string? Status { get; set; }
    }
}
