using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql;

/// <summary>Parâmetros comerciais explícitos da locadora por categoria.</summary>
public class ConfiguracaoCobrancaExtra : BaseEntities
{
    [Column("id_configuracao")] public int IdConfiguracao { get; set; }
    [Column("id_empresa")] public int IdEmpresa { get; set; }
    [Column("id_categoria")] public int IdCategoria { get; set; }
    [Column("versao")] public int Versao { get; set; }
    [Column("base_franquia")] public string BaseFranquia { get; set; } = string.Empty;
    [Column("franquia_km")] public int FranquiaKm { get; set; }
    [Column("valor_km_excedente")] public decimal ValorKmExcedente { get; set; }
    [Column("valor_tanque_completo")] public decimal ValorTanqueCompleto { get; set; }
    [Column("tolerancia_atraso_minutos")] public int ToleranciaAtrasoMinutos { get; set; }
    [Column("unidade_atraso")] public string UnidadeAtraso { get; set; } = string.Empty;
    [Column("fracionamento_atraso")] public string FracionamentoAtraso { get; set; } = string.Empty;
    [Column("valor_unidade_atraso")] public decimal ValorUnidadeAtraso { get; set; }
    [Column("base_avarias")] public string BaseAvarias { get; set; } = string.Empty;
    public virtual Empresa Empresa { get; set; } = null!;
    public virtual Categoria Categoria { get; set; } = null!;
}
