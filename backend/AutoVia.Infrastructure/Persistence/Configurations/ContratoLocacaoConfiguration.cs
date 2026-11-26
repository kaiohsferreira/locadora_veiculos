using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapeamento Fluent API de contrato_locacao (desvio D10, contrato-locacao v5): colunas e tipos, sem CHECK, FKs NoAction para a
    /// locacao e o funcionario, indice comum por locacao (sem unico) e auditoria de BaseEntities.
    /// </summary>
    public class ContratoLocacaoConfiguration : IEntityTypeConfiguration<ContratoLocacao>
    {
        public void Configure(EntityTypeBuilder<ContratoLocacao> builder)
        {
            builder.ToTable("contrato_locacao");

            builder.HasKey(x => x.IdContratoLocacao).HasName("contrato_locacao_pkey");
            builder.Property(x => x.IdContratoLocacao).ValueGeneratedOnAdd();

            builder.Property(x => x.IdLocacao).IsRequired(true);
            builder.Property(x => x.IdFuncionario).IsRequired(true);
            builder.Property(x => x.DocumentoJson).IsRequired(true).HasColumnType("text");
            builder.Property(x => x.HashSha256).IsRequired(true).HasMaxLength(64);
            builder.Property(x => x.NomeAssinante).IsRequired(true).HasMaxLength(100);
            builder.Property(x => x.AceiteCondicoesGerais).IsRequired(true);
            builder.Property(x => x.AceitePoliticaCombustivel).IsRequired(true);
            builder.Property(x => x.AssinaturaPngBase64).IsRequired(true).HasColumnType("text");

            builder.Property(x => x.AssinadoEm)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone");

            builder.HasIndex(x => x.IdLocacao).HasDatabaseName("idx_contrato_locacao_locacao");

            builder.HasOne(x => x.Locacao)
                .WithMany()
                .HasForeignKey(x => x.IdLocacao)
                .HasConstraintName("fk_contrato_locacao_locacao")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Funcionario)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionario)
                .HasConstraintName("fk_contrato_locacao_funcionario")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
