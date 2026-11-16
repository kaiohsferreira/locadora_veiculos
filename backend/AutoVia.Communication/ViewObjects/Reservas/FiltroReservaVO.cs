using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    /// <summary>Filtros para consultar as reservas do cliente autenticado: paginacao, status OU situacao, e busca.</summary>
    public class FiltroReservaVO : FiltroPaginacaoVO
    {
        /// <summary>Nome exato de um membro de StatusReserva. Exclusivo com situacao.</summary>
        public string? Status { get; set; }

        /// <summary>ConstantsValues.SituacaoReservaAtivas ou ConstantsValues.SituacaoReservaHistorico. Exclusivo com status.</summary>
        public string? Situacao { get; set; }

        /// <summary>Codigo (AV-00042 ou 42) ou modelo do veiculo, com ate ConstantsValues.TamanhoMaximoBuscaReserva caracteres.</summary>
        public string? Busca { get; set; }
    }
}
