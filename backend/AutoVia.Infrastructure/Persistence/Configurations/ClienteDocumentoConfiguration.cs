using AutoVia.Core.Entities.PgSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapeamento Fluent API da tabela cliente_documento (CNH e comprovante de residencia, RF-08). Desvio aprovado por Kaio em
    /// 19/11/2026 (bloco cliente_documento de desvios_aprovados.json). CHECK ck_cliente_documento_tipo; FK NO ACTION para o cliente;
    /// indice comum idx_cliente_documento_cliente_tipo (id_cliente, tipo). Nenhum indice unico: um ativo por tipo fica no servico.
    /// </summary>
    public class ClienteDocumentoConfiguration : IEntityTypeConfiguration<ClienteDocumento>
    {
        public void Configure(EntityTypeBuilder<ClienteDocumento> builder)
        {
            builder.ToTable("cliente_documento", table =>
            {
                table.HasCheckConstraint("ck_cliente_documento_tipo", "tipo IN ('CNH', 'COMPROVANTE_RESIDENCIA')");
            });

            builder.HasKey(x => x.IdClienteDocumento).HasName("cliente_documento_pkey");
            builder.Property(x => x.IdClienteDocumento).ValueGeneratedOnAdd();

            builder.Property(x => x.IdCliente).IsRequired(true);
            builder.Property(x => x.Tipo)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(30);
            builder.Property(x => x.ChaveObjeto).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.NomeArquivo).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.TipoConteudo).IsRequired(true).HasMaxLength(50);
            builder.Property(x => x.TamanhoBytes).IsRequired(true);

            builder.HasIndex(x => new { x.IdCliente, x.Tipo }).HasDatabaseName("idx_cliente_documento_cliente_tipo");

            builder.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.IdCliente)
                .HasConstraintName("fk_cliente_documento_cliente")
                .OnDelete(DeleteBehavior.NoAction);

            // --- BaseEntities (created_at, updated_at, disabled_at: exclusao logica) ---
            builder.ConfigureBaseEntities();
        }
    }
}
