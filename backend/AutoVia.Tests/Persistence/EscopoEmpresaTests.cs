using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
        private const int IdClienteDoTeste = 5;

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

        /// <summary>Contexto do vinculo cliente x locadora: a locadora (empresa) e/ou o cliente, cada um com o seu id.</summary>
        private static AutoViaDbContext CriarContextoDoVinculo(int idEmpresa, int idCliente)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 1, AutoVia.Communication.Enums.CargoFuncionario.ADMINISTRADOR, false);
            }

            if (idCliente > 0)
            {
                contexto.DefinirCliente(idCliente);
            }

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>
        /// Reconhece o WHERE do filtro de cliente_locadora pela estrutura (nao pelo numero do parametro p, que depende
        /// da ordem do modelo): ramo da empresa AND ramo do cliente. Devolve os nomes das duas guardas booleanas.
        /// </summary>
        private static Match CasarFiltroDoVinculo(string sql)
        {
            Regex filtro = new Regex(
                @"WHERE \(@(?<guardaEmpresa>\w+) AND c\.id_empresa = @ef_filter__IdEmpresaContexto\) OR \(@(?<guardaCliente>\w+) AND c\.id_cliente = @ef_filter__IdClienteContexto\)");

            Match casamento = filtro.Match(sql);
            Assert.True(casamento.Success, "O filtro de cliente_locadora deveria ser (guarda empresa AND id_empresa) OR (guarda cliente AND id_cliente).");

            return casamento;
        }

        /// <summary>Valor que o ToQueryString mostra para um parametro (linha "-- @nome='valor'").</summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        [Fact]
        public void ClienteLocadora_ContextoDaEmpresa_VeSoOSeuPar()
        {
            using AutoViaDbContext db = CriarContextoDoVinculo(EmpresaDoTeste, 0);

            string sql = db.ClientesLocadoras.ToQueryString();
            Match casamento = CasarFiltroDoVinculo(sql);

            Assert.Equal("True", ValorDoParametro(sql, casamento.Groups["guardaEmpresa"].Value));
            Assert.Equal("False", ValorDoParametro(sql, casamento.Groups["guardaCliente"].Value));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, "ef_filter__IdEmpresaContexto"));
            Assert.Equal("0", ValorDoParametro(sql, "ef_filter__IdClienteContexto"));
        }

        [Fact]
        public void ClienteLocadora_ContextoDoCliente_VeSoOSeuVinculo()
        {
            using AutoViaDbContext db = CriarContextoDoVinculo(0, IdClienteDoTeste);

            string sql = db.ClientesLocadoras.ToQueryString();
            Match casamento = CasarFiltroDoVinculo(sql);

            Assert.Equal("False", ValorDoParametro(sql, casamento.Groups["guardaEmpresa"].Value));
            Assert.Equal("True", ValorDoParametro(sql, casamento.Groups["guardaCliente"].Value));
            Assert.Equal("0", ValorDoParametro(sql, "ef_filter__IdEmpresaContexto"));
            Assert.Equal(IdClienteDoTeste.ToString(), ValorDoParametro(sql, "ef_filter__IdClienteContexto"));
        }

        [Fact]
        public void ClienteLocadora_SemContexto_FailClosedNenhumRamoVale()
        {
            using AutoViaDbContext db = CriarContextoDoVinculo(0, 0);

            string sql = db.ClientesLocadoras.ToQueryString();
            Match casamento = CasarFiltroDoVinculo(sql);

            Assert.Equal("False", ValorDoParametro(sql, casamento.Groups["guardaEmpresa"].Value));
            Assert.Equal("False", ValorDoParametro(sql, casamento.Groups["guardaCliente"].Value));
        }

        /// <summary>
        /// Confere SO a forma do SQL quando os dois guardas estao ligados: cada ramo usa o proprio id. NAO prova a regra
        /// "nenhum contexto com os dois ids": essa regra e do ContextoEmpresa, que recusa o contexto misto em Definir e em
        /// DefinirCliente (ContextoMistoTests). Aqui o contexto e um fixo de teste, para a forma do filtro ser conferida sozinha.
        /// </summary>
        [Fact]
        public void ClienteLocadora_ComOsDoisIds_ConfereSoAFormaDoSql()
        {
            using AutoViaDbContext db = new AutoViaDbContext(Opcoes, new ContextoFixoDeTeste(EmpresaDoTeste, IdClienteDoTeste));

            string sql = db.ClientesLocadoras.ToQueryString();
            Match casamento = CasarFiltroDoVinculo(sql);

            Assert.Equal("True", ValorDoParametro(sql, casamento.Groups["guardaEmpresa"].Value));
            Assert.Equal("True", ValorDoParametro(sql, casamento.Groups["guardaCliente"].Value));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, "ef_filter__IdEmpresaContexto"));
            Assert.Equal(IdClienteDoTeste.ToString(), ValorDoParametro(sql, "ef_filter__IdClienteContexto"));
            Assert.Contains("c.id_empresa = @ef_filter__IdEmpresaContexto", sql);
            Assert.Contains("c.id_cliente = @ef_filter__IdClienteContexto", sql);
        }

        /// <summary>Contexto com valores fixos (sem a regra do ContextoEmpresa), so para conferir a forma do SQL.</summary>
        private sealed class ContextoFixoDeTeste : IContextoEmpresa
        {
            public ContextoFixoDeTeste(int idEmpresa, int idCliente)
            {
                IdEmpresa = idEmpresa;
                IdCliente = idCliente;
            }

            public int IdEmpresa { get; }

            public int IdFuncionario { get; } = 0;

            public int IdCliente { get; }

            public AutoVia.Communication.Enums.CargoFuncionario? Cargo { get; } = null;

            public bool EmpresaOperadora { get; } = false;

            public bool PodeVerValores { get; } = false;

            public void Definir(int idEmpresa, int idFuncionario, AutoVia.Communication.Enums.CargoFuncionario cargo, bool empresaOperadora)
            {
            }

            public void DefinirCliente(int idCliente)
            {
            }
        }
    }
}
