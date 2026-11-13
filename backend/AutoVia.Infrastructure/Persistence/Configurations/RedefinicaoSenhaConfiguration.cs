using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapeamento Fluent API da tabela redefinicao_senha (link de recuperacao de senha, RF-49).
    /// O CHECK garante exatamente um titular (cliente ou funcionario). Sem indice unico:
    /// o token tem indice comum, e a unicidade fica no servico.
    /// </summary>
    public class RedefinicaoSenhaConfiguration : IEntityTypeConfiguration<RedefinicaoSenha>
    {
        public void Configure(EntityTypeBuilder<RedefinicaoSenha> builder)
        {
            builder.ToTable("redefinicao_senha", table =>
            {
                table.HasCheckConstraint(
                    "ck_redefinicao_titular",
                    "(id_cliente IS NOT NULL AND id_funcionario IS NULL) OR (id_cliente IS NULL AND id_funcionario IS NOT NULL)");
            });

            builder.HasKey(x => x.IdRedefinicaoSenha).HasName("redefinicao_senha_pkey");
            builder.Property(x => x.IdRedefinicaoSenha).ValueGeneratedOnAdd();

            builder.Property(x => x.IdCliente).IsRequired(false);
            builder.Property(x => x.IdFuncionario).IsRequired(false);

            builder.Property(x => x.TokenHash).IsRequired(true).HasMaxLength(64).IsFixedLength();

            builder.Property(x => x.ExpiraEm)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone");

            // Busca do link pelo hash do token. Indice comum (sem UNIQUE).
            builder.HasIndex(x => x.TokenHash).HasDatabaseName("idx_redefinicao_token");

            // 1:N no modelo sem lista de navegacao inversa em Cliente/Funcionario.
            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_redefinicao_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Funcionario)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionario)
                .HasConstraintName("fk_redefinicao_funcionario")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
