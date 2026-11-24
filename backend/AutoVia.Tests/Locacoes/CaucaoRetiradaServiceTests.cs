using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Abertura da locacao com caucao (contrato caucao-retirada v1.1) no servico, com o FakeCaucaoGateway e o repositorio em
    /// memoria: valor 0 sem gateway; recusas antes do provedor; solicitacao montada com o valor da configuracao e o pagador do
    /// banco; so RESERVADA (devolvida e confirmada) abre; EM_ANALISE e falha da confirmacao compensam so o id novo; estados
    /// encerrados e divergencia nao cancelam; compensacao depois da falha da gravacao (releitura antes e depois de cancelar);
    /// logs so com ids e referencia. A corrida com o gateway real e o FAKE fica em CaucaoRetiradaFluxoTests.
    /// </summary>
    public class CaucaoRetiradaServiceTests
    {
        private const int IdReserva = 42;
        private const int IdFuncionario = 20;
        private const decimal ValorDaCaucao = 1200.00m;
        private const string Referencia = "CAUCAO-RES-42";
        private const string Token = "FAKE-tok-0001abcdef";
        private const string Bandeira = "visa";
        private const long Emissor = 123L;
        private const string Dispositivo = "dispositivo-teste-01";
        private const string EmailCliente = "cliente.teste@exemplo.com";
        private const string CpfCliente = "12345678909";
        private const string Identificador = FakeCaucaoGateway.IdentificadorPadrao;
        private static readonly DateTime AgoraDoRelogio = new DateTime(2026, 11, 24, 10, 0, 0);
        private static readonly DateTime DevolucaoPrevista = new DateTime(2026, 11, 27, 10, 0, 0);

        private sealed class Cenario
        {
            public Cenario(decimal valorCaucao = ValorDaCaucao, Action<Reserva>? ajustarReserva = null, bool funcionarioAtivo = true)
            {
                Reserva reserva = CriarReserva();
                ajustarReserva?.Invoke(reserva);
                Repositorio.Adicionar(reserva);
                Repositorio.Adicionar(new Funcionario { IdFuncionario = IdFuncionario, Ativo = funcionarioAtivo });
                Caucao = new CaucaoRetiradaService(Gateway, Repositorio, new CaucaoRetiradaOptions(valorCaucao), Log);
                Servico = new LocacaoService(Repositorio, Relogio, CriarMapper(), Log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), Log), new FakeClienteDocumentoRepository(), Caucao);
            }

            public FakeLocacaoRepository Repositorio { get; } = new FakeLocacaoRepository();

            public FakeCaucaoGateway Gateway { get; } = new FakeCaucaoGateway();

            public FakeLogService Log { get; } = new FakeLogService();

            public FakeRelogio Relogio { get; } = new FakeRelogio(AgoraDoRelogio);

            public CaucaoRetiradaService Caucao { get; }

            public LocacaoService Servico { get; }

            public Task<ReturnLocacaoVO> AbrirAsync(LocacaoVO? locacaoVO = null)
            {
                return Servico.StartFromReservaAsync(locacaoVO ?? VOComCartao(), IdFuncionario);
            }

            /// <summary>Faz a pre-autorizacao devolver o estado informado (mesmo identificador, valor e referencia pedidos).</summary>
            public void PreAutorizacaoDevolve(CaucaoStatus status)
            {
                Gateway.ResponderPreAutorizacao = (CaucaoSolicitacao solicitacao) => FakeCaucaoGateway.RetornoNoEstado(Identificador, status, solicitacao.Valor, solicitacao.ReferenciaExterna);
            }

            /// <summary>Faz a consulta (confirmacao) devolver o estado informado.</summary>
            public void ConsultaDevolve(CaucaoStatus status)
            {
                Gateway.ResponderConsulta = (string identificador) => FakeCaucaoGateway.RetornoNoEstado(identificador, status, ValorDaCaucao, Referencia);
            }
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<LocacaoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static Reserva CriarReserva()
        {
            Cliente cliente = new Cliente { IdCliente = 30, Nome = "Maria da Silva", Email = EmailCliente, Cpf = CpfCliente };
            Veiculo veiculo = new Veiculo { IdVeiculo = 40, IdEmpresa = 1, KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO };

            return new Reserva
            {
                IdReserva = IdReserva,
                IdCliente = 30,
                Cliente = cliente,
                IdVeiculo = 40,
                Veiculo = veiculo,
                DataRetiradaPrevista = new DateTime(2026, 11, 24, 9, 0, 0),
                DataDevolucaoPrevista = DevolucaoPrevista,
                Status = StatusReserva.ATIVA
            };
        }

        private static LocacaoVO VOComCartao(string token = Token)
        {
            return new LocacaoVO
            {
                IdReserva = IdReserva,
                CaucaoTokenCartao = token,
                CaucaoMetodoPagamentoId = Bandeira,
                CaucaoEmissorId = Emissor,
                CaucaoIdentificadorDispositivo = Dispositivo
            };
        }

        private static string Registro(string modelo, params object[] valores)
        {
            return string.Format(CultureInfo.InvariantCulture, modelo, valores);
        }

        // --- Valor 0: RF-17 como hoje ---

        [Fact]
        public async Task ValorZero_ComCamposDoCartao_GatewayNaoChamado_SemCaucaoNemPagamentoNemLog()
        {
            Cenario cenario = new Cenario(0m);

            ReturnLocacaoVO retorno = await cenario.AbrirAsync(VOComCartao());

            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Equal(1, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Null(cenario.Repositorio.UltimaCaucao);
            Assert.Equal(0m, retorno.ValorCaucao);
            Locacao gravada = Assert.Single(cenario.Repositorio.LocacoesGravadas());
            Assert.Equal(0m, gravada.ValorCaucao);
            Assert.Empty(gravada.Pagamentos);
            Assert.Equal(0, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task ValorZero_SoComAReserva_AberturaDeHoje()
        {
            Cenario cenario = new Cenario(0m);

            ReturnLocacaoVO retorno = await cenario.AbrirAsync(new LocacaoVO { IdReserva = IdReserva });

            Assert.True(retorno.IdLocacao > 0);
            Assert.Equal(AgoraDoRelogio, retorno.DataRetirada);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Null(cenario.Repositorio.UltimaCaucao);
        }

        // --- Recusas antes do provedor ---

        [Theory]
        [InlineData(null, null)]
        [InlineData("", Bandeira)]
        [InlineData("   ", Bandeira)]
        [InlineData(Token, null)]
        [InlineData(Token, "  ")]
        public async Task CaucaoExigidaSemTokenOuSemBandeira_400SemChamarOGatewayESemLog(string? token, string? bandeira)
        {
            Cenario cenario = new Cenario();
            LocacaoVO vo = new LocacaoVO { IdReserva = IdReserva, CaucaoTokenCartao = token, CaucaoMetodoPagamentoId = bandeira };

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => cenario.AbrirAsync(vo));

            ValidationFailure falha = Assert.Single(excecao.Errors);
            Assert.Equal(nameof(LocacaoVO.CaucaoTokenCartao), falha.PropertyName);
            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoCartaoObrigatorio, falha.ErrorMessage);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task CaucaoExigida_ReservaInexistenteOuDeOutraLocadora_404SemGateway()
        {
            Cenario cenario = new Cenario();

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.AbrirAsync(new LocacaoVO { IdReserva = 99, CaucaoTokenCartao = Token, CaucaoMetodoPagamentoId = Bandeira }));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        [Theory]
        [InlineData(StatusReserva.CONCLUIDA)]
        [InlineData(StatusReserva.CANCELADA)]
        public async Task CaucaoExigida_ReservaNaoElegivel_409SemGateway(StatusReserva status)
        {
            Cenario cenario = new Cenario(ajustarReserva: reserva => reserva.Status = status);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaEstadoInvalido, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task CaucaoExigida_VeiculoNaoReservado_409SemGateway()
        {
            Cenario cenario = new Cenario(ajustarReserva: reserva => reserva.Veiculo.Status = StatusVeiculo.DISPONIVEL);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoVeiculoIndisponivel, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task CaucaoExigida_LocacaoJaExistente_409SemGateway()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Adicionar(new Locacao { IdReserva = IdReserva });

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task CaucaoExigida_FuncionarioInativo_409SemGateway()
        {
            Cenario cenario = new Cenario(funcionarioAtivo: false);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        [Fact]
        public async Task CaucaoExigida_ClienteDesativado_404SemGateway()
        {
            Cenario cenario = new Cenario(ajustarReserva: reserva => reserva.Cliente.Disable());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task CaucaoExigida_ClienteAusente_404SemGateway()
        {
            Cenario cenario = new Cenario(ajustarReserva: reserva => reserva.Cliente = null!);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        // --- Solicitacao e aprovacao ---

        [Fact]
        public async Task Solicitacao_ValorDaConfiguracaoReferenciaDescricaoPagadorDoBancoECartaoDoPedido_SemCancelamentoPossivel()
        {
            Cenario cenario = new Cenario();

            await cenario.AbrirAsync();

            CaucaoSolicitacao solicitacao = Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Equal(Referencia, solicitacao.ReferenciaExterna);
            Assert.Equal(ValorDaCaucao, solicitacao.Valor);
            Assert.Equal(ConstantsValues.CaucaoDescricaoRetirada, solicitacao.Descricao);
            Assert.Equal(EmailCliente, solicitacao.Pagador.Email);
            Assert.Equal(CpfCliente, solicitacao.Pagador.Documento);
            Assert.Equal(Token, solicitacao.TokenCartao);
            Assert.Equal(Bandeira, solicitacao.MetodoPagamentoId);
            Assert.Equal(Emissor, solicitacao.EmissorId);
            Assert.Equal(Dispositivo, solicitacao.IdentificadorDispositivo);
            Assert.NotEmpty(cenario.Gateway.TokensRecebidos);
            Assert.All(cenario.Gateway.TokensRecebidos, (CancellationToken token) => Assert.False(token.CanBeCanceled));
        }

        [Fact]
        public async Task Solicitacao_DispositivoEmBranco_VaiNuloAoGateway()
        {
            Cenario cenario = new Cenario();
            LocacaoVO vo = VOComCartao();
            vo.CaucaoIdentificadorDispositivo = "   ";

            await cenario.AbrirAsync(vo);

            Assert.Null(Assert.Single(cenario.Gateway.Solicitacoes).IdentificadorDispositivo);
        }

        [Fact]
        public async Task Reservada_ConfirmadaAntesDeGravar_LocacaoComValorEUmaCaucao_ELogComIds()
        {
            Cenario cenario = new Cenario();
            int aberturasAntesDaConsulta = -1;
            cenario.Gateway.AoConsultar = (string identificador) => aberturasAntesDaConsulta = cenario.Repositorio.ChamadasStartFromReserva;

            ReturnLocacaoVO retorno = await cenario.AbrirAsync();

            Assert.Equal(new List<string> { nameof(FakeCaucaoGateway.PreAutorizarAsync), nameof(FakeCaucaoGateway.ConsultarAsync) }, cenario.Gateway.Chamadas);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Consultas);
            Assert.Equal(0, aberturasAntesDaConsulta);
            Assert.Empty(cenario.Gateway.Cancelamentos);

            CaucaoAberturaRegistro recebida = Assert.IsType<CaucaoAberturaRegistro>(cenario.Repositorio.UltimaCaucao);
            Assert.Equal(ValorDaCaucao, recebida.Valor);
            Assert.Equal(Identificador, recebida.IdTransacao);
            Assert.Equal(Referencia, recebida.ReferenciaExterna);

            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(ValorDaCaucao), retorno.ValorCaucaoStr);

            Locacao gravada = Assert.Single(cenario.Repositorio.LocacoesGravadas());
            Assert.Equal(ValorDaCaucao, gravada.ValorCaucao);
            Pagamento caucao = Assert.Single(gravada.Pagamentos);
            Assert.Equal(TipoPagamento.CAUCAO, caucao.Tipo);
            Assert.Equal(gravada.IdLocacao, caucao.IdLocacao);
            Assert.Null(caucao.IdVenda);
            Assert.Equal(ValorDaCaucao, caucao.Valor);
            Assert.Equal(ConstantsValues.FormaCartao, caucao.Forma);
            Assert.Equal(StatusPagamento.PENDENTE, caucao.Status);
            Assert.Equal(Identificador, caucao.IdTransacao);
            Assert.Equal(DateOnly.FromDateTime(DevolucaoPrevista), caucao.DataVencimento);
            Assert.Null(caucao.DataPagamento);
            Assert.Null(caucao.DisabledAt);

            Assert.Contains(Registro(ConstantsMessage.InfoCaucaoRetiradaGravada, IdReserva, gravada.IdLocacao, Referencia, Identificador), cenario.Log.Entries);
        }

        /// <summary>A data da retirada e lida do IRelogio DEPOIS da confirmacao: o relogio muda dentro da consulta e a locacao sai com o instante novo.</summary>
        [Fact]
        public async Task DataDaRetirada_LidaDepoisDaConfirmacao()
        {
            Cenario cenario = new Cenario();
            DateTime depoisDaConfirmacao = AgoraDoRelogio.AddMinutes(3);
            cenario.Gateway.AoConsultar = (string identificador) => cenario.Relogio.Agora = depoisDaConfirmacao;

            ReturnLocacaoVO retorno = await cenario.AbrirAsync();

            Assert.Equal(depoisDaConfirmacao, retorno.DataRetirada);
            Assert.Equal(depoisDaConfirmacao, cenario.Repositorio.UltimaDataRetirada);
        }

        // --- Recusa, estados e divergencia ---

        [Fact]
        public async Task RecusaDoGateway_409ComAMensagemDele_SemConsultaCancelamentoNemAbertura_ELogDaRecusa()
        {
            Cenario cenario = new Cenario();
            string mensagem = string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro);
            cenario.Gateway.ExcecaoPreAutorizacao = new BusinessException(mensagem);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(mensagem, excecao.Message);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Empty(cenario.Gateway.Consultas);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Equal(0, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            string registro = Registro(ConstantsMessage.LogCaucaoRetiradaRecusada, IdReserva, Referencia);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
        }

        [Fact]
        public async Task EmAnaliseDevolvida_IdNovo_CancelaE409EmAnalise_SemConsultaNemAbertura()
        {
            Cenario cenario = new Cenario();
            cenario.PreAutorizacaoDevolve(CaucaoStatus.EM_ANALISE);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoEmAnalise, excecao.Message);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            Assert.Empty(cenario.Gateway.Consultas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Equal(2, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Contains(Registro(ConstantsMessage.LogCaucaoRetiradaNaoAprovada, IdReserva, Referencia, nameof(CaucaoStatus.EM_ANALISE)), cenario.Log.Entries);
            Assert.Contains(Registro(ConstantsMessage.InfoCaucaoRetiradaCompensada, IdReserva, Referencia, Identificador), cenario.Log.Entries);
        }

        [Fact]
        public async Task EmAnaliseDevolvida_MesmoIdJaGravado_NaoCancelaE409Duplicada()
        {
            Cenario cenario = new Cenario();
            cenario.PreAutorizacaoDevolve(CaucaoStatus.EM_ANALISE);
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => true;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Contains(Registro(ConstantsMessage.LogCaucaoRetiradaJaGravada, IdReserva, Identificador), cenario.Log.Entries);
        }

        [Theory]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.RECUSADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        [InlineData(CaucaoStatus.CAPTURADA)]
        public async Task EstadoEncerradoDevolvido_409NovaTentativa_SemCancelarNemConsultarNemAbrir(CaucaoStatus status)
        {
            Cenario cenario = new Cenario();
            cenario.PreAutorizacaoDevolve(status);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoNovaTentativa, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Empty(cenario.Gateway.Consultas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Equal(0, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Contains(Registro(ConstantsMessage.LogCaucaoRetiradaNaoAprovada, IdReserva, Referencia, status.ToString()), cenario.Log.Entries);
        }

        /// <summary>v1.1, CS-1: o Mercado Pago pode repetir o 'authorized' guardado de um pagamento ja cancelado; a consulta manda.</summary>
        [Theory]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.RECUSADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        [InlineData(CaucaoStatus.CAPTURADA)]
        public async Task ReservadaMasConfirmacaoEncerrada_409NovaTentativa_SemCancelarNemAbrir(CaucaoStatus status)
        {
            Cenario cenario = new Cenario();
            cenario.ConsultaDevolve(status);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoNovaTentativa, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Contains(Registro(ConstantsMessage.LogCaucaoRetiradaNaoAprovada, IdReserva, Referencia, status.ToString()), cenario.Log.Entries);
        }

        [Fact]
        public async Task ReservadaMasConfirmacaoEmAnalise_CancelaOIdE409EmAnalise()
        {
            Cenario cenario = new Cenario();
            cenario.ConsultaDevolve(CaucaoStatus.EM_ANALISE);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoEmAnalise, excecao.Message);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task ConfirmacaoLanca_CancelaOIdNovoEA502OriginalSobe_SemAbrir()
        {
            Cenario cenario = new Cenario();
            IntegrationException original = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            cenario.Gateway.ExcecaoConsulta = original;

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Consultas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task ConfirmacaoLanca_MesmoIdJaGravado_NaoCancelaE409Duplicada()
        {
            Cenario cenario = new Cenario();
            cenario.Gateway.ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => true;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
        }

        public static TheoryData<string> CasosDeDivergencia => new TheoryData<string> { "valor", "referencia", "id-vazio", "id-com-41" };

        [Theory]
        [MemberData(nameof(CasosDeDivergencia))]
        public async Task RetornoDivergente_502RespostaInesperada_SemCancelarNemConsultarNemAbrir_ELogComMotivoEId(string caso)
        {
            Cenario cenario = new Cenario();
            string idDevolvido = caso == "id-vazio" ? string.Empty : caso == "id-com-41" ? new string('1', 41) : Identificador;
            decimal valorDevolvido = caso == "valor" ? ValorDaCaucao - 0.01m : ValorDaCaucao;
            string referenciaDevolvida = caso == "referencia" ? "CAUCAO-RES-43" : Referencia;
            cenario.Gateway.ResponderPreAutorizacao = (CaucaoSolicitacao solicitacao) => FakeCaucaoGateway.RetornoNoEstado(idDevolvido, CaucaoStatus.RESERVADA, valorDevolvido, referenciaDevolvida);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Empty(cenario.Gateway.Consultas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Equal(0, cenario.Repositorio.ChamadasExisteCaucaoGravada);

            string motivo = caso == "valor" ? ConstantsValues.MotivoDivergenciaValor
                : caso == "referencia" ? ConstantsValues.MotivoDivergenciaReferencia
                : ConstantsValues.MotivoDivergenciaIdentificador;
            string idRegistrado = caso == "id-vazio" ? ConstantsCaucao.MarcadorAusente : idDevolvido;
            string registro = Registro(ConstantsMessage.LogCaucaoRetiradaDivergente, IdReserva, Referencia, idRegistrado, motivo);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.Contains(registro, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDoProvedorSemId_502SemCancelarNemAbrir_ERegistradaPeloCatchGenerico()
        {
            Cenario cenario = new Cenario();
            IntegrationException original = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            cenario.Gateway.ExcecaoPreAutorizacao = original;

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Empty(cenario.Gateway.Consultas);
            Assert.Equal(0, cenario.Repositorio.ChamadasStartFromReserva);
            Assert.Equal(0, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Contains(ConstantsMessage.ErroSalvarLocacao + " | " + ConstantsMessage.ErroCaucaoProvedorIndisponivel, cenario.Log.Entries);
        }

        // --- Falha da gravacao depois de RESERVADA: compensacao ---

        [Fact]
        public async Task FalhaDoBancoDepoisDeReservada_IdNovo_CancelaEAExcecaoOriginalSobe()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException original = new InvalidOperationException();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(original);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            Assert.Equal(2, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Empty(cenario.Repositorio.LocacoesGravadas());
            Assert.Contains(Registro(ConstantsMessage.InfoCaucaoRetiradaCompensada, IdReserva, Referencia, Identificador), cenario.Log.Entries);
        }

        [Fact]
        public async Task PerdedorDaCorridaComIdNovo_CancelaEO409OriginalSobe()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaEstadoInvalido, excecao.Message);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
        }

        [Fact]
        public async Task MesmoIdJaGravadoPorOutraAbertura_NaoCancelaE409Duplicada()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido));
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => true;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.AbrirAsync());

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Equal(1, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            Assert.Contains(Registro(ConstantsMessage.LogCaucaoRetiradaJaGravada, IdReserva, Identificador), cenario.Log.Entries);
        }

        [Fact]
        public async Task ReleituraLanca_NaoCancela_ExcecaoOriginalSobe_ELogDeConferenciaIndisponivel()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException original = new InvalidOperationException();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(original);
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => throw new TimeoutException();

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Empty(cenario.Gateway.Cancelamentos);
            string registro = Registro(ConstantsMessage.LogCaucaoRetiradaConferenciaIndisponivel, IdReserva, Referencia, Identificador);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
        }

        [Fact]
        public async Task CancelamentoLanca_ARespostaContinuaOErroOriginal_ELogDeCancelamentoManual()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException original = new InvalidOperationException();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(original);
            cenario.Gateway.ExcecaoCancelamento = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            Assert.Equal(1, cenario.Repositorio.ChamadasExisteCaucaoGravada);
            string registro = Registro(ConstantsMessage.ErroCaucaoRetiradaCompensacao, IdReserva, Referencia, Identificador);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
        }

        /// <summary>v1.1, CS-1: a outra abertura gravou o mesmo id entre a primeira releitura e o cancelamento: a segunda releitura registra.</summary>
        [Fact]
        public async Task ReleituraDepoisDeCancelarAchaOId_RegistraParaTratamentoManual_SemMudarAResposta()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException original = new InvalidOperationException();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(original);
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => chamada > 1;

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            string registro = Registro(ConstantsMessage.LogCaucaoRetiradaConferirDepoisDoCancelamento, IdReserva, Referencia, Identificador);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.Contains(registro, StringComparison.Ordinal));
        }

        [Fact]
        public async Task ReleituraDepoisDeCancelarLanca_RegistraParaTratamentoManual_SemMudarAResposta()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException original = new InvalidOperationException();
            cenario.Repositorio.FalhasNaAbertura.Enqueue(original);
            cenario.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => chamada == 1 ? false : throw new TimeoutException();

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AbrirAsync());

            Assert.Same(original, excecao);
            Assert.Equal(new List<string> { Identificador }, cenario.Gateway.Cancelamentos);
            string registro = Registro(ConstantsMessage.LogCaucaoRetiradaConferirDepoisDoCancelamento, IdReserva, Referencia, Identificador);
            Assert.Contains(cenario.Log.Entries, (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Compensar_Direto_NuncaLancaEDevolveSeOIdJaEstavaGravado()
        {
            Cenario gravada = new Cenario();
            gravada.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => true;
            Cenario naoGravada = new Cenario();
            Cenario releituraQuebrada = new Cenario();
            releituraQuebrada.Repositorio.RoteiroConferenciaDaCaucao = (int chamada) => throw new TimeoutException();
            CaucaoAberturaRegistro caucao = new CaucaoAberturaRegistro(ValorDaCaucao, Identificador, Referencia);

            Assert.True(await gravada.Caucao.CompensarAsync(IdReserva, caucao));
            Assert.False(await naoGravada.Caucao.CompensarAsync(IdReserva, caucao));
            Assert.False(await releituraQuebrada.Caucao.CompensarAsync(IdReserva, caucao));
            Assert.Empty(gravada.Gateway.Cancelamentos);
            Assert.Equal(new List<string> { Identificador }, naoGravada.Gateway.Cancelamentos);
            Assert.Empty(releituraQuebrada.Gateway.Cancelamentos);
        }

        // --- Log sem dado sensivel ---

        public static TheoryData<string> CasosDeLog => new TheoryData<string> { "aprovado", "recusa", "em-analise", "falha-do-banco", "divergencia", "falha-do-provedor" };

        [Theory]
        [MemberData(nameof(CasosDeLog))]
        public async Task Log_SemTokenEmailCpfDispositivoFinalDoCartaoNemChaveDeIdempotencia(string caso)
        {
            const string TokenSecreto = "FAKE-tok-SEGREDO-7391";
            const string EmailSecreto = "segredo.cliente@exemplo.com";
            const string CpfSecreto = "98765432100";
            const string DispositivoSecreto = "dispositivo-SEGREDO-7391";
            Cenario cenario = new Cenario(ajustarReserva: reserva =>
            {
                reserva.Cliente.Email = EmailSecreto;
                reserva.Cliente.Cpf = CpfSecreto;
            });
            LocacaoVO vo = new LocacaoVO { IdReserva = IdReserva, CaucaoTokenCartao = TokenSecreto, CaucaoMetodoPagamentoId = Bandeira, CaucaoIdentificadorDispositivo = DispositivoSecreto };

            if (caso == "recusa")
            {
                cenario.Gateway.ExcecaoPreAutorizacao = new BusinessException(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro));
            }
            else if (caso == "em-analise")
            {
                cenario.PreAutorizacaoDevolve(CaucaoStatus.EM_ANALISE);
            }
            else if (caso == "falha-do-banco")
            {
                cenario.Repositorio.FalhasNaAbertura.Enqueue(new InvalidOperationException());
            }
            else if (caso == "divergencia")
            {
                cenario.Gateway.ResponderPreAutorizacao = (CaucaoSolicitacao solicitacao) => FakeCaucaoGateway.RetornoNoEstado(Identificador, CaucaoStatus.RESERVADA, solicitacao.Valor + 1m, solicitacao.ReferenciaExterna);
            }
            else if (caso == "falha-do-provedor")
            {
                cenario.Gateway.ExcecaoPreAutorizacao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            }

            Exception? excecao = await Record.ExceptionAsync(() => cenario.Servico.StartFromReservaAsync(vo, IdFuncionario));

            Assert.Equal(caso == "aprovado", excecao == null);
            Assert.NotEmpty(cenario.Log.Entries);
            Assert.Equal(TokenSecreto, Assert.Single(cenario.Gateway.Solicitacoes).TokenCartao);

            foreach (string entrada in cenario.Log.Entries)
            {
                Assert.DoesNotContain(TokenSecreto, entrada);
                Assert.DoesNotContain(EmailSecreto, entrada);
                Assert.DoesNotContain(CpfSecreto, entrada);
                Assert.DoesNotContain(DispositivoSecreto, entrada);
                Assert.DoesNotContain(FakeCaucaoGateway.CartaoFinalDeTeste, entrada);
                Assert.DoesNotMatch("caucao-[0-9a-f]{40}", entrada);
            }
        }
    }
}
