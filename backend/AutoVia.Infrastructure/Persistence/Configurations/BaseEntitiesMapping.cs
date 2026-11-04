using AutoVia.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoVia.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapeamento Fluent API das colunas de auditoria de BaseEntities, igual em todas as tabelas:
    /// created_at e updated_at obrigatorias (DEFAULT CURRENT_TIMESTAMP) e disabled_at anulavel
    /// (exclusao logica). Os nomes das colunas vem do [Column] de BaseEntities.
    /// </summary>
    public static class BaseEntitiesMapping
    {
        /// <summary>Configura created_at, updated_at e disabled_at na tabela da entidade.</summary>
        public static EntityTypeBuilder<TEntity> ConfigureBaseEntities<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : BaseEntities
        {
            builder.Property(x => x.CreatedAt)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.UpdatedAt)
                .IsRequired(true)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.DisabledAt)
                .IsRequired(false)
                .HasColumnType("timestamp without time zone");

            return builder;
        }
    }
}
