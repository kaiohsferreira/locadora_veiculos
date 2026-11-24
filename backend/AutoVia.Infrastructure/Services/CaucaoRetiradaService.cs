using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Caucao na abertura da locacao (P4-BE-04, RF-20; contrato caucao-retirada v1.1). Ordem: cartao informado (400), cliente da
    /// reserva presente (404), regras do lancamento (defensivo), solicitacao com o valor da configuracao, a referencia
    /// CAUCAO-RES-{idReserva}, a descricao fixa e o pagador do banco; pre-autorizacao no gateway FORA de transacao e de trava, com
    /// CancellationToken.None; conferencia do retorno (divergencia 502, sem cancelar); decisao pelo estado devolvido; confirmacao do
    /// estado atual por ConsultarAsync e a mesma decisao pelo estado confirmado. So RESERVADA (devolvida e confirmada) abre.
    /// Cancela no provedor so o id conhecido e NOVO: antes rele sem trava se o mesmo id ja foi gravado (molde
    /// CobrancaPixService.CancelarPixDescartadoSeNovoAsync). O log leva so ids, a referencia, o identificador do provedor, o estado
    /// e o motivo: nunca token, final do cartao, e-mail, CPF, device id nem chave de idempotencia. Na leitura da retirada (GET),
    /// so consulta o gateway com a caucao exigida e sem locacao ja aberta; a falha da consulta nao derruba a leitura.
    /// </summary>
    public class CaucaoRetiradaService : ICaucaoRetiradaService
    {
        private readonly ICaucaoGateway _caucaoGateway;
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly CaucaoRetiradaOptions _opcoes;
        private readonly ILogService _logService;

        public CaucaoRetiradaService(ICaucaoGateway caucaoGateway, ILocacaoRepository locacaoRepository, CaucaoRetiradaOptions opcoes, ILogService logService)
        {
            _caucaoGateway = caucaoGateway;
            _locacaoRepository = locacaoRepository;
            _opcoes = opcoes;
            _logService = logService;
        }

        /// <summary>Verdadeiro quando a retirada exige caucao (Pagamentos:ValorCaucao maior que 0).</summary>
        public bool CaucaoExigida
        {
            get
            {
                bool exigida = _opcoes.CaucaoExigida;

                return exigida;
            }
        }

        /// <summary>
        /// Passos 6 a 10 da abertura com caucao. Devolve a caucao a gravar junto com a locacao so quando o estado devolvido e o
        /// confirmado sao RESERVADA. Cartao ausente relanca a ValidationException sem log (400); as demais falhas registram e relancam.
        /// </summary>
        public async Task<CaucaoAberturaRegistro> PreAutorizarAsync(Reserva reserva, LocacaoVO locacaoVO)
        {
            try
            {
                ConferirCartaoInformado(locacaoVO);
                Cliente pagador = ConferirPagador(reserva);
                decimal valor = _opcoes.ValorCaucao;
                ConferirRegrasDoLancamento(valor);

                string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(reserva.IdReserva);
                CaucaoSolicitacao solicitacao = MontarSolicitacao(referencia, valor, locacaoVO, pagador);

                CaucaoResultado devolvido = await PreAutorizarNoProvedorAsync(reserva.IdReserva, referencia, solicitacao);
                string idTransacao = ConferirRetorno(reserva.IdReserva, referencia, valor, devolvido);
                CaucaoAberturaRegistro caucao = new CaucaoAberturaRegistro(valor, idTransacao, referencia);

                await RecusarSeNaoAbreAsync(reserva.IdReserva, caucao, devolvido.Status);

                CaucaoResultado confirmado = await ConfirmarEstadoAtualAsync(reserva.IdReserva, caucao);

                await RecusarSeNaoAbreAsync(reserva.IdReserva, caucao, confirmado.Status);

                return caucao;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Compensacao em melhor esforco (nunca lanca). Releitura sem trava: o mesmo id gravado devolve verdadeiro e nada e
        /// cancelado; releitura que falha nao cancela (reserva orfa no log em vez de cancelar uma caucao gravada). Id novo: cancela
        /// no provedor e, se cancelou, rele de novo para registrar o caso que precisa de tratamento manual.
        /// </summary>
        public async Task<bool> CompensarAsync(int idReserva, CaucaoAberturaRegistro caucao)
        {
            try
            {
                bool jaGravada;

                try
                {
                    jaGravada = await _locacaoRepository.ExisteCaucaoGravadaAsync(idReserva, caucao.IdTransacao);
                }
                catch (Exception ex)
                {
                    _logService.Write(ex, FormatarRegistro(ConstantsMessage.LogCaucaoRetiradaConferenciaIndisponivel, idReserva, caucao), GetType().ToString());
                    return false;
                }

                if (jaGravada)
                {
                    string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaJaGravada, idReserva, caucao.IdTransacao);
                    _logService.WriteInfo(registro, GetType().ToString());

                    return true;
                }

                bool cancelou = await CancelarNoProvedorAsync(idReserva, caucao);

                if (cancelou)
                {
                    await ConferirDepoisDoCancelamentoAsync(idReserva, caucao);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                return false;
            }
        }

        /// <summary>
        /// Leitura da retirada (contrato caucao-retirada v1.1, leitura_da_retirada; correcao f). Os cinco campos saem sempre
        /// preenchidos pela configuracao: sem caucao exigida, neutros e sem consulta ao gateway (integracao quebrada com valor 0 =
        /// GET igual ao de hoje); com a locacao ja aberta, a configuracao e os dois indicadores falsos, sem consulta e sem log
        /// (CE-5). Senao consulta a configuracao do cartao com CancellationToken.None: o modo de simulacao vem do mesmo predicado
        /// da trava do FAKE (ICaucaoProvider.SimulacaoLiberada, pelo gateway), para o GET e o POST concordarem. A falha da consulta
        /// e tratada em ConsultarModoSimulacaoAsync e nunca derruba a leitura (degradacao parcial).
        /// </summary>
        public async Task PreencherRetiradaAsync(ReturnRetiradaVO retorno)
        {
            try
            {
                bool exigida = _opcoes.CaucaoExigida;
                decimal valor = exigida ? _opcoes.ValorCaucao : 0m;

                retorno.CaucaoExigida = exigida;
                retorno.ValorCaucao = valor;
                retorno.ValorCaucaoStr = StaticMethods.FormateToMonetary(valor);
                retorno.CaucaoModoSimulacao = false;
                retorno.CaucaoDisponivel = false;

                if (!RegrasCaucaoRetirada.ConsultaConfiguracaoNaRetirada(exigida, retorno.IdLocacaoExistente))
                {
                    return;
                }

                bool modoSimulacao = await ConsultarModoSimulacaoAsync(retorno.IdReserva);

                retorno.CaucaoModoSimulacao = modoSimulacao;
                retorno.CaucaoDisponivel = RegrasCaucaoRetirada.CaucaoDisponivelNaRetirada(exigida, modoSimulacao);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Configuracao do cartao no gateway (CancellationToken.None), so para saber se a simulacao esta liberada. Qualquer falha
        /// (integracao inativa, provedor desconhecido, chave publica do Mercado Pago ausente...) registra so a reserva e devolve
        /// falso: a caucao sai exigida e indisponivel e a leitura continua 200. Nada da configuracao (chave publica, provedor) sai.
        /// </summary>
        private async Task<bool> ConsultarModoSimulacaoAsync(int idReserva)
        {
            try
            {
                CaucaoConfiguracaoCartao configuracao = await _caucaoGateway.ObterConfiguracaoCartaoAsync(CancellationToken.None);
                bool modoSimulacao = configuracao.ModoSimulacao;

                return modoSimulacao;
            }
            catch (Exception ex)
            {
                string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaConfiguracaoIndisponivel, idReserva);
                _logService.Write(ex, registro, GetType().ToString());

                return false;
            }
        }

        /// <summary>Com a caucao exigida, o token e a bandeira do cartao sao obrigatorios: 400 do servico, antes do provedor.</summary>
        private static void ConferirCartaoInformado(LocacaoVO locacaoVO)
        {
            bool semToken = string.IsNullOrWhiteSpace(locacaoVO.CaucaoTokenCartao);
            bool semBandeira = string.IsNullOrWhiteSpace(locacaoVO.CaucaoMetodoPagamentoId);

            if (semToken || semBandeira)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure(nameof(LocacaoVO.CaucaoTokenCartao), ConstantsMessage.ErroLocacaoCaucaoCartaoObrigatorio)
                });
            }
        }

        /// <summary>O pagador e o cliente da reserva, lido do banco. Ausente ou desativado: 404, como a leitura da retirada.</summary>
        private static Cliente ConferirPagador(Reserva reserva)
        {
            Cliente? cliente = reserva.Cliente;

            if (cliente == null || cliente.DisabledAt != null)
            {
                throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
            }

            return cliente;
        }

        /// <summary>Regras do lancamento de pagamento (valor e tipo x origem), reaproveitadas sem chamar o LancarAsync. Defensivo.</summary>
        private static void ConferirRegrasDoLancamento(decimal valor)
        {
            string? erroDeValor = RegrasEntradaPagamento.ErroDeValor(valor);

            if (erroDeValor != null)
            {
                throw new BusinessException(erroDeValor);
            }

            string? erroDeTipo = RegrasEntradaPagamento.ErroDeTipoPorOrigem(TipoPagamento.CAUCAO, temLocacao: true, temVenda: false);

            if (erroDeTipo != null)
            {
                throw new BusinessException(erroDeTipo);
            }
        }

        /// <summary>Valor da configuracao, referencia por reserva, descricao fixa, pagador do banco e o cartao do pedido (so o token e o que o formulario gera).</summary>
        private static CaucaoSolicitacao MontarSolicitacao(string referencia, decimal valor, LocacaoVO locacaoVO, Cliente pagador)
        {
            string? dispositivo = string.IsNullOrWhiteSpace(locacaoVO.CaucaoIdentificadorDispositivo)
                ? null
                : locacaoVO.CaucaoIdentificadorDispositivo;

            CaucaoSolicitacao solicitacao = new CaucaoSolicitacao
            {
                ReferenciaExterna = referencia,
                Valor = valor,
                Descricao = ConstantsValues.CaucaoDescricaoRetirada,
                TokenCartao = locacaoVO.CaucaoTokenCartao ?? string.Empty,
                MetodoPagamentoId = locacaoVO.CaucaoMetodoPagamentoId ?? string.Empty,
                EmissorId = locacaoVO.CaucaoEmissorId,
                IdentificadorDispositivo = dispositivo,
                Pagador = new CaucaoPagador
                {
                    Email = pagador.Email,
                    Documento = pagador.Cpf
                }
            };

            return solicitacao;
        }

        /// <summary>
        /// Pre-autorizacao com CancellationToken.None (nunca o do pedido). Recusa do gateway (BusinessException: cartao recusado,
        /// solicitacao ou pagador recusados) registra so reserva e referencia e relanca: nada foi reservado, nada a cancelar.
        /// IntegrationException (sem id: rede, tempo esgotado, trava do FAKE...) sobe sem cancelar; o gateway ja registrou a referencia.
        /// </summary>
        private async Task<CaucaoResultado> PreAutorizarNoProvedorAsync(int idReserva, string referencia, CaucaoSolicitacao solicitacao)
        {
            try
            {
                CaucaoResultado devolvido = await _caucaoGateway.PreAutorizarAsync(solicitacao, CancellationToken.None);

                return devolvido;
            }
            catch (BusinessException ex)
            {
                string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaRecusada, idReserva, referencia);
                _logService.Write(ex, registro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Defesa sobre o retorno: identificador do provedor preenchido e que cabe em id_transacao, valor e referencia iguais aos
        /// pedidos. Divergencia registra o motivo e responde 502 SEM cancelar (decisao de 10/11/2026: tratamento manual).
        /// </summary>
        private string ConferirRetorno(int idReserva, string referencia, decimal valorPedido, CaucaoResultado devolvido)
        {
            string? identificador = devolvido.IdentificadorProvedor;
            bool identificadorValido = !string.IsNullOrWhiteSpace(identificador)
                && identificador.Length <= ConstantsValues.TamanhoMaximoIdTransacaoPagamento;

            string? motivo = identificadorValido
                ? RegrasPagamento.MotivoDivergenciaRetornoPix(valorPedido, devolvido.Valor, referencia, devolvido.ReferenciaExterna)
                : ConstantsValues.MotivoDivergenciaIdentificador;

            if (motivo == null && identificador != null)
            {
                return identificador;
            }

            string identificadorRegistrado = string.IsNullOrWhiteSpace(identificador) ? ConstantsCaucao.MarcadorAusente : identificador;
            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaDivergente, idReserva, referencia, identificadorRegistrado, motivo);
            _logService.Write(registro, ConstantsMessage.ErroCaucaoRespostaInesperada, GetType().ToString());

            throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoRespostaInesperada);
        }

        /// <summary>
        /// Decisao sobre o estado (RegrasDecisaoCaucaoRetirada): RESERVADA segue. EM_ANALISE e compensada (so id novo) e responde
        /// 409 de em analise; os estados encerrados (RECUSADA, CAPTURADA, CANCELADA, EXPIRADA, ESTORNADA) respondem 409 de nova
        /// tentativa sem cancelar. Se a compensacao achar o mesmo id ja gravado, responde 409 de locacao duplicada.
        /// </summary>
        private async Task RecusarSeNaoAbreAsync(int idReserva, CaucaoAberturaRegistro caucao, CaucaoStatus status)
        {
            if (RegrasDecisaoCaucaoRetirada.PermiteAbrir(status))
            {
                return;
            }

            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaNaoAprovada, idReserva, caucao.ReferenciaExterna, status.ToString());
            _logService.WriteInfo(registro, GetType().ToString());

            if (RegrasDecisaoCaucaoRetirada.CancelarAntesDeRecusar(status))
            {
                bool jaGravada = await CompensarAsync(idReserva, caucao);

                if (jaGravada)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }
            }

            throw new BusinessException(RegrasDecisaoCaucaoRetirada.MensagemDaRecusa(status));
        }

        /// <summary>
        /// Confirma o estado ATUAL do id devolvido, FORA de transacao e de trava (v1.1, CS-1: o Mercado Pago pode repetir o
        /// 'authorized' guardado de um pagamento ja cancelado). Se a consulta lancar, o id e conhecido e novo: compensa e a
        /// excecao ORIGINAL sobe (ou 409 de duplicada, se o mesmo id ja estava gravado).
        /// </summary>
        private async Task<CaucaoResultado> ConfirmarEstadoAtualAsync(int idReserva, CaucaoAberturaRegistro caucao)
        {
            try
            {
                CaucaoResultado confirmado = await _caucaoGateway.ConsultarAsync(caucao.IdTransacao, CancellationToken.None);

                return confirmado;
            }
            catch (Exception)
            {
                bool jaGravada = await CompensarAsync(idReserva, caucao);

                if (jaGravada)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                throw;
            }
        }

        /// <summary>Cancelamento de compensacao em melhor esforco, fora de transacao: falha so registra (cancelamento manual).</summary>
        private async Task<bool> CancelarNoProvedorAsync(int idReserva, CaucaoAberturaRegistro caucao)
        {
            try
            {
                await _caucaoGateway.CancelarAsync(caucao.IdTransacao, CancellationToken.None);
                _logService.WriteInfo(FormatarRegistro(ConstantsMessage.InfoCaucaoRetiradaCompensada, idReserva, caucao), GetType().ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, FormatarRegistro(ConstantsMessage.ErroCaucaoRetiradaCompensacao, idReserva, caucao), GetType().ToString());

                return false;
            }
        }

        /// <summary>
        /// Segunda releitura (v1.1, CS-1), depois de um cancelamento que deu certo: se o mesmo id aparecer gravado numa locacao
        /// ativa da reserva, ou se a releitura falhar, registra para tratamento manual. Nao muda a resposta.
        /// </summary>
        private async Task ConferirDepoisDoCancelamentoAsync(int idReserva, CaucaoAberturaRegistro caucao)
        {
            string registro = FormatarRegistro(ConstantsMessage.LogCaucaoRetiradaConferirDepoisDoCancelamento, idReserva, caucao);

            try
            {
                bool gravadaDepois = await _locacaoRepository.ExisteCaucaoGravadaAsync(idReserva, caucao.IdTransacao);

                if (gravadaDepois)
                {
                    _logService.Write(registro, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, registro, GetType().ToString());
            }
        }

        /// <summary>Registro com reserva, referencia e identificador do provedor, nessa ordem ({0}, {1} e {2}).</summary>
        private static string FormatarRegistro(string modelo, int idReserva, CaucaoAberturaRegistro caucao)
        {
            string registro = string.Format(CultureInfo.InvariantCulture, modelo, idReserva, caucao.ReferenciaExterna, caucao.IdTransacao);

            return registro;
        }
    }
}
