using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Security.Cryptography;
using System.Text.Json;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do fluxo de login: credenciais validas, normalizacao do CPF, regra de Ativo do
    /// funcionario, mesma mensagem em toda recusa, simulacao quando o CPF nao existe e falha
    /// do banco relancada sem virar 401. Senha e hash nunca aparecem no log.
    /// </summary>
    public class AutenticacaoServiceTests
    {
        private const string CpfCliente = "52998224725";
        private const string CpfCliente_Mascarado = "529.982.247-25";
        private const string SenhaCliente = "SenhaCliente#2026";
        private const string CpfFuncionario = "11144477735";
        private const string SenhaFuncionario = "SenhaFunc#2026";
        private const string PadraoIsoSemFuso = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$";

        private static readonly string ChaveDeTeste = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        private static string GerarHash(string senha)
        {
            return new SenhaHasher(new FakeLogService()).Hash(senha);
        }

        private static AutenticacaoService CriarServico(FakeAutenticacaoRepository repositorio, ISenhaHasher hasher, FakeLogService log)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = "AutoVia.Api",
                Audiencia = "AutoVia.Web",
                ExpiracaoMinutos = 120,
                Chave = ChaveDeTeste
            };

            TokenService tokens = new TokenService(opcoes, log);

            return new AutenticacaoService(repositorio, hasher, tokens, log);
        }

        private static LoginVO Login(PerfilUsuario? perfil, string cpf, string senha)
        {
            return new LoginVO { Perfil = perfil, Cpf = cpf, Senha = senha };
        }

        [Fact]
        public async Task Cliente_CredenciaisValidasComCpfMascarado_DevolveTokenEStatusDoCadastro()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            Cliente cliente = new Cliente { Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.PENDENTE };
            repositorio.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            ReturnLoginVO retorno = await servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente_Mascarado, SenhaCliente));

            Assert.False(string.IsNullOrWhiteSpace(retorno.Token));
            Assert.Equal(ConstantsValues.TipoTokenBearer, retorno.TipoToken);
            Assert.Equal(PerfilUsuario.CLIENTE, retorno.Perfil);
            Assert.Equal(cliente.IdCliente, retorno.IdUsuario);
            Assert.Equal("Maria Cliente", retorno.Nome);
            Assert.Equal(StatusCadastroCliente.PENDENTE, retorno.StatusCadastro);
            Assert.Null(retorno.Cargo);
            Assert.False(string.IsNullOrWhiteSpace(retorno.ExpiraEmStr));
        }

        [Fact]
        public async Task Login_ExpiraEmSaiSemFusoNoJson()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.PENDENTE });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            ReturnLoginVO retorno = await servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente));

            Assert.Equal(DateTimeKind.Unspecified, retorno.ExpiraEm.Kind);
            JsonSerializerOptions opcoes = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            using JsonDocument documento = JsonDocument.Parse(JsonSerializer.Serialize(retorno, opcoes));
            string expiraEmJson = documento.RootElement.GetProperty("expiraEm").GetString()!;
            Assert.Matches(PadraoIsoSemFuso, expiraEmJson);
        }

        [Fact]
        public async Task Cliente_CadastroRecusado_AindaEntraComStatusInformado()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Joao", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.RECUSADO });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            ReturnLoginVO retorno = await servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente));

            Assert.Equal(StatusCadastroCliente.RECUSADO, retorno.StatusCadastro);
        }

        [Fact]
        public async Task Funcionario_Ativo_DevolveCargoEIdDoFuncionario()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            Funcionario funcionario = new Funcionario { Nome = "Ana Funcionaria", Cpf = CpfFuncionario, Matricula = "F-001", Cargo = CargoFuncionario.VENDEDOR, Ativo = true, SenhaHash = GerarHash(SenhaFuncionario) };
            repositorio.AdicionarFuncionario(funcionario);
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            ReturnLoginVO retorno = await servico.LoginAsync(Login(PerfilUsuario.FUNCIONARIO, CpfFuncionario, SenhaFuncionario));

            Assert.Equal(PerfilUsuario.FUNCIONARIO, retorno.Perfil);
            Assert.Equal(funcionario.IdFuncionario, retorno.IdUsuario);
            Assert.Equal(CargoFuncionario.VENDEDOR, retorno.Cargo);
            Assert.Null(retorno.StatusCadastro);
        }

        [Fact]
        public async Task Funcionario_Inativo_ComSenhaCorreta_Recusa()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarFuncionario(new Funcionario { Nome = "Ana", Cpf = CpfFuncionario, Matricula = "F-002", Cargo = CargoFuncionario.ATENDENTE, Ativo = false, SenhaHash = GerarHash(SenhaFuncionario) });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(PerfilUsuario.FUNCIONARIO, CpfFuncionario, SenhaFuncionario)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        [Fact]
        public async Task CpfNaoCadastrado_RecusaComMensagemPadraoESimulaVerificacao()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            SenhaHasherEspiao espiao = new SenhaHasherEspiao(new SenhaHasher(log));
            AutenticacaoService servico = CriarServico(repositorio, espiao, log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
            Assert.Equal(1, espiao.Simulacoes);
            Assert.Equal(0, espiao.Verificacoes);
        }

        [Fact]
        public async Task SenhaErrada_RecusaComAMesmaMensagem()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente) });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, "senha-errada")));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        [Fact]
        public async Task ContaSemHashDeSenha_RecusaESimulaSemVerificar()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria", Cpf = CpfCliente, SenhaHash = null });
            FakeLogService log = new FakeLogService();
            SenhaHasherEspiao espiao = new SenhaHasherEspiao(new SenhaHasher(log));
            AutenticacaoService servico = CriarServico(repositorio, espiao, log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
            Assert.Equal(1, espiao.Simulacoes);
            Assert.Equal(0, espiao.Verificacoes);
        }

        [Fact]
        public async Task ClienteDesativado_TratadoComoInexistente()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            Cliente cliente = new Cliente { Nome = "Maria", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente) };
            cliente.Disable();
            repositorio.AdicionarCliente(cliente);
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        [Fact]
        public async Task PerfilAusente_RecusaComAMesmaMensagem()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.LoginAsync(Login(null, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        [Fact]
        public async Task FalhaDoBanco_NaoVira401_RegistraErroSemSenhaERelanca()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.ExcecaoAoBuscar = new InvalidOperationException("falha simulada do banco");
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal("falha simulada do banco", excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroAutenticar));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(SenhaCliente));
        }

        [Fact]
        public async Task Login_SemLembrar_ExpiraEm120Minutos()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.APROVADO });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);
            DateTime antes = DateTime.Now;

            ReturnLoginVO retorno = await servico.LoginAsync(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente));
            DateTime depois = DateTime.Now;

            Assert.InRange(retorno.ExpiraEm, antes.AddMinutes(120).AddSeconds(-5), depois.AddMinutes(120).AddSeconds(5));
        }

        [Fact]
        public async Task Login_ComLembrar_ExpiraEm30DiasCorridos()
        {
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.APROVADO });
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(repositorio, new SenhaHasher(log), log);
            DateTime antes = DateTime.Now;

            ReturnLoginVO retorno = await servico.LoginAsync(new LoginVO { Perfil = PerfilUsuario.CLIENTE, Cpf = CpfCliente, Senha = SenhaCliente, Lembrar = true });
            DateTime depois = DateTime.Now;

            Assert.InRange(retorno.ExpiraEm, antes.AddDays(30).AddSeconds(-5), depois.AddDays(30).AddSeconds(5));
        }

        [Fact]
        public void EmitirSessaoCliente_ComLembrar_ExpiraEm30DiasEMantemStatusDoCadastro()
        {
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(new FakeAutenticacaoRepository(), new SenhaHasher(log), log);
            Cliente cliente = new Cliente { IdCliente = 5, Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = GerarHash(SenhaCliente), StatusCadastro = StatusCadastroCliente.PENDENTE };
            DateTime antes = DateTime.Now;

            ReturnLoginVO retorno = servico.EmitirSessaoCliente(cliente, true);
            DateTime depois = DateTime.Now;

            Assert.Equal(PerfilUsuario.CLIENTE, retorno.Perfil);
            Assert.Equal(5, retorno.IdUsuario);
            Assert.Equal(StatusCadastroCliente.PENDENTE, retorno.StatusCadastro);
            Assert.InRange(retorno.ExpiraEm, antes.AddDays(30).AddSeconds(-5), depois.AddDays(30).AddSeconds(5));
        }

        [Fact]
        public void EmitirSessaoCliente_SemSenhaCadastrada_RecusaComAMesmaMensagem()
        {
            FakeLogService log = new FakeLogService();
            AutenticacaoService servico = CriarServico(new FakeAutenticacaoRepository(), new SenhaHasher(log), log);
            Cliente cliente = new Cliente { IdCliente = 5, Nome = "Maria Cliente", Cpf = CpfCliente, SenhaHash = null };

            UnauthorizedException excecao = Assert.Throws<UnauthorizedException>(() => servico.EmitirSessaoCliente(cliente, false));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        /// <summary>Espiao do ISenhaHasher: conta as chamadas de verificacao e de simulacao, sem o custo do PBKDF2 na simulacao.</summary>
        private sealed class SenhaHasherEspiao : ISenhaHasher
        {
            private readonly SenhaHasher _real;

            public SenhaHasherEspiao(SenhaHasher real)
            {
                _real = real;
            }

            public int Verificacoes { get; private set; }

            public int Simulacoes { get; private set; }

            public string Hash(string senha)
            {
                return _real.Hash(senha);
            }

            public bool Verify(string senha, string senhaHash)
            {
                Verificacoes++;
                return _real.Verify(senha, senhaHash);
            }

            public void SimularVerificacao(string senha)
            {
                Simulacoes++;
            }
        }
    }
}
