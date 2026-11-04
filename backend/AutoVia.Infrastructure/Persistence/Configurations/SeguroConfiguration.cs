using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class SeguroConfiguration : IEntityTypeConfiguration<Seguro>
    {
        public void Configure(EntityTypeBuilder<Seguro> builder)
        {
            builder.ToTable("seguro", table =>
            {
                table.HasCheckConstraint("ck_seguro_vigencia", "data_fim > data_inicio");
            });

            builder.HasKey(x => x.IdSeguro).HasName("seguro_pkey");
            builder.Property(x => x.IdSeguro).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.Seguradora).IsRequired(true).HasMaxLength(100);
            builder.Property(x => x.NumeroApolice).IsRequired(true).HasMaxLength(30);
            builder.Property(x => x.DataInicio).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.DataFim).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.ValorPremio).IsRequired(true).HasPrecision(10, 2);
            builder.Property(x => x.Cobertura).IsRequired(false).HasMaxLength(200);

            builder.HasIndex(x => x.NumeroApolice).HasDatabaseName("idx_seguro_numero_apolice");

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Seguros)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_seguro_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
