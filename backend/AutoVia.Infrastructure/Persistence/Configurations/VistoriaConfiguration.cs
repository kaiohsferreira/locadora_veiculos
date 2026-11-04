using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class VistoriaConfiguration : IEntityTypeConfiguration<Vistoria>
    {
        public void Configure(EntityTypeBuilder<Vistoria> builder)
        {
            builder.ToTable("vistoria", table =>
            {
                table.HasCheckConstraint("ck_vistoria_tipo", "tipo IN ('ENTRADA', 'SAIDA')");
                table.HasCheckConstraint("ck_vistoria_combustivel", "nivel_combustivel BETWEEN 0 AND 1");
            });

            builder.HasKey(x => x.IdVistoria).HasName("vistoria_pkey");
            builder.Property(x => x.IdVistoria).ValueGeneratedOnAdd();

            builder.Property(x => x.IdLocacao).IsRequired(true);
            builder.Property(x => x.IdFuncionario).IsRequired(true);
            builder.Property(x => x.Tipo).IsRequired(true).HasConversion<string>().HasMaxLength(10);

            builder.Property(x => x.DataHora)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.KmRegistrado).IsRequired(true);
            builder.Property(x => x.NivelCombustivel).IsRequired(false).HasPrecision(4, 2);
            builder.Property(x => x.Observacoes).IsRequired(false).HasColumnType("text");

            builder.HasIndex(x => new { x.IdLocacao, x.Tipo }).IsUnique().HasDatabaseName("uq_vistoria_locacao_tipo");

            // ON DELETE CASCADE da especificacao: a vistoria pertence a locacao.
            builder.HasOne(x => x.Locacao)
                .WithMany(x => x.Vistorias)
                .HasForeignKey(x => x.IdLocacao)
                .HasConstraintName("fk_vistoria_locacao")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Funcionario)
                .WithMany(x => x.Vistorias)
                .HasForeignKey(x => x.IdFuncionario)
                .HasConstraintName("fk_vistoria_funcionario")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
