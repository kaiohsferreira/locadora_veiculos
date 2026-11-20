using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Vistorias
{
    /// <summary>Verifica a traducao SQL sem abrir conexao; nao substitui teste de concorrencia no PostgreSQL.</summary>
    public class VistoriaConsultasSqlTests
    {
        [Theory]
        [InlineData(4242, "True")]
        [InlineData(0, "False")]
        public void ConsultaEntradaFiltraEmpresaLocacaoTipoEDesativados(int idEmpresa, string guarda)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 20, CargoFuncionario.ATENDENTE, false);
            }
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste").Options;
            using AutoViaDbContext db = new AutoViaDbContext(opcoes, contexto);
            VistoriaRepository repositorio = new VistoriaRepository(db, new FakeLogService());
            string sql = repositorio.ConsultaEntradaAtiva(77).ToQueryString();
            Assert.Contains($"ef_filter__IdEmpresaContexto='{idEmpresa}'", sql);
            Assert.Contains($"='{guarda}'", sql);
            Assert.Contains("id_locacao = @", sql);
            Assert.Contains("='77'", sql);
            Assert.Contains("tipo = 'ENTRADA'", sql);
            Assert.Contains("disabled_at IS NULL", sql);
            Assert.Contains("id_empresa", sql);
            Assert.Empty(db.ChangeTracker.Entries());
        }
    }
}
