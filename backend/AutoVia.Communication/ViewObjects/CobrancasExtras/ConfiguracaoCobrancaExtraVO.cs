using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.CobrancasExtras;

public class ConfiguracaoCobrancaExtraVO
{
    public int? IdCategoria { get; set; }
    public string? BaseFranquia { get; set; }
    public int? FranquiaKm { get; set; }
    public decimal? ValorKmExcedente { get; set; }
    public decimal? ValorTanqueCompleto { get; set; }
    public int? ToleranciaAtrasoMinutos { get; set; }
    public string? UnidadeAtraso { get; set; }
    public string? FracionamentoAtraso { get; set; }
    public decimal? ValorUnidadeAtraso { get; set; }
    public string? BaseAvarias { get; set; }
    public int? VersaoEsperada { get; set; }
}

public sealed class ReturnConfiguracaoCobrancaExtraVO
{
    public int IdConfiguracao { get; set; }
    public int IdCategoria { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public int Versao { get; set; }
    public string BaseFranquia { get; set; } = string.Empty;
    public int FranquiaKm { get; set; }
    public decimal ValorKmExcedente { get; set; }
    public decimal ValorTanqueCompleto { get; set; }
    public int ToleranciaAtrasoMinutos { get; set; }
    public string UnidadeAtraso { get; set; } = string.Empty;
    public string FracionamentoAtraso { get; set; } = string.Empty;
    public decimal ValorUnidadeAtraso { get; set; }
    public string BaseAvarias { get; set; } = string.Empty;
    public string UpdatedAtStr { get; set; } = string.Empty;
}

public sealed class FiltroConfiguracaoCobrancaExtraVO : FiltroPaginacaoVO
{
    public int? IdCategoria { get; set; }
}
