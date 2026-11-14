using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    public class ReturnReservaCriadaVO
    {
        public int IdReserva { get; set; }
        public int IdVeiculo { get; set; }
        public DateOnly DataRetirada { get; set; }
        public DateOnly DataDevolucao { get; set; }
        public StatusReserva Status { get; set; }
    }
}
