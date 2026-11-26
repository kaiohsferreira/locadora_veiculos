using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>Filtros da lista de locacoes do proprio cliente: paginacao e situacao (em andamento ou historico).</summary>
    public class FiltroMinhaLocacaoVO : FiltroPaginacaoVO
    {
        /// <summary>
        /// ConstantsValues.SituacaoMinhasLocacoesEmAndamento ou ConstantsValues.SituacaoMinhasLocacoesHistorico. Ausente ou
        /// vazio vale em andamento.
        /// </summary>
        public string? Situacao { get; set; }
    }
}
