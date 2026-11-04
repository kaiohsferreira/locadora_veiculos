using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class SinistroConfiguration : IEntityTypeConfiguration<Sinistro>
    {
        public void Configure(EntityTypeBuilder<Sinistro> builder)
        {
            builder.ToTable("sinistro", table =>
            {
                table.HasCheckConstraint(
                    "ck_sinistro_tipo",
                    "tipo IN ('COLISAO', 'ROUBO', 'FURTO', 'INCENDIO', 'FENOMENO_NATURAL', 'TERCEIROS')");
                table.HasCheckConstraint(
                    "ck_sinistro_status",
                    "status IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')");
            });

            builder.HasKey(x => x.IdSinistro).HasName("sinistro_pkey");
            builder.Property(x => x.IdSinistro).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.IdSeguro).IsRequired(false);
            builder.Property(x => x.IdLocacao).IsRequired(false);
            builder.Property(x => x.DataOcorrencia).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.Tipo).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Descricao).IsRequired(false).HasColumnType("text");
            builder.Property(x => x.ValorPrejuizo).IsRequired(false).HasPrecision(12, 2);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusSinistro.ABERTO);

            builder.HasIndex(x => new { x.IdVeiculo, x.DataOcorrencia }).HasDatabaseName("idx_sinistro_veiculo");

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Sinistros)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_sinistro_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Seguro)
                .WithMany(x => x.Sinistros)
                .HasForeignKey(x => x.IdSeguro)
                .IsRequired(false)
                .HasConstraintName("fk_sinistro_seguro")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Locacao)
                .WithMany(x => x.Sinistros)
                .HasForeignKey(x => x.IdLocacao)
                .IsRequired(false)
                .HasConstraintName("fk_sinistro_locacao")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
