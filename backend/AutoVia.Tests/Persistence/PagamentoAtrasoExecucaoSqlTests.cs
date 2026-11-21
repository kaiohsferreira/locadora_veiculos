using System.Data.Common;
using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// SQL que a atualizacao da rotina de atraso executa (contrato pagamento-atraso v1), capturado sem banco: um interceptor
    /// registra o comando do UPDATE e suprime a execucao. Prova que a rota manual (da locadora) leva o filtro de empresa e que
    /// a rotina do sistema (todas as locadoras) nao leva. Complementa PagamentoAtrasoConsultasSqlTests (ToQueryString).
    /// </summary>
    public class PagamentoAtrasoExecucaoSqlTests
    {
        private const string ConexaoInacessivel = "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste";
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private static readonly DateOnly HojeDoTeste = new DateOnly(2026, 11, 19);
        private static readonly DateTime AgoraDoTeste = new DateTime(2026, 11, 19, 10, 0, 0);

        /// <summary>Registra o texto de cada UPDATE de pagamento e devolve zero linhas, sem abrir conexao com o banco.</summary>
        private sealed class CapturaDeComandosInterceptor : DbCommandInterceptor
        {
            public List<string> Comandos { get; } = new List<string>();

            public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                Comandos.Add(command.CommandText);

                return InterceptionResult<int>.SuppressWithResult(0);
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Comandos.Add(command.CommandText);

                return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
            }
        }

        /// <summary>Nao deixa abrir a conexao: sem isso o EF tenta conectar antes de chegar ao comando.</summary>
        private sealed class SupressaoDeAberturaDeConexaoInterceptor : DbConnectionInterceptor
        {
            public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(InterceptionResult.Suppress());
            }
        }

        private static AutoViaDbContext CriarDbComCaptura(CapturaDeComandosInterceptor captura)
        {
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql(ConexaoInacessivel)
                .AddInterceptors(new SupressaoDeAberturaDeConexaoInterceptor(), captura)
                .Options;
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.FINANCEIRO, false);

            return new AutoViaDbContext(opcoes, contexto);
        }

        [Fact]
        public async Task AtualizarAtrasosDaLocadoraAsync_LevaOFiltroDaEmpresaNoUpdate()
        {
            CapturaDeComandosInterceptor captura = new CapturaDeComandosInterceptor();

            using AutoViaDbContext db = CriarDbComCaptura(captura);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            int quantidade = await repositorio.AtualizarAtrasosDaLocadoraAsync(HojeDoTeste, AgoraDoTeste);

            Assert.Equal(0, quantidade);
            string comando = Assert.Single(captura.Comandos);
            Assert.Contains(FiltroEmpresa, comando);
        }

        [Fact]
        public async Task AtualizarAtrasosTodasLocadorasAsync_NaoLevaOFiltroDaEmpresaNoUpdate()
        {
            CapturaDeComandosInterceptor captura = new CapturaDeComandosInterceptor();

            using AutoViaDbContext db = CriarDbComCaptura(captura);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            int quantidade = await repositorio.AtualizarAtrasosTodasLocadorasAsync(HojeDoTeste, AgoraDoTeste);

            Assert.Equal(0, quantidade);
            string comando = Assert.Single(captura.Comandos);
            Assert.DoesNotContain(FiltroEmpresa, comando);
        }
    }
}
