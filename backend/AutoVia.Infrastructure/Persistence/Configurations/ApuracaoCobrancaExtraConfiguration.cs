using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations;

public class ApuracaoCobrancaExtraConfiguration : IEntityTypeConfiguration<ApuracaoCobrancaExtra>
{
    public void Configure(EntityTypeBuilder<ApuracaoCobrancaExtra> builder)
    {
        builder.ToTable("apuracao_cobranca_extra", table =>
        {
            table.HasCheckConstraint("ck_apuracao_extra_total", "total >= 0");
            table.HasCheckConstraint("ck_apuracao_extra_versoes", "versao_configuracao > 0 AND versao_snapshot > 0");
        });
        builder.HasKey(x => x.IdApuracao).HasName("apuracao_cobranca_extra_pkey");
        builder.Property(x => x.IdApuracao).ValueGeneratedOnAdd();
        builder.Property(x => x.IdLocacao).IsRequired();
        builder.Property(x => x.IdConfiguracao).IsRequired();
        builder.Property(x => x.VersaoConfiguracao).IsRequired();
        builder.Property(x => x.IdVistoriaEntrada).IsRequired();
        builder.Property(x => x.IdVistoriaSaida).IsRequired();
        builder.Property(x => x.IdFuncionario).IsRequired();
        builder.Property(x => x.DataHora).IsRequired().HasColumnType("timestamp without time zone");
        builder.Property(x => x.VersaoSnapshot).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.Total).IsRequired().HasPrecision(12, 2);
        builder.HasIndex(x => x.IdLocacao).HasDatabaseName("idx_apuracao_extra_locacao");
        builder.HasOne(x => x.Locacao).WithMany().HasForeignKey(x => x.IdLocacao).HasConstraintName("fk_apuracao_extra_locacao").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Configuracao).WithMany().HasForeignKey(x => x.IdConfiguracao).HasConstraintName("fk_apuracao_extra_configuracao").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.VistoriaEntrada).WithMany().HasForeignKey(x => x.IdVistoriaEntrada).HasConstraintName("fk_apuracao_extra_entrada").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.VistoriaSaida).WithMany().HasForeignKey(x => x.IdVistoriaSaida).HasConstraintName("fk_apuracao_extra_saida").OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Funcionario).WithMany().HasForeignKey(x => x.IdFuncionario).HasConstraintName("fk_apuracao_extra_funcionario").OnDelete(DeleteBehavior.NoAction);
        builder.ConfigureBaseEntities();
    }
}
