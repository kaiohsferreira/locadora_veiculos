using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Requests.Caucao;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Caucao
{
    /// <summary>
    /// Provedor de caucao do Mercado Pago (Nome = MERCADOPAGO), pela Payments API (/v1/payments).
    /// Usa IComunicacaoService para toda chamada HTTP; as requisicoes sao montadas por MercadoPagoCaucaoRequest.
    ///
    /// Mensagens ao cliente sao fixas. Texto do provedor (corpo, status, detalhe) nunca vai para mensagem:
    /// no log entram so valores limpos (sem controle, mascarados sem diferenciar maiusculas, truncados).
    /// Cartao recusado na pre-autorizacao (201 com status rejected) vira BusinessException com o motivo traduzido.
    /// Erros do provedor viram BusinessException (400/422), NotFoundException (404) ou IntegrationException.
    /// </summary>
    public class MercadoPagoCaucaoProvider : ICaucaoProvider
    {
        private readonly IComunicacaoService _comunicacao;
        private readonly ILogService _logService;

        public MercadoPagoCaucaoProvider(IComunicacaoService comunicacao, ILogService logService)
        {
            _comunicacao = comunicacao;
            _logService = logService;
        }

        public string Nome => ConstantsCaucao.ProvedorMercadoPago;

        public bool IdentificadorValido(string identificadorProvedor)
        {
            return Regex.IsMatch(identificadorProvedor, ConstantsCaucao.PadraoIdentificadorMercadoPago);
        }

        public async Task<CaucaoResultado> PreAutorizarAsync(ConfiguracaoIntegracao configuracao, CaucaoSolicitacao solicitacao, string chaveIdempotencia, int validadeReservaDias, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsCaucao.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(configuracao, accessToken, solicitacao, chaveIdempotencia);

                RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentCaucaoResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentCaucaoResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, null);

                // Recusa e tratada antes do confronto: um 201 rejected sem referencia continua sendo cartao recusado.
                if (MapearStatus(pagamento.Status, pagamento.StatusDetail) == CaucaoStatus.RECUSADA)
                {
                    throw new BusinessException(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, TraduzirMotivoRecusa(pagamento.StatusDetail)));
                }

                ValidarRespostaPreAutorizacao(pagamento, solicitacao, requisicao, resposta.StatusCode, configuracao.Codigo);

                return MapearResultado(pagamento, resposta.StatusCode, requisicao, configuracao.Codigo, solicitacao.Valor, solicitacao.ReferenciaExterna);
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
                _logService.Write(ex, ConstantsMessage.ErroCaucaoPreAutorizar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoPreAutorizar, ex);
            }
        }

        public async Task<CaucaoResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsCaucao.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoCaucaoRequest.Consultar(configuracao, accessToken, identificadorProvedor);

                RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentCaucaoResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentCaucaoResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, identificadorProvedor);

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
                _logService.Write(ex, ConstantsMessage.ErroCaucaoConsultar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoConsultar, ex);
            }
        }

        public async Task<CaucaoResultado> CapturarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, decimal? valor, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsCaucao.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoCaucaoRequest.Capturar(configuracao, accessToken, identificadorProvedor, valor);

                RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentCaucaoResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentCaucaoResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, identificadorProvedor);

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
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCapturar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoCapturar, ex);
            }
        }

        public async Task<CaucaoResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                string accessToken = configuracao.GetRequired(ConstantsCaucao.ChaveAccessToken);
                RequestInfo requisicao = MercadoPagoCaucaoRequest.Cancelar(configuracao, accessToken, identificadorProvedor);

                RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta = await _comunicacao.SendAsync<MercadoPagoPaymentCaucaoResponse>(requisicao, cancellationToken);
                MercadoPagoPaymentCaucaoResponse pagamento = ObterPagamento(resposta, requisicao, configuracao.Codigo, identificadorProvedor);

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
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCancelar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoCancelar, ex);
            }
        }

        // ------------------------------------------------------------------
        // Leitura da resposta HTTP
        // ------------------------------------------------------------------

        /// <summary>Devolve o pagamento de uma resposta 2xx; em erro, lanca a excecao do contrato.</summary>
        private MercadoPagoPaymentCaucaoResponse ObterPagamento(RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta, RequestInfo requisicao, string codigo, string? identificador)
        {
            if (!resposta.IsSuccess)
            {
                throw ConstruirErroHttp(resposta, requisicao, codigo, identificador);
            }

            if (resposta.Data == null)
            {
                RegistrarRespostaInesperada(codigo, requisicao, resposta.StatusCode, null, null, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
            }

            return resposta.Data;
        }

        private Exception ConstruirErroHttp(RequestResponse<MercadoPagoPaymentCaucaoResponse> resposta, RequestInfo requisicao, string codigo, string? identificador)
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

            if (status == ConstantsCaucao.HttpStatusRequisicaoRecusada || status == ConstantsCaucao.HttpStatusNaoProcessavel)
            {
                // Mensagem ao cliente fixa. No log, so os codigos de causa (sem texto livre do provedor).
                RegistrarRecusaSolicitacao(codigo, requisicao, status, resposta.RawContent);
                return new BusinessException(ConstantsMessage.ErroCaucaoSolicitacaoRecusada);
            }

            if (status == ConstantsCaucao.HttpStatusNaoEncontrado)
            {
                return new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada);
            }

            if (status == ConstantsCaucao.HttpStatusNaoAutorizado || status == ConstantsCaucao.HttpStatusProibido)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroCaucaoCredenciaisRecusadas);
            }

            if (status == ConstantsCaucao.HttpStatusLimiteRequisicoes)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroCaucaoLimiteRequisicoes);
            }

            if (status >= ConstantsCaucao.HttpStatusErroServidorMinimo || status == 0)
            {
                return new IntegrationException(codigo, ConstantsMessage.ErroCaucaoProvedorIndisponivel);
            }

            // Status sem tratamento no contrato (ex.: 3xx, 418): resposta inesperada.
            RegistrarRespostaInesperada(codigo, requisicao, status, null, null, identificador);
            return new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
        }

        /// <summary>
        /// Confere a pre-autorizacao devolvida com o pedido: external_reference e transaction_amount devem bater.
        /// Divergencia = resposta inesperada, com o identificador no log para tratamento manual.
        /// </summary>
        private void ValidarRespostaPreAutorizacao(MercadoPagoPaymentCaucaoResponse pagamento, CaucaoSolicitacao solicitacao, RequestInfo requisicao, int statusHttp, string codigo)
        {
            bool referenciaConfere = string.Equals(pagamento.ExternalReference, solicitacao.ReferenciaExterna, StringComparison.Ordinal);
            bool valorConfere = pagamento.TransactionAmount.HasValue
                && decimal.Round(pagamento.TransactionAmount.Value, 2) == decimal.Round(solicitacao.Valor, 2);

            if (referenciaConfere && valorConfere)
            {
                return;
            }

            string identificador = pagamento.Id.ToString(CultureInfo.InvariantCulture);
            RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, pagamento.StatusDetail, identificador);
            throw new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
        }

        // ------------------------------------------------------------------
        // Mapeamento para o resultado do gateway
        // ------------------------------------------------------------------

        private CaucaoResultado MapearResultado(MercadoPagoPaymentCaucaoResponse pagamento, int statusHttp, RequestInfo requisicao, string codigo, decimal? valorPadrao, string? referenciaPadrao)
        {
            string identificador = pagamento.Id.ToString(CultureInfo.InvariantCulture);

            if (pagamento.Id <= 0)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, pagamento.StatusDetail, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
            }

            CaucaoStatus? status = MapearStatus(pagamento.Status, pagamento.StatusDetail);

            if (status == null)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, pagamento.StatusDetail, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
            }

            decimal? valor = pagamento.TransactionAmount ?? valorPadrao;

            if (valor == null || valor.Value <= 0)
            {
                RegistrarRespostaInesperada(codigo, requisicao, statusHttp, pagamento.Status, pagamento.StatusDetail, identificador);
                throw new IntegrationException(codigo, ConstantsMessage.ErroCaucaoRespostaInesperada);
            }

            bool capturada = status.Value == CaucaoStatus.CAPTURADA;

            CaucaoResultado resultado = new CaucaoResultado
            {
                IdentificadorProvedor = identificador,
                ReferenciaExterna = pagamento.ExternalReference ?? referenciaPadrao ?? string.Empty,
                Status = status.Value,
                Valor = valor.Value,
                ValorCapturado = capturada ? valor : null,
                DetalheProvedor = MontarDetalheProvedor(pagamento.StatusDetail, requisicao),
                CartaoFinal = MontarCartaoFinal(pagamento.Card?.LastFourDigits),
                CriadoEm = ConverterDataLocal(pagamento.DateCreated),
                CapturadoEm = capturada ? ConverterDataLocal(pagamento.DateApproved) : null,
                Provedor = ConstantsCaucao.ProvedorMercadoPago
            };

            return resultado;
        }

        /// <summary>
        /// Status do Mercado Pago para o estado da caucao. authorized vira RESERVADA com qualquer status_detail
        /// (o detalhe fica em DetalheProvedor). Outros status desconhecidos sao resposta inesperada.
        /// </summary>
        private static CaucaoStatus? MapearStatus(string? status, string? statusDetail)
        {
            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusAutorizado, StringComparison.OrdinalIgnoreCase))
            {
                return CaucaoStatus.RESERVADA;
            }

            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusAprovado, StringComparison.OrdinalIgnoreCase))
            {
                return CaucaoStatus.CAPTURADA;
            }

            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusEmAnalise, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ConstantsCaucao.MercadoPagoStatusPendente, StringComparison.OrdinalIgnoreCase))
            {
                return CaucaoStatus.EM_ANALISE;
            }

            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusRejeitado, StringComparison.OrdinalIgnoreCase))
            {
                return CaucaoStatus.RECUSADA;
            }

            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusCancelado, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ConstantsCaucao.MercadoPagoStatusCanceladoAlternativo, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(statusDetail, ConstantsCaucao.MercadoPagoDetalheExpirado, StringComparison.OrdinalIgnoreCase))
                {
                    return CaucaoStatus.EXPIRADA;
                }

                return CaucaoStatus.CANCELADA;
            }

            if (string.Equals(status, ConstantsCaucao.MercadoPagoStatusEstornado, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, ConstantsCaucao.MercadoPagoStatusContestado, StringComparison.OrdinalIgnoreCase))
            {
                return CaucaoStatus.ESTORNADA;
            }

            return null;
        }

        /// <summary>Traduz o status_detail de recusa para o motivo do contrato. Texto bruto do provedor nunca sai daqui.</summary>
        private static string TraduzirMotivoRecusa(string? statusDetail)
        {
            if (string.Equals(statusDetail, ConstantsCaucao.MercadoPagoDetalheSaldoInsuficiente, StringComparison.OrdinalIgnoreCase))
            {
                return ConstantsMessage.MotivoCaucaoSaldoInsuficiente;
            }

            if (!string.IsNullOrEmpty(statusDetail) && statusDetail.StartsWith(ConstantsCaucao.MercadoPagoDetalheDadosIncorretosPrefixo, StringComparison.OrdinalIgnoreCase))
            {
                return ConstantsMessage.MotivoCaucaoDadosIncorretos;
            }

            if (string.Equals(statusDetail, ConstantsCaucao.MercadoPagoDetalheAutorizacaoEmissor, StringComparison.OrdinalIgnoreCase))
            {
                return ConstantsMessage.MotivoCaucaoAutorizacaoEmissor;
            }

            if (string.Equals(statusDetail, ConstantsCaucao.MercadoPagoDetalheCartaoDesabilitado, StringComparison.OrdinalIgnoreCase))
            {
                return ConstantsMessage.MotivoCaucaoCartaoDesabilitado;
            }

            if (string.Equals(statusDetail, ConstantsCaucao.MercadoPagoDetalheRiscoAlto, StringComparison.OrdinalIgnoreCase))
            {
                return ConstantsMessage.MotivoCaucaoRiscoSeguranca;
            }

            return ConstantsMessage.MotivoCaucaoOutro;
        }

        /// <summary>Detalhe do status (limpo, mascarado, truncado). Fica em DetalheProvedor para suporte; nunca vai ao cliente.</summary>
        private static string? MontarDetalheProvedor(string? statusDetail, RequestInfo requisicao)
        {
            string detalhe = LimparTexto(statusDetail, requisicao);
            return string.IsNullOrWhiteSpace(detalhe) ? null : detalhe;
        }

        /// <summary>Quatro digitos do cartao, so se estiverem no formato esperado; qualquer outra coisa vira nulo.</summary>
        private static string? MontarCartaoFinal(string? ultimosDigitos)
        {
            if (string.IsNullOrEmpty(ultimosDigitos) || !Regex.IsMatch(ultimosDigitos, ConstantsCaucao.PadraoCartaoFinal))
            {
                return null;
            }

            return ultimosDigitos;
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
        // Log: texto do provedor limpo antes de qualquer uso
        // ------------------------------------------------------------------

        /// <summary>
        /// Limpa um texto do provedor para log ou detalhe: remove caracteres de controle (menores que 0x20) ANTES
        /// de mascarar (assim um segredo quebrado por controle nao escapa), mascara sem diferenciar maiusculas e trunca.
        /// </summary>
        private static string LimparTexto(string? valor, RequestInfo requisicao)
        {
            // Remove todo caractere de controle (inclui 0x7F e C1) e os separadores de linha/paragrafo Unicode.
            string semControle = new string((valor ?? string.Empty)
                .Where(caractere => !char.IsControl(caractere) && caractere != (char)0x2028 && caractere != (char)0x2029)
                .ToArray());
            string mascarado = MascararValores(semControle, requisicao.SensitiveValues);

            if (mascarado.Length > ConstantsCaucao.TamanhoMaximoDetalheProvedor)
            {
                return mascarado.Substring(0, ConstantsCaucao.TamanhoMaximoDetalheProvedor);
            }

            return mascarado;
        }

        /// <summary>Substitui cada valor protegido por *** sem diferenciar maiusculas de minusculas.</summary>
        private static string MascararValores(string texto, IEnumerable<string> valores)
        {
            string resultado = texto;

            foreach (string valor in valores)
            {
                if (string.IsNullOrEmpty(valor))
                {
                    continue;
                }

                resultado = Regex.Replace(resultado, Regex.Escape(valor), ConstantsIntegration.Mascara, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return resultado;
        }

        /// <summary>
        /// Registra a resposta inesperada com so os campos permitidos: codigo, provedor, HTTP, status do provedor,
        /// detalhe do provedor (ambos limpos) e identificador. Corpo bruto, token, CartaoFinal, e-mail e CPF nunca entram.
        /// </summary>
        private void RegistrarRespostaInesperada(string codigo, RequestInfo requisicao, int statusHttp, string? statusProvedor, string? detalheProvedor, string? identificador)
        {
            string statusLimpo = LimparTexto(statusProvedor, requisicao);
            string detalheLimpo = LimparTexto(detalheProvedor, requisicao);

            string identificadorLog = string.IsNullOrWhiteSpace(identificador) ? ConstantsCaucao.MarcadorAusente : identificador;

            string registro = string.Format(
                CultureInfo.InvariantCulture,
                ConstantsMessage.LogCaucaoRespostaInesperada,
                codigo,
                ConstantsCaucao.ProvedorMercadoPago,
                statusHttp,
                string.IsNullOrWhiteSpace(statusLimpo) ? ConstantsCaucao.MarcadorAusente : statusLimpo,
                string.IsNullOrWhiteSpace(detalheLimpo) ? ConstantsCaucao.MarcadorAusente : detalheLimpo,
                identificadorLog);

            _logService.Write(registro, ConstantsMessage.ErroCaucaoRespostaInesperada, GetType().ToString());
        }

        /// <summary>
        /// Registra a recusa 400/422 com HTTP e os codigos de causa (cause[].code). O corpo e mascarado, lido so
        /// para esses codigos, e cada codigo fica so com [A-Za-z0-9_-], truncado. Texto livre nunca entra no log.
        /// </summary>
        private void RegistrarRecusaSolicitacao(string codigo, RequestInfo requisicao, int statusHttp, string? corpo)
        {
            string codigos = ExtrairCodigosCausa(corpo, requisicao);

            string registro = string.Format(
                CultureInfo.InvariantCulture,
                ConstantsMessage.LogCaucaoCausaRecusada,
                codigo,
                ConstantsCaucao.ProvedorMercadoPago,
                statusHttp,
                codigos);

            _logService.Write(registro, ConstantsMessage.ErroCaucaoSolicitacaoRecusada, GetType().ToString());
        }

        private static string ExtrairCodigosCausa(string? corpo, RequestInfo requisicao)
        {
            if (string.IsNullOrWhiteSpace(corpo))
            {
                return ConstantsCaucao.MarcadorAusente;
            }

            // Mascara antes de ler: nenhum valor protegido chega ao parser nem ao log.
            string corpoMascarado = MascararValores(corpo, requisicao.SensitiveValues);

            MercadoPagoErroCaucaoResponse? erro;

            try
            {
                erro = IntegrationSerializer.Deserialize<MercadoPagoErroCaucaoResponse>(corpoMascarado);
            }
            catch (JsonException)
            {
                return ConstantsCaucao.MarcadorAusente;
            }

            if (erro?.Cause == null)
            {
                return ConstantsCaucao.MarcadorAusente;
            }

            List<string> codigos = new List<string>();

            foreach (MercadoPagoCausaCaucaoResponse? causa in erro.Cause.Take(ConstantsCaucao.MaximoCodigosCausa))
            {
                // Item nulo na lista de causas nao derruba a recusa: e pulado.
                if (causa == null)
                {
                    continue;
                }

                string codigo = TextoDoCodigo(causa.Code);

                // So codigo numerico curto entra no log; qualquer outra coisa e descartada.
                if (!Regex.IsMatch(codigo, ConstantsCaucao.PadraoCodigoCausa))
                {
                    continue;
                }

                codigos.Add(codigo);
            }

            return codigos.Count == 0 ? ConstantsCaucao.MarcadorAusente : string.Join(",", codigos);
        }

        private static string TextoDoCodigo(JsonElement? code)
        {
            if (!code.HasValue)
            {
                return string.Empty;
            }

            JsonElement valor = code.Value;

            if (valor.ValueKind == JsonValueKind.String)
            {
                return valor.GetString() ?? string.Empty;
            }

            if (valor.ValueKind == JsonValueKind.Number)
            {
                return valor.GetRawText();
            }

            return string.Empty;
        }
    }
}
