using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Interfaces;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Gera ou reabre o PIX de uma cobranca (RF-32). Leitura sem trava; reabre sem chamar o provedor quando o PIX gravado ainda
    /// vale (trava curta e nova conferencia); senao chama o provedor FORA de transacao e grava sob trava. Confere o status do
    /// provedor (PAGA recusa; EXPIRADA, CANCELADA ou RECUSADA repetem uma vez com a referencia de recuperacao), o retorno (copia
    /// e cola, QR e validade) e o valor e a referencia devolvidos. Valor, pagador e locadora vem do banco, nunca do pedido.
    /// Registra no log so o id da cobranca (sem valor, nome, CPF, e-mail, copia e cola ou QR).
    /// </summary>
    public class CobrancaPixService : ICobrancaPixService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IPixGateway _pixGateway;
        private readonly IMapper _mapper;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public CobrancaPixService(IPagamentoRepository pagamentoRepository, IPixGateway pixGateway, IMapper mapper, IRelogio relogio, ILogService logService)
        {
            _pagamentoRepository = pagamentoRepository;
            _pixGateway = pixGateway;
            _mapper = mapper;
            _relogio = relogio;
            _logService = logService;
        }

        /// <summary>Gera ou reabre o PIX de uma cobranca do cliente (posse pelo cliente do token). A saida nao traz o identificador do provedor.</summary>
        public async Task<ReturnCobrancaPixVO> GerarOuReabrirDoClienteAsync(int idCliente, int idPagamento)
        {
            try
            {
                (Pagamento Cobranca, DateTime Agora) resultado = await GerarOuReabrirAsync(idCliente, idPagamento);

                ReturnCobrancaPixVO saida = _mapper.Map<ReturnCobrancaPixVO>(resultado.Cobranca);
                saida.SegundosParaExpirar = RegrasPagamento.SegundosParaExpirar(resultado.Cobranca.PixExpiraEm ?? resultado.Agora, resultado.Agora);

                return saida;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGerarPixPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Gera ou reabre o PIX de uma cobranca da locadora do contexto (funcionario). A saida traz o identificador do provedor.</summary>
        public async Task<ReturnCobrancaPixFuncionarioVO> GerarOuReabrirDaLocadoraAsync(int idPagamento)
        {
            try
            {
                (Pagamento Cobranca, DateTime Agora) resultado = await GerarOuReabrirAsync(null, idPagamento);

                ReturnCobrancaPixFuncionarioVO saida = _mapper.Map<ReturnCobrancaPixFuncionarioVO>(resultado.Cobranca);
                saida.SegundosParaExpirar = RegrasPagamento.SegundosParaExpirar(resultado.Cobranca.PixExpiraEm ?? resultado.Agora, resultado.Agora);

                return saida;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGerarPixPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Fluxo comum: leitura sem trava; recusa de status; reabre o PIX valido (trava curta, sem provedor); senao gera no provedor
        /// e grava sob trava. Devolve a cobranca como ficou gravada e a hora de agora (para os segundos da resposta).
        /// </summary>
        private async Task<(Pagamento Cobranca, DateTime Agora)> GerarOuReabrirAsync(int? idCliente, int idPagamento)
        {
            if (idPagamento <= 0)
            {
                throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
            }

            DateTime agora = _relogio.Agora;

            Pagamento leitura = idCliente.HasValue
                ? await _pagamentoRepository.ConsultarCobrancaDoClienteParaPixAsync(idCliente.Value, idPagamento)
                : await _pagamentoRepository.ConsultarCobrancaDaLocadoraParaPixAsync(idPagamento);

            ConferirPodeGerar(leitura);

            DateTime? validadeAnterior = leitura.PixExpiraEm;
            string? idTransacaoGravado = leitura.IdTransacao;

            if (RegrasPagamento.PixAindaValido(leitura.IdTransacao, leitura.PixExpiraEm, agora))
            {
                Pagamento travada = await _pagamentoRepository.TravarCobrancaParaPixAsync(idCliente, idPagamento);

                ConferirPodeGerar(travada);

                if (RegrasPagamento.PixAindaValido(travada.IdTransacao, travada.PixExpiraEm, agora))
                {
                    _logService.WriteInfo(string.Format(ConstantsMessage.InfoPixReaberto, travada.IdPagamento), GetType().ToString());

                    return (travada, _relogio.Agora);
                }

                validadeAnterior = travada.PixExpiraEm;
                idTransacaoGravado = travada.IdTransacao;
            }

            PixCobrancaResultado retorno = await GerarNoProvedorAsync(leitura, validadeAnterior, idTransacaoGravado);

            PixGravacaoRegistro dados = new PixGravacaoRegistro(
                retorno.IdentificadorProvedor,
                retorno.QrCodeBase64 ?? string.Empty,
                retorno.CopiaECola ?? string.Empty,
                retorno.ExpiraEm ?? _relogio.Agora);

            (Pagamento Pagamento, bool Gravou) gravacao;

            try
            {
                gravacao = await _pagamentoRepository.GravarPixAtomicamenteAsync(idCliente, idPagamento, dados, _relogio.Agora);
            }
            catch (Exception)
            {
                await CancelarPixDescartadoSeNovoAsync(idPagamento, retorno, idTransacaoGravado);

                throw;
            }

            if (gravacao.Gravou)
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.InfoPixGerado, idPagamento), GetType().ToString());
            }
            else if (gravacao.Pagamento.IdTransacao != retorno.IdentificadorProvedor)
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.InfoPixDescartadoPorConcorrencia, idPagamento), GetType().ToString());

                // PIX descartado por concorrencia: o criado nesta chamada nao vai para a cobranca; cancela no provedor (melhor esforco).
                await CancelarPixDescartadoAsync(idPagamento, retorno.IdentificadorProvedor);
            }

            return (gravacao.Pagamento, _relogio.Agora);
        }

        /// <summary>Gera no provedor com a referencia da cobranca, confere o status, repete uma vez quando for recuperavel, e confere o retorno.</summary>
        private async Task<PixCobrancaResultado> GerarNoProvedorAsync(Pagamento cobranca, DateTime? validadeAnterior, string? idTransacaoGravado)
        {
            string referencia = RegrasPagamento.ReferenciaDaCobranca(cobranca.IdPagamento, validadeAnterior);
            PixCobrancaResultado resposta = await _pixGateway.CriarCobrancaAsync(MontarSolicitacao(cobranca, referencia));

            if (resposta.Status == PixStatusCobranca.PAGA)
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.LogPixPagoNoProvedor, cobranca.IdPagamento), GetType().ToString());
                throw new BusinessException(ConstantsMessage.ErroPixGerarIndisponivel);
            }

            if (resposta.Status != PixStatusCobranca.AGUARDANDO_PAGAMENTO)
            {
                if (!EstadoRepetivelNoProvedor(resposta.Status) || !resposta.ExpiraEm.HasValue)
                {
                    RegistrarStatusNaoAguardando(cobranca.IdPagamento, resposta.Status);
                    throw new BusinessException(ConstantsMessage.ErroPixGerarIndisponivel);
                }

                referencia = RegrasPagamento.ReferenciaDaCobranca(cobranca.IdPagamento, resposta.ExpiraEm);
                resposta = await _pixGateway.CriarCobrancaAsync(MontarSolicitacao(cobranca, referencia));

                if (resposta.Status != PixStatusCobranca.AGUARDANDO_PAGAMENTO)
                {
                    RegistrarStatusNaoAguardando(cobranca.IdPagamento, resposta.Status);
                    throw new BusinessException(ConstantsMessage.ErroPixGerarIndisponivel);
                }
            }

            if (!RegrasPagamento.RetornoPixUtilizavel(resposta.CopiaECola, resposta.QrCodeBase64, resposta.ExpiraEm, _relogio.Agora))
            {
                await CancelarPixDescartadoSeNovoAsync(cobranca.IdPagamento, resposta, idTransacaoGravado);

                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixRespostaInesperada);
            }

            string? motivoDivergencia = RegrasPagamento.MotivoDivergenciaRetornoPix(cobranca.Valor, resposta.Valor, referencia, resposta.ReferenciaExterna);

            if (motivoDivergencia != null)
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.LogPixRespostaDivergente, cobranca.IdPagamento, motivoDivergencia), GetType().ToString());
                await CancelarPixDescartadoSeNovoAsync(cobranca.IdPagamento, resposta, idTransacaoGravado);

                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixRespostaInesperada);
            }

            return resposta;
        }

        /// <summary>Recusa (409) quando a cobranca nao pode gerar PIX: caucao, paga ou cancelada.</summary>
        private static void ConferirPodeGerar(Pagamento cobranca)
        {
            string? erro = RegrasPagamento.ErroDaGeracaoPix(cobranca.Tipo, cobranca.Status);

            if (erro != null)
            {
                throw new BusinessException(erro);
            }
        }

        /// <summary>Monta a solicitacao com o valor do banco, a descricao e o pagador do cliente dono da origem (nunca do pedido).</summary>
        private static PixCobrancaSolicitacao MontarSolicitacao(Pagamento cobranca, string referencia)
        {
            Cliente? cliente = cobranca.Locacao?.Cliente ?? cobranca.Venda?.Cliente;

            if (cliente == null)
            {
                throw new BusinessException(ConstantsMessage.ErroPixPagadorInvalido);
            }

            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = referencia,
                Valor = cobranca.Valor,
                Descricao = string.Format(ConstantsValues.PixDescricaoCobranca, cobranca.IdPagamento),
                Pagador = new PixPagador
                {
                    Nome = cliente.Nome,
                    Email = cliente.Email,
                    Documento = cliente.Cpf
                }
            };
        }

        /// <summary>
        /// Cancela no provedor o PIX criado que nao vai para a cobranca (retorno inutilizavel, divergente ou gravacao que falhou).
        /// Nao cancela quando o identificador e o do PIX ja gravado: a cobranca usa esse PIX (o provedor o devolve pela referencia).
        /// </summary>
        private async Task CancelarPixDescartadoSeNovoAsync(int idPagamento, PixCobrancaResultado retorno, string? idTransacaoGravado)
        {
            if (retorno.IdentificadorProvedor == idTransacaoGravado)
            {
                return;
            }

            await CancelarPixDescartadoAsync(idPagamento, retorno.IdentificadorProvedor);
        }

        /// <summary>Cancela no provedor o PIX que a gravacao recusou (melhor esforco): a falha so registra o id da cobranca.</summary>
        private async Task CancelarPixDescartadoAsync(int idPagamento, string identificadorProvedor)
        {
            try
            {
                await _pixGateway.CancelarCobrancaAsync(identificadorProvedor);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, string.Format(ConstantsMessage.ErroCancelarPixDescartado, idPagamento), GetType().ToString());
            }
        }

        /// <summary>Estados que o provedor devolve para uma cobranca que nao aguarda pagamento e que a repeticao pode recuperar.</summary>
        private static bool EstadoRepetivelNoProvedor(PixStatusCobranca status)
        {
            return status == PixStatusCobranca.EXPIRADA
                || status == PixStatusCobranca.CANCELADA
                || status == PixStatusCobranca.RECUSADA;
        }

        private void RegistrarStatusNaoAguardando(int idPagamento, PixStatusCobranca status)
        {
            _logService.WriteInfo(string.Format(ConstantsMessage.LogPixStatusNaoAguardandoNoProvedor, idPagamento, status.ToString()), GetType().ToString());
        }
    }
}
