using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using VinculoClienteLocadora = AutoVia.Core.Entities.PgSql.ClienteLocadora;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Testes do ClienteLocadoraService (T-BE-06): as seis situacoes de ExigirCadastroAprovadoAsync (com a locadora
    /// inexistente, inativa e desativada), a corrida do INSERT (menor id vale, a mais nova se desativa), a decisao da
    /// locadora (transicoes, verificador, data, justificativa), o escopo do contexto e as listagens de "minhas locadoras".
    /// O contexto e o ContextoEmpresa real; o repositorio e o fake com o mesmo escopo do filtro "Empresa".
    /// </summary>
    public class ClienteLocadoraServiceTests
    {
        private const int EmpresaUm = 1;
        private const int EmpresaDois = 2;
        private const int ClienteCinco = 5;
        private const int ClienteSeis = 6;
        private const int FuncionarioNove = 9;
        private const string NomeLocadoraUm = "Locadora Um";
        private const string NomeLocadoraDois = "Locadora Dois";
        private const string JustificativaTeste = "Documento ilegível";
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 14, 10, 0, 0);

        /// <summary>Servico real com o repositorio fake, o contexto informado, o relogio fixo e o log em memoria.</summary>
        private sealed class Cenario
        {
            public Cenario(ArmazenamentoClienteLocadora armazenamento, FakeClienteLocadoraRepository repositorio, ClienteLocadoraService servico, FakeLogService log)
            {
                Armazenamento = armazenamento;
                Repositorio = repositorio;
                Servico = servico;
                Log = log;
            }

            public ArmazenamentoClienteLocadora Armazenamento { get; }

            public FakeClienteLocadoraRepository Repositorio { get; }

            public ClienteLocadoraService Servico { get; }

            /// <summary>Log em memoria do cenario: o teto de pedidos nao grava log (e um caso esperado, nao erro).</summary>
            public FakeLogService Log { get; }
        }

        /// <summary>Cenario com o teto padrao (5), ou com o valor informado na secao Clientes (zero ou negativo cai no padrao).</summary>
        private static Cenario CriarCenario(Action<ContextoEmpresa> definirContexto, int? limitePedidos = null)
        {
            FakeLogService log = new FakeLogService();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            definirContexto(contexto);

            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = EmpresaUm, NomeFantasia = NomeLocadoraUm, Ativo = true });
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = EmpresaDois, NomeFantasia = NomeLocadoraDois, Ativo = true });

            ClientesOptions clientesOptions = new ClientesOptions();

            if (limitePedidos.HasValue)
            {
                clientesOptions.LimitePedidosAprovacaoPorDia = limitePedidos.Value;
            }

            FakeClienteLocadoraRepository repositorio = new FakeClienteLocadoraRepository(contexto, armazenamento);
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClienteLocadoraProfile>(), NullLoggerFactory.Instance).CreateMapper();
            ClienteLocadoraService servico = new ClienteLocadoraService(repositorio, contexto, new FakeRelogio(AgoraFixo), mapper, log, clientesOptions);

            return new Cenario(armazenamento, repositorio, servico, log);
        }

        private static Action<ContextoEmpresa> ContextoCliente(int idCliente)
        {
            return contexto => contexto.DefinirCliente(idCliente);
        }

        private static Action<ContextoEmpresa> ContextoFuncionario(int idEmpresa, CargoFuncionario cargo)
        {
            return contexto => contexto.Definir(idEmpresa, FuncionarioNove, cargo, false);
        }

        // --- ExigirCadastroAprovadoAsync: as seis situacoes do contrato ---

        [Fact]
        public async Task Exigir_CadastroAprovado_SegueSemExcecaoESemCriarLinha()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.APROVADO);

            await cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm);

            Assert.Single(cenario.Repositorio.Armazenados);
        }

        [Fact]
        public async Task Exigir_SemVinculo_ProprioCliente_GravaPendenteEDevolve409EmAnalise()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadoraUm), excecao.Message);
        }

        /// <summary>O PENDENTE fica gravado mesmo com a excecao 409 que o chamador recebe em seguida.</summary>
        [Fact]
        public async Task Exigir_SemVinculo_PendenteFicaGravadoMesmoComA409()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            VinculoClienteLocadora gravado = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.PENDENTE, gravado.Status);
            Assert.Equal(ClienteCinco, gravado.IdCliente);
            Assert.Equal(EmpresaUm, gravado.IdEmpresa);
            Assert.Null(gravado.DisabledAt);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task Exigir_SemVinculo_FuncionarioDaLocadora_Devolve409SemCriarNada(CargoFuncionario cargo)
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, cargo));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(ConstantsMessage.ErroReservaCadastroNaoAprovado, excecao.Message);
            Assert.Empty(cenario.Repositorio.Armazenados);
        }

        [Fact]
        public async Task Exigir_SemVinculo_OutroCliente_Devolve409SemCriarNada()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteSeis));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(ConstantsMessage.ErroReservaCadastroNaoAprovado, excecao.Message);
            Assert.Empty(cenario.Repositorio.Armazenados);
        }

        [Fact]
        public async Task Exigir_Pendente_Devolve409EmAnaliseSemLinhaNova()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadoraUm), excecao.Message);
            Assert.Single(cenario.Repositorio.Armazenados);
        }

        [Fact]
        public async Task Exigir_Recusado_Devolve409RecusadoSemLinhaNova()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.RECUSADO, null, JustificativaTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroRecusado, NomeLocadoraUm), excecao.Message);
            Assert.Single(cenario.Repositorio.Armazenados);
        }

        /// <summary>Locadora inexistente, inativa ou desativada: 404, sem criar vinculo.</summary>
        [Theory]
        [InlineData("inexistente")]
        [InlineData("inativa")]
        [InlineData("desativada")]
        public async Task Exigir_LocadoraInexistenteInativaOuDesativada_Devolve404SemCriarVinculo(string caso)
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            int idEmpresa = EmpresaUm;

            if (caso == "inexistente")
            {
                idEmpresa = 99;
            }
            else if (caso == "inativa")
            {
                cenario.Armazenamento.Empresas[0].Ativo = false;
            }
            else
            {
                cenario.Armazenamento.Empresas[0].DisabledAt = DateTime.Now;
            }

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, idEmpresa));

            Assert.Empty(cenario.Repositorio.Armazenados);
        }

        /// <summary>
        /// Cliente desativado (ou inexistente, que o banco trata do mesmo jeito) sem vinculo: a trava do teto responde 404 com a
        /// mensagem de cliente inexistente, antes de contar e sem gravar nada. A trava do cliente e liberada no finally.
        /// </summary>
        [Fact]
        public async Task Exigir_ClienteDesativadoSemVinculo_Devolve404SemContarNemGravar()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.ClientesDesativados.Add(ClienteCinco);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
            Assert.Empty(cenario.Repositorio.Armazenados);
            Assert.Equal(0, cenario.Repositorio.ChamadasDeContagem);
            Assert.Equal(1, cenario.Armazenamento.TravasDoCliente[ClienteCinco].CurrentCount);
        }

        // --- Corrida do INSERT: duplicata usa o menor id e a mais nova se desativa ---

        [Fact]
        public async Task Exigir_Corrida_ConcorrentePendenteComMenorId_ValeEAMaisNovaSeDesativa()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Repositorio.AoInserir = armazenamento => armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadoraUm), excecao.Message);
            List<VinculoClienteLocadora> linhas = cenario.Repositorio.Armazenados.ToList();
            Assert.Equal(2, linhas.Count);
            Assert.Null(linhas[0].DisabledAt);
            Assert.NotNull(linhas[1].DisabledAt);
            Assert.Equal(1, cenario.Repositorio.ChamadasDeDesativar);
        }

        [Fact]
        public async Task Exigir_Corrida_ConcorrenteAprovadoComMenorId_ReservaSegue()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Repositorio.AoInserir = armazenamento => armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.APROVADO);

            await cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm);

            List<VinculoClienteLocadora> linhas = cenario.Repositorio.Armazenados.ToList();
            Assert.Null(linhas[0].DisabledAt);
            Assert.Equal(StatusCadastroCliente.APROVADO, linhas[0].Status);
            Assert.NotNull(linhas[1].DisabledAt);
        }

        // --- DecidirAsync: transicoes, verificador, data e justificativa ---

        [Fact]
        public async Task Decidir_Aprovado_GravaVerificadorEDataEAnulaAJustificativa()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            await cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO, Justificativa = "ignorada" });

            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.APROVADO, linha.Status);
            Assert.Null(linha.JustificativaRecusa);
            Assert.Equal(FuncionarioNove, linha.IdFuncionarioVerificador);
            Assert.Equal(AgoraFixo, linha.DataVerificacao);
        }

        [Fact]
        public async Task Decidir_Recusado_GravaJustificativaAparadaComVerificadorEData()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ADMINISTRADOR));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            await cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = "  " + JustificativaTeste + "  " });

            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.RECUSADO, linha.Status);
            Assert.Equal(JustificativaTeste, linha.JustificativaRecusa);
            Assert.Equal(FuncionarioNove, linha.IdFuncionarioVerificador);
            Assert.Equal(AgoraFixo, linha.DataVerificacao);
        }

        [Fact]
        public async Task Decidir_ClienteDeOutraLocadora_Devolve404SemAlterarNada()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaDois, StatusCadastroCliente.PENDENTE);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO }));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.PENDENTE, linha.Status);
            Assert.Null(linha.IdFuncionarioVerificador);
        }

        [Fact]
        public async Task Decidir_ClienteInexistenteNaLocadora_Devolve404ComAMesmaMensagem()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO }));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
        }

        /// <summary>Recusa sem justificativa (nula ou em branco) e barrada no SERVICO com 400 tipado (Q2). Nada e gravado.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task Decidir_RecusadoSemJustificativa_Barra400NoServico(string? justificativa)
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = justificativa }));

            Assert.Equal(ConstantsMessage.ErroVerificacaoJustificativaObrigatoria, excecao.Errors.Single().ErrorMessage);
            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.PENDENTE, linha.Status);
            Assert.Null(linha.JustificativaRecusa);
        }

        /// <summary>Q4: a data de solicitacao do pedido novo vem do IRelogio (relogio fixo), nao do DateTime.Now do repositorio.</summary>
        [Fact]
        public async Task Exigir_PedidoNovo_DataSolicitacaoVemDoRelogioFixo()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, EmpresaUm));

            VinculoClienteLocadora gravado = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(AgoraFixo, gravado.DataSolicitacao);
        }

        /// <summary>Cliente desativado com vinculo ativo nao e decidido: 404 com a mensagem de inexistente, nada gravado (C3).</summary>
        [Fact]
        public async Task Decidir_ClienteDesativadoComVinculoAtivo_Devolve404SemAlterarNada()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.ClientesDesativados.Add(ClienteCinco);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO }));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.PENDENTE, linha.Status);
            Assert.Null(linha.IdFuncionarioVerificador);
        }

        [Fact]
        public async Task Decidir_JaAprovado_Devolve409SemAlterarNada()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.APROVADO);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = JustificativaTeste }));

            Assert.Equal(ConstantsMessage.ErroVerificacaoJaAprovado, excecao.Message);
            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.APROVADO, linha.Status);
            Assert.Null(linha.IdFuncionarioVerificador);
        }

        [Fact]
        public async Task Decidir_RecusadoParaAprovado_Permitido()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.RECUSADO, null, JustificativaTeste);

            await cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO });

            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.APROVADO, linha.Status);
            Assert.Null(linha.JustificativaRecusa);
        }

        [Fact]
        public async Task Decidir_RecusadoParaRecusado_TrocaAJustificativa()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.RECUSADO, null, "Motivo antigo");

            await cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = JustificativaTeste });

            VinculoClienteLocadora linha = Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.RECUSADO, linha.Status);
            Assert.Equal(JustificativaTeste, linha.JustificativaRecusa);
        }

        /// <summary>A decisao grava em todas as linhas ativas do par; a linha desativada fica como esta.</summary>
        [Fact]
        public async Task Decidir_AtualizaTodasAsLinhasAtivasDoPar_SemMexerNaDesativada()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE, DateTime.Now);

            await cenario.Servico.DecidirAsync(ClienteCinco, new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO });

            List<VinculoClienteLocadora> linhas = cenario.Repositorio.Armazenados.ToList();
            Assert.Equal(StatusCadastroCliente.APROVADO, linhas[0].Status);
            Assert.Equal(StatusCadastroCliente.APROVADO, linhas[1].Status);
            Assert.Equal(StatusCadastroCliente.PENDENTE, linhas[2].Status);
        }

        // --- GetMinhasAsync: so o cliente do contexto, uma linha por locadora, sem justificativa ---

        [Fact]
        public async Task GetMinhas_SoOsVinculosDoClienteDoContexto_SemJustificativaNoVO()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.RECUSADO, null, "Motivo interno");
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaDois, StatusCadastroCliente.APROVADO);
            cenario.Armazenamento.Semear(ClienteSeis, EmpresaUm, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteLocadoraVO> pagina = await cenario.Servico.GetMinhasAsync(new FiltroPaginacaoVO { Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.Contains(pagina.Itens, item => item.NomeFantasia == NomeLocadoraUm && item.Status == StatusCadastroCliente.RECUSADO);
            Assert.Contains(pagina.Itens, item => item.NomeFantasia == NomeLocadoraDois && item.Status == StatusCadastroCliente.APROVADO);
            Assert.DoesNotContain(typeof(ReturnClienteLocadoraVO).GetProperties(), propriedade => propriedade.Name.Contains("Justificativa", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetMinhas_DatasFormatadasEVerificacaoVaziaQuandoPendente()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteLocadoraVO> pagina = await cenario.Servico.GetMinhasAsync(new FiltroPaginacaoVO());

            ReturnClienteLocadoraVO item = Assert.Single(pagina.Itens);
            Assert.False(string.IsNullOrWhiteSpace(item.DataSolicitacaoStr));
            Assert.Equal(string.Empty, item.DataVerificacaoStr);
        }

        [Fact]
        public async Task GetMinhas_PaginaRespeitaTamanhoEConservaOTotal()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = 3, NomeFantasia = "Locadora Tres", Ativo = true });
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaDois, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.Semear(ClienteCinco, 3, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteLocadoraVO> pagina = await cenario.Servico.GetMinhasAsync(new FiltroPaginacaoVO { Pagina = 1, TamanhoPagina = 2 });

            Assert.Equal(3, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
        }

        /// <summary>Duplicata do mesmo par: a listagem mostra uma linha por locadora (a de menor id).</summary>
        [Fact]
        public async Task GetMinhas_DuplicataDoMesmoPar_ContaUmaVez()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteLocadoraVO> pagina = await cenario.Servico.GetMinhasAsync(new FiltroPaginacaoVO());

            Assert.Equal(1, pagina.Total);
            Assert.Single(pagina.Itens);
        }

        /// <summary>Sem cliente no contexto (funcionario), a pagina volta vazia e nao enxerga os vinculos da locadora.</summary>
        [Fact]
        public async Task GetMinhas_SemClienteNoContexto_DevolvePaginaVazia()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ADMINISTRADOR));
            cenario.Armazenamento.Semear(ClienteCinco, EmpresaUm, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteLocadoraVO> pagina = await cenario.Servico.GetMinhasAsync(new FiltroPaginacaoVO());

            Assert.Equal(0, pagina.Total);
            Assert.Empty(pagina.Itens);
        }

        // --- Teto de pedidos de aprovacao por cliente (v2): locadoras distintas na janela de 24 horas ---

        /// <summary>Cadastra locadoras de id 3 ate o informado (as de id 1 e 2 ja existem), cada uma com nome proprio.</summary>
        private static void AdicionarLocadoras(Cenario cenario, int ateIdEmpresa)
        {
            for (int id = EmpresaDois + 1; id <= ateIdEmpresa; id++)
            {
                cenario.Armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = id, NomeFantasia = $"Locadora {id}", Ativo = true });
            }
        }

        /// <summary>Semeia um pedido PENDENTE do cliente em cada locadora da lista, com a data de solicitacao informada.</summary>
        private static void SemearPedidos(Cenario cenario, int idCliente, IEnumerable<int> idsEmpresa, DateTime dataSolicitacao)
        {
            foreach (int idEmpresa in idsEmpresa)
            {
                cenario.Armazenamento.Semear(idCliente, idEmpresa, StatusCadastroCliente.PENDENTE, null, null, dataSolicitacao);
            }
        }

        /// <summary>Com o teto em 5 e cinco pedidos recentes a locadoras distintas, o sexto pedido a locadora nova da 429 e nao grava vinculo nem log.</summary>
        [Fact]
        public async Task Teto_SextoPedidoALocadoraNova_Devolve429SemGravarVinculo()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-1));

            LimiteExcedidoException excecao = await Assert.ThrowsAsync<LimiteExcedidoException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(ConstantsMessage.ErroLimitePedidosAprovacao, excecao.Message);
            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
            Assert.Equal(1, cenario.Repositorio.ChamadasDeContagem);
            Assert.Empty(cenario.Log.Snapshot());
        }

        /// <summary>Pedido a locadora em que o cliente ja tem vinculo nao e barrado pelo teto: segue a regra do status (aqui, em analise).</summary>
        [Fact]
        public async Task Teto_PedidoALocadoraComVinculoExistenteNaoEBarrado()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-1));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 1));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadoraUm), excecao.Message);
            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
            Assert.Equal(0, cenario.Repositorio.ChamadasDeContagem);
        }

        /// <summary>A linha desativada tambem conta no teto: quatro ativas mais uma desativada, todas na janela, chegam ao limite.</summary>
        [Fact]
        public async Task Teto_LinhaDesativadaConta()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4 }, AgoraFixo.AddHours(-1));
            cenario.Armazenamento.Semear(ClienteCinco, 5, StatusCadastroCliente.PENDENTE, AgoraFixo, null, AgoraFixo.AddHours(-1));

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>Duas linhas da mesma locadora contam uma vez: com quatro locadoras distintas, o quinto pedido ainda passa.</summary>
        [Fact]
        public async Task Teto_DuasLinhasDaMesmaLocadoraContamUmaVez()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 1, 2, 3, 4 }, AgoraFixo.AddHours(-1));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 5));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, "Locadora 5"), excecao.Message);
            Assert.Equal(6, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>Pedidos com mais de 24 horas (pelo IRelogio) nao contam: o sexto pedido passa.</summary>
        [Fact]
        public async Task Teto_PedidoComMaisDe24HorasNaoConta()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-25));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, "Locadora 6"), excecao.Message);
            Assert.Equal(6, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>A janela e de 24 horas corridas com fronteira fechada: pedido com exatamente 24 horas ainda conta.</summary>
        [Fact]
        public async Task Teto_PedidoComExatas24HorasAindaConta()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-24));

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>Funcionario sem vinculo continua com 409 de nao aprovado, sem contar pedidos e sem criar nada.</summary>
        [Fact]
        public async Task Teto_FuncionarioSemVinculo_Devolve409SemContarNemDar429()
        {
            Cenario cenario = CriarCenario(ContextoFuncionario(EmpresaUm, CargoFuncionario.ATENDENTE));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-1));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(ConstantsMessage.ErroReservaCadastroNaoAprovado, excecao.Message);
            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
            Assert.Equal(0, cenario.Repositorio.ChamadasDeContagem);
        }

        /// <summary>Limite zero ou negativo na secao Clientes cai no padrao de 5: o sexto pedido recebe 429.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Teto_LimiteZeroOuNegativoCaiNoPadrao(int limiteConfigurado)
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco), limiteConfigurado);
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-1));

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, 6));

            Assert.Equal(5, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>Os pedidos de outro cliente nao contam: o cliente do contexto so conta os seus proprios pedidos.</summary>
        [Fact]
        public async Task Teto_OutroClienteNaoEAfetado()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteSeis));
            AdicionarLocadoras(cenario, 6);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4, 5 }, AgoraFixo.AddHours(-1));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ExigirCadastroAprovadoAsync(ClienteSeis, 6));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, "Locadora 6"), excecao.Message);
            Assert.Equal(6, cenario.Repositorio.Armazenados.Count);
        }

        // --- Corrida do teto (item 6, 20/11): a trava do cliente serializa os pedidos simultaneos do mesmo cliente ---

        private const int LocadoraCinco = 5;
        private const int LocadoraSeis = 6;
        private const int PedidosAntesDaCorrida = 4;
        private static readonly TimeSpan TempoLimiteDaCorrida = TimeSpan.FromSeconds(10);

        /// <summary>Cenario da corrida: cliente cinco com quatro pedidos recentes (locadoras 1 a 4) e locadoras de 1 a 6 cadastradas.</summary>
        private static Cenario CriarCenarioComQuatroPedidos()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, LocadoraSeis);
            SemearPedidos(cenario, ClienteCinco, new int[] { 1, 2, 3, 4 }, AgoraFixo.AddHours(-1));

            return cenario;
        }

        /// <summary>Pede aprovacao do cliente cinco na locadora informada (o contexto e o proprio cliente).</summary>
        private static Task ExigirAprovacaoDoClienteCinco(Cenario cenario, int idEmpresa)
        {
            return cenario.Servico.ExigirCadastroAprovadoAsync(ClienteCinco, idEmpresa);
        }

        /// <summary>Roda a chamada e devolve a excecao que ela lancou (nulo se nao lancou), para os dois pedidos sempre terminarem.</summary>
        private static async Task<Exception?> CapturarExcecao(Func<Task> chamada)
        {
            try
            {
                await chamada();

                return null;
            }
            catch (Exception excecao)
            {
                return excecao;
            }
        }

        /// <summary>Barreira de duas chegadas: cada pedido so segue depois que o outro chegou ao mesmo ponto (a janela da corrida). Serve para dois pedidos.</summary>
        private static Func<Task> BarreiraDeDuasChegadas()
        {
            TaskCompletionSource<bool> ambosChegaram = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int chegadas = 0;

            return () =>
            {
                if (Interlocked.Increment(ref chegadas) == 2)
                {
                    ambosChegaram.TrySetResult(true);
                }

                return ambosChegaram.Task;
            };
        }

        /// <summary>
        /// Controle do proprio teste: com a trava desligada, dois pedidos simultaneos a locadoras diferentes passam juntos (quatro
        /// pedidos mais dois, seis linhas: o teto furou). Prova que a barreira reproduz a corrida de verdade.
        /// </summary>
        [Fact]
        public async Task Teto_Corrida_ControleSemTrava_DoisPedidosPassamJuntosEFuramOTeto()
        {
            Cenario cenario = CriarCenarioComQuatroPedidos();
            cenario.Repositorio.SemTrava = true;
            cenario.Repositorio.AposContar = BarreiraDeDuasChegadas();

            Task<Exception?> primeiro = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraCinco));
            Task<Exception?> segundo = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraSeis));

            Exception?[] resultados = await Task.WhenAll(primeiro, segundo).WaitAsync(TempoLimiteDaCorrida);

            Assert.All(resultados, excecao => Assert.IsType<BusinessException>(excecao));
            Assert.Equal(PedidosAntesDaCorrida + 2, cenario.Repositorio.Armazenados.Count);
        }

        /// <summary>
        /// Com a trava do cliente: o pedido que pega a trava primeiro grava o quinto pedido; o segundo re-le a contagem ja com o quinto
        /// e recebe 429, sem gravar. Um 409 em analise e um 429; o teto fica exato (cinco locadoras distintas).
        /// </summary>
        [Fact]
        public async Task Teto_Corrida_ComTrava_UmPedidoGravaOQuintoOutroRecebe429()
        {
            Cenario cenario = CriarCenarioComQuatroPedidos();
            cenario.Repositorio.AposContar = async () => await Task.Yield();

            Task<Exception?> primeiro = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraCinco));
            Task<Exception?> segundo = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraSeis));

            Exception?[] resultados = await Task.WhenAll(primeiro, segundo).WaitAsync(TempoLimiteDaCorrida);
            List<Exception> erros = resultados.Where(excecao => excecao != null).Select(excecao => excecao!).ToList();

            Assert.Equal(2, erros.Count);
            Assert.Single(erros.OfType<BusinessException>());
            Assert.Single(erros.OfType<LimiteExcedidoException>());
            Assert.Equal(PedidosAntesDaCorrida + 1, cenario.Repositorio.Armazenados.Count);
            Assert.Equal(PedidosAntesDaCorrida + 1, cenario.Repositorio.Armazenados.Select(x => x.IdEmpresa).Distinct().Count());
        }

        /// <summary>
        /// Mesmo par em paralelo: o segundo pedido acha a linha que o primeiro acabou de gravar e nao conta no teto. Resultado: uma
        /// linha ativa, nenhuma desativada e dois 409 em analise (a mensagem da mesma locadora).
        /// </summary>
        [Fact]
        public async Task Teto_Corrida_MesmoPar_UmaLinhaAtivaEDoisEmAnalise()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));
            AdicionarLocadoras(cenario, LocadoraSeis);
            cenario.Repositorio.AposContar = async () => await Task.Yield();

            Task<Exception?> primeiro = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraCinco));
            Task<Exception?> segundo = CapturarExcecao(() => ExigirAprovacaoDoClienteCinco(cenario, LocadoraCinco));

            Exception?[] resultados = await Task.WhenAll(primeiro, segundo).WaitAsync(TempoLimiteDaCorrida);
            string mensagemEsperada = string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, "Locadora 5");

            Assert.All(resultados, excecao => Assert.IsType<BusinessException>(excecao));
            Assert.All(resultados, excecao => Assert.Equal(mensagemEsperada, excecao!.Message));
            Assert.Single(cenario.Repositorio.Armazenados);
            Assert.Null(cenario.Repositorio.Armazenados[0].DisabledAt);
            Assert.Equal(0, cenario.Repositorio.ChamadasDeDesativar);
            Assert.Equal(1, cenario.Repositorio.ChamadasDeContagem);
        }

        /// <summary>O metodo atomico recebe a janela de 24 horas pelo IRelogio e o limite padrao (5).</summary>
        [Fact]
        public async Task Teto_PassaAJanelaDoRelogioELimitePadrao()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco));

            await Assert.ThrowsAsync<BusinessException>(() => ExigirAprovacaoDoClienteCinco(cenario, EmpresaUm));

            Assert.Equal(AgoraFixo.AddHours(-24), cenario.Repositorio.UltimoDesde);
            Assert.Equal(5, cenario.Repositorio.UltimoLimite);
        }

        /// <summary>O metodo atomico recebe o limite configurado na opcao Clientes (3 neste teste).</summary>
        [Fact]
        public async Task Teto_PassaOLimiteConfiguradoNaOpcao()
        {
            Cenario cenario = CriarCenario(ContextoCliente(ClienteCinco), 3);

            await Assert.ThrowsAsync<BusinessException>(() => ExigirAprovacaoDoClienteCinco(cenario, EmpresaUm));

            Assert.Equal(3, cenario.Repositorio.UltimoLimite);
        }
    }
}
