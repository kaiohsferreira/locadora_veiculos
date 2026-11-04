using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
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

            // ON DELETE CASCADE da especificacao: a avaria pertence a vistoria.
            builder.HasOne(x => x.Vistoria)
                .WithMany(x => x.Avarias)
                .HasForeignKey(x => x.IdVistoria)
                .HasConstraintName("fk_avaria_vistoria")
                .OnDelete(DeleteBehavior.Cascade);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
