using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do administrador inicial: nao faz nada sem CPF configurado, cria uma vez com
    /// hash (nunca a senha) e e idempotente. Configuracao invalida lanca excecao e o log nao
    /// recebe a senha.
    /// </summary>
    public class AdminInicialServiceTests
    {
        private const string CpfAdmin = "529.982.247-25";
        private const string CpfAdminDigitos = "52998224725";
        private const string SenhaAdmin = "SenhaAdmin#2026";

        private static AdminInicialOptions OpcoesValidas()
        {
            AdminInicialOptions opcoes = new AdminInicialOptions
            {
                Cpf = CpfAdmin,
                Nome = "Administrador Inicial",
                Matricula = "ADM-001",
                Senha = SenhaAdmin
            };

            return opcoes;
        }

        private static AdminInicialService CriarServico(FakeAutenticacaoRepository repositorio, AdminInicialOptions opcoes, FakeLogService log)
        {
            return new AdminInicialService(repositorio, new SenhaHasher(log), opcoes, log);
        }

        [Fact]
        public async Task SemCpfConfigurado_NaoCriaNada()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, new AdminInicialOptions(), log);

            bool criou = await servico.GarantirAdminInicialAsync();

            Assert.False(criou);
            Assert.Empty(repositorio.Funcionarios);
        }

        [Fact]
        public async Task CriaAdministrador_ComHashENaoGravaASenha()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, OpcoesValidas(), log);

            bool criou = await servico.GarantirAdminInicialAsync();

            Assert.True(criou);
            Funcionario administrador = Assert.Single(repositorio.Funcionarios);
            Assert.Equal(CpfAdminDigitos, administrador.Cpf);
            Assert.Equal(CargoFuncionario.ADMINISTRATIVO, administrador.Cargo);
            Assert.True(administrador.Ativo);
            Assert.StartsWith("v1.PBKDF2-SHA256.", administrador.SenhaHash);
            Assert.NotEqual(SenhaAdmin, administrador.SenhaHash);
            Assert.True(new SenhaHasher(log).Verify(SenhaAdmin, administrador.SenhaHash!));
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.InfoAdminInicialCriado));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(SenhaAdmin));
        }

        [Fact]
        public async Task Idempotente_SegundaExecucaoNaoCriaOutro()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, OpcoesValidas(), log);

            bool primeira = await servico.GarantirAdminInicialAsync();
            bool segunda = await servico.GarantirAdminInicialAsync();

            Assert.True(primeira);
            Assert.False(segunda);
            Assert.Single(repositorio.Funcionarios);
        }

        [Fact]
        public async Task JaExisteFuncionarioComOCpf_NaoCriaOutro()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarFuncionario(new Funcionario { Nome = "Antigo", Cpf = CpfAdminDigitos, Matricula = "X-1", Cargo = CargoFuncionario.GESTOR_FROTA, Ativo = false, SenhaHash = null });
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, OpcoesValidas(), log);

            bool criou = await servico.GarantirAdminInicialAsync();

            Assert.False(criou);
            Assert.Single(repositorio.Funcionarios);
        }

        [Fact]
        public async Task SenhaCurta_LancaExcecaoSemGravarENaoLogaASenha()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialOptions opcoes = OpcoesValidas();
            opcoes.Senha = "curta";
            AdminInicialService servico = CriarServico(repositorio, opcoes, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GarantirAdminInicialAsync());

            Assert.Equal(ConstantsMessage.ErroCriarAdminInicial, excecao.Message);
            Assert.Empty(repositorio.Funcionarios);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCriarAdminInicial));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("curta"));
        }

        [Fact]
        public async Task CpfInvalido_LancaExcecaoSemGravar()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialOptions opcoes = OpcoesValidas();
            opcoes.Cpf = "123.456.789-00";
            AdminInicialService servico = CriarServico(repositorio, opcoes, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GarantirAdminInicialAsync());

            Assert.Equal(ConstantsMessage.ErroCriarAdminInicial, excecao.Message);
            Assert.Empty(repositorio.Funcionarios);
        }

        [Fact]
        public async Task FalhaDoBanco_RegistraERelanca()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.ExcecaoAoBuscar = new InvalidOperationException("falha simulada do banco");
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, OpcoesValidas(), log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GarantirAdminInicialAsync());

            Assert.Equal("falha simulada do banco", excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCriarAdminInicial));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(SenhaAdmin));
        }

        [Fact]
        public async Task AdministradorDesativadoComOMesmoCpf_NaoEhRecriado()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            Funcionario desativado = new Funcionario { Nome = "Administrador antigo", Cpf = CpfAdminDigitos, Matricula = "ADM-ANTIGO", Cargo = CargoFuncionario.ADMINISTRATIVO, Ativo = false, SenhaHash = null };
            desativado.Disable();
            repositorio.AdicionarFuncionario(desativado);
            FakeLogService log = new FakeLogService();
            AdminInicialService servico = CriarServico(repositorio, OpcoesValidas(), log);

            bool criou = await servico.GarantirAdminInicialAsync();

            Assert.False(criou);
            Funcionario unico = Assert.Single(repositorio.Funcionarios);
            Assert.NotNull(unico.DisabledAt);
        }

        [Fact]
        public async Task NomeAcimaDoTamanhoDaColuna_LancaExcecaoSemGravar()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialOptions opcoes = OpcoesValidas();
            opcoes.Nome = new string('A', ConstantsValues.TamanhoMaximoNomeAdminInicial + 1);
            AdminInicialService servico = CriarServico(repositorio, opcoes, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GarantirAdminInicialAsync());

            Assert.Equal(ConstantsMessage.ErroCriarAdminInicial, excecao.Message);
            Assert.Empty(repositorio.Funcionarios);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCriarAdminInicial));
        }

        [Fact]
        public async Task MatriculaAcimaDoTamanhoDaColuna_LancaExcecaoSemGravar()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AdminInicialOptions opcoes = OpcoesValidas();
            opcoes.Matricula = new string('M', ConstantsValues.TamanhoMaximoMatriculaAdminInicial + 1);
            AdminInicialService servico = CriarServico(repositorio, opcoes, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GarantirAdminInicialAsync());

            Assert.Equal(ConstantsMessage.ErroCriarAdminInicial, excecao.Message);
            Assert.Empty(repositorio.Funcionarios);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCriarAdminInicial));
        }
    }
}
