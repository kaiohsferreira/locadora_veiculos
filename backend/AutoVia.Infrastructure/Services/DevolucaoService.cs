using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Devolucao;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.ViewObjects.Devolucao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services;

/// <summary>
/// Encerramento da locacao na devolucao (P5-BE-04, RF-25; devolucao-encerramento v4). Ordem: politica e corpo, leitura da
/// locacao (404), repeticao (200 sem provedor), cancelada (409), vistoria de saida (409), veiculo LOCADO (409), pre-validacao
/// sem gravar, total confirmado (409), alvo da retencao (409), caucao no provedor (502 sem gravar), apuracao gravada e, por
/// fim, a transacao final que grava tudo num SaveChanges. Nada e gravado antes do provedor.
/// </summary>
public sealed class DevolucaoService : IDevolucaoService
{
    private readonly ILocacaoRepository _locacoes;
    private readonly IVistoriaSaidaRepository _saidas;
    private readonly IApuracaoCobrancasExtrasService _apuracao;
    private readonly ICaucaoDevolucaoService _caucao;
    private readonly IRelogio _relogio;
    private readonly ILogService _logService;

    public DevolucaoService(ILocacaoRepository locacoes, IVistoriaSaidaRepository saidas, IApuracaoCobrancasExtrasService apuracao,
        ICaucaoDevolucaoService caucao, IRelogio relogio, ILogService logService)
    {
        _locacoes = locacoes;
        _saidas = saidas;
        _apuracao = apuracao;
        _caucao = caucao;
        _relogio = relogio;
        _logService = logService;
    }

    /// <summary>Encerra a locacao: confere tudo sem gravar, decide a caucao no provedor e grava numa transacao unica.</summary>
    public async Task<ReturnEncerramentoLocacaoVO> EncerrarAsync(int idLocacao, EncerrarLocacaoVO model)
    {
        try
        {
            EncerrarLocacaoVOValidator validator = new EncerrarLocacaoVOValidator();
            await validator.ValidateAndThrowAsync(model);

            Locacao locacao = await ObterLocacaoAsync(idLocacao);

            if (locacao.Status == StatusLocacao.FINALIZADA)
            {
                return await MontarRepeticaoAsync(locacao);
            }

            if (locacao.Status == StatusLocacao.CANCELADA)
            {
                throw new BusinessException(ConstantsMessage.ErroDevolucaoLocacaoCancelada);
            }

            Vistoria? saida = await _saidas.FindSaidaByLocacaoAsync(idLocacao);

            if (saida == null)
            {
                throw new BusinessException(ConstantsMessage.ErroDevolucaoSemVistoriaSaida);
            }

            if (locacao.Veiculo.Status != StatusVeiculo.LOCADO)
            {
                throw new BusinessException(ConstantsMessage.ErroDevolucaoVeiculoEstadoInvalido);
            }

            PreValidacaoEncerramentoRegistro pre = await _apuracao.TotalParaEncerramentoAsync(idLocacao, model.IdConfiguracao, model.VersaoEsperada);
            int diarias = RegrasDevolucao.CalcularDiarias(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
            decimal valorTotal = RegrasDevolucao.CalcularValorTotal(diarias, locacao.ValorDiaria, pre.Total);

            if (!RegrasDevolucao.TotalConfere(model.TotalConfirmado, valorTotal))
            {
                throw new BusinessException(ConstantsMessage.ErroDevolucaoTotalDivergente);
            }

            List<Pagamento> pagamentos = await _locacoes.ListarPagamentosAtivosDaLocacaoAsync(idLocacao);
            AlvoRetencao alvo = ConferirAlvoRetencao(idLocacao, pre, pagamentos);
            Pagamento? linhaCaucao = pagamentos.FirstOrDefault(x => x.Tipo == TipoPagamento.CAUCAO);
            decimal totalParaCaucao = alvo.JaQuitado ? 0m : pre.Total;
            DecisaoCaucaoEncerramento decisao = await _caucao.DecidirAsync(locacao, linhaCaucao, totalParaCaucao);
            int? idAlvoAposApuracao = null;

            if (!pre.ApuracaoExistente)
            {
                ResultadoRegistroApuracaoVO registro = await _apuracao.RegistrarAsync(idLocacao,
                    new ApurarCobrancasExtrasVO { IdConfiguracao = model.IdConfiguracao, VersaoEsperada = model.VersaoEsperada });

                if (registro.Apuracao.Total != pre.Total)
                {
                    _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoTotalDivergente, idLocacao, registro.Apuracao.Total), GetType().ToString());
                    throw new BusinessException(ConstantsMessage.ErroDevolucaoTotalDivergente);
                }

                idAlvoAposApuracao = registro.Apuracao.IdPagamentoLancado;
            }

            DateTime agora = _relogio.Agora;
            bool retida = decisao.QuitarAlvo && pre.Total > 0m;
            int? idAlvo = alvo.IdAlvo ?? idAlvoAposApuracao;
            bool lancarFaltante = alvo.LancarFaltante && pre.Total > 0m;

            if (retida && !lancarFaltante && !idAlvo.HasValue)
            {
                _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoAlvoRetencaoInconsistente, idLocacao, string.Empty), GetType().ToString());

                throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
            }

            EncerramentoRegistro registroFinal = new EncerramentoRegistro
            {
                IdLocacao = idLocacao,
                IdReserva = locacao.IdReserva,
                IdVeiculo = locacao.IdVeiculo,
                DataDevolucaoEfetiva = saida.DataHora,
                KmChegada = saida.KmRegistrado,
                ValorTotal = valorTotal,
                Agora = agora,
                Hoje = DateOnly.FromDateTime(agora),
                IdPagamentoCaucao = linhaCaucao?.IdPagamento,
                NovoStatusCaucao = decisao.NovoStatusCaucao,
                ValorCaucaoGravado = decisao.ValorCaucaoGravado,
                DataHojeNaCaucao = decisao.DataHojeNaCaucao,
                IdPagamentoAlvo = retida && !lancarFaltante ? idAlvo : null,
                QuitarAlvo = retida && !lancarFaltante && idAlvo.HasValue,
                LancarCobrancaFaltante = lancarFaltante,
                ValorCobrancaFaltante = pre.Total,
                CobrancaFaltanteRetida = retida,
                LancarDiaria = RegrasDevolucao.DiariaDeveLancar(diarias, locacao.ValorDiaria),
                ValorDiaria = RegrasDevolucao.ValorDaDiaria(diarias, locacao.ValorDiaria)
            };

            ResultadoEncerramento resultado = await _locacoes.EncerrarAtomicamenteAsync(registroFinal);

            if (!resultado.Criada)
            {
                return await MontarRepeticaoAsync(resultado.Locacao);
            }

            ReturnEncerramentoLocacaoVO resposta = MontarResposta(locacao, saida, resultado, pre.Total, valorTotal, diarias, decisao, linhaCaucao, idAlvo ?? alvo.IdJaQuitado, resultado.IdPagamentoCobrancaFaltante);

            _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoEncerrada, idLocacao, locacao.IdVeiculo,
                resultado.StatusVeiculo.ToString(), decisao.Acao.ToString(), resultado.IdPagamentoDiaria), GetType().ToString());

            return resposta;
        }
        catch (BusinessException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
        catch (NotFoundException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
        catch (ValidationException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerramentoParametros, GetType().ToString());
            throw;
        }
        catch (IntegrationException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroDevolucaoProvedorIndisponivel, GetType().ToString());
            throw new IntegrationException(ex.CodigoIntegracao, ConstantsMessage.ErroDevolucaoProvedorIndisponivel, ex);
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Lista as cobrancas extras ativas da locacao, sem recalcular e sem provedor (200 com lista vazia quando nao ha).</summary>
    public async Task<ReturnCobrancasLocacaoVO> ListarCobrancasAsync(int idLocacao)
    {
        try
        {
            await ObterLocacaoAsync(idLocacao);
            List<CobrancaExtra> cobrancas = await _locacoes.ListarCobrancasExtrasAtivasDaLocacaoAsync(idLocacao);
            decimal total = cobrancas.Sum(x => x.Valor);

            ReturnCobrancasLocacaoVO resposta = new ReturnCobrancasLocacaoVO
            {
                IdLocacao = idLocacao,
                Total = total,
                TotalStr = StaticMethods.FormateToMonetary(total),
                Cobrancas = cobrancas.Select(x => new ReturnCobrancaLocacaoVO
                {
                    IdCobranca = x.IdCobranca,
                    IdApuracao = x.IdApuracao,
                    Tipo = x.Tipo.ToString(),
                    Descricao = x.Descricao ?? string.Empty,
                    Valor = x.Valor,
                    ValorStr = StaticMethods.FormateToMonetary(x.Valor),
                    DataLancamento = x.DataLancamento,
                    DataLancamentoStr = StaticMethods.FormateDateToPTBR(x.DataLancamento)
                }).ToList()
            };

            return resposta;
        }
        catch (BusinessException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
            throw;
        }
        catch (NotFoundException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
            throw;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Locacao ativa da locadora do contexto (404 quando nao existe, esta desativada ou e de outra locadora).</summary>
    private async Task<Locacao> ObterLocacaoAsync(int idLocacao)
    {
        Locacao? locacao;

        try
        {
            locacao = await _locacoes.FindParaEncerramentoAsync(idLocacao);
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }

        if (locacao == null)
        {
            throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
        }

        return locacao;
    }

    /// <summary>
    /// Alvo da retencao, conferido ANTES do provedor (O-3 e O-10): apuracao antiga com alvo lancado precisa ser a COBRANCA_EXTRA
    /// EM ABERTO (PENDENTE ou ATRASADO) de valor igual a T; se ja esta PAGO, nao ha o que reter (JaQuitado). Apuracao antiga sem
    /// cobranca lancada (O-11): entre as COBRANCA_EXTRA de valor T (PENDENTE, ATRASADO ou PAGO), uma so vira o alvo; nenhuma lanca
    /// a que falta (na transacao final); mais de uma, ou COBRANCA_EXTRA em aberto de outro valor, e 409. Apuracao nova nao pode ter
    /// COBRANCA_EXTRA em aberto. Qualquer outro caso: 409, sem provedor.
    /// </summary>
    private AlvoRetencao ConferirAlvoRetencao(int idLocacao, PreValidacaoEncerramentoRegistro pre, List<Pagamento> pagamentos)
    {
        try
        {
            AlvoRetencao alvo = new AlvoRetencao();

            if (pre.Total <= 0m)
            {
                return alvo;
            }

            List<Pagamento> abertas = pagamentos.Where(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA
                && RegrasDevolucao.AlvoRetencaoEmAberto(x.Status)).ToList();

            if (!pre.ApuracaoExistente)
            {
                if (abertas.Count > 0)
                {
                    throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
                }

                return alvo;
            }

            if (pre.IdPagamentoLancado.HasValue)
            {
                Pagamento? lancado = pagamentos.FirstOrDefault(x => x.IdPagamento == pre.IdPagamentoLancado.Value && x.Tipo == TipoPagamento.COBRANCA_EXTRA
                    && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO || x.Status == StatusPagamento.PAGO));

                if (lancado == null || lancado.Valor != pre.Total)
                {
                    _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoAlvoRetencaoInconsistente, idLocacao, pre.IdPagamentoLancado.Value), GetType().ToString());
                    throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
                }

                if (lancado.Status == StatusPagamento.PAGO)
                {
                    alvo.JaQuitado = true;
                    alvo.IdJaQuitado = lancado.IdPagamento;

                    return alvo;
                }

                alvo.IdAlvo = lancado.IdPagamento;

                return alvo;
            }

            List<Pagamento> iguais = pagamentos.Where(x => x.Tipo == TipoPagamento.COBRANCA_EXTRA
                && (RegrasDevolucao.AlvoRetencaoEmAberto(x.Status) || x.Status == StatusPagamento.PAGO)
                && x.Valor == pre.Total).ToList();
            bool haAbertaDeOutroValor = abertas.Any(x => x.Valor != pre.Total);

            if (iguais.Count > 1 || haAbertaDeOutroValor)
            {
                _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoAlvoRetencaoInconsistente, idLocacao, iguais.Count), GetType().ToString());

                throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
            }

            if (iguais.Count == 0)
            {
                alvo.LancarFaltante = true;

                return alvo;
            }

            Pagamento unico = iguais[0];

            if (unico.Status == StatusPagamento.PAGO)
            {
                alvo.JaQuitado = true;
                alvo.IdJaQuitado = unico.IdPagamento;

                return alvo;
            }

            alvo.IdAlvo = unico.IdPagamento;

            return alvo;
        }
        catch (BusinessException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Resposta da repeticao (locacao ja FINALIZADA): tudo do banco, sem provedor, sem avisos.</summary>
    private async Task<ReturnEncerramentoLocacaoVO> MontarRepeticaoAsync(Locacao locacao)
    {
        try
        {
            List<Pagamento> pagamentos = await _locacoes.ListarPagamentosAtivosDaLocacaoAsync(locacao.IdLocacao);
            Pagamento? linhaCaucao = pagamentos.FirstOrDefault(x => x.Tipo == TipoPagamento.CAUCAO);
            Pagamento? diaria = pagamentos.FirstOrDefault(x => x.Tipo == TipoPagamento.DIARIA);
            TotaisDevolucao totais = await LerTotaisAsync(locacao.IdLocacao);
            int diarias = RegrasDevolucao.CalcularDiarias(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
            decimal totalDiarias = RegrasDevolucao.ValorDaDiaria(diarias, locacao.ValorDiaria);
            AcaoCaucaoDevolucao acao = _caucao.AcaoDoBanco(linhaCaucao, locacao.ValorCaucao);
            decimal retido = acao == AcaoCaucaoDevolucao.RETIDA || acao == AcaoCaucaoDevolucao.RETIDA_PARCIAL ? linhaCaucao?.Valor ?? 0m : 0m;
            decimal liberado = acao == AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR || acao == AcaoCaucaoDevolucao.SEM_CAUCAO ? 0m : RegrasDevolucao.ValorLiberadoDaCaucao(locacao.ValorCaucao, retido);
            int kmChegada = locacao.KmChegada.GetValueOrDefault();
            DateTime dataDevolucao = locacao.DataDevolucaoEfetiva.GetValueOrDefault();

            ReturnEncerramentoLocacaoVO resposta = new ReturnEncerramentoLocacaoVO
            {
                Locacao = MontarLocacaoEncerrada(locacao, kmChegada, dataDevolucao, StatusLocacao.FINALIZADA, diarias, locacao.ValorTotal.GetValueOrDefault()),
                Veiculo = new ReturnVeiculoEncerradoVO { IdVeiculo = locacao.IdVeiculo, Status = locacao.Veiculo.Status.ToString(), KmAtual = locacao.Veiculo.KmAtual },
                Cobrancas = MontarCobrancas(totais.TotalExtras, totalDiarias, totais.IdPagamentoCobrancaExtra, diaria?.IdPagamento),
                Caucao = MontarCaucao(locacao.ValorCaucao, acao, retido, liberado, linhaCaucao?.Status.ToString(), acao == AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR),
                Avisos = new List<string>()
            };

            return resposta;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Total das extras e id do pagamento da apuracao, lidos do snapshot (sem gravar e sem provedor).</summary>
    private async Task<TotaisDevolucao> LerTotaisAsync(int idLocacao)
    {
        try
        {
            TotaisContextoApuracaoRegistro totais = await _apuracao.TotaisDoContextoAsync(idLocacao);
            int? idPagamentoCobrancaExtra = null;

            try
            {
                ReturnApuracaoCobrancasExtrasVO apuracao = await _apuracao.BuscarAsync(idLocacao);
                idPagamentoCobrancaExtra = apuracao.IdPagamentoLancado;
            }
            catch (NotFoundException)
            {
                _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoSemApuracao, idLocacao), GetType().ToString());
                idPagamentoCobrancaExtra = null;
            }

            TotaisDevolucao resultado = new TotaisDevolucao
            {
                TotalExtras = totais.TotalExtras ?? 0m,
                IdPagamentoCobrancaExtra = idPagamentoCobrancaExtra
            };

            return resultado;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    private static ReturnEncerramentoLocacaoVO MontarResposta(Locacao locacao, Vistoria saida, ResultadoEncerramento resultado, decimal totalExtras, decimal valorTotal,
        int diarias, DecisaoCaucaoEncerramento decisao, Pagamento? linhaCaucao, int? idAlvo, int? idCobrancaFaltante)
    {
        decimal totalDiarias = RegrasDevolucao.ValorDaDiaria(diarias, locacao.ValorDiaria);
        int? idCobrancaExtra = totalExtras > 0m ? idAlvo ?? idCobrancaFaltante : null;
        ReturnEncerramentoLocacaoVO resposta = new ReturnEncerramentoLocacaoVO
        {
            Locacao = MontarLocacaoEncerrada(locacao, saida.KmRegistrado, saida.DataHora, StatusLocacao.FINALIZADA, diarias, valorTotal),
            Veiculo = new ReturnVeiculoEncerradoVO { IdVeiculo = locacao.IdVeiculo, Status = resultado.StatusVeiculo.ToString(), KmAtual = resultado.KmAtualVeiculo },
            Cobrancas = MontarCobrancas(totalExtras, totalDiarias, idCobrancaExtra, resultado.IdPagamentoDiaria),
            Caucao = MontarCaucao(locacao.ValorCaucao, decisao.Acao, decisao.ValorRetido, decisao.ValorLiberado,
                decisao.NovoStatusCaucao?.ToString() ?? linhaCaucao?.Status.ToString(), decisao.TratarAMao),
            Avisos = decisao.MensagemAviso == null ? new List<string>() : new List<string> { decisao.MensagemAviso }
        };

        return resposta;
    }

    private static ReturnLocacaoEncerradaVO MontarLocacaoEncerrada(Locacao locacao, int kmChegada, DateTime dataDevolucao, StatusLocacao status, int? diarias = null, decimal? valorTotal = null)
    {
        int diariasPrevistas = diarias ?? RegrasDevolucao.CalcularDiarias(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
        decimal total = valorTotal ?? locacao.ValorTotal.GetValueOrDefault();
        ReturnLocacaoEncerradaVO vo = new ReturnLocacaoEncerradaVO
        {
            IdLocacao = locacao.IdLocacao,
            Status = status.ToString(),
            KmSaida = locacao.KmSaida,
            KmChegada = kmChegada,
            DataDevolucaoEfetiva = dataDevolucao.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
            DataDevolucaoEfetivaStr = StaticMethods.FormateDateTimeToPTBR(dataDevolucao),
            DiariasPrevistas = diariasPrevistas,
            ValorDiaria = locacao.ValorDiaria,
            ValorDiariaStr = StaticMethods.FormateToMonetary(locacao.ValorDiaria),
            ValorTotal = total,
            ValorTotalStr = StaticMethods.FormateToMonetary(total)
        };

        return vo;
    }

    private static ReturnCobrancasEncerramentoVO MontarCobrancas(decimal totalExtras, decimal totalDiarias, int? idCobrancaExtra, int? idDiaria)
    {
        ReturnCobrancasEncerramentoVO vo = new ReturnCobrancasEncerramentoVO
        {
            TotalExtras = totalExtras,
            TotalExtrasStr = StaticMethods.FormateToMonetary(totalExtras),
            TotalDiarias = totalDiarias,
            TotalDiariasStr = StaticMethods.FormateToMonetary(totalDiarias),
            IdPagamentoCobrancaExtra = idCobrancaExtra,
            IdPagamentoDiaria = idDiaria
        };

        return vo;
    }

    private static ReturnCaucaoEncerramentoVO MontarCaucao(decimal valorReservado, AcaoCaucaoDevolucao acao, decimal valorRetido, decimal valorLiberado, string? statusPagamento, bool tratarAMao)
    {
        ReturnCaucaoEncerramentoVO vo = new ReturnCaucaoEncerramentoVO
        {
            Acao = acao.ToString(),
            ValorReservado = valorReservado,
            ValorReservadoStr = StaticMethods.FormateToMonetary(valorReservado),
            ValorRetido = valorRetido,
            ValorRetidoStr = StaticMethods.FormateToMonetary(valorRetido),
            ValorLiberado = valorLiberado,
            ValorLiberadoStr = StaticMethods.FormateToMonetary(valorLiberado),
            StatusPagamento = acao == AcaoCaucaoDevolucao.SEM_CAUCAO ? null : statusPagamento,
            TratarAMao = tratarAMao
        };

        return vo;
    }

    /// <summary>Alvo da retencao decidido na pre-validacao (IdAlvo) ou lancamento da cobranca que faltava (LancarFaltante).</summary>
    private sealed class AlvoRetencao
    {
        public int? IdAlvo { get; set; }

        public bool LancarFaltante { get; set; }

        /// <summary>Alvo ja PAGO (O-10): nada a reter; a caucao segue como no caso sem extras.</summary>
        public bool JaQuitado { get; set; }

        public int? IdJaQuitado { get; set; }
    }

    /// <summary>Totais lidos do banco para a repeticao.</summary>
    private sealed class TotaisDevolucao
    {
        public decimal TotalExtras { get; set; }

        public int? IdPagamentoCobrancaExtra { get; set; }
    }
}
