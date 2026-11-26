using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using System.Globalization;
using System.Threading;

namespace AutoVia.Infrastructure.Services;

/// <summary>
/// Caucao no encerramento da locacao (P5-BE-04, RF-20 e RP-12; caucao-devolucao v4). Consulta o provedor e age nele FORA de
/// transacao e de trava, com CancellationToken.None. Depois de uma acao que falhou, reconsulta uma vez (C14). Nao grava nada:
/// devolve a decisao para a transacao final. Qualquer falha de integracao sobe como 502 com a mensagem aprovada e nada e encerrado.
/// </summary>
public sealed class CaucaoDevolucaoService : ICaucaoDevolucaoService
{
    private readonly ICaucaoGateway _gateway;
    private readonly ILogService _logService;

    public CaucaoDevolucaoService(ICaucaoGateway gateway, ILogService logService)
    {
        _gateway = gateway;
        _logService = logService;
    }

    /// <summary>Decide a caucao da locacao pela tabela C1 a C14, consultando e agindo no provedor fora de transacao.</summary>
    public async Task<DecisaoCaucaoEncerramento> DecidirAsync(Locacao locacao, Pagamento? linhaCaucao, decimal totalExtras)
    {
        try
        {
            decimal valorReservado = locacao.ValorCaucao;

            if (linhaCaucao == null)
            {
                return DecisaoSemCaucao();
            }

            if (linhaCaucao.Status != StatusPagamento.PENDENTE)
            {
                return DecisaoDoBanco(linhaCaucao, valorReservado);
            }

            string? idTransacao = linhaCaucao.IdTransacao;
            CaucaoResultado? estado = string.IsNullOrWhiteSpace(idTransacao) ? null : await ConsultarAsync(linhaCaucao, idTransacao);
            AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(estado?.Status, valorReservado, totalExtras);
            DecisaoCaucaoEncerramento decisao;

            if (acao == AcaoProvedorCaucao.NENHUMA)
            {
                decisao = RegrasDecisaoCaucaoDevolucao.Decidir(estado?.Status, estado?.ValorCapturado, valorReservado, totalExtras);
            }
            else
            {
                decisao = await AgirNoProvedorAsync(locacao, linhaCaucao, idTransacao!, acao, valorReservado, totalExtras);
            }

            RegistrarDecisao(locacao, linhaCaucao, estado?.Status, acao, decisao);

            return decisao;
        }
        catch (IntegrationException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroDevolucaoProvedorIndisponivel, GetType().ToString());

            throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroDevolucaoProvedorIndisponivel, ex);
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

