using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.ViewObjects.Devolucao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Tests.Fakes;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Devolucao;

/// <summary>
/// Encerramento da locacao (DevolucaoService) com fakes: ordem de recusas, repeticao sem provedor, caucao pelo provedor (tabela
/// C1 a C14), alvo da retencao conferido antes do provedor, cobranca que faltava, status do veiculo e leitura das cobrancas.
/// </summary>
public class DevolucaoServiceTests
{
    private static readonly DateTime Agora = new DateTime(2026, 11, 26, 10, 0, 0);
    private const string IdCaucao = "FAKE-CAU-1";

    /// <summary>Locacao de 2 diarias (18/11 10h ate 20/11 0h), diaria 100 e caucao informada.</summary>
    private static Locacao CriarLocacao(StatusLocacao status = StatusLocacao.EM_ANDAMENTO, StatusVeiculo statusVeiculo = StatusVeiculo.LOCADO, decimal valorCaucao = 0m)
    {
        Veiculo veiculo = new Veiculo { IdVeiculo = 5, IdCategoria = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", ValorDiaria = 100m, KmAtual = 100, Status = statusVeiculo };
        Locacao locacao = new Locacao
        {
            IdLocacao = 10, IdVeiculo = 5, Veiculo = veiculo, IdCliente = 1, IdFuncionario = 20, KmSaida = 100, ValorDiaria = 100m,
            ValorCaucao = valorCaucao, DataRetirada = new DateTime(2026, 11, 18, 10, 0, 0), DataDevolucaoPrevista = new DateTime(2026, 11, 20, 0, 0, 0),
            Status = status
        };

        return locacao;
    }

    private static Vistoria CriarSaida(int km = 150)
    {
        Vistoria saida = new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, KmRegistrado = km, DataHora = new DateTime(2026, 11, 26, 9, 0, 0), ComparacaoRealizada = true };

        return saida;
    }

    private static Pagamento CriarPagamento(int id, TipoPagamento tipo, StatusPagamento status, decimal valor, string? idTransacao = null)
    {
        Pagamento pagamento = new Pagamento
        {
            IdPagamento = id, IdLocacao = 10, Tipo = tipo, Status = status, Valor = valor, DataVencimento = new DateOnly(2026, 11, 18),
            IdTransacao = idTransacao, CreatedAt = Agora, UpdatedAt = Agora
        };

        return pagamento;
    }

    /// <summary>Total confirmado da tela: 2 diarias de 100 mais as extras.</summary>
    private static EncerrarLocacaoVO Modelo(decimal total) => new EncerrarLocacaoVO { IdConfiguracao = 1, VersaoEsperada = 1, TotalConfirmado = total };

    private static PreValidacaoEncerramentoRegistro Pre(decimal total, bool apuracaoExistente = true, int? idLancado = null, int pendentes = 0)
    {
        PreValidacaoEncerramentoRegistro pre = new PreValidacaoEncerramentoRegistro
        {
            Total = total, ApuracaoExistente = apuracaoExistente, IdPagamentoLancado = idLancado, QuantidadeCobrancasPendentes = pendentes
        };

        return pre;
    }

    private static CaucaoResultado Resultado(string id, CaucaoStatus status, decimal valor, decimal? valorCapturado = null)
    {
        CaucaoResultado resultado = FakeCaucaoGateway.RetornoNoEstado(id, status, valor, string.Empty);
        resultado.ValorCapturado = valorCapturado;

        return resultado;
    }

    private sealed class Cenario
    {
        public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();
        public FakeVistoriaSaidaRepository Saidas { get; } = new FakeVistoriaSaidaRepository();
        public FakeApuracaoCobrancasExtrasService Apuracao { get; } = new FakeApuracaoCobrancasExtrasService();
        public FakeCaucaoGateway Gateway { get; } = new FakeCaucaoGateway();
        public FakeLogService Log { get; } = new FakeLogService();

        public DevolucaoService Servico()
        {
            DevolucaoService servico = new DevolucaoService(Locacoes, Saidas, Apuracao, new CaucaoDevolucaoService(Gateway, Log), new FakeRelogio(Agora), Log);

            return servico;
        }

