using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class FuncionarioConfiguration : IEntityTypeConfiguration<Funcionario>
    {
        public void Configure(EntityTypeBuilder<Funcionario> builder)
        {
            builder.ToTable("funcionario", table =>
            {
                table.HasCheckConstraint(
                    "ck_funcionario_cargo",
                    "cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO')");
            });

            builder.HasKey(x => x.IdFuncionario).HasName("funcionario_pkey");
            builder.Property(x => x.IdFuncionario).ValueGeneratedOnAdd();

            builder.Property(x => x.Nome).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Cpf).IsRequired(true).HasMaxLength(11).IsFixedLength();
            builder.Property(x => x.Matricula).IsRequired(true).HasMaxLength(20);
            builder.Property(x => x.Cargo).IsRequired(true).HasConversion<string>().HasMaxLength(40);
            builder.Property(x => x.Email).IsRequired(false).HasMaxLength(120);
            builder.Property(x => x.DataAdmissao).IsRequired(true).HasColumnType("date");

            // DEFAULT TRUE. O sentinela true faz o EF omitir a coluna no INSERT
            // quando o valor e true (o banco aplica o DEFAULT) e enviar false
            // explicitamente. Sem ele, false seria confundido com "nao informado"
            // e o banco gravaria TRUE.
            builder.Property(x => x.Ativo).IsRequired(true).HasDefaultValue(true).HasSentinel(true);

            builder.HasIndex(x => x.Cpf).IsUnique().HasDatabaseName("funcionario_cpf_key");
            builder.HasIndex(x => x.Matricula).IsUnique().HasDatabaseName("funcionario_matricula_key");
        }
    }
}
