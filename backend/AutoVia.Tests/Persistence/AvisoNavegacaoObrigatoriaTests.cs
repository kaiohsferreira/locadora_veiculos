using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Prova de mutacao do aviso PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning (A11 da revisao
    /// de fundacao): um modelo minimo com entidade principal FILTRADA e dependente OBRIGATORIO SEM filtro precisa
    /// falhar na construcao do modelo (o aviso vira excecao, com as mesmas opcoes do SchemaModelTests). O controle
    /// usa o mesmo modelo com o filtro tambem no dependente: la a construcao passa. Assim o teste so passa se o
    /// aviso de fato disparar, e nao por acaso.
    /// </summary>
    public class AvisoNavegacaoObrigatoriaTests
    {
        private const string NomeFiltro = "Empresa";
        private const string NomeAviso = "PossibleIncorrectRequiredNavigationWithQueryFilterInteraction";

        [Fact]
        public void DependenteObrigatorioSemFiltroComPrincipalFiltrado_FazAConstrucaoFalhar()
        {
            using ModeloSemFiltroNoDependente contexto = new ModeloSemFiltroNoDependente(OpcoesComAvisoComoErro<ModeloSemFiltroNoDependente>());

            Exception excecao = Assert.ThrowsAny<Exception>(() => _ = contexto.Model);

            Assert.Contains(NomeAviso, excecao.Message);
        }

        [Fact]
        public void MesmoModeloComFiltroNoDependente_ConstroiSemAviso()
        {
            using ModeloComFiltroNoDependente contexto = new ModeloComFiltroNoDependente(OpcoesComAvisoComoErro<ModeloComFiltroNoDependente>());

            Assert.NotNull(contexto.Model);
        }

        private static DbContextOptions<TContexto> OpcoesComAvisoComoErro<TContexto>()
            where TContexto : DbContext
        {
            return new DbContextOptionsBuilder<TContexto>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .ConfigureWarnings(avisos => avisos.Throw(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;
        }

        public sealed class PrincipalFiltrado
        {
            public int Id { get; set; }

            public int IdEmpresa { get; set; }

            public List<DependenteObrigatorio> Dependentes { get; set; } = new List<DependenteObrigatorio>();
        }

        public sealed class DependenteObrigatorio
        {
            public int Id { get; set; }

            public int PrincipalId { get; set; }

            public PrincipalFiltrado Principal { get; set; } = null!;
        }

        /// <summary>Principal com filtro "Empresa"; o dependente obrigatorio NAO tem filtro (o caso que o aviso pega).</summary>
        public sealed class ModeloSemFiltroNoDependente : DbContext
        {
            public ModeloSemFiltroNoDependente(DbContextOptions<ModeloSemFiltroNoDependente> opcoes)
                : base(opcoes)
            {
            }

            public int IdEmpresaContexto { get; set; } = 1;

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<PrincipalFiltrado>().HasQueryFilter(NomeFiltro, x => x.IdEmpresa == IdEmpresaContexto);
                modelBuilder.Entity<PrincipalFiltrado>()
                    .HasMany(x => x.Dependentes)
                    .WithOne(x => x.Principal)
                    .HasForeignKey(x => x.PrincipalId)
                    .IsRequired();
            }
        }

        /// <summary>Mesmo modelo, com o filtro tambem no dependente (controle: nao pode disparar o aviso).</summary>
        public sealed class ModeloComFiltroNoDependente : DbContext
        {
            public ModeloComFiltroNoDependente(DbContextOptions<ModeloComFiltroNoDependente> opcoes)
                : base(opcoes)
            {
            }

            public int IdEmpresaContexto { get; set; } = 1;

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<PrincipalFiltrado>().HasQueryFilter(NomeFiltro, x => x.IdEmpresa == IdEmpresaContexto);
                modelBuilder.Entity<DependenteObrigatorio>().HasQueryFilter(NomeFiltro, x => x.Principal.IdEmpresa == IdEmpresaContexto);
                modelBuilder.Entity<PrincipalFiltrado>()
                    .HasMany(x => x.Dependentes)
                    .WithOne(x => x.Principal)
                    .HasForeignKey(x => x.PrincipalId)
                    .IsRequired();
            }
        }
    }
}
