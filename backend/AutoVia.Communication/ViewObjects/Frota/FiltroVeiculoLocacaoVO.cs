using System;

namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>Par opcional de datas ISO da ficha pública de locação.</summary>
    public class FiltroVeiculoLocacaoVO
    {
        public DateOnly? DataRetirada { get; set; }
        public DateOnly? DataDevolucao { get; set; }
    }
}
