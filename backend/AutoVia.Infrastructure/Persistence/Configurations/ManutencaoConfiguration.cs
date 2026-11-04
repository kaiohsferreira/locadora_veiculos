using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class ManutencaoConfiguration : IEntityTypeConfiguration<Manutencao>
    {
        public void Configure(EntityTypeBuilder<Manutencao> builder)
        {
            builder.ToTable("manutencao", table =>
            {
                table.HasCheckConstraint("ck_manutencao_tipo", "tipo IN ('PREVENTIVA', 'CORRETIVA')");
                table.HasCheckConstraint("ck_manutencao_periodo", "data_saida IS NULL OR data_saida >= data_entrada");
            });

            builder.HasKey(x => x.IdManutencao).HasName("manutencao_pkey");
            builder.Property(x => x.IdManutencao).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.Tipo).IsRequired(true).HasConversion<string>().HasMaxLength(15);
            builder.Property(x => x.DataEntrada).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.DataSaida).IsRequired(false).HasColumnType("date");
            builder.Property(x => x.KmRegistrado).IsRequired(true);
            builder.Property(x => x.Oficina).IsRequired(false).HasMaxLength(100);
            builder.Property(x => x.Descricao).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Custo).IsRequired(false).HasPrecision(10, 2);

            builder.HasIndex(x => new { x.IdVeiculo, x.DataEntrada }).HasDatabaseName("idx_manutencao_veiculo");

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Manutencoes)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_manutencao_veiculo")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
