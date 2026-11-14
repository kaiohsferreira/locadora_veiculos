using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
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
            public Cenario(ArmazenamentoClienteLocadora armazenamento, FakeClienteLocadoraRepository repositorio, ClienteLocadoraService servico)
            {
                Armazenamento = armazenamento;
                Repositorio = repositorio;
                Servico = servico;
            }

            public ArmazenamentoClienteLocadora Armazenamento { get; }

            public FakeClienteLocadoraRepository Repositorio { get; }

            public ClienteLocadoraService Servico { get; }
        }

        private static Cenario CriarCenario(Action<ContextoEmpresa> definirContexto)
        {
            FakeLogService log = new FakeLogService();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            definirContexto(contexto);

            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = EmpresaUm, NomeFantasia = NomeLocadoraUm, Ativo = true });
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = EmpresaDois, NomeFantasia = NomeLocadoraDois, Ativo = true });

            FakeClienteLocadoraRepository repositorio = new FakeClienteLocadoraRepository(contexto, armazenamento);
            IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClienteLocadoraProfile>(), NullLoggerFactory.Instance).CreateMapper();
            ClienteLocadoraService servico = new ClienteLocadoraService(repositorio, contexto, new FakeRelogio(AgoraFixo), mapper, log);

            return new Cenario(armazenamento, repositorio, servico);
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
    }
}
