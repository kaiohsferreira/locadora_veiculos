using AutoVia.Integrations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    public class IntegrationSettingConfiguration : IEntityTypeConfiguration<IntegrationSetting>
    {
        public void Configure(EntityTypeBuilder<IntegrationSetting> builder)
        {
            builder.ToTable("integration_setting");

            builder.HasKey(x => x.IdSetting).HasName("integration_setting_pkey");
            builder.Property(x => x.IdSetting).ValueGeneratedOnAdd();

            builder.Property(x => x.IdIntegration).IsRequired(true);
            builder.Property(x => x.Chave).IsRequired(true).HasMaxLength(60);
            builder.Property(x => x.Valor).IsRequired(true).HasColumnType("text");
            builder.Property(x => x.Protegido).IsRequired(true).HasDefaultValue(false);

            // --- BaseEntities (inclui disabled_at: exclusao logica tambem nos settings) ---
            builder.ConfigureBaseEntities();

            // Chave unica por integracao entre os settings ativos: validada no codigo, sem indice unico.
            builder.HasIndex(x => new { x.IdIntegration, x.Chave }).HasDatabaseName("idx_setting_chave");
            builder.HasIndex(x => x.IdIntegration).HasDatabaseName("idx_setting_integration");

            // ON DELETE CASCADE da especificacao: o setting pertence a integracao.
            builder.HasOne(x => x.Integration)
                .WithMany(x => x.Settings)
                .HasForeignKey(x => x.IdIntegration)
                .HasConstraintName("fk_setting_integration")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