    /// <summary>Acao e valores da caucao gravada no banco (repeticao), sem chamar o provedor.</summary>
    public AcaoCaucaoDevolucao AcaoDoBanco(Pagamento? linhaCaucao, decimal valorReservado)
    {
        try
        {
            AcaoCaucaoDevolucao acao = AcaoCaucaoDevolucao.SEM_CAUCAO;

            if (linhaCaucao != null)
            {
                acao = linhaCaucao.Status switch
                {
                    StatusPagamento.PAGO => linhaCaucao.Valor >= valorReservado ? AcaoCaucaoDevolucao.RETIDA : AcaoCaucaoDevolucao.RETIDA_PARCIAL,
                    StatusPagamento.PENDENTE => AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR,
                    _ => AcaoCaucaoDevolucao.BAIXADA
                };
            }

            return acao;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Consulta o estado atual. NotFound do provedor (API reiniciada) vira nulo (C12) e so gera log de decisao; falha de rede sobe.</summary>
    private async Task<CaucaoResultado?> ConsultarAsync(Pagamento linhaCaucao, string idTransacao)
    {
        try
        {
            CaucaoResultado resultado = await _gateway.ConsultarAsync(idTransacao, CancellationToken.None);

            return resultado;
        }
        catch (NotFoundException)
        {
            _logService.WriteInfo(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoCaucaoNaoEncontrada, linhaCaucao.IdLocacao, linhaCaucao.IdPagamento), GetType().ToString());

            return null;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>
    /// Executa Capturar ou Cancelar no provedor. Depois de falha de integracao, reconsulta UMA vez: RESERVADA ainda vira 502
    /// (nada gravado). Depois de BusinessException inesperada, reconsulta e, sem estado final, fica nao confirmada (C14).
    /// </summary>
    private async Task<DecisaoCaucaoEncerramento> AgirNoProvedorAsync(Locacao locacao, Pagamento linhaCaucao, string idTransacao, AcaoProvedorCaucao acao, decimal valorReservado, decimal totalExtras)
    {
        CaucaoResultado? estado = null;
        bool naoConfirmada = false;

        try
        {
            estado = await ExecutarAcaoAsync(idTransacao, acao, totalExtras);
        }
        catch (IntegrationException ex)
        {
            _logService.Write(ex, FormatarReconsulta(locacao, linhaCaucao), GetType().ToString());
            estado = await ConsultarAsync(linhaCaucao, idTransacao);

            if (estado != null && estado.Status == CaucaoStatus.RESERVADA)
            {
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroDevolucaoProvedorIndisponivel, ex);
            }
        }
        catch (BusinessException ex)
        {
            _logService.Write(ex, FormatarReconsulta(locacao, linhaCaucao), GetType().ToString());
            (estado, naoConfirmada) = await ReconsultarDepoisDeRecusaAsync(linhaCaucao, idTransacao);
        }

        DecisaoCaucaoEncerramento decisao = naoConfirmada
            ? RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.RESERVADA, null, valorReservado, totalExtras)
            : RegrasDecisaoCaucaoDevolucao.Decidir(estado?.Status, estado?.ValorCapturado, valorReservado, totalExtras);

        string? aviso = RegrasDecisaoCaucaoDevolucao.AvisoDaAcao(acao, valorReservado, totalExtras);

        if (aviso != null && !naoConfirmada)
        {
            decisao.MensagemAviso = aviso;
        }

        return decisao;
    }

    /// <summary>Executa a acao no provedor (captura parcial ou integral, ou cancelamento). Sem transacao e sem trava.</summary>
    private async Task<CaucaoResultado> ExecutarAcaoAsync(string idTransacao, AcaoProvedorCaucao acao, decimal totalExtras)
    {
        try
        {
            CaucaoResultado resultado = acao == AcaoProvedorCaucao.CAPTURAR
                ? await _gateway.CapturarAsync(idTransacao, RegrasDecisaoCaucaoDevolucao.ValorDaCaptura(totalExtras), CancellationToken.None)
                : await _gateway.CancelarAsync(idTransacao, CancellationToken.None);

            return resultado;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Reconsulta depois de BusinessException: sem estado final (RESERVADA ou nulo) vira nao confirmada (C14).</summary>
    private async Task<(CaucaoResultado? Estado, bool NaoConfirmada)> ReconsultarDepoisDeRecusaAsync(Pagamento linhaCaucao, string idTransacao)
    {
        try
        {
            CaucaoResultado? reconsulta = await ConsultarAsync(linhaCaucao, idTransacao);

            if (reconsulta == null || reconsulta.Status == CaucaoStatus.RESERVADA)
            {
                return (null, true);
            }

            return (reconsulta, false);
        }
        catch (BusinessException ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());

            return (null, true);
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    private static DecisaoCaucaoEncerramento DecisaoSemCaucao()
    {
        DecisaoCaucaoEncerramento decisao = new DecisaoCaucaoEncerramento
        {
            Acao = AcaoCaucaoDevolucao.SEM_CAUCAO,
            NovoStatusCaucao = null,
            ValorRetido = 0m,
            ValorLiberado = 0m
        };

        return decisao;
    }

    /// <summary>Caucao ja resolvida (repeticao ou corrida): nao chama o provedor e mantem o status gravado.</summary>
    private DecisaoCaucaoEncerramento DecisaoDoBanco(Pagamento linhaCaucao, decimal valorReservado)
    {
        try
        {
            AcaoCaucaoDevolucao acao = AcaoDoBanco(linhaCaucao, valorReservado);
            decimal retido = acao == AcaoCaucaoDevolucao.BAIXADA ? 0m : decimal.Round(linhaCaucao.Valor, 2);

            if (acao == AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR)
            {
                retido = 0m;
            }

            DecisaoCaucaoEncerramento decisao = new DecisaoCaucaoEncerramento
            {
                Acao = acao,
                NovoStatusCaucao = null,
                TratarAMao = acao == AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR,
                ValorRetido = retido,
                ValorLiberado = acao == AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR ? 0m : RegrasDevolucao.ValorLiberadoDaCaucao(valorReservado, retido)
            };

            return decisao;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    private void RegistrarDecisao(Locacao locacao, Pagamento linhaCaucao, CaucaoStatus? estado, AcaoProvedorCaucao acao, DecisaoCaucaoEncerramento decisao)
    {
        try
        {
            string motivo = decisao.MensagemAviso ?? decisao.Acao.ToString();
            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoCaucaoDecidida,
                locacao.IdLocacao, ConstantsValues.CaucaoPrefixoReferencia + locacao.IdReserva, linhaCaucao.IdPagamento,
                estado?.ToString() ?? string.Empty, acao.ToString(), motivo);

            _logService.WriteInfo(registro, GetType().ToString());
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>Texto de log da reconsulta com ids internos (locacao e pagamento); sem id_transacao do provedor.</summary>
    private string FormatarReconsulta(Locacao locacao, Pagamento linhaCaucao)
    {
        try
        {
            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogDevolucaoCaucaoReconsulta, locacao.IdLocacao, linhaCaucao.IdPagamento);

            return registro;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }
}
