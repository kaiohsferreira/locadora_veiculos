using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    /// <summary>Corpo do 201 do POST de reserva: dados da reserva criada, com os campos Str para exibicao.</summary>
    public class ReturnReservaCriadaVO
    {
        public int IdReserva { get; set; }

        /// <summary>Codigo de exibicao (AV- com cinco digitos no minimo). So para exibicao.</summary>
        public string Codigo { get; set; } = string.Empty;

        public int IdVeiculo { get; set; }
        public DateOnly DataRetirada { get; set; }
        public string DataRetiradaStr { get; set; } = string.Empty;
        public DateOnly DataDevolucao { get; set; }
        public string DataDevolucaoStr { get; set; } = string.Empty;

        /// <summary>Diarias: dataDevolucao menos dataRetirada, por DayNumber.</summary>
        public int Diarias { get; set; }

        public StatusReserva Status { get; set; }
        public decimal? ValorEstimado { get; set; }
        public string ValorEstimadoStr { get; set; } = string.Empty;
    }
}
