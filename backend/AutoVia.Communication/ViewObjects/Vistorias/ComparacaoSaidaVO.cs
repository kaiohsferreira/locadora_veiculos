using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;

namespace AutoVia.Communication.ViewObjects.Vistorias;

public class AvariaComparacaoVO
{
    public int IdAvaria { get; set; }
    public int IdVistoria { get; set; }
    public int? IdAvariaEntrada { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public GravidadeAvaria Gravidade { get; set; }
    public decimal? ValorEstimado { get; set; }
    public string ValorEstimadoStr { get; set; } = string.Empty;
}

public class ComparacaoSaidaVO
{
    public ReturnVistoriaVO? Entrada { get; set; }
    public ReturnVistoriaVO Saida { get; set; } = null!;
    public bool ComparacaoRealizada { get; set; }
    public List<AvariaComparacaoVO> AvariasPreexistentes { get; set; } = new List<AvariaComparacaoVO>();
    public List<AvariaComparacaoVO> AvariasNovas { get; set; } = new List<AvariaComparacaoVO>();
    public List<AvariaComparacaoVO> AvariasIndeterminadas { get; set; } = new List<AvariaComparacaoVO>();
}

public class ContextoDevolucaoVO
{
    public ReturnLocacaoVO Locacao { get; set; } = null!;
    public ReturnVistoriaVO? Entrada { get; set; }
    public List<AvariaComparacaoVO> AvariasEntrada { get; set; } = new List<AvariaComparacaoVO>();
    public ComparacaoSaidaVO? Comparacao { get; set; }
}
