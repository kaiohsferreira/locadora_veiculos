using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tabela veiculo_foto (fotos do veiculo, RF-94 a RF-101 e RF-105). Desvio aprovado por Kaio em 16/11/2026
    /// (bloco veiculo_foto de desvios_aprovados.json). CHECK ck_veiculo_foto_ordem (ordem maior ou igual a 1); FKs NO ACTION para
    /// veiculo, funcionario que enviou (obrigatoria) e funcionario que removeu (anulavel); indice comum idx_veiculo_foto_veiculo
    /// (id_veiculo, ordem). Nenhum indice unico: o teto de 15 fotos ativas fica no servico.
    /// </summary>
    public class VeiculoFotoConfiguration : IEntityTypeConfiguration<VeiculoFoto>
    {
        public void Configure(EntityTypeBuilder<VeiculoFoto> builder)
        {
            builder.ToTable("veiculo_foto", table =>
            {
                table.HasCheckConstraint("ck_veiculo_foto_ordem", "ordem >= 1");
            });

            builder.HasKey(x => x.IdVeiculoFoto).HasName("veiculo_foto_pkey");
            builder.Property(x => x.IdVeiculoFoto).ValueGeneratedOnAdd();

            builder.Property(x => x.IdVeiculo).IsRequired(true);
            builder.Property(x => x.Ordem).IsRequired(true);
            builder.Property(x => x.ChaveObjeto).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.ChaveMiniatura).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.IdFuncionarioEnvio).IsRequired(true);
            builder.Property(x => x.IdFuncionarioRemocao).IsRequired(false);

            builder.HasIndex(x => new { x.IdVeiculo, x.Ordem }).HasDatabaseName("idx_veiculo_foto_veiculo");

            builder.HasOne(x => x.Veiculo)
                .WithMany(x => x.Fotos)
                .HasForeignKey(x => x.IdVeiculo)
                .HasConstraintName("fk_veiculo_foto_veiculo")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.FuncionarioEnvio)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionarioEnvio)
                .IsRequired(true)
                .HasConstraintName("fk_veiculo_foto_funcionario_envio")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.FuncionarioRemocao)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionarioRemocao)
                .IsRequired(false)
                .HasConstraintName("fk_veiculo_foto_funcionario_remocao")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
