using System;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    /// <summary>Dados de reserva expostos ao cliente autenticado.</summary>
    public class ReturnReservaVO
    {
        public int IdReserva { get; set; }
        public int IdVeiculo { get; set; }
        public DateOnly DataRetirada { get; set; }
        public DateOnly DataDevolucao { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? ValorEstimado { get; set; }
    }
}