        /// <summary>Locacao com caucao PENDENTE de R e, quando ha extras, a COBRANCA_EXTRA 77 PENDENTE de T como alvo da apuracao.</summary>
        public static Cenario ComCaucao(decimal valorCaucao, decimal totalExtras)
        {
            Cenario cenario = new Cenario();
            Locacao locacao = CriarLocacao(valorCaucao: valorCaucao);
            locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, valorCaucao, IdCaucao));

            if (totalExtras > 0m)
            {
                locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, totalExtras));
                cenario.Apuracao.Pre = Pre(totalExtras, apuracaoExistente: true, idLancado: 77, pendentes: 1);
            }
            else
            {
                cenario.Apuracao.Pre = Pre(0m);
            }

            cenario.Locacoes.Adicionar(locacao);
            cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());

            return cenario;
        }

        public Pagamento PagamentoGravado(int idPagamento)
        {
            Pagamento pagamento = Locacoes.LocacoesGravadas().Single().Pagamentos.Single(x => x.IdPagamento == idPagamento);

            return pagamento;
        }
    }

    [Fact]
    public async Task Encerrar_SemCaucaoNemExtras_FinalizaVeiculoDisponivelELancaADiaria()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(0m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(200m));

        Assert.Equal(StatusLocacao.FINALIZADA.ToString(), resposta.Locacao.Status);
        Assert.Equal(StatusVeiculo.DISPONIVEL.ToString(), resposta.Veiculo.Status);
        Assert.Equal(150, resposta.Veiculo.KmAtual);
        Assert.Equal(AcaoCaucaoDevolucao.SEM_CAUCAO.ToString(), resposta.Caucao.Acao);
        Assert.Null(resposta.Caucao.StatusPagamento);
        Assert.Equal(200m, resposta.Locacao.ValorTotal);
        Assert.Equal(2, resposta.Locacao.DiariasPrevistas);
        Assert.NotNull(resposta.Cobrancas.IdPagamentoDiaria);
        Assert.Null(resposta.Cobrancas.IdPagamentoCobrancaExtra);
        Assert.Empty(resposta.Avisos);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.True(cenario.Locacoes.UltimoEncerramento!.LancarDiaria);
        Assert.Equal(200m, cenario.Locacoes.UltimoEncerramento.ValorDiaria);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task Encerrar_OutraReservaAtivaNoVeiculo_DeixaOVeiculoReservado()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());
        cenario.Locacoes.OutraReservaEmAberto = true;
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(0m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(200m));

        Assert.Equal(StatusVeiculo.RESERVADO.ToString(), resposta.Veiculo.Status);
        Assert.Equal(StatusVeiculo.RESERVADO, cenario.Locacoes.LocacoesGravadas().Single().Veiculo.Status);
    }

    [Fact]
    public async Task Encerrar_SemVistoriaDeSaida_Recusa409SemGravarNemChamarOProvedor()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoSemVistoriaSaida, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, cenario.Apuracao.ChamadasTotal);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task Encerrar_LocacaoCancelada_Recusa409()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao(StatusLocacao.CANCELADA));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoLocacaoCancelada, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_VeiculoForaDeLocado_Recusa409()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao(statusVeiculo: StatusVeiculo.MANUTENCAO));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoVeiculoEstadoInvalido, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_JaFinalizada_RepeteComAsMesmasInformacoesSemProvedorENemGravacao()
    {
        Cenario cenario = new Cenario();
        Locacao finalizada = CriarLocacao(StatusLocacao.FINALIZADA);
        finalizada.KmChegada = 150;
        finalizada.DataDevolucaoEfetiva = new DateTime(2026, 11, 26, 9, 0, 0);
        finalizada.ValorTotal = 200m;
        finalizada.Veiculo.Status = StatusVeiculo.DISPONIVEL;
        cenario.Locacoes.Adicionar(finalizada);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(200m));

        Assert.Equal(StatusLocacao.FINALIZADA.ToString(), resposta.Locacao.Status);
        Assert.Equal(150, resposta.Locacao.KmChegada);
        Assert.Equal(200m, resposta.Locacao.ValorTotal);
        Assert.Empty(resposta.Avisos);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task Encerrar_TotalConfirmadoDiferenteDoApurado_Recusa409SemGravarNemProvedor()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(0m);

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(199m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoTotalDivergente, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
    }

    /// <summary>
    /// Passo 9: a apuracao nova grava um total diferente do simulado. O 409 sai depois de a caucao ser consultada no provedor
    /// (risco R-2 aceito, registrado no relatorio), mas antes de qualquer gravacao da transacao final.
    /// </summary>
    [Fact]
    public async Task Encerrar_ApuracaoNovaComTotalDivergenteNoPasso9_Recusa409SemGravarDepoisDeConsultarOProvedor()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        cenario.Apuracao.Pre = Pre(0m, apuracaoExistente: false);
        cenario.Apuracao.Registro = new ResultadoRegistroApuracaoVO { Criada = true, Apuracao = new ReturnApuracaoCobrancasExtrasVO { Total = 50m } };

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoTotalDivergente, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, cenario.Locacoes.LocacoesGravadas().Single().Status);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(1).Status);
        Assert.Contains(IdCaucao, cenario.Gateway.Consultas);
    }

    [Fact]
    public async Task Encerrar_FalhaDeRedeNaConsultaDaCaucao_Devolve502ComTextoAprovadoSemEncerrarNemGravar()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        cenario.Gateway.ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, "falha de rede simulada");

        IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico().EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoProvedorIndisponivel, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, cenario.Locacoes.LocacoesGravadas().Single().Status);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
    }

    /// <summary>Apuracao cuja pre-validacao falha por integracao (fora da caucao): so TotalParaEncerramentoAsync e usado pelo teste.</summary>
    private sealed class ApuracaoComFalhaDeIntegracao : IApuracaoCobrancasExtrasService
    {
        private readonly IntegrationException _falha;

        public ApuracaoComFalhaDeIntegracao(IntegrationException falha)
        {
            _falha = falha;
        }

        public Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao)
        {
            return Task.FromException<ReturnSimulacaoCobrancasExtrasVO>(_falha);
        }

        public Task<ReturnApuracaoCobrancasExtrasVO> BuscarAsync(int idLocacao)
        {
            return Task.FromException<ReturnApuracaoCobrancasExtrasVO>(_falha);
        }

        public Task<ResultadoRegistroApuracaoVO> RegistrarAsync(int idLocacao, ApurarCobrancasExtrasVO model)
        {
            return Task.FromException<ResultadoRegistroApuracaoVO>(_falha);
        }

        public Task<PreValidacaoEncerramentoRegistro> TotalParaEncerramentoAsync(int idLocacao, int idConfiguracao, int versaoEsperada)
        {
            return Task.FromException<PreValidacaoEncerramentoRegistro>(_falha);
        }

        public Task<TotaisContextoApuracaoRegistro> TotaisDoContextoAsync(int idLocacao)
        {
            return Task.FromException<TotaisContextoApuracaoRegistro>(_falha);
        }
    }

    [Fact]
    public async Task Encerrar_FalhaDeIntegracaoDaApuracao_Devolve502ComTextoAprovadoGuardandoAFalhaOriginal()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        IntegrationException original = new IntegrationException(ConstantsIntegration.Pagamento, "falha simulada da apuracao");
        DevolucaoService servico = new DevolucaoService(cenario.Locacoes, cenario.Saidas, new ApuracaoComFalhaDeIntegracao(original),
            new CaucaoDevolucaoService(cenario.Gateway, cenario.Log), new FakeRelogio(Agora), cenario.Log);

        IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => servico.EncerrarAsync(10, Modelo(200m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoProvedorIndisponivel, erro.Message);
        Assert.Equal(ConstantsIntegration.Pagamento, erro.CodigoIntegracao);
        Assert.Same(original, erro.InnerException);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, cenario.Locacoes.LocacoesGravadas().Single().Status);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
    }

    [Fact]
    public async Task Encerrar_ApuracaoAntigaComCaucaoEmFalhaDeRede_NaoLancaACobrancaQueFalta()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 0);
        cenario.Gateway.ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, "falha de rede simulada");

        IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoProvedorIndisponivel, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.DoesNotContain(cenario.Locacoes.LocacoesGravadas().Single().Pagamentos, x => x.Tipo == TipoPagamento.COBRANCA_EXTRA);
    }

    [Fact]
    public async Task Encerrar_RetencaoParcial_CapturaSoAsExtrasQuitaOAlvoSemIdDeTransacaoENaoVazaOIdNoLog()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL.ToString(), resposta.Caucao.Acao);
        Assert.Equal(300m, resposta.Caucao.ValorRetido);
        Assert.Equal(200m, resposta.Caucao.ValorLiberado);
        Assert.Equal(new[] { IdCaucao }, cenario.Gateway.Capturas);
        Assert.Equal(300m, cenario.Gateway.ValoresCapturados.Single());
        Assert.Empty(cenario.Gateway.Cancelamentos);
        Assert.True(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Equal(77, cenario.Locacoes.UltimoEncerramento.IdPagamentoAlvo);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
        Assert.Equal(ConstantsValues.FormaCartao, cenario.PagamentoGravado(77).Forma);
        Assert.Null(cenario.PagamentoGravado(77).IdTransacao);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(1).Status);
        Assert.DoesNotContain(cenario.Log.Entries, x => x.Contains(IdCaucao, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Encerrar_RetencaoTotal_CapturaOValorIntegralDaCaucao()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 500m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(700m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA.ToString(), resposta.Caucao.Acao);
        Assert.Equal(500m, resposta.Caucao.ValorRetido);
        Assert.Equal(0m, resposta.Caucao.ValorLiberado);
        Assert.Equal(500m, cenario.Gateway.ValoresCapturados.Single());
        Assert.Empty(cenario.Gateway.Cancelamentos);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
    }

    [Fact]
    public async Task Encerrar_TotalMaiorQueACaucao_CancelaLiberaACaucaoELancaTudoPorPix()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 600m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(800m));

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA.ToString(), resposta.Caucao.Acao);
        Assert.Equal(StatusPagamento.CANCELADO.ToString(), resposta.Caucao.StatusPagamento);
        Assert.Equal(new[] { IdCaucao }, cenario.Gateway.Cancelamentos);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Equal(new[] { ConstantsMessage.AvisoDevolucaoExtrasAcimaDaCaucao }, resposta.Avisos);
        Assert.False(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Null(cenario.Locacoes.UltimoEncerramento.IdPagamentoAlvo);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(77).Status);
    }

    [Fact]
    public async Task Encerrar_CapturaComFalhaDeRedeSeguidaDeReconsultaRESERVADA_Devolve502SemGravar()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);
        cenario.Gateway.ExcecaoCaptura = new IntegrationException(ConstantsIntegration.Pagamento, "falha de rede na captura");

        IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroDevolucaoProvedorIndisponivel, erro.Message);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, cenario.Locacoes.LocacoesGravadas().Single().Status);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(1).Status);
    }

    [Fact]
    public async Task Encerrar_CapturaComFalhaDeRedeSeguidaDeReconsultaCapturada_SegueComoRetencao()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);
        cenario.Gateway.ExcecaoCaptura = new IntegrationException(ConstantsIntegration.Pagamento, "falha de rede na captura");
        int consultas = 0;
        cenario.Gateway.ResponderConsulta = id =>
        {
            consultas++;

            return consultas == 1 ? FakeCaucaoGateway.RetornoNoEstado(id, CaucaoStatus.RESERVADA, 500m, string.Empty) : Resultado(id, CaucaoStatus.CAPTURADA, 500m, 300m);
        };

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL.ToString(), resposta.Caucao.Acao);
        Assert.Equal(300m, resposta.Caucao.ValorRetido);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
    }

    [Fact]
    public async Task Encerrar_ErroInesperadoNaCapturaSeguidoDeReconsultaRESERVADA_EncerraComoNaoConfirmada()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);
        cenario.Gateway.ExcecaoCaptura = new BusinessException("erro inesperado do provedor na captura");

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR.ToString(), resposta.Caucao.Acao);
        Assert.True(resposta.Caucao.TratarAMao);
        Assert.Equal(new[] { ConstantsMessage.AvisoDevolucaoCaucaoDivergente }, resposta.Avisos);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Null(cenario.Locacoes.UltimoEncerramento!.IdPagamentoAlvo);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(1).Status);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(77).Status);
    }

    [Fact]
    public async Task Encerrar_CaucaoJaCapturadaComOTotal_NaoChamaCapturarNemCancelarEFazARetencao()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);
        cenario.Gateway.ResponderConsulta = id => Resultado(id, CaucaoStatus.CAPTURADA, 500m, 300m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL.ToString(), resposta.Caucao.Acao);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
        Assert.Single(cenario.Gateway.Consultas);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
    }

    [Theory]
    [InlineData(CaucaoStatus.EXPIRADA)]
    [InlineData(CaucaoStatus.RECUSADA)]
    [InlineData(CaucaoStatus.ESTORNADA)]
    public async Task Encerrar_CaucaoSemGarantia_BaixaELiberaAsExtrasParaPixComAvisoSemGarantia(CaucaoStatus estado)
    {
        Cenario cenario = Cenario.ComCaucao(500m, 300m);
        cenario.Gateway.ResponderConsulta = id => Resultado(id, estado, 500m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA.ToString(), resposta.Caucao.Acao);
        Assert.Equal(StatusPagamento.CANCELADO.ToString(), resposta.Caucao.StatusPagamento);
        Assert.Equal(new[] { ConstantsMessage.AvisoDevolucaoCaucaoSemGarantia }, resposta.Avisos);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
        Assert.False(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Equal(StatusPagamento.PENDENTE, cenario.PagamentoGravado(77).Status);
    }

    [Fact]
    public async Task Encerrar_CaucaoEmAnalise_NaoCapturaNemCancelaEEncerraTratandoAMao()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        cenario.Gateway.ResponderConsulta = id => Resultado(id, CaucaoStatus.EM_ANALISE, 500m);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(200m));

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR.ToString(), resposta.Caucao.Acao);
        Assert.True(resposta.Caucao.TratarAMao);
        Assert.Equal(new[] { ConstantsMessage.AvisoDevolucaoCaucaoEmAnalise }, resposta.Avisos);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_CaucaoNaoEncontradaNoProvedor_EncerraComAvisoDeNaoEncontrada()
    {
        Cenario cenario = Cenario.ComCaucao(500m, 0m);
        cenario.Gateway.ExcecaoConsulta = new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(200m));

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR.ToString(), resposta.Caucao.Acao);
        Assert.True(resposta.Caucao.TratarAMao);
        Assert.Equal(new[] { ConstantsMessage.AvisoDevolucaoCaucaoNaoEncontrada }, resposta.Avisos);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.Empty(cenario.Gateway.Cancelamentos);
    }

    [Fact]
    public async Task Encerrar_AlvoComValorDiferenteDeT_Recusa409SemChamarOProvedor()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacaoComAlvo(500m, valorAlvo: 250m, statusAlvo: StatusPagamento.PENDENTE));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: 77, pendentes: 1);

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito, erro.Message);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Theory]
    [InlineData(StatusPagamento.PENDENTE)]
    [InlineData(StatusPagamento.ATRASADO)]
    public async Task Encerrar_AlvoEmAberto_RetemSoAsExtrasEQuitaOAlvo(StatusPagamento statusAlvo)
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacaoComAlvo(500m, valorAlvo: 300m, statusAlvo: statusAlvo));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: 77, pendentes: 1);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL.ToString(), resposta.Caucao.Acao);
        Assert.Equal(300m, cenario.Gateway.ValoresCapturados.Single());
        Assert.True(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Equal(77, cenario.Locacoes.UltimoEncerramento.IdPagamentoAlvo);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_AlvoJaPago_LiberaACaucaoComoNoCasoSemExtrasSemAvisoENaoRetemNada()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacaoComAlvo(500m, valorAlvo: 300m, statusAlvo: StatusPagamento.PAGO));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: 77, pendentes: 0);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA.ToString(), resposta.Caucao.Acao);
        Assert.Equal(0m, resposta.Caucao.ValorRetido);
        Assert.Equal(500m, resposta.Caucao.ValorLiberado);
        Assert.Empty(resposta.Avisos);
        Assert.Equal(new[] { IdCaucao }, cenario.Gateway.Cancelamentos);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.False(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Null(cenario.Locacoes.UltimoEncerramento.IdPagamentoAlvo);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_AlvoAmbiguoComDuasCobrancasPendentes_Recusa409SemProvedor()
    {
        Cenario cenario = new Cenario();
        Locacao locacao = CriarLocacao(valorCaucao: 500m);
        locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 500m, IdCaucao));
        locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, 300m));
        locacao.Pagamentos.Add(CriarPagamento(78, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, 300m));
        cenario.Locacoes.Adicionar(locacao);
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 2);

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito, erro.Message);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task Encerrar_ApuracaoAntigaSemCobrancaLancada_LancaACobrancaQueFaltaNaTransacaoFinal()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 0);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.True(cenario.Locacoes.UltimoEncerramento!.LancarCobrancaFaltante);
        Assert.Equal(300m, cenario.Locacoes.UltimoEncerramento.ValorCobrancaFaltante);
        Assert.False(cenario.Locacoes.UltimoEncerramento.CobrancaFaltanteRetida);
        Assert.NotNull(resposta.Cobrancas.IdPagamentoCobrancaExtra);
        Assert.Equal(0, cenario.Apuracao.ChamadasRegistrar);
        Assert.Equal(1, cenario.Locacoes.LocacoesGravadas().Single().Pagamentos.Count(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA));
    }

    /// <summary>O-11: apuracao antiga sem cobranca lancada; a unica COBRANCA_EXTRA de valor T em aberto vira o alvo (sem lancar outra).</summary>
    [Theory]
    [InlineData(StatusPagamento.PENDENTE)]
    [InlineData(StatusPagamento.ATRASADO)]
    public async Task Encerrar_ApuracaoAntigaComUmaCobrancaEmAbertoDeValorT_RetemEQuitaEsseAlvoSemLancarOutro(StatusPagamento statusAlvo)
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacaoComAlvo(500m, valorAlvo: 300m, statusAlvo: statusAlvo));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 1);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL.ToString(), resposta.Caucao.Acao);
        Assert.Equal(300m, cenario.Gateway.ValoresCapturados.Single());
        Assert.True(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.Equal(77, cenario.Locacoes.UltimoEncerramento.IdPagamentoAlvo);
        Assert.False(cenario.Locacoes.UltimoEncerramento.LancarCobrancaFaltante);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
        Assert.Equal(1, cenario.Locacoes.LocacoesGravadas().Single().Pagamentos.Count(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA));
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    /// <summary>O-11: apuracao antiga com a unica COBRANCA_EXTRA de valor T ja PAGA: nada a reter, caucao liberada, nada lancado de novo.</summary>
    [Fact]
    public async Task Encerrar_ApuracaoAntigaComCobrancaDeValorTJaPaga_NaoRetemNadaNemLancaOutra()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacaoComAlvo(500m, valorAlvo: 300m, statusAlvo: StatusPagamento.PAGO));
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 0);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA.ToString(), resposta.Caucao.Acao);
        Assert.Equal(0m, resposta.Caucao.ValorRetido);
        Assert.Equal(500m, resposta.Caucao.ValorLiberado);
        Assert.Empty(cenario.Gateway.Capturas);
        Assert.False(cenario.Locacoes.UltimoEncerramento!.QuitarAlvo);
        Assert.False(cenario.Locacoes.UltimoEncerramento.LancarCobrancaFaltante);
        Assert.Equal(StatusPagamento.PAGO, cenario.PagamentoGravado(77).Status);
        Assert.Equal(1, cenario.Locacoes.LocacoesGravadas().Single().Pagamentos.Count(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA));
        Assert.Equal(1, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    /// <summary>O-11: nenhuma COBRANCA_EXTRA de valor T (so uma PAGA de outro valor): lanca a cobranca que falta na transacao final.</summary>
    [Fact]
    public async Task Encerrar_ApuracaoAntigaSemCobrancaDeValorT_LancaAQueFaltaMesmoComOutraJaPaga()
    {
        Cenario cenario = new Cenario();
        Locacao locacao = CriarLocacao(valorCaucao: 500m);
        locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 500m, IdCaucao));
        locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PAGO, 250m));
        cenario.Locacoes.Adicionar(locacao);
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 0);

        ReturnEncerramentoLocacaoVO resposta = await cenario.Servico().EncerrarAsync(10, Modelo(500m));

        Assert.True(cenario.Locacoes.UltimoEncerramento!.LancarCobrancaFaltante);
        Assert.Equal(300m, cenario.Locacoes.UltimoEncerramento.ValorCobrancaFaltante);
        Assert.Equal(2, cenario.Locacoes.LocacoesGravadas().Single().Pagamentos.Count(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA));
        Assert.Equal(0, cenario.Apuracao.ChamadasRegistrar);
        Assert.NotNull(resposta.Cobrancas.IdPagamentoCobrancaExtra);
    }

    /// <summary>O-11: mais de uma COBRANCA_EXTRA de valor T (uma PAGA e uma PENDENTE): ambiguo, 409 sem provedor.</summary>
    [Fact]
    public async Task Encerrar_ApuracaoAntigaComDuasCobrancasDeValorT_Recusa409SemProvedor()
    {
        Cenario cenario = new Cenario();
        Locacao locacao = CriarLocacao(valorCaucao: 500m);
        locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 500m, IdCaucao));
        locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PAGO, 300m));
        locacao.Pagamentos.Add(CriarPagamento(78, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, 300m));
        cenario.Locacoes.Adicionar(locacao);
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 1);

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito, erro.Message);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    /// <summary>O-11 (escolha conservadora): alvo de valor T e outra COBRANCA_EXTRA em aberto de outro valor: 409 sem provedor, para nao cobrar em dobro.</summary>
    [Fact]
    public async Task Encerrar_ApuracaoAntigaComAlvoDeValorTEOutraEmAbertoDeOutroValor_Recusa409SemProvedor()
    {
        Cenario cenario = new Cenario();
        Locacao locacao = CriarLocacao(valorCaucao: 500m);
        locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 500m, IdCaucao));
        locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, 300m));
        locacao.Pagamentos.Add(CriarPagamento(78, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, 250m));
        cenario.Locacoes.Adicionar(locacao);
        cenario.Saidas.Vistorias.Vistorias.Add(CriarSaida());
        cenario.Apuracao.Pre = Pre(300m, apuracaoExistente: true, idLancado: null, pendentes: 2);

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico().EncerrarAsync(10, Modelo(500m)));

        Assert.Equal(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito, erro.Message);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        Assert.Equal(0, cenario.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task ListarCobrancas_SemApuracao_Devolve200ComListaVazia()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());

        ReturnCobrancasLocacaoVO resposta = await cenario.Servico().ListarCobrancasAsync(10);

        Assert.Equal(10, resposta.IdLocacao);
        Assert.Empty(resposta.Cobrancas);
        Assert.Equal(0m, resposta.Total);
    }

    [Fact]
    public async Task ListarCobrancas_ComCobrancasDaLocacao_SomaSemRecalcular()
    {
        Cenario cenario = new Cenario();
        cenario.Locacoes.Adicionar(CriarLocacao());
        cenario.Locacoes.CobrancasExtrasSemeadas.Add(new CobrancaExtra { IdCobranca = 1, IdLocacao = 10, Tipo = TipoCobrancaExtra.KM_EXCEDENTE, Valor = 120m, DataLancamento = new DateOnly(2026, 11, 26), Descricao = "Km" });
        cenario.Locacoes.CobrancasExtrasSemeadas.Add(new CobrancaExtra { IdCobranca = 2, IdLocacao = 10, Tipo = TipoCobrancaExtra.ATRASO, Valor = 80m, DataLancamento = new DateOnly(2026, 11, 26), Descricao = "Atraso" });

        ReturnCobrancasLocacaoVO resposta = await cenario.Servico().ListarCobrancasAsync(10);

        Assert.Equal(200m, resposta.Total);
        Assert.Equal(2, resposta.Cobrancas.Count);
        Assert.Equal(TipoCobrancaExtra.KM_EXCEDENTE.ToString(), resposta.Cobrancas[0].Tipo);
        Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task ListarCobrancas_LocacaoInexistente_Devolve404()
    {
        Cenario cenario = new Cenario();

        await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico().ListarCobrancasAsync(999));
    }

    /// <summary>Locacao com caucao PENDENTE de R e a COBRANCA_EXTRA 77 com o valor e o status pedidos (alvo para os casos de conferencia).</summary>
    private static Locacao CriarLocacaoComAlvo(decimal valorCaucao, decimal valorAlvo, StatusPagamento statusAlvo)
    {
        Locacao locacao = CriarLocacao(valorCaucao: valorCaucao);
        locacao.Pagamentos.Add(CriarPagamento(1, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, valorCaucao, IdCaucao));
        locacao.Pagamentos.Add(CriarPagamento(77, TipoPagamento.COBRANCA_EXTRA, statusAlvo, valorAlvo));

        return locacao;
    }
}
