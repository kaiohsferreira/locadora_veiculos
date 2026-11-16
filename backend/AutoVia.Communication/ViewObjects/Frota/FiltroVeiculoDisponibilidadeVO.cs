using AutoVia.Communication.ViewObjects.Utils;
using System;

namespace AutoVia.Communication.ViewObjects.Frota
{
    public class FiltroVeiculoDisponibilidadeVO : FiltroPaginacaoVO
    {
        public DateOnly DataRetirada { get; set; }
        public DateOnly DataDevolucao { get; set; }
        public int? IdCategoria { get; set; }
        public decimal? ValorDiariaMin { get; set; }
        public decimal? ValorDiariaMax { get; set; }
    }
}
