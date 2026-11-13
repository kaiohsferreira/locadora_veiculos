using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tabela sinistro_historico (linha do tempo do RF-43). Desvio aprovado pelo Kaio em
    /// 13/11/2026. CHECKs: os cinco status validos em status_anterior (ou nulo) e status_novo,
    /// a origem, e id_funcionario obrigatorio quando a origem for FUNCIONARIO.
    /// </summary>
    public class SinistroHistoricoConfiguration : IEntityTypeConfiguration<SinistroHistorico>
    {
        private const string StatusValidos = "('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')";

        public void Configure(EntityTypeBuilder<SinistroHistorico> builder)
        {
            builder.ToTable("sinistro_historico", table =>
            {
                table.HasCheckConstraint(
                    "ck_sinistro_historico_status_anterior",
                    "status_anterior IS NULL OR status_anterior IN " + StatusValidos);
                table.HasCheckConstraint(
                    "ck_sinistro_historico_status_novo",
                    "status_novo IN " + StatusValidos);
                table.HasCheckConstraint(
                    "ck_sinistro_historico_origem",
                    "origem IN ('FUNCIONARIO', 'SEGURADORA')");
                table.HasCheckConstraint(
                    "ck_sinistro_historico_funcionario",
                    "origem <> 'FUNCIONARIO' OR id_funcionario IS NOT NULL");
            });

            builder.HasKey(x => x.IdSinistroHistorico).HasName("sinistro_historico_pkey");
            builder.Property(x => x.IdSinistroHistorico).ValueGeneratedOnAdd();

            builder.Property(x => x.IdSinistro).IsRequired(true);
            builder.Property(x => x.StatusAnterior).IsRequired(false).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.StatusNovo).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.Origem).IsRequired(true).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.IdFuncionario).IsRequired(false);
            builder.Property(x => x.Observacao).IsRequired(false).HasMaxLength(500);
            builder.Property(x => x.DataAlteracao).IsRequired(true).HasColumnType("timestamp without time zone");

            builder.HasIndex(x => new { x.IdSinistro, x.DataAlteracao }).HasDatabaseName("idx_sinistro_historico_sinistro");

            builder.HasOne(x => x.Sinistro)
                .WithMany(x => x.Historicos)
                .HasForeignKey(x => x.IdSinistro)
                .HasConstraintName("fk_sinistro_historico_sinistro")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Funcionario)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionario)
                .IsRequired(false)
                .HasConstraintName("fk_sinistro_historico_funcionario")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
