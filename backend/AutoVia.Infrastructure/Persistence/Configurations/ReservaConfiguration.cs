using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
    {
        public void Configure(EntityTypeBuilder<Reserva> builder)
        {
            builder.ToTable("reserva", table =>
            {
                table.HasCheckConstraint("ck_reserva_status", "status IN ('ATIVA', 'CONFIRMADA', 'CANCELADA', 'CONCLUIDA')");
                table.HasCheckConstraint("ck_reserva_periodo", "data_devolucao_prevista > data_retirada_prevista");
            });

            builder.HasKey(x => x.IdReserva).HasName("reserva_pkey");
            builder.Property(x => x.IdReserva).ValueGeneratedOnAdd();

            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.IdVeiculo).IsRequired(true);

            builder.Property(x => x.DataReserva)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.DataRetiradaPrevista).IsRequired(true).HasColumnType("timestamp without time zone");
            builder.Property(x => x.DataDevolucaoPrevista).IsRequired(true).HasColumnType("timestamp without time zone");
            builder.Property(x => x.ValorEstimado).IsRequired(false).HasPrecision(10, 2);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(StatusReserva.ATIVA);

            builder.HasIndex(x => new { x.DataRetiradaPrevista, x.DataDevolucaoPrevista }).HasDatabaseName("idx_reserva_periodo");
            builder.HasIndex(x => x.IdCliente).HasDatabaseName("idx_reserva_cliente");

            builder.HasOne(x => x.Cliente)
                .WithMany(x => x.Reservas)
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_reserva_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Reservas)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_reserva_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
