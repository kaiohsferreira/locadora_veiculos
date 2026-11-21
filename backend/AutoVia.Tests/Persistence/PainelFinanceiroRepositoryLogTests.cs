using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Log do repositorio do painel financeiro (painel-financeiro v1): cada indicador registra a sua propria mensagem de log.
    /// Sem banco: a conexao aponta para uma porta sem servico (Port=1), entao a consulta falha e o catch precisa registrar.
    /// </summary>
    public class PainelFinanceiroRepositoryLogTests
    {
        private const string ConexaoInacessivel = "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste";
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private static readonly DateOnly DeDoTeste = new DateOnly(2026, 11, 1);
        private static readonly DateOnly AteDoTeste = new DateOnly(2026, 11, 30);

        [Fact]
        public async Task ObterIndicadorTicketMedioAsync_FalhaDeConsulta_RegistraOLogDoIndicadorFinanceiro()
        {
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql(ConexaoInacessivel)
                .Options;
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.FINANCEIRO, false);
            FakeLogService log = new FakeLogService();

            using AutoViaDbContext db = new AutoViaDbContext(opcoes, contexto);
            PainelFinanceiroRepository repositorio = new PainelFinanceiroRepository(db, log);

            await Assert.ThrowsAnyAsync<Exception>(() => repositorio.ObterIndicadorTicketMedioAsync(DeDoTeste, AteDoTeste));

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLogPainelFinanceiro, StringComparison.Ordinal));
        }
    }
}
