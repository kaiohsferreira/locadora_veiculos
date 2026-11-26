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

    /// <summary>Lado do carro: copiado da avaria de entrada; nulo nas novas da saida (N8).</summary>
    public AnguloAvaria? Angulo { get; set; }

    /// <summary>Posicao X de 0 a 100, copiada da avaria de entrada; nula nas novas da saida.</summary>
    public decimal? PosX { get; set; }

    /// <summary>Posicao Y de 0 a 100, copiada da avaria de entrada; nula nas novas da saida.</summary>
    public decimal? PosY { get; set; }
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

    /// <summary>Diarias pela retirada real (N3), igual a apuracao.</summary>
    public int DiariasPrevistas { get; set; }

    public decimal ValorDiaria { get; set; }

    public string ValorDiariaStr { get; set; } = string.Empty;

    /// <summary>Diarias previstas x valor da diaria, 2 casas.</summary>
    public decimal TotalDiarias { get; set; }

    public string TotalDiariasStr { get; set; } = string.Empty;

    /// <summary>Total das extras (T). Nulo, nunca 409, sem vistoria de saida ou sem politica da categoria (O-5).</summary>
    public decimal? TotalExtras { get; set; }

    public string? TotalExtrasStr { get; set; }

    /// <summary>Total previsto da locacao, que a caixa de confirmacao reenvia como totalConfirmado. Nulo quando TotalExtras e nulo.</summary>
    public decimal? ValorTotalPrevisto { get; set; }

    public string? ValorTotalPrevistoStr { get; set; }

    /// <summary>Politica de cobranca da categoria da locacao. Nulo sem politica ativa.</summary>
    public int? IdConfiguracao { get; set; }

    /// <summary>Versao da politica. Nulo sem politica ativa.</summary>
    public int? VersaoEsperada { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    /// <summary>Situacao e acao da caucao, lidas do banco (sem provedor e sem id_transacao).</summary>
    public ReturnCaucaoContextoVO Caucao { get; set; } = new ReturnCaucaoContextoVO();
}

/// <summary>Caucao no contexto da devolucao.</summary>
public class ReturnCaucaoContextoVO
{
    /// <summary>SEM_CAUCAO ou o status gravado do pagamento CAUCAO (PAGO, CANCELADO, PENDENTE).</summary>
    public string Situacao { get; set; } = string.Empty;

    /// <summary>Texto de AcaoCaucaoDevolucao so depois do encerramento; nulo antes.</summary>
    public string? Acao { get; set; }
}
