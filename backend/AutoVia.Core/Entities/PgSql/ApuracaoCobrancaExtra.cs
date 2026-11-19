using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql;

/// <summary>Apuração imutável com memória versionada e vínculo dos lançamentos.</summary>
public class ApuracaoCobrancaExtra : BaseEntities
{
    [Column("id_apuracao")] public int IdApuracao { get; set; }
    [Column("id_locacao")] public int IdLocacao { get; set; }
    [Column("id_configuracao")] public int IdConfiguracao { get; set; }
    [Column("versao_configuracao")] public int VersaoConfiguracao { get; set; }
    [Column("id_vistoria_entrada")] public int IdVistoriaEntrada { get; set; }
    [Column("id_vistoria_saida")] public int IdVistoriaSaida { get; set; }
    [Column("id_funcionario")] public int IdFuncionario { get; set; }
    [Column("data_hora")] public DateTime DataHora { get; set; }
    [Column("versao_snapshot")] public int VersaoSnapshot { get; set; }
    [Column("snapshot_json")] public string SnapshotJson { get; set; } = string.Empty;
    [Column("total")] public decimal Total { get; set; }
    public virtual Locacao Locacao { get; set; } = null!;
    public virtual ConfiguracaoCobrancaExtra Configuracao { get; set; } = null!;
    public virtual Vistoria VistoriaEntrada { get; set; } = null!;
    public virtual Vistoria VistoriaSaida { get; set; } = null!;
    public virtual Funcionario Funcionario { get; set; } = null!;
    public virtual List<CobrancaExtra> Cobrancas { get; set; } = new List<CobrancaExtra>();
}
