using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Requests.Pix;
using System.Globalization;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Provedor PIX do Mercado Pago (Nome = MERCADOPAGO). Usa IComunicacaoService para
    /// toda chamada HTTP; as requisicoes sao montadas por MercadoPagoPixRequest.
    ///
    /// Erros do provedor viram excecao de negocio (400/422 recusada, 404 nao encontrada)
    /// ou de integracao (credencial, limite, 5xx, rede, resposta inesperada).
    /// access_token, e-mail e CPF do pagador nunca entram em mensagem, log ou exception.Message.
    /// </summary>
    public class MercadoPagoPixProvider : IPixProvider
    {
        private readonly IComunicacaoService _comunicacao;
        private readonly ILogService _logService;

        public MercadoPagoPixProvider(IComunicacaoService comunicacao, ILogService logService)
        {
            _comunicacao = comunicacao;
            _logService = logService;
        }

        public string Nome => ConstantsPix.ProvedorMercadoPago;

        public async Task<PixCobrancaResultado> CriarAsync(ConfiguracaoIntegracao configuracao, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsPix.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoPixRequest.Criar(configuracao, accessToken, solicitacao, expiracaoSegundos, DateTime.Now);

                RequestResponse<MercadoPagoPaymentResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, null);

                PixCobrancaResultado resultado = MapearResultado(pagamento, resposta.StatusCode, requisicao, configuracao.Codigo, solicitacao.Valor, solicitacao.ReferenciaExterna);

                if (string.IsNullOrWhiteSpace(resultado.CopiaECola) || string.IsNullOrWhiteSpace(resultado.QrCodeBase64))
                {
                    RegistrarRespostaInesperada(configuracao.Codigo, requisicao, resposta.StatusCode, pagamento.Status, resultado.IdentificadorProvedor);
                    throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixRespostaInesperada);
                }

                return resultado;
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixCriarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixCriarCobranca, ex);
            }
        }

        public async Task<PixCobrancaResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsPix.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoPixRequest.Consultar(configuracao, accessToken, identificadorProvedor);

                RequestResponse<MercadoPagoPaymentResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, identificadorProvedor);

                return MapearResultado(pagamento, resposta.StatusCode, requisicao, configuracao.Codigo, null, null);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixConsultarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixConsultarCobranca, ex);
            }
        }

        public async Task<PixCobrancaResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsPix.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoPixRequest.Cancelar(configuracao, accessToken, identificadorProvedor);

                RequestResponse<MercadoPagoPaymentResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, identificadorProvedor);

                return MapearResultado(pagamento, resposta.StatusCode, requisicao, configuracao.Codigo, null, null);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixCancelarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixCancelarCobranca, ex);
            }
        }

        // ------------------------------------------------------------------
        // Leitura da resposta HTTP
        // ------------------------------------------------------------------

        /// <summary>Devolve o pagamento de uma resposta 2xx; em erro, lanca a excecao do contrato.</summary>
        private MercadoPagoPaymentResponse ObterPagamento(RequestResponse<MercadoPagoPaymentResponse> resposta, RequestInfo requisicao, string codigo, string? identificador)
        {
            if (!resposta.IsSuccess)
            {
                throw ConstruirErroHttp(resposta, requisicao, codigo, identificador);
            }

            if (resposta.Data == null)
            {
                RegistrarRespostaInesperada(codigo, requisicao, resposta.StatusCode, null, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroPixRespostaInesperada);
            }

            return resposta.Data;
        }

        private Exception ConstruirErroHttp(RequestResponse<MercadoPagoPaymentResponse> resposta, RequestInfo requisicao, string codigo, string? identificador)
        {
            if (resposta.IsTimeout)
            {
                return new IntegrationException(codigo, ConstantsMessageIntegration.ErroComunicacaoTimeout);
            }

            if (resposta.IsNetworkFailure)
            {
                return new IntegrationException(codigo, ConstantsMessageIntegration.ErroComunicacaoFalhaRede);
            }

            int status = resposta.StatusCode;

            if (status == ConstantsPix.HttpStatusRequisicaoRecusada || status == ConstantsPix.HttpStatusNaoProcessavel)
            {
                return new BusinessException(string.Format(ConstantsMessage.ErroPixCobrancaRecusada, MontarMotivoRecusa(resposta, requisicao)));
            }

            if (status == ConstantsPix.HttpStatusNaoEncontrado)
            {
                return new NotFoundException(ConstantsMessage.ErroPixCobrancaNaoEncontrada);
            }

            if (status == ConstantsPix.HttpStatusNaoAutorizado || status == ConstantsPix.HttpStatusProibido)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroPixCredenciaisRecusadas);
            }

            if (status == ConstantsPix.HttpStatusLimiteRequisicoes)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroPixLimiteRequisicoes);
            }

            if (status >= ConstantsPix.HttpStatusErroServidorMinimo || status == 0)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroPixProvedorIndisponivel);
            }

            // Status sem tratamento no contrato (ex.: 3xx, 418, corpo 2xx que nao virou JSON): resposta inesperada.
            RegistrarRespostaInesperada(codigo, requisicao, status, null, identificador);
            return new IntegrationException(codigo, ConstantsMessage.ErroPixRespostaInesperada);
        }

        /// <summary>
        /// Motivo do provedor mascarado (access_token, e-mail e CPF do pagador) e truncado no limite do contrato.
        /// Mascarar antes de truncar garante que nenhum valor protegido sobra pela metade.
        /// </summary>
        private static string MontarMotivoRecusa(RequestResponse<MercadoPagoPaymentResponse> resposta, RequestInfo requisicao)
        {
            string motivo = SensitiveDataMasker.Mask(resposta.RawContent, requisicao.SensitiveValues);

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ConstantsMessage.ErroRequisicaoInvalida;
            }

            if (motivo.Length > ConstantsPix.TamanhoMaximoMotivoRecusa)
            {
                return motivo.Substring(0, ConstantsPix.TamanhoMaximoMotivoRecusa);
            }

            return motivo;
        }

        // ------------------------------------------------------------------
        // Mapeamento para o resultado do gateway
        // ------------------------------------------------------------------

        private PixCobrancaResultado MapearResultado(MercadoPagoPaymentResponse pagamento, int statusHttp, RequestInfo requisicao, string codigo, decimal? valorPadrao, string? referenciaPadrao)
        {
            string identificador = pagamento.Id.ToString(CultureInfo.InvariantCulture);

            if (pagamento.Id <= 0)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroPixRespostaInesperada);
            }

            PixStatusCobranca? status = MapearStatus(pagamento.Status, pagamento.StatusDetail);

            if (status == null)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroPixRespostaInesperada);
            }

            decimal? valor = pagamento.TransactionAmount ?? valorPadrao;

            if (valor == null)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroPixRespostaInesperada);
            }

            MercadoPagoTransactionData? dadosPix = pagamento.PointOfInteraction?.TransactionData;

            PixCobrancaResultado resultado = new PixCobrancaResultado
            {
                IdentificadorProvedor = identificador,
                ReferenciaExterna = pagamento.ExternalReference ?? referenciaPadrao ?? string.Empty,
                Status = status.Value,
                Valor = valor.Value,
                CopiaECola = dadosPix?.QrCode,
                QrCodeBase64 = dadosPix?.QrCodeBase64,
                UrlCobranca = dadosPix?.TicketUrl,
                ExpiraEm = ConverterDataLocal(pagamento.DateOfExpiration),
                PagoEm = ConverterDataLocal(pagamento.DateApproved),
                Provedor = ConstantsPix.ProvedorMercadoPago
            };

            return resultado;
        }

        private static PixStatusCobranca? MapearStatus(string? status, string? statusDetail)
        {
            if (string.Equals(status, ConstantsPix.MercadoPagoStatusPendente, StringComparison.OrdinalIgnoreCase))
            {
                return PixStatusCobranca.AGUARDANDO_PAGAMENTO;
            }

            if (string.Equals(status, ConstantsPix.MercadoPagoStatusAprovado, StringComparison.OrdinalIgnoreCase))
            {
                return PixStatusCobranca.PAGA;
            }

            if (string.Equals(status, ConstantsPix.MercadoPagoStatusCancelado, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(statusDetail, ConstantsPix.MercadoPagoStatusDetalheExpirado, StringComparison.OrdinalIgnoreCase))
                {
                    return PixStatusCobranca.EXPIRADA;
                }

                return PixStatusCobranca.CANCELADA;
            }

            if (string.Equals(status, ConstantsPix.MercadoPagoStatusRejeitado, StringComparison.OrdinalIgnoreCase))
            {
                return PixStatusCobranca.RECUSADA;
            }

            return null;
        }

        /// <summary>Data do Mercado Pago (com offset) convertida para hora local, como manda o contrato.</summary>
        private static DateTime? ConverterDataLocal(string? dataComOffset)
        {
            DateTimeOffset dataOffset;

            if (string.IsNullOrWhiteSpace(dataComOffset) || !DateTimeOffset.TryParse(dataComOffset, CultureInfo.InvariantCulture, DateTimeStyles.None, out dataOffset))
            {
                return null;
            }

            return dataOffset.ToLocalTime().DateTime;
        }

        // ------------------------------------------------------------------
        // Log de resposta inesperada
        // ------------------------------------------------------------------

        /// <summary>
        /// Registra a resposta inesperada com so os campos permitidos: codigo, provedor, status HTTP,
        /// status do provedor (texto, mascarado e limitado) e identificador. QR, copia-e-cola, token,
        /// CPF e e-mail nunca entram no registro.
        /// </summary>
        private void RegistrarRespostaInesperada(string codigo, RequestInfo requisicao, int statusHttp, string? statusProvedor, string? identificador)
        {
            string statusLimpo = SensitiveDataMasker.Mask(statusProvedor, requisicao.SensitiveValues);

            if (statusLimpo.Length > ConstantsPix.TamanhoMaximoStatusLog)
            {
                statusLimpo = statusLimpo.Substring(0, ConstantsPix.TamanhoMaximoStatusLog);
            }

            if (string.IsNullOrWhiteSpace(statusLimpo))
            {
                statusLimpo = ConstantsPix.MarcadorAusente;
            }

            string identificadorLog = string.IsNullOrWhiteSpace(identificador) ? ConstantsPix.MarcadorAusente : identificador;

            string registro = string.Format(
                CultureInfo.InvariantCulture,
                ConstantsMessage.LogPixRespostaInesperada,
                codigo,
                ConstantsPix.ProvedorMercadoPago,
                statusHttp,
                statusLimpo,
                identificadorLog);

            _logService.Write(registro, ConstantsMessage.ErroPixRespostaInesperada, GetType().ToString());
        }
    }
}
