using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Testes do filtro global "Empresa" (multi-locadora, T-BE-03b) SEM banco. O ToQueryString do EF
    /// traduz a consulta com o filtro e mostra o valor de cada parametro do filtro (o id do contexto
    /// que executa a consulta). Assim conferimos, para cada entidade escopada: o filtro esta aplicado,
    /// le o contexto ATUAL (duas instancias do mesmo modelo) e, sem contexto, o guarda vira falso (fail-closed).
    /// </summary>
    public class EscopoEmpresaTests
    {
        private const string ParametroDoContexto = "@ef_filter__IdEmpresaContexto";
        private const string GuardaVerdadeiro = "='True'";
        private const string GuardaFalso = "='False'";
        private const int EmpresaDoTeste = 4242;
        private const int OutraEmpresaDoTeste = 7373;

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarContexto(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 1, AutoVia.Communication.Enums.CargoFuncionario.ADMINISTRADOR, false);
            }

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>Uma consulta por entidade escopada. Pagamento aparece uma vez: o filtro dele cobre os dois caminhos.</summary>
        private static Dictionary<string, Func<AutoViaDbContext, string>> ConsultasEscopadas()
        {
            return new Dictionary<string, Func<AutoViaDbContext, string>>
            {
                ["Veiculo"] = db => db.Veiculos.ToQueryString(),
                ["Funcionario"] = db => db.Funcionarios.ToQueryString(),
                ["Reserva"] = db => db.Reservas.ToQueryString(),
                ["Locacao"] = db => db.Locacoes.ToQueryString(),
                ["Venda"] = db => db.Vendas.ToQueryString(),
                ["Manutencao"] = db => db.Manutencoes.ToQueryString(),
                ["Seguro"] = db => db.Seguros.ToQueryString(),
                ["Sinistro"] = db => db.Sinistros.ToQueryString(),
                ["Vistoria"] = db => db.Vistorias.ToQueryString(),
                ["CobrancaExtra"] = db => db.CobrancasExtras.ToQueryString(),
                ["Avaria"] = db => db.Avarias.ToQueryString(),
                ["Pagamento"] = db => db.Pagamentos.ToQueryString()
            };
        }

        [Fact]
        public void TodaEntidadeEscopada_TemOFiltroAplicadoComOIdDoContextoAtual()
        {
            using AutoViaDbContext db = CriarContexto(EmpresaDoTeste);

            foreach (KeyValuePair<string, Func<AutoViaDbContext, string>> caso in ConsultasEscopadas())
            {
                string sql = caso.Value(db);

                Assert.True(sql.Contains($"{ParametroDoContexto}='{EmpresaDoTeste}'"), $"{caso.Key}: o filtro deveria usar o id {EmpresaDoTeste} do contexto.");
                Assert.True(sql.Contains(GuardaVerdadeiro), $"{caso.Key}: o guarda de contexto positivo deveria estar ativo.");
            }
        }

        [Fact]
        public void DuasInstanciasDoMesmoModelo_CadaUmaUsaOProprioContexto()
        {
            using AutoViaDbContext primeira = CriarContexto(EmpresaDoTeste);
            using AutoViaDbContext segunda = CriarContexto(OutraEmpresaDoTeste);

            string sqlPrimeira = primeira.Veiculos.ToQueryString();
            string sqlSegunda = segunda.Veiculos.ToQueryString();

            Assert.Contains($"{ParametroDoContexto}='{EmpresaDoTeste}'", sqlPrimeira);
            Assert.Contains($"{ParametroDoContexto}='{OutraEmpresaDoTeste}'", sqlSegunda);
            Assert.DoesNotContain($"{ParametroDoContexto}='{EmpresaDoTeste}'", sqlSegunda);
        }

        [Fact]
        public void SemContexto_FailClosed_OGuardaViraFalsoEmTodaEntidadeEscopada()
        {
            using AutoViaDbContext db = CriarContexto(0);

            foreach (KeyValuePair<string, Func<AutoViaDbContext, string>> caso in ConsultasEscopadas())
            {
                string sql = caso.Value(db);

                Assert.True(sql.Contains($"{ParametroDoContexto}='0'"), $"{caso.Key}: sem contexto o id deveria ser 0.");
                Assert.True(sql.Contains(GuardaFalso), $"{caso.Key}: sem contexto o guarda deveria ser falso (nenhuma linha).");
            }
        }

        [Fact]
        public void SemEscopoEmpresa_NaoGeraFiltroNenhum()
        {
            using AutoViaDbContext db = CriarContexto(EmpresaDoTeste);

            string comFiltro = db.Veiculos.ToQueryString();
            string semFiltro = db.Veiculos.SemEscopoEmpresa().ToQueryString();

            Assert.Contains(ParametroDoContexto, comFiltro);
            Assert.DoesNotContain(ParametroDoContexto, semFiltro);
            Assert.DoesNotContain("WHERE", semFiltro);
        }

        [Fact]
        public void IdEmpresaContexto_AcompanhaOContextoDaRequisicao()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            using AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            Assert.Equal(0, db.IdEmpresaContexto);

            contexto.Definir(OutraEmpresaDoTeste, 3, AutoVia.Communication.Enums.CargoFuncionario.ATENDENTE, false);

            Assert.Equal(OutraEmpresaDoTeste, db.IdEmpresaContexto);
        }

        [Fact]
        public void ApenasEmpresasAtivas_FiltraPelaEmpresaAtivaENaoDesativada()
        {
            using AutoViaDbContext db = CriarContexto(0);

            string sql = db.Veiculos.SemEscopoEmpresa().ApenasEmpresasAtivas().ToQueryString();

            Assert.Contains("ativo", sql);
            Assert.Contains("disabled_at", sql);
        }
    }
}
