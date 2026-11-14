using System;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    public class CriarReservaVO
    {
        public int IdVeiculo { get; set; }
        public DateOnly DataRetirada { get; set; }
        public DateOnly DataDevolucao { get; set; }
    }
}
