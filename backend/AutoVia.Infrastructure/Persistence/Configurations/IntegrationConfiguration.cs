using AutoVia.Communication.Enums;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
    {
        public void Configure(EntityTypeBuilder<Integration> builder)
        {
            builder.ToTable("integration", table =>
            {
                table.HasCheckConstraint("ck_integration_ambiente", "ambiente IN ('SANDBOX', 'HOMOLOGACAO', 'PRODUCAO')");
                table.HasCheckConstraint("ck_integration_timeout", "timeout_ms > 0");
            });

            builder.HasKey(x => x.IdIntegration).HasName("integration_pkey");
            builder.Property(x => x.IdIntegration).ValueGeneratedOnAdd();

            builder.Property(x => x.Codigo).IsRequired(true).HasMaxLength(40);
            builder.Property(x => x.Nome).IsRequired(true).HasMaxLength(120);
            builder.Property(x => x.Descricao).IsRequired(false).HasMaxLength(200);

            builder.Property(x => x.Ambiente)
                .IsRequired(true)
                .HasConversion<string>()
                .HasMaxLength(15)
                .HasDefaultValue(AmbienteIntegracao.PRODUCAO);

            builder.Property(x => x.BaseUrl).IsRequired(true).HasMaxLength(255);

            // Sentinela = valor padrao: o EF omite a coluna quando ela tem o valor
            // do DEFAULT e envia qualquer outro valor explicitamente.
            builder.Property(x => x.TimeoutMs)
                .IsRequired(true)
                .HasDefaultValue(ConstantsIntegration.TimeoutPadraoMs)
                .HasSentinel(ConstantsIntegration.TimeoutPadraoMs);

            builder.Property(x => x.Ativo).IsRequired(true).HasDefaultValue(true).HasSentinel(true);

            // --- BaseEntities ---
            builder.ConfigureBaseEntities();

            // Codigo unico entre as integracoes ativas: validado no codigo, sem indice unico no banco.
            builder.HasIndex(x => x.Codigo).HasDatabaseName("idx_integration_codigo");
        }
    }
}
