using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tabela proposta_compra (P6-BE-02, RF-27). Desvio aprovado por Kaio em 21/11/2026 (bloco proposta_compra de
    /// desvios_aprovados.json): 5 CHECKs, FKs NO ACTION para veiculo e cliente e dois indices comuns. Nenhum indice
    /// unico: o cliente pode enviar mais de uma proposta para o mesmo veiculo.
    /// </summary>
    public class PropostaCompraConfiguration : IEntityTypeConfiguration<PropostaCompra>
    {
        public void Configure(EntityTypeBuilder<PropostaCompra> builder)
        {
            builder.ToTable("proposta_compra", table =>
            {
                table.HasCheckConstraint("ck_proposta_compra_valor", "valor_venda > 0");
                table.HasCheckConstraint("ck_proposta_compra_entrada", "valor_entrada >= 0 AND valor_entrada < valor_venda");
                table.HasCheckConstraint("ck_proposta_compra_parcelas", "numero_parcelas BETWEEN 1 AND 60");
                table.HasCheckConstraint("ck_proposta_compra_parcela", "valor_parcela > 0 AND valor_ultima_parcela >= valor_parcela");
                table.HasCheckConstraint("ck_proposta_compra_status", "status IN ('ENVIADA', 'ACEITA', 'RECUSADA', 'CANCELADA')");
            });

            builder.HasKey(x => x.IdPropostaCompra).HasName("proposta_compra_pkey");
            builder.Property(x => x.IdPropostaCompra).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.ValorVenda).IsRequired(true).HasPrecision(12, 2);
            builder.Property(x => x.ValorEntrada).IsRequired(true).HasPrecision(12, 2);
            builder.Property(x => x.NumeroParcelas).IsRequired(true);
            builder.Property(x => x.ValorParcela).IsRequired(true).HasPrecision(12, 2);
            builder.Property(x => x.ValorUltimaParcela).IsRequired(true).HasPrecision(12, 2);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusPropostaCompra.ENVIADA);

            builder.HasIndex(x => x.IdVeiculo).HasDatabaseName("idx_proposta_compra_veiculo");
            builder.HasIndex(x => x.IdCliente).HasDatabaseName("idx_proposta_compra_cliente");

            builder.HasOne(x => x.Veiculo)
                .WithMany()
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_proposta_compra_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_proposta_compra_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
