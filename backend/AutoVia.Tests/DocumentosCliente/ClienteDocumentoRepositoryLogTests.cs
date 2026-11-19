using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// Log do ClienteDocumentoRepository REAL, sem banco e sem rede: o contexto e descartado antes do uso, entao toda consulta e
    /// toda gravacao falham dentro do repositorio. Prova que cada metodo registra a constante da operacao (leitura ou gravacao)
    /// e relanca a excecao, como o padrao do projeto.
    /// </summary>
    public sealed class ClienteDocumentoRepositoryLogTests
    {
        private const int IdClienteDoTeste = 7;
        private const string ChaveDoTeste = "clientes/7/documentos/teste.pdf";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Repositorio real sobre um contexto ja descartado: qualquer uso do banco lanca ObjectDisposedException.</summary>
        private static ClienteDocumentoRepository CriarRepositorioComContextoDescartado(FakeLogService log)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);
            db.Dispose();

            return new ClienteDocumentoRepository(db, log);
        }

        private static ClienteDocumento CriarDocumentoDoTeste()
        {
            ClienteDocumento documento = new ClienteDocumento
            {
                IdCliente = IdClienteDoTeste,
                Tipo = TipoDocumentoCliente.CNH,
                ChaveObjeto = ChaveDoTeste,
                NomeArquivo = "cnh.pdf",
                TipoConteudo = ConstantsValues.TipoConteudoPdf,
                TamanhoBytes = 1
            };

            return documento;
        }

        private static void AssertUltimoRegistroEhConstante(FakeLogService log, string mensagemEsperada)
        {
            List<string> entradas = log.Snapshot();

            Assert.NotEmpty(entradas);
            Assert.Contains(mensagemEsperada, entradas[entradas.Count - 1], StringComparison.Ordinal);
        }

        [Fact]
        public async Task ClienteAtivoAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.ClienteAtivoAsync(IdClienteDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }

        [Fact]
        public async Task CountEnviadosDesdeWithDisabledsAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.CountEnviadosDesdeWithDisabledsAsync(IdClienteDoTeste, DateTime.Now));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }

        [Fact]
        public async Task FindAtivosDoClienteAsync_FalhaDeLeitura_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.FindAtivosDoClienteAsync(IdClienteDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }

        [Fact]
        public async Task SubstituirAsync_FalhaDeGravacao_RegistraGravarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => repositorio.SubstituirAsync(CriarDocumentoDoTeste()));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroGravarClienteDocumento);
        }

        [Fact]
        public void ConsultaEnviosDesdeWithDisableds_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaEnviosDesdeWithDisableds(IdClienteDoTeste, DateTime.Now));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }

        [Fact]
        public void ConsultaAtivosDoCliente_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaAtivosDoCliente(IdClienteDoTeste));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }

        [Fact]
        public void ConsultaAtivosDoClienteETipo_FalhaAoMontarAConsulta_RegistraConsultarERelancaAExcecao()
        {
            FakeLogService log = new FakeLogService();
            ClienteDocumentoRepository repositorio = CriarRepositorioComContextoDescartado(log);

            Assert.Throws<ObjectDisposedException>(() => repositorio.ConsultaAtivosDoClienteETipo(IdClienteDoTeste, TipoDocumentoCliente.CNH));

            AssertUltimoRegistroEhConstante(log, ConstantsMessage.ErroConsultarClienteDocumento);
        }
    }
}
