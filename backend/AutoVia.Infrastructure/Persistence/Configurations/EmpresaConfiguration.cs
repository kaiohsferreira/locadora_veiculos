using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>Mapeamento Fluent API da tabela empresa (locadora cadastrada na plataforma).</summary>
    public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
    {
        public void Configure(EntityTypeBuilder<Empresa> builder)
        {
            builder.ToTable("empresa", table =>
            {
                table.HasCheckConstraint("ck_empresa_cnpj", "cnpj ~ '^[0-9]{14}$'");
            });

            builder.HasKey(x => x.IdEmpresa).HasName("empresa_pkey");
            builder.Property(x => x.IdEmpresa).ValueGeneratedOnAdd();

            builder.Property(x => x.RazaoSocial).IsRequired(true).HasMaxLength(150);
            builder.Property(x => x.NomeFantasia).IsRequired(true).HasMaxLength(100);
            builder.Property(x => x.Cnpj).IsRequired(true).HasMaxLength(14).IsFixedLength();
            builder.Property(x => x.Telefone).IsRequired(true).HasMaxLength(20);
            builder.Property(x => x.Email).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Cep).IsRequired(true).HasMaxLength(8).IsFixedLength();
            builder.Property(x => x.Logradouro).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Numero).IsRequired(true).HasMaxLength(10);
            builder.Property(x => x.Complemento).IsRequired(false).HasMaxLength(60);
            builder.Property(x => x.Bairro).IsRequired(true).HasMaxLength(60);
            builder.Property(x => x.Cidade).IsRequired(true).HasMaxLength(60);
            builder.Property(x => x.Uf).IsRequired(true).HasMaxLength(2).IsFixedLength();

            // DEFAULT TRUE, com o mesmo sentinela de funcionario.ativo: o EF omite a coluna
            // no INSERT quando o valor e true (o banco aplica o DEFAULT) e envia false explicitamente.
            builder.Property(x => x.Ativo).IsRequired(true).HasDefaultValue(true).HasSentinel(true);

            // DEFAULT FALSE. So a empresa inicial (operadora da plataforma) recebe TRUE.
            builder.Property(x => x.Operadora).IsRequired(true).HasDefaultValue(false);

            // Indices comuns (nenhum e unico: a unicidade de CNPJ e e-mail e validada no servico).
            builder.HasIndex(x => x.Cnpj).HasDatabaseName("idx_empresa_cnpj");
            builder.HasIndex(x => x.Email).HasDatabaseName("idx_empresa_email");

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
