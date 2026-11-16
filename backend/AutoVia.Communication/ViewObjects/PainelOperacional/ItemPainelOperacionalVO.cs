namespace AutoVia.Communication.ViewObjects.PainelOperacional
{
    /// <summary>Movimento do painel operacional associado a uma reserva ou locacao.</summary>
    public class ItemPainelOperacionalVO
    {
        public string Origem { get; set; } = string.Empty;
        public int? IdReserva { get; set; }
        public int? IdLocacao { get; set; }
        public string DataHora { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
    }
}
