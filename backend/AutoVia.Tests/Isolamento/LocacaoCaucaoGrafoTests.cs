using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoVia.Tests.Isolamento
{
    /// <summary>
    /// Gravacao conjunta da caucao (contrato caucao-retirada v1.1, CE-2) provada SEM banco: a locacao montada pela regra pura
    /// RegrasLocacao.CriarLocacaoDeAbertura, depois do Add que o LocacaoRepository faz, deixa no ChangeTracker a locacao e a linha
    /// CAUCAO no estado Added, ligadas pela navegacao Locacao.Pagamentos (o EF grava as duas no mesmo SaveChanges). Nada e salvo
    /// nem lido: o contexto nao abre conexao. A transacao e as travas so sao provadas no roteiro manual (PostgreSQL).
    /// </summary>
    public class LocacaoCaucaoGrafoTests
    {
        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDb()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(1, 9, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static Locacao Montar(CaucaoAberturaRegistro? caucao)
        {
            Reserva reserva = new Reserva
            {
                IdReserva = 42,
                IdCliente = 30,
                IdVeiculo = 40,
                DataDevolucaoPrevista = new DateTime(2026, 11, 27, 9, 0, 0),
                Status = StatusReserva.ATIVA
            };
            Veiculo veiculo = new Veiculo { IdVeiculo = 40, KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO };

            return RegrasLocacao.CriarLocacaoDeAbertura(reserva, veiculo, 20, new DateTime(2026, 11, 24, 10, 0, 0), new DateTime(2026, 11, 24, 10, 0, 1), caucao);
        }

        [Fact]
        public void ComCaucao_DepoisDoAdd_UmaLocacaoEUmaCaucaoAdded_LigadasPelaNavegacao()
        {
            using AutoViaDbContext db = CriarDb();
            Locacao locacao = Montar(new CaucaoAberturaRegistro(1200.00m, "FAKE-CAU-0123456789ab", "CAUCAO-RES-42"));

            db.Locacoes.Add(locacao);

            List<EntityEntry> entradas = db.ChangeTracker.Entries().ToList();
            Assert.Equal(2, entradas.Count);
            Assert.All(entradas, (EntityEntry entrada) => Assert.Equal(EntityState.Added, entrada.State));

            EntityEntry<Locacao> entradaLocacao = Assert.Single(db.ChangeTracker.Entries<Locacao>());
            EntityEntry<Pagamento> entradaCaucao = Assert.Single(db.ChangeTracker.Entries<Pagamento>());
            Pagamento caucao = entradaCaucao.Entity;

            Assert.Same(locacao, entradaLocacao.Entity);
            Assert.Equal(TipoPagamento.CAUCAO, caucao.Tipo);
            Assert.Equal(1200.00m, caucao.Valor);
            Assert.Null(caucao.IdVenda);
            Assert.Contains(caucao, locacao.Pagamentos);
            Assert.Same(locacao, caucao.Locacao);
            Assert.Equal(entradaLocacao.Property((Locacao x) => x.IdLocacao).CurrentValue, entradaCaucao.Property((Pagamento x) => x.IdLocacao).CurrentValue);
            Assert.Equal(1200.00m, locacao.ValorCaucao);
        }

        [Fact]
        public void SemCaucao_DepoisDoAdd_SoALocacao()
        {
            using AutoViaDbContext db = CriarDb();
            Locacao locacao = Montar(null);

            db.Locacoes.Add(locacao);

            EntityEntry entrada = Assert.Single(db.ChangeTracker.Entries());
            Assert.Same(locacao, entrada.Entity);
            Assert.Equal(EntityState.Added, entrada.State);
            Assert.Empty(db.ChangeTracker.Entries<Pagamento>());
            Assert.Equal(0m, locacao.ValorCaucao);
        }
    }
}
