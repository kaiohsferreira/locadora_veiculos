using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio da cobranca (RF-31 e RF-36). Confere a origem unica, o tipo x origem, o valor, o vencimento e a
    /// forma pelas regras puras (RegrasEntradaPagamento); calcula a data de hoje pelo relogio; monta os predicados das abas,
    /// da listagem e do resumo (RegrasPagamento); deixa a trava, a existencia e as recusas de status para o repositorio.
    /// Registra no log uma linha informativa por lancamento e por baixa (id do pagamento e do funcionario, sem valor).
    /// Os valores e as datas saem formatados pelo Profile; o id da transacao nunca sai.
    /// </summary>
    public class PagamentoService : IPagamentoService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IMapper _mapper;
        private readonly IRelogio _relogio;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;
        private readonly IPixGateway _pixGateway;

        public PagamentoService(IPagamentoRepository pagamentoRepository, IMapper mapper, IRelogio relogio, IContextoEmpresa contextoEmpresa, ILogService logService, IPixGateway pixGateway)
        {
            _pagamentoRepository = pagamentoRepository;
            _mapper = mapper;
            _relogio = relogio;
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
            _pixGateway = pixGateway;
        }

        /// <summary>
        /// Lanca a cobranca: origem unica, tipo pelo nome (CAUCAO aceita aqui, para a chamada direta da caucao), tipo x origem,
        /// valor e vencimento obrigatorio (400); locacao ou venda inexistente ou de outra locadora (404, no repositorio).
        /// Nasce PENDENTE. A rota recusa CAUCAO antes, no validador.
        /// </summary>
        public async Task<ReturnPagamentoVO> LancarAsync(LancarPagamentoVO entrada)
        {
            try
            {
                string? erroDeOrigem = RegrasEntradaPagamento.ErroDeOrigem(entrada.IdLocacao, entrada.IdVenda);

                if (erroDeOrigem != null)
                {
                    throw CriarRecusaDeCampo(nameof(LancarPagamentoVO.IdLocacao), erroDeOrigem);
                }

                if (!RegrasEntradaPagamento.TentarLerTipo(entrada.Tipo, out TipoPagamento tipo))
                {
                    throw CriarRecusaDeCampo(nameof(LancarPagamentoVO.Tipo), ConstantsMessage.ErroPagamentoTipoInvalido);
                }

                string? erroDoTipo = RegrasEntradaPagamento.ErroDeTipoPorOrigem(tipo, entrada.IdLocacao.HasValue, entrada.IdVenda.HasValue);

                if (erroDoTipo != null)
                {
                    throw CriarRecusaDeCampo(nameof(LancarPagamentoVO.Tipo), erroDoTipo);
                }

                string? erroDoValor = RegrasEntradaPagamento.ErroDeValor(entrada.Valor);

                if (erroDoValor != null)
                {
                    throw CriarRecusaDeCampo(nameof(LancarPagamentoVO.Valor), erroDoValor);
                }

                if (!entrada.DataVencimento.HasValue)
                {
                    throw CriarRecusaDeCampo(nameof(LancarPagamentoVO.DataVencimento), ConstantsMessage.ErroPagamentoVencimentoObrigatorio);
                }

                Pagamento novo = new Pagamento
                {
                    IdLocacao = entrada.IdLocacao,
                    IdVenda = entrada.IdVenda,
                    Tipo = tipo,
                    Valor = entrada.Valor,
                    DataVencimento = entrada.DataVencimento.Value
                };

                DateTime agora = _relogio.Agora;
                Pagamento gravado = await _pagamentoRepository.LancarAtomicamenteAsync(novo, agora);

                string mensagem = string.Format(ConstantsMessage.InfoPagamentoLancado, gravado.IdPagamento, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

                return _mapper.Map<ReturnPagamentoVO>(gravado);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLancarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Da baixa: forma obrigatoria e pelo nome exato (400); a data da baixa e a de hoje pelo relogio. Com PIX gerado valido,
        /// forma diferente de PIX e confirmacao (decisao 2 de 21/11/2026), cancela o PIX no provedor ANTES, fora de transacao; so
        /// depois o repositorio trava, reconfere e da a baixa. O repositorio recusa caucao, pago e cancelado (409) e inexistente (404).
        /// </summary>
        public async Task<ReturnPagamentoVO> BaixarAsync(int idPagamento, BaixarPagamentoVO entrada)
        {
            try
            {
                string? forma = entrada.Forma?.Trim();

                if (string.IsNullOrWhiteSpace(forma))
                {
                    throw CriarRecusaDeCampo(nameof(BaixarPagamentoVO.Forma), ConstantsMessage.ErroPagamentoFormaObrigatoria);
                }

                if (!RegrasEntradaPagamento.FormaValida(forma))
                {
                    throw CriarRecusaDeCampo(nameof(BaixarPagamentoVO.Forma), ConstantsMessage.ErroPagamentoFormaInvalida);
                }

                DateTime agora = _relogio.Agora;
                DateOnly hoje = DateOnly.FromDateTime(agora);

                string? idTransacaoCancelado = await CancelarPixAntesDaBaixaAsync(idPagamento, forma, entrada.ConfirmarBaixaComPixGerado, agora);

                Pagamento baixado;

                try
                {
                    baixado = await _pagamentoRepository.BaixarAtomicamenteAsync(idPagamento, forma, hoje, agora, entrada.ConfirmarBaixaComPixGerado, idTransacaoCancelado);
                }
                catch (Exception)
                {
                    await LimparPixCanceladoAposFalhaDaBaixaAsync(idPagamento, idTransacaoCancelado, agora);

                    throw;
                }

                string mensagem = string.Format(ConstantsMessage.InfoPagamentoBaixado, baixado.IdPagamento, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

                return _mapper.Map<ReturnPagamentoVO>(baixado);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBaixarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Listagem da locadora: status exato (400 se nao for um nome da lista), intervalo de vencimento (400 se o inicio passa do fim) e pagina.</summary>
        public async Task<PaginaVO<ReturnPagamentoVO>> ListarAsync(FiltroPagamentoVO filtro)
        {
            try
            {
                StatusPagamento? status = null;

                if (filtro.Status != null)
                {
                    if (!RegrasEntradaPagamento.TentarLerStatus(filtro.Status, out StatusPagamento statusLido))
                    {
                        throw CriarRecusaDeCampo(nameof(FiltroPagamentoVO.Status), ConstantsMessage.ErroPagamentoStatusInvalido);
                    }

                    status = statusLido;
                }

                if (filtro.VencimentoDe.HasValue && filtro.VencimentoAte.HasValue && filtro.VencimentoDe.Value > filtro.VencimentoAte.Value)
                {
                    throw CriarRecusaDeCampo(nameof(FiltroPagamentoVO.VencimentoAte), ConstantsMessage.ErroPagamentoVencimentoIntervalo);
                }

                Expression<Func<Pagamento, bool>> predicado = RegrasPagamento.PredicadoDaListagem(status, filtro.VencimentoDe, filtro.VencimentoAte);

                (List<PagamentoListagemRegistro> Itens, int Total) consulta = await _pagamentoRepository.ListarPagamentosDaLocadoraAsync(predicado, filtro.Deslocamento, filtro.TamanhoPagina);

                List<ReturnPagamentoVO> itens = new List<ReturnPagamentoVO>();

                foreach (PagamentoListagemRegistro pagamento in consulta.Itens)
                {
                    itens.Add(_mapper.Map<ReturnPagamentoVO>(pagamento));
                }

                return new PaginaVO<ReturnPagamentoVO>(itens, consulta.Total, filtro.Pagina, filtro.TamanhoPagina);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cobrancas do cliente: aba e periodo validados (400); o periodo limita so o inicio pelo vencimento; a situacao e os
        /// dias para vencer saem calculados pela data de hoje do relogio.
        /// </summary>
        public async Task<PaginaVO<ReturnMeuPagamentoVO>> ListarMeusAsync(int idCliente, FiltroMeusPagamentosVO filtro)
        {
            try
            {
                string aba = filtro.Aba ?? ConstantsValues.AbaPadrao;
                string periodo = filtro.Periodo ?? ConstantsValues.PeriodoPadrao;

                if (!RegrasEntradaPagamento.EhAbaValida(aba))
                {
                    throw CriarRecusaDeCampo(nameof(FiltroMeusPagamentosVO.Aba), ConstantsMessage.ErroPagamentoAbaInvalida);
                }

                if (!RegrasEntradaPagamento.EhPeriodoValido(periodo))
                {
                    throw CriarRecusaDeCampo(nameof(FiltroMeusPagamentosVO.Periodo), ConstantsMessage.ErroPagamentoPeriodoInvalido);
                }

                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly? inicio = RegrasPagamento.InicioDoPeriodo(periodo, hoje);
                Expression<Func<Pagamento, bool>> predicado = RegrasPagamento.PredicadoDaAba(aba, inicio);

                (List<PagamentoListagemRegistro> Itens, int Total) consulta = await _pagamentoRepository.ListarMeusPagamentosAsync(idCliente, predicado, filtro.Deslocamento, filtro.TamanhoPagina);

                List<ReturnMeuPagamentoVO> itens = new List<ReturnMeuPagamentoVO>();

                foreach (PagamentoListagemRegistro pagamento in consulta.Itens)
                {
                    itens.Add(MontarMeuPagamento(pagamento, hoje));
                }

                return new PaginaVO<ReturnMeuPagamentoVO>(itens, consulta.Total, filtro.Pagina, filtro.TamanhoPagina);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Resumo do cliente: periodo validado (400); totais e pendencias sem periodo; o ano e o do relogio (quitadas pela data
        /// de pagamento); a contagem da aba Pagos segue o periodo pedido.
        /// </summary>
        public async Task<ReturnResumoMeusPagamentosVO> ResumirMeusAsync(int idCliente, FiltroResumoMeusPagamentosVO filtro)
        {
            try
            {
                string periodo = filtro.Periodo ?? ConstantsValues.PeriodoPadrao;

                if (!RegrasEntradaPagamento.EhPeriodoValido(periodo))
                {
                    throw CriarRecusaDeCampo(nameof(FiltroResumoMeusPagamentosVO.Periodo), ConstantsMessage.ErroPagamentoPeriodoInvalido);
                }

                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly? inicio = RegrasPagamento.InicioDoPeriodo(periodo, hoje);
                DateOnly inicioAno = new DateOnly(hoje.Year, 1, 1);
                DateOnly inicioProximoAno = new DateOnly(hoje.Year + 1, 1, 1);

                ResumoPagamentosClienteRegistro registro = await _pagamentoRepository.ResumirMeusPagamentosAsync(
                    idCliente,
                    RegrasPagamento.PredicadoPendente(),
                    RegrasPagamento.PredicadoPagoNoPeriodo(inicio),
                    RegrasPagamento.PredicadoQuitadoNoAno(inicioAno, inicioProximoAno));

                ReturnResumoMeusPagamentosVO resumo = _mapper.Map<ReturnResumoMeusPagamentosVO>(registro);
                resumo.AnoExercicio = hoje.Year;
                resumo.QuantidadeAbaPendentes = registro.QuantidadePendentes;
                resumo.QuantidadeAbaPagos = registro.QuantidadePagosNoPeriodo;

                return resumo;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroResumirMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>Monta a saida do cliente: formatacao pelo Profile e, aqui, a situacao e os dias para vencer pela data de hoje.</summary>
        private ReturnMeuPagamentoVO MontarMeuPagamento(PagamentoListagemRegistro pagamento, DateOnly hoje)
        {
            ReturnMeuPagamentoVO saida = _mapper.Map<ReturnMeuPagamentoVO>(pagamento);
            saida.Situacao = RegrasPagamento.Situacao(pagamento.Tipo, pagamento.Status, pagamento.DataVencimento, hoje).ToString();
            saida.DiasParaVencer = RegrasPagamento.DiasParaVencer(pagamento.DataVencimento, hoje);

            return saida;
        }

        /// <summary>
        /// Decisao 2 de 21/11/2026: com confirmacao, forma diferente de PIX e PIX gravado valido, cancela o PIX no provedor antes
        /// da baixa. Pre-leitura sem trava; so chama o provedor quando a regra pura manda. Devolve o identificador cancelado (para
        /// a reconferencia da trava) ou nulo quando nao houve cancelamento. Nunca roda dentro de transacao de banco.
        /// </summary>
        private async Task<string?> CancelarPixAntesDaBaixaAsync(int idPagamento, string forma, bool confirmar, DateTime agora)
        {
            if (!confirmar || string.Equals(forma.Trim(), ConstantsValues.FormaPix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Pagamento leitura = await _pagamentoRepository.ConsultarCobrancaDaLocadoraParaPixAsync(idPagamento);

            if (!RegrasPagamento.PrecisaCancelarPixAntesDaBaixa(leitura.Tipo, leitura.Status, leitura.IdTransacao, leitura.PixExpiraEm, forma, confirmar, agora))
            {
                return null;
            }

            string? identificadorProvedor = leitura.IdTransacao;

            if (identificadorProvedor == null)
            {
                return null;
            }

            await CancelarPixNoProvedorAsync(idPagamento, identificadorProvedor);

            return identificadorProvedor;
        }

        /// <summary>
        /// Cancela o PIX no provedor. Cancelado ou ja encerrado (CANCELADA, EXPIRADA, RECUSADA) segue; PAGA recusa com 409 e log;
        /// qualquer falha vira 502 e a baixa nao acontece. Inclui o provedor que nao conhece o PIX (NotFound): decisao do Kaio de 21/11/2026 ('Nao dar baixa').
        /// </summary>
        private async Task CancelarPixNoProvedorAsync(int idPagamento, string identificadorProvedor)
        {
            PixCobrancaResultado resultado;

            try
            {
                resultado = await _pixGateway.CancelarCobrancaAsync(identificadorProvedor);
            }
            catch (BusinessException ex) when (string.Equals(ex.Message, ConstantsMessage.ErroPixCancelarCobrancaPaga, StringComparison.Ordinal))
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.LogPagamentoBaixaPixJaPagoNoProvedor, idPagamento), GetType().ToString());

                throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaPixJaPago);
            }
            catch (BusinessException ex)
            {
                // Recusa do provedor no cancelamento (400/422): a baixa nao acontece, com o texto aprovado de falha (502). A causa vai em innerException.
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, ex);
            }
            catch (NotFoundException ex)
            {
                // Decisao do Kaio de 21/11/2026 ('Nao dar baixa'): o provedor que nao conhece o PIX e falha de cancelamento; a causa vai em innerException.
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, ex);
            }
            catch (IntegrationException ex)
            {
                // A causa (timeout, rede, credencial, FAKE sem a chave) vai em innerException; o middleware registra a cadeia uma vez.
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, ex);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBaixarPagamento, GetType().ToString());
                throw;
            }

            if (resultado.Status == PixStatusCobranca.PAGA)
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.LogPagamentoBaixaPixJaPagoNoProvedor, idPagamento), GetType().ToString());

                throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaPixJaPago);
            }

            if (!CancelamentoPixAceito(resultado.Status))
            {
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou);
            }

            _logService.WriteInfo(string.Format(ConstantsMessage.LogPagamentoPixCanceladoAntesDaBaixa, idPagamento), GetType().ToString());
        }

        /// <summary>
        /// Depois de cancelar o PIX no provedor, a baixa falhou: zera o id cancelado (so se ainda for o gravado) para a reabertura
        /// gerar PIX novo com a validade gravada. Melhor esforco: a falha so registra o id, e a excecao original segue.
        /// </summary>
        private async Task LimparPixCanceladoAposFalhaDaBaixaAsync(int idPagamento, string? idTransacaoCancelado, DateTime agora)
        {
            if (idTransacaoCancelado == null)
            {
                return;
            }

            try
            {
                await _pagamentoRepository.LimparPixCanceladoAsync(idPagamento, idTransacaoCancelado, agora);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, string.Format(ConstantsMessage.ErroLimparPixCancelado, idPagamento), GetType().ToString());
            }
        }

        /// <summary>Estados em que o PIX nao e mais pagavel no provedor: cancelado, expirado ou recusado.</summary>
        private static bool CancelamentoPixAceito(PixStatusCobranca status)
        {
            return status == PixStatusCobranca.CANCELADA
                || status == PixStatusCobranca.EXPIRADA
                || status == PixStatusCobranca.RECUSADA;
        }

        /// <summary>Monta a excecao de validacao de um campo (400), com a mensagem da regra.</summary>
        private static ValidationException CriarRecusaDeCampo(string campo, string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(campo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }
    }
}
