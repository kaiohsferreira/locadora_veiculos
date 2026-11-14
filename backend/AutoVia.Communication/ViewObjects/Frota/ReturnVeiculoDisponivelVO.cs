namespace AutoVia.Communication.ViewObjects.Frota
{
    public class ReturnVeiculoDisponivelVO
    {
        public int IdVeiculo { get; set; }
        public int IdCategoria { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoModelo { get; set; }
        public string? Cor { get; set; }
        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;
    }
}
