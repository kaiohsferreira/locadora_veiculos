using AutoVia.Core.Entities.PgSql;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("cliente", table =>
            {
                table.HasCheckConstraint("ck_cliente_status", "status_cadastro IN ('PENDENTE', 'APROVADO', 'RECUSADO')");
                table.HasCheckConstraint("ck_cliente_cnh_cat", "cnh_categoria IN ('A', 'B', 'AB', 'C', 'D', 'E')");
            });

            builder.HasKey(x => x.IdCliente).HasName("cliente_pkey");
            builder.Property(x => x.IdCliente).ValueGeneratedOnAdd();

            builder.Property(x => x.Nome).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Cpf).IsRequired(true).HasMaxLength(11).IsFixedLength();
            builder.Property(x => x.DataNascimento).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.Email).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Telefone).IsRequired(true).HasMaxLength(20);
            builder.Property(x => x.CnhNumero).IsRequired(true).HasMaxLength(20);

            // CHAR(2) completa com espaco ('A ' para a categoria A). O conversor
            // remove o espaco na leitura antes de converter para o enum.
            ValueConverter<CategoriaCnh, string> categoriaCnhConverter = new ValueConverter<CategoriaCnh, string>(
                value => value.ToString(),
                value => Enum.Parse<CategoriaCnh>(value.Trim()));

            builder.Property(x => x.CnhCategoria)
                .IsRequired(true)
                .HasConversion(categoriaCnhConverter)
                .HasMaxLength(2)
                .IsFixedLength();

            builder.Property(x => x.CnhValidade).IsRequired(true).HasColumnType("date");
            builder.Property(x => x.Logradouro).IsRequired(false).HasMaxLength(120);
            builder.Property(x => x.Numero).IsRequired(false).HasMaxLength(10);
            builder.Property(x => x.Complemento).IsRequired(false).HasMaxLength(60);
            builder.Property(x => x.Bairro).IsRequired(false).HasMaxLength(60);
            builder.Property(x => x.Cidade).IsRequired(false).HasMaxLength(60);
            builder.Property(x => x.Uf).IsRequired(false).HasMaxLength(2).IsFixedLength();
            builder.Property(x => x.Cep).IsRequired(false).HasMaxLength(8).IsFixedLength();

            builder.Property(x => x.DataCadastro)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.StatusCadastro)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(15)
                .HasDefaultValue(StatusCadastroCliente.PENDENTE);

            // UNIQUE da especificacao (nome no padrao do PostgreSQL para UNIQUE em linha).
            builder.HasIndex(x => x.Cpf).HasDatabaseName("idx_cliente_cpf");
            builder.HasIndex(x => x.CnhNumero).HasDatabaseName("idx_cliente_cnh_numero");

            builder.Property(x => x.SenhaHash).IsRequired(false).HasMaxLength(ConstantsValues.TamanhoMaximoSenhaHash);

            // Vinculo com a conta Google (RN3.5: uma conta Google para no maximo um cliente ativo, validado no servico).
            builder.Property(x => x.GoogleSub).IsRequired(false).HasMaxLength(255);
            builder.HasIndex(x => x.GoogleSub).HasDatabaseName("idx_cliente_google_sub");

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
