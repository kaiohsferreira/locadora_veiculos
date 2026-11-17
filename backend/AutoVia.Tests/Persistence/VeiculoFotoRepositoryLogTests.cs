using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Log do VeiculoFotoRepository REAL, sem banco e sem rede: o AutoViaDbContext e descartado antes do uso, entao toda
    /// consulta e toda gravacao falham dentro do repositorio. Prova que cada metodo publico registra a constante da
    /// operacao (leitura ou gravacao) e relanca a excecao, como o padrao do projeto.
    /// </summary>
    public class VeiculoFotoRepositoryLogTests
    {
        private const int IdVeiculoDoTeste = 7;
        private const int IdVeiculoFotoDoTeste = 3;
        private const int IdEmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ChaveDoTeste = "veiculos/7/fotos/teste.jpg";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Repositorio real sobre um contexto ja descartado: qualquer uso do banco lanca ObjectDisposedException.</summary>
        private static VeiculoFotoRepository CriarRepositorioComContextoDescartado(FakeLogService log)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(IdEmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);
            db.Dispose();

            return new VeiculoFotoRepository(db, log);
        }

        private static VeiculoFoto CriarFotoDoTeste()
        {
            VeiculoFoto foto = new VeiculoFoto
            {
                IdVeiculo = IdVeiculoDoTeste,
                Ordem = 1,
                ChaveObjeto = ChaveDoTeste,
                IdFuncionarioEnvio = IdFuncionarioDoTeste
            };

            return foto;
        }

        /// <summary>
        /// Confere que o log tem um registro com a constante da operacao. Pode haver mais de um: a consulta interna
        /// (ConsultaFotoAtiva) tambem registra quando falha ao montar, e o metodo publico registra a propria falha.
        /// </summary>
        private static void AssertRegistrouConstante(FakeLogService log, string mensagemEsperada)
        {
            List<string> entradas = log.Snapshot();

            Assert.Contains(entradas, entrada => entrada.Contains(mensagemEsperada, StringComparison.Ordinal));
        }

        /// <summary>
        /// Confere que o ULTIMO registro e a constante da propria operacao publica: a consulta interna registra antes e o
        /// metodo publico, que relanca a falha, registra por ultimo. Sem isso, trocar a constante do metodo publico passaria,
        /// porque a consulta interna ja registra a mesma constante de leitura.
        /// </summary>
        private static void AssertUltimoRegistroEhConstante(FakeLogService log, string mensagemEsperada)
        {
            List<string> entradas = log.Snapshot();

            Assert.NotEmpty(entradas);
            Assert.Contains(mensagemEsperada, entradas[entradas.Count - 1], StringComparison.Ordinal);
        }

        [Fact]
        public async Task VeiculoAtivoExisteAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.VeiculoAtivoExisteAsync(IdVeiculoDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public async Task CountAtivasAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.CountAtivasAsync(IdVeiculoDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public async Task FindAtivasAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.FindAtivasAsync(IdVeiculoDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public async Task FindAtivaAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.FindAtivaAsync(IdVeiculoDoTeste, IdVeiculoFotoDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public void ConsultaVeiculoAtiva_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaVeiculoAtivo(IdVeiculoDoTeste));

            AssertRegistrouConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public void ConsultaFotosAtivasDoVeiculo_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaFotosAtivasDoVeiculo(IdVeiculoDoTeste));

            AssertRegistrouConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public void ConsultaFotoAtiva_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaFotoAtiva(IdVeiculoDoTeste, IdVeiculoFotoDoTeste));

            AssertRegistrouConstante(log, ConstantsMessage.ErroConsultarFotoVeiculo);
        }

        [Fact]
        public async Task InsertAsync_FalhaDeGravacao_RegistraGravarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.InsertAsync(CriarFotoDoTeste()));

            AssertRegistrouConstante(log, ConstantsMessage.ErroGravarFotoVeiculo);
        }

        [Fact]
        public async Task DesativarAsync_FalhaDeGravacao_RegistraGravarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.DesativarAsync(IdVeiculoDoTeste, IdVeiculoFotoDoTeste, IdFuncionarioDoTeste));

            AssertRegistrouConstante(log, ConstantsMessage.ErroGravarFotoVeiculo);
        }
    }
}
