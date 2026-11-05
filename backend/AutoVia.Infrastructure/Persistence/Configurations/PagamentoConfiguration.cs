using AutoVia.Core.Entities.PgSql;
using AutoVia.Communication.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
    {
        public void Configure(EntityTypeBuilder<Pagamento> builder)
        {
            builder.ToTable("pagamento", table =>
            {
                table.HasCheckConstraint("ck_pagamento_tipo", "tipo IN ('CAUCAO', 'DIARIA', 'COBRANCA_EXTRA', 'VENDA')");
                table.HasCheckConstraint("ck_pagamento_status", "status IN ('PENDENTE', 'PAGO', 'ATRASADO', 'CANCELADO')");
                table.HasCheckConstraint("ck_pagamento_valor", "valor > 0");

                // Um pagamento refere-se a uma locacao OU a uma venda, nunca a ambos.
                table.HasCheckConstraint(
                    "ck_pagamento_origem",
                    "(id_locacao IS NOT NULL AND id_venda IS NULL) OR (id_locacao IS NULL AND id_venda IS NOT NULL)");
            });

            builder.HasKey(x => x.IdPagamento).HasName("pagamento_pkey");
            builder.Property(x => x.IdPagamento).ValueGeneratedOnAdd();

            builder.Property(x => x.IdLocacao).IsRequired(false);
            builder.Property(x => x.IdVenda).IsRequired(false);
            builder.Property(x => x.Tipo).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Valor).IsRequired(true).HasPrecision(12, 2);
            builder.Property(x => x.DataVencimento).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.DataPagamento).IsRequired(false).HasColumnType("date");
            builder.Property(x => x.Forma).IsRequired(false).HasMaxLength(20);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(15)
                .HasDefaultValue(StatusPagamento.PENDENTE);

            builder.HasIndex(x => new { x.Status, x.DataVencimento }).HasDatabaseName("idx_pagamento_status");

            builder.HasOne(x => x.Locacao)
                .WithMany(x => x.Pagamentos)
                .HasForeignKey(x => x.IdLocacao)
                .IsRequired(false)
                .HasConstraintName("fk_pagamento_locacao")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Venda)
                .WithMany(x => x.Pagamentos)
                .HasForeignKey(x => x.IdVenda)
                .IsRequired(false)
                .HasConstraintName("fk_pagamento_venda")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
