using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class CobrancaExtraConfiguration : IEntityTypeConfiguration<CobrancaExtra>
    {
        public void Configure(EntityTypeBuilder<CobrancaExtra> builder)
        {
            builder.ToTable("cobranca_extra", table =>
            {
                table.HasCheckConstraint(
                    "ck_cobranca_tipo",
                    "tipo IN ('MULTA_TRANSITO', 'KM_EXCEDENTE', 'ATRASO', 'AVARIA', 'COMBUSTIVEL', 'LIMPEZA')");
                table.HasCheckConstraint("ck_cobranca_valor", "valor >= 0");
            });

            builder.HasKey(x => x.IdCobranca).HasName("cobranca_extra_pkey");
            builder.Property(x => x.IdCobranca).ValueGeneratedOnAdd();

            builder.Property(x => x.IdLocacao).IsRequired(true);
            builder.Property(x => x.Tipo).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Descricao).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Valor).IsRequired(true).HasPrecision(10, 2);

            builder.Property(x => x.DataLancamento)
                .IsRequired(true)
                .HasColumnType("date")
                .HasDefaultValueSql("CURRENT_DATE");

            // ON DELETE CASCADE da especificacao: a cobranca pertence a locacao.
            builder.HasOne(x => x.Locacao)
                .WithMany(x => x.CobrancasExtras)
                .HasForeignKey(x => x.IdLocacao)
                .HasConstraintName("fk_cobranca_locacao")
                .OnDelete(DeleteBehavior.Cascade);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
