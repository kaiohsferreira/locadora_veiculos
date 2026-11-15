using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    /// <summary>Filtros para consultar as reservas do cliente autenticado.</summary>
    public class FiltroReservaVO : FiltroPaginacaoVO
    {
        public string? Status { get; set; }
    }
}
