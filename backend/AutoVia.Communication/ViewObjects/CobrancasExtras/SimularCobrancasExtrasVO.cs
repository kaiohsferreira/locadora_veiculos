namespace AutoVia.Communication.ViewObjects.CobrancasExtras;

public sealed class SimularCobrancasExtrasVO
{
    public long? PacoteKmTotal { get; set; }
    public decimal? ValorKmExcedente { get; set; }
    public decimal? ValorTanqueCompleto { get; set; }
    public int? ToleranciaAtrasoMinutos { get; set; }
    public string? UnidadeAtraso { get; set; }
    public string? FracionamentoAtraso { get; set; }
    public decimal? ValorUnidadeAtraso { get; set; }
}
