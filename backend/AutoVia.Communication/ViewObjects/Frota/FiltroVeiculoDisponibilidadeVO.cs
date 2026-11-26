using AutoVia.Communication.ViewObjects.Utils;
using System;
using System.ComponentModel.DataAnnotations;

namespace AutoVia.Communication.ViewObjects.Frota
{
    public class FiltroVeiculoDisponibilidadeVO : FiltroPaginacaoVO
    {
        public DateOnly DataRetirada { get; set; }
        public DateOnly DataDevolucao { get; set; }
        public int? IdCategoria { get; set; }
        public decimal? ValorDiariaMin { get; set; }
        public decimal? ValorDiariaMax { get; set; }

        // Uma query enviada vazia é inválida. Só a ausência do campo deve chegar como null.
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string? Cidade { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string? Uf { get; set; }
    }
}
