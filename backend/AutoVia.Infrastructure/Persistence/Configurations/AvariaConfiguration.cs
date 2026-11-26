using AutoVia.Core.Entities.PgSql;
using AutoVia.Communication.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class AvariaConfiguration : IEntityTypeConfiguration<Avaria>
    {
        public void Configure(EntityTypeBuilder<Avaria> builder)
        {
            builder.ToTable("avaria", table =>
            {
                table.HasCheckConstraint("ck_avaria_gravidade", "gravidade IN ('LEVE', 'MEDIA', 'GRAVE')");
                table.HasCheckConstraint("ck_avaria_angulo", "angulo IS NULL OR angulo IN ('FRENTE', 'TRASEIRA', 'LATERAL_ESQUERDA', 'LATERAL_DIREITA')");
                table.HasCheckConstraint("ck_avaria_pos_x", "pos_x IS NULL OR (pos_x >= 0 AND pos_x <= 100)");
                table.HasCheckConstraint("ck_avaria_pos_y", "pos_y IS NULL OR (pos_y >= 0 AND pos_y <= 100)");
            });

            builder.HasKey(x => x.IdAvaria).HasName("avaria_pkey");
            builder.Property(x => x.IdAvaria).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVistoria).IsRequired(true);
            builder.Property(x => x.Descricao).IsRequired(true).HasMaxLength(200);

            builder.Property(x => x.Gravidade)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(15)
                .HasDefaultValue(GravidadeAvaria.LEVE);

            builder.Property(x => x.ValorEstimado).IsRequired(false).HasPrecision(10, 2);

            builder.Property(x => x.Angulo)
                .IsRequired(false)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.PosX).IsRequired(false).HasPrecision(5, 2);
            builder.Property(x => x.PosY).IsRequired(false).HasPrecision(5, 2);

            // ON DELETE CASCADE da especificacao: a avaria pertence a vistoria.
            builder.HasOne(x => x.Vistoria)
                .WithMany(x => x.Avarias)
                .HasForeignKey(x => x.IdVistoria)
                .HasConstraintName("fk_avaria_vistoria")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.IdAvariaEntrada).IsRequired(false);
            builder.HasIndex(x => x.IdAvariaEntrada).HasDatabaseName("idx_avaria_entrada");
            builder.HasOne(x => x.AvariaEntrada).WithMany().HasForeignKey(x => x.IdAvariaEntrada)
                .HasConstraintName("fk_avaria_entrada").OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
