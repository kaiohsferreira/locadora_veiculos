using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations;

public class ConfiguracaoCobrancaExtraConfiguration : IEntityTypeConfiguration<ConfiguracaoCobrancaExtra>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoCobrancaExtra> builder)
    {
        builder.ToTable("configuracao_cobranca_extra", table =>
        {
            table.HasCheckConstraint("ck_configuracao_extra_versao", "versao > 0");
            table.HasCheckConstraint("ck_configuracao_extra_franquia", "franquia_km >= 0 AND base_franquia IN ('TOTAL', 'POR_DIARIA')");
            table.HasCheckConstraint("ck_configuracao_extra_tarifas", "valor_km_excedente > 0 AND valor_tanque_completo > 0 AND valor_unidade_atraso > 0");
            table.HasCheckConstraint("ck_configuracao_extra_atraso", "tolerancia_atraso_minutos >= 0 AND unidade_atraso IN ('HORA', 'DIARIA') AND fracionamento_atraso IN ('PROPORCIONAL', 'INICIADA')");
            table.HasCheckConstraint("ck_configuracao_extra_avarias", "base_avarias = 'ESTIMATIVA_VISTORIA'");
        });
        builder.HasKey(x => x.IdConfiguracao).HasName("configuracao_cobranca_extra_pkey");
        builder.Property(x => x.IdConfiguracao).ValueGeneratedOnAdd();
        builder.Property(x => x.IdEmpresa).IsRequired();
        builder.Property(x => x.IdCategoria).IsRequired();
        builder.Property(x => x.Versao).IsRequired();
        builder.Property(x => x.BaseFranquia).IsRequired().HasMaxLength(20);
        builder.Property(x => x.FranquiaKm).IsRequired();
        builder.Property(x => x.ValorKmExcedente).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.ValorTanqueCompleto).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.ToleranciaAtrasoMinutos).IsRequired();
        builder.Property(x => x.UnidadeAtraso).IsRequired().HasMaxLength(10);
        builder.Property(x => x.FracionamentoAtraso).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ValorUnidadeAtraso).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.BaseAvarias).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => new { x.IdEmpresa, x.IdCategoria }).HasDatabaseName("idx_configuracao_extra_empresa_categoria");
        builder.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.IdEmpresa).HasConstraintName("fk_configuracao_extra_empresa").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.IdCategoria).HasConstraintName("fk_configuracao_extra_categoria").OnDelete(DeleteBehavior.NoAction);
        builder.ConfigureBaseEntities();
    }
}
