using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class LocacaoConfiguration : IEntityTypeConfiguration<Locacao>
    {
        public void Configure(EntityTypeBuilder<Locacao> builder)
        {
            builder.ToTable("locacao", table =>
            {
                table.HasCheckConstraint("ck_locacao_status", "status IN ('EM_ANDAMENTO', 'FINALIZADA', 'ATRASADA', 'CANCELADA')");
                table.HasCheckConstraint("ck_locacao_km", "km_chegada IS NULL OR km_chegada >= km_saida");
                table.HasCheckConstraint(
                    "ck_locacao_devolucao",
                    "data_devolucao_efetiva IS NULL OR data_devolucao_efetiva >= data_retirada");
            });

            builder.HasKey(x => x.IdLocacao).HasName("locacao_pkey");
            builder.Property(x => x.IdLocacao).ValueGeneratedOnAdd();

            builder.Property(x => x.IdReserva).IsRequired(false);
            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.IdFuncionario).IsRequired(true);
            builder.Property(x => x.DataRetirada).IsRequired(true).HasColumnType("timestamp without time zone");
            builder.Property(x => x.DataDevolucaoPrevista).IsRequired(true).HasColumnType("timestamp without time zone");
            builder.Property(x => x.DataDevolucaoEfetiva).IsRequired(false).HasColumnType("timestamp without time zone");
            builder.Property(x => x.KmSaida).IsRequired(true);
            builder.Property(x => x.KmChegada).IsRequired(false);
            builder.Property(x => x.ValorDiaria).IsRequired(true).HasPrecision(10, 2);
            builder.Property(x => x.ValorCaucao).IsRequired(true).HasPrecision(10, 2).HasDefaultValueSql("0");
            builder.Property(x => x.ValorTotal).IsRequired(false).HasPrecision(12, 2);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusLocacao.EM_ANDAMENTO);

            builder.HasIndex(x => x.IdReserva).IsUnique().HasDatabaseName("locacao_id_reserva_key");
            builder.HasIndex(x => x.IdCliente).HasDatabaseName("idx_locacao_cliente");
            builder.HasIndex(x => x.IdVeiculo).HasDatabaseName("idx_locacao_veiculo");
            builder.HasIndex(x => x.Status).HasDatabaseName("idx_locacao_status");

            // 1:1 — uma reserva origina no maximo uma locacao.
            builder.HasOne(x => x.Reserva)
                .WithOne(x => x.Locacao)
                .HasForeignKey<Locacao>(x => x.IdReserva)
                .IsRequired(false)
                .HasConstraintName("fk_locacao_reserva")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Cliente)
                .WithMany(x => x.Locacoes)
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_locacao_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Locacoes)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_locacao_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Funcionario)
                .WithMany(x => x.Locacoes)
                .HasForeignKey(x => x.IdFuncionario)
                .HasConstraintName("fk_locacao_funcionario")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
