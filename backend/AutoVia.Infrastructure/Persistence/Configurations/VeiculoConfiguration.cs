using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
    {
        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {
            builder.ToTable("veiculo", table =>
            {
                table.HasCheckConstraint(
                    "ck_veiculo_status",
                    "status IN ('DISPONIVEL', 'RESERVADO', 'LOCADO', 'MANUTENCAO', 'VENDIDO', 'BAIXADO')");
                table.HasCheckConstraint("ck_veiculo_km", "km_atual >= 0");
                table.HasCheckConstraint("ck_veiculo_diaria", "valor_diaria > 0");
                table.HasCheckConstraint("ck_veiculo_ano", "ano_modelo >= ano_fabricacao");
            });

            builder.HasKey(x => x.IdVeiculo).HasName("veiculo_pkey");
            builder.Property(x => x.IdVeiculo).ValueGeneratedOnAdd();

            builder.Property(x => x.IdCategoria).IsRequired(true);
            builder.Property(x => x.Placa).IsRequired(true).HasMaxLength(7).IsFixedLength();
            builder.Property(x => x.Chassi).IsRequired(true).HasMaxLength(17);
            builder.Property(x => x.Renavam).IsRequired(true).HasMaxLength(11);
            builder.Property(x => x.Marca).IsRequired(true).HasMaxLength(40);
            builder.Property(x => x.Modelo).IsRequired(true).HasMaxLength(40);
            builder.Property(x => x.AnoFabricacao).IsRequired(true);
            builder.Property(x => x.AnoModelo).IsRequired(true);
            builder.Property(x => x.Cor).IsRequired(false).HasMaxLength(30);
            builder.Property(x => x.KmAtual).IsRequired(true).HasDefaultValue(0);
            builder.Property(x => x.ValorDiaria).IsRequired(true).HasPrecision(10, 2);
            builder.Property(x => x.ValorVenda).IsRequired(false).HasPrecision(12, 2);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusVeiculo.DISPONIVEL);

            builder.Property(x => x.DataAquisicao).IsRequired(false).HasColumnType("date");

            builder.HasIndex(x => x.Placa).HasDatabaseName("idx_veiculo_placa");
            builder.HasIndex(x => x.Chassi).HasDatabaseName("idx_veiculo_chassi");
            builder.HasIndex(x => x.Renavam).HasDatabaseName("idx_veiculo_renavam");
            builder.HasIndex(x => x.Status).HasDatabaseName("idx_veiculo_status");
            builder.HasIndex(x => x.IdCategoria).HasDatabaseName("idx_veiculo_categoria");

            builder.HasOne(x => x.Categoria)
                .WithMany(x => x.Veiculos)
                .HasForeignKey(x => x.IdCategoria)
                .HasConstraintName("fk_veiculo_categoria")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
