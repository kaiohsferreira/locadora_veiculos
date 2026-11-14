using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapeamento Fluent API da tabela cliente_locadora (aprovacao do cadastro do cliente por locadora).
    /// Os dois indices sao comuns (nenhum e unico) e as tres chaves estrangeiras usam NO ACTION.
    /// </summary>
    public class ClienteLocadoraConfiguration : IEntityTypeConfiguration<ClienteLocadora>
    {
        public void Configure(EntityTypeBuilder<ClienteLocadora> builder)
        {
            builder.ToTable("cliente_locadora", table =>
            {
                table.HasCheckConstraint("ck_cliente_locadora_status", "status IN ('PENDENTE', 'APROVADO', 'RECUSADO')");
            });

            builder.HasKey(x => x.IdClienteLocadora).HasName("cliente_locadora_pkey");
            builder.Property(x => x.IdClienteLocadora).ValueGeneratedOnAdd();

            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.IdEmpresa).IsRequired(true);

            builder.Property(x => x.Status)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(15)
                .HasDefaultValue(StatusCadastroCliente.PENDENTE);

            builder.Property(x => x.JustificativaRecusa).IsRequired(false).HasMaxLength(500);

            builder.Property(x => x.IdFuncionarioVerificador).IsRequired(false);

            builder.Property(x => x.DataSolicitacao)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.DataVerificacao).IsRequired(false).HasColumnType("timestamp without time zone");

            // Indices comuns (nenhum e unico: a unicidade do vinculo ativo fica no servico).
            builder.HasIndex(x => new { x.IdEmpresa, x.IdCliente }).HasDatabaseName("idx_cliente_locadora_empresa_cliente");
            builder.HasIndex(x => x.IdCliente).HasDatabaseName("idx_cliente_locadora_cliente");

            builder.HasOne(x => x.Cliente)
                .WithMany(x => x.Locadoras)
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_cliente_locadora_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Clientes)
                .HasForeignKey(x => x.IdEmpresa)
                .HasConstraintName("fk_cliente_locadora_empresa")
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.FuncionarioVerificador)
                .WithMany()
                .HasForeignKey(x => x.IdFuncionarioVerificador)
                .HasConstraintName("fk_cliente_locadora_funcionario")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
