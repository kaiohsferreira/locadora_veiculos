namespace AutoVia.Communication.ViewObjects.CobrancasExtras;

public sealed class ApurarCobrancasExtrasVO
{
    public int? IdConfiguracao { get; set; }
    public int? VersaoEsperada { get; set; }
}

public sealed class ReturnApuracaoCobrancasExtrasVO
{
    public int IdApuracao { get; set; }
    public int? IdPagamentoLancado { get; set; }
    public int IdLocacao { get; set; }
    public int IdConfiguracao { get; set; }
    public int VersaoConfiguracao { get; set; }
    public string DataApuracaoStr { get; set; } = string.Empty;
    public bool Simulacao { get; set; }
    public ReturnConfiguracaoCobrancaExtraVO Configuracao { get; set; } = new ReturnConfiguracaoCobrancaExtraVO();
    public ReturnSimulacaoCobrancasExtrasVO Calculo { get; set; } = new ReturnSimulacaoCobrancasExtrasVO();
    public List<ReturnCobrancaApuradaVO> Cobrancas { get; set; } = new List<ReturnCobrancaApuradaVO>();
    public decimal Total { get; set; }
}

public sealed class ReturnCobrancaApuradaVO
{
    public int IdCobranca { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string DataLancamentoStr { get; set; } = string.Empty;
}

public sealed class ResultadoRegistroApuracaoVO
{
    public bool Criada { get; set; }
    public ReturnApuracaoCobrancasExtrasVO Apuracao { get; set; } = new ReturnApuracaoCobrancasExtrasVO();
}
