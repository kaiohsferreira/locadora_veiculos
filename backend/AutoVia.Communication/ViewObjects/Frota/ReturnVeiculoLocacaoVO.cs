using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>Ficha pública de um veículo elegível para locação.</summary>
    public class ReturnVeiculoLocacaoVO
    {
        public int IdVeiculo { get; set; }
        public int IdCategoria { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoFabricacao { get; set; }
        public int AnoModelo { get; set; }
        public string? Cor { get; set; }
        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;
        public string LocadoraNomeFantasia { get; set; } = string.Empty;
        public string LocadoraCidade { get; set; } = string.Empty;
        public string LocadoraUf { get; set; } = string.Empty;
        public bool? DisponivelNoPeriodo { get; set; }
        public List<FotoCapaVeiculoVO> Fotos { get; set; } = new List<FotoCapaVeiculoVO>();
        public string? ErroFotos { get; set; }
    }
}
