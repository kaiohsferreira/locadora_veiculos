using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do SessaoService: conta ativa (cliente sem DisabledAt; funcionario tambem com Ativo),
    /// versao da sessao (sv) igual a atual, cache por usuario (inclusive o resultado negativo),
    /// Invalidar, usuario inexistente e falha do banco relancada sem vazar sv ou hash no log.
    /// Usa o FakeAutenticacaoRepository com um contador de consultas e IMemoryCache real.
    /// </summary>
    public class SessaoServiceTests
    {
        private const string CpfCliente = "52998224725";
        private const string CpfFuncionario = "11144477735";
        private const string HashSenhaA = "v1.PBKDF2-SHA256.600000.salt-a.hash-a";
        private const string HashSenhaB = "v1.PBKDF2-SHA256.600000.salt-b.hash-b";
        private const int IdInexistente = 999;

        private static readonly string ChaveDeTeste = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        private static TokenService CriarTokens(FakeLogService log)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = "AutoVia.Api",
                Audiencia = "AutoVia.Web",
                ExpiracaoMinutos = 120,
                Chave = ChaveDeTeste
            };

            return new TokenService(opcoes, log);
        }

        private static string VersaoDe(string senhaHash)
        {
            return CriarTokens(new FakeLogService()).CalcularVersaoSessao(senhaHash);
        }

        private static SessaoService CriarServico(IAutenticacaoRepository repositorio, FakeLogService log)
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            SessaoOptions opcoes = new SessaoOptions { CacheSegundos = 60 };

            return new SessaoService(repositorio, CriarTokens(log), cache, opcoes, log);
        }

        private static Cliente ClienteComHash(string? senhaHash, bool desativado = false)
        {
            Cliente cliente = new Cliente { Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = senhaHash };

            if (desativado)
            {
                cliente.Disable();
            }

            return cliente;
        }

        private static Funcionario FuncionarioComHash(string senhaHash, bool ativo)
        {
            return new Funcionario { Nome = "Ana Funcionaria", Cpf = CpfFuncionario, Matricula = "F-001", Cargo = CargoFuncionario.ATENDENTE, Ativo = ativo, SenhaHash = senhaHash };
        }

        [Fact]
        public async Task ClienteAtivoComVersaoAtual_SessaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaA);
            interno.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, VersaoDe(HashSenhaA));

            Assert.True(valida);
        }

        [Fact]
        public async Task ClienteDesativado_NaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaA, desativado: true);
            interno.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, VersaoDe(HashSenhaA));

            Assert.False(valida);
        }

        [Fact]
        public async Task ClienteSemSenhaCadastrada_NaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(null);
            interno.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, VersaoDe(HashSenhaA));

            Assert.False(valida);
        }

        [Fact]
        public async Task FuncionarioAtivoComVersaoAtual_SessaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Funcionario funcionario = FuncionarioComHash(HashSenhaA, ativo: true);
            interno.AdicionarFuncionario(funcionario);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, VersaoDe(HashSenhaA));

            Assert.True(valida);
        }

        [Fact]
        public async Task FuncionarioComAtivoFalso_NaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Funcionario funcionario = FuncionarioComHash(HashSenhaA, ativo: false);
            interno.AdicionarFuncionario(funcionario);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, VersaoDe(HashSenhaA));

            Assert.False(valida);
        }

        [Fact]
        public async Task VersaoDivergente_SenhaTrocada_NaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaB);
            interno.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, VersaoDe(HashSenhaA));

            Assert.False(valida);
        }

        [Fact]
        public async Task TokenSemVersaoDeSessao_NaoValida_SemConsultarOBanco()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaA);
            interno.AdicionarCliente(cliente);
            RepositorioContado contado = new RepositorioContado(interno);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(contado, log);

            bool semVersao = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, null);
            bool versaoVazia = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, string.Empty);

            Assert.False(semVersao);
            Assert.False(versaoVazia);
            Assert.Equal(0, contado.ConsultasCliente);
        }

        [Fact]
        public async Task UsuarioInexistente_NaoValida()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);

            bool valida = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, IdInexistente, VersaoDe(HashSenhaA));

            Assert.False(valida);
        }

        [Fact]
        public async Task SegundaChamada_UsaCache_NaoConsultaORepositorio()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaA);
            interno.AdicionarCliente(cliente);
            RepositorioContado contado = new RepositorioContado(interno);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(contado, log);
            string versao = VersaoDe(HashSenhaA);

            bool primeira = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, versao);
            bool segunda = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, versao);

            Assert.True(primeira);
            Assert.True(segunda);
            Assert.Equal(1, contado.ConsultasCliente);
        }

        [Fact]
        public async Task ResultadoNegativo_TambemFicaEmCache()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            RepositorioContado contado = new RepositorioContado(interno);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(contado, log);
            string versao = VersaoDe(HashSenhaA);

            bool primeira = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, IdInexistente, versao);
            bool segunda = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, IdInexistente, versao);

            Assert.False(primeira);
            Assert.False(segunda);
            Assert.Equal(1, contado.ConsultasCliente);
        }

        [Fact]
        public async Task Invalidar_ProximaChamadaConsultaDeNovo()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            Cliente cliente = ClienteComHash(HashSenhaA);
            interno.AdicionarCliente(cliente);
            RepositorioContado contado = new RepositorioContado(interno);
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(contado, log);
            string versao = VersaoDe(HashSenhaA);

            await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, versao);
            cliente.Disable();
            servico.Invalidar(PerfilUsuario.CLIENTE, cliente.IdCliente);
            bool depois = await servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, cliente.IdCliente, versao);

            Assert.False(depois);
            Assert.Equal(2, contado.ConsultasCliente);
        }

        [Fact]
        public async Task FalhaDoBanco_RelancaERegistraSemVazarSvNemHash()
        {
            FakeAutenticacaoRepository interno = new FakeAutenticacaoRepository();
            interno.ExcecaoAoBuscar = new InvalidOperationException("falha simulada do banco");
            FakeLogService log = new FakeLogService();
            SessaoService servico = CriarServico(new RepositorioContado(interno), log);
            string versao = VersaoDe(HashSenhaA);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.SessaoValidaAsync(PerfilUsuario.CLIENTE, 1, versao));

            Assert.Equal("falha simulada do banco", excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroValidarSessao));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(versao));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(HashSenhaA));
        }

        /// <summary>Repositorio que delega ao fake e conta as consultas por id, para provar o cache.</summary>
        private sealed class RepositorioContado : IAutenticacaoRepository
        {
            private readonly FakeAutenticacaoRepository _interno;

            public RepositorioContado(FakeAutenticacaoRepository interno)
            {
                _interno = interno;
            }

            public int ConsultasCliente { get; private set; }

            public int ConsultasFuncionario { get; private set; }

            public Task<Cliente?> FindClienteByCpfAsync(string cpf)
            {
                return _interno.FindClienteByCpfAsync(cpf);
            }

            public Task<Cliente?> FindClienteByIdAsync(int idCliente)
            {
                ConsultasCliente++;

                return _interno.FindClienteByIdAsync(idCliente);
            }

            public Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf)
            {
                return _interno.FindFuncionarioByCpfAsync(cpf);
            }

            public Task<Funcionario?> FindFuncionarioByIdAsync(int idFuncionario)
            {
                ConsultasFuncionario++;

                return _interno.FindFuncionarioByIdAsync(idFuncionario);
            }

            public Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
            {
                return _interno.ExistsFuncionarioByCpfAsync(cpf);
            }

            public Task<bool> ExistsFuncionarioByCpfWithDisabledsAsync(string cpf)
            {
                return _interno.ExistsFuncionarioByCpfWithDisabledsAsync(cpf);
            }

            public Task<Funcionario> InsertFuncionarioAsync(Funcionario funcionario)
            {
                return _interno.InsertFuncionarioAsync(funcionario);
            }
        }
    }
}
