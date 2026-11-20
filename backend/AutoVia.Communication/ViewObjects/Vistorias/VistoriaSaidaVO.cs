using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Vistorias;

public class VistoriaSaidaVO
{
    public int? KmRegistrado { get; set; }
    public decimal? NivelCombustivel { get; set; }
    public string? Observacoes { get; set; }
    public List<int>? IdsAvariasEntrada { get; set; }
    public List<NovaAvariaSaidaVO>? NovasAvarias { get; set; }
}

public class NovaAvariaSaidaVO
{
    public string Descricao { get; set; } = string.Empty;
    public GravidadeAvaria? Gravidade { get; set; }
    public decimal? ValorEstimado { get; set; }
}
