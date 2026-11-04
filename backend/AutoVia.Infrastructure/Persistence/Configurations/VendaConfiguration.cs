using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class VendaConfiguration : IEntityTypeConfiguration<Venda>
    {
        public void Configure(EntityTypeBuilder<Venda> builder)
        {
            builder.ToTable("venda", table =>
            {
                table.HasCheckConstraint("ck_venda_valor", "valor_venda > 0");
                table.HasCheckConstraint("ck_venda_forma", "forma_pagamento IN ('A_VISTA', 'FINANCIAMENTO', 'CONSORCIO')");
                table.HasCheckConstraint("ck_venda_doc", "status_documentacao IN ('PENDENTE', 'EM_TRANSFERENCIA', 'CONCLUIDA')");
            });

            builder.HasKey(x => x.IdVenda).HasName("venda_pkey");
            builder.Property(x => x.IdVenda).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.IdFuncionario).IsRequired(true);

            builder.Property(x => x.DataVenda)
                .IsRequired(true)
                .HasColumnType("date")
                .HasDefaultValueSql("CURRENT_DATE");

            builder.Property(x => x.ValorVenda).IsRequired(true).HasPrecision(12, 2);
            builder.Property(x => x.FormaPagamento).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.NumeroNotaFiscal).IsRequired(false).HasMaxLength(20);

            builder.Property(x => x.StatusDocumentacao)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusDocumentacaoVenda.PENDENTE);

            // Uma venda ativa por veiculo e nota fiscal unica: validadas no servico, sem indice unico no banco.
            builder.HasIndex(x => x.IdVeiculo).HasDatabaseName("idx_venda_veiculo");
            builder.HasIndex(x => x.NumeroNotaFiscal).HasDatabaseName("idx_venda_numero_nota_fiscal");

            // 1:N no modelo (com soft delete, uma venda desativada nao impede outra);
            // a regra "no maximo uma venda ativa por veiculo" fica no servico.
            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Vendas)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_venda_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Cliente)
                .WithMany(x => x.Vendas)
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_venda_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Funcionario)
                .WithMany(x => x.Vendas)
                .HasForeignKey(x => x.IdFuncionario)
                .HasConstraintName("fk_venda_funcionario")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
