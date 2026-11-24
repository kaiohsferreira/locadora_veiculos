namespace AutoVia.Communication.ViewObjects.CobrancasExtras;

public sealed class ReturnSimulacaoCobrancasExtrasVO
{
    public int IdLocacao { get; set; }
    public int IdVistoriaEntrada { get; set; }
    public int IdVistoriaSaida { get; set; }
    public bool Simulacao { get; set; }
    public bool Provisorio { get; set; }
    public string Aviso { get; set; } = string.Empty;
    public MemoriaQuilometragemVO Quilometragem { get; set; } = new MemoriaQuilometragemVO();
    public MemoriaCombustivelVO Combustivel { get; set; } = new MemoriaCombustivelVO();
    public MemoriaAtrasoVO Atraso { get; set; } = new MemoriaAtrasoVO();
    public MemoriaAvariasVO Avarias { get; set; } = new MemoriaAvariasVO();
    public decimal Total { get; set; }
    public ReturnConfiguracaoCobrancaExtraVO Configuracao { get; set; } = new ReturnConfiguracaoCobrancaExtraVO();
    public int DiariasPrevistas { get; set; }
}

public sealed class MemoriaQuilometragemVO
{
    public int KmEntrada { get; set; }
    public int KmSaida { get; set; }
    public long PacoteKmTotal { get; set; }
    public long KmExcedentes { get; set; }
    public decimal ValorKmExcedente { get; set; }
    public decimal Valor { get; set; }
}

public sealed class MemoriaCombustivelVO
{
    public decimal NivelEntrada { get; set; }
    public decimal NivelSaida { get; set; }
    public decimal FracaoFaltante { get; set; }
    public decimal ValorTanqueCompleto { get; set; }
    public decimal Valor { get; set; }
}

public sealed class MemoriaAtrasoVO
{
    public string DataPrevistaStr { get; set; } = string.Empty;
    public DateTime? DataPrevista { get; set; }
    public string DataSaidaStr { get; set; } = string.Empty;
    public int ToleranciaAtrasoMinutos { get; set; }
    public string UnidadeAtraso { get; set; } = string.Empty;
    public string FracionamentoAtraso { get; set; } = string.Empty;
    public decimal UnidadesAposTolerancia { get; set; }
    public decimal UnidadesCobradas { get; set; }
    public decimal ValorUnidadeAtraso { get; set; }
    public decimal Valor { get; set; }
}

public sealed class MemoriaAvariasVO
{
    public bool ValoresEstimados { get; set; }
    public List<ItemAvariaSimulacaoVO> Itens { get; set; } = new List<ItemAvariaSimulacaoVO>();
    public decimal Valor { get; set; }
}

public sealed class ItemAvariaSimulacaoVO
{
    public int IdAvaria { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorEstimado { get; set; }
}
