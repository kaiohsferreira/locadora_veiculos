using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
    {
        public void Configure(EntityTypeBuilder<Categoria> builder)
        {
            builder.ToTable("categoria", table =>
            {
                table.HasCheckConstraint("ck_categoria_valor", "valor_diaria_base > 0");
            });

            builder.HasKey(x => x.IdCategoria).HasName("categoria_pkey");
            builder.Property(x => x.IdCategoria).ValueGeneratedOnAdd();

            builder.Property(x => x.Nome).IsRequired(true).HasMaxLength(50);
            builder.Property(x => x.Descricao).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.ValorDiariaBase).IsRequired(true).HasPrecision(10, 2);

            builder.HasIndex(x => x.Nome).HasDatabaseName("idx_categoria_nome");

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
