using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Requests.Caucao
{
    /// <summary>
    /// Monta as RequestInfo do Mercado Pago para caucao. Classe pura: nao faz IO e nao captura
    /// excecao; quem chama (MercadoPagoCaucaoProvider) envia e trata o erro.
    ///
    /// Todo request leva SensitiveValues = valores protegidos da integracao + access_token. A
    /// pre-autorizacao acrescenta token do cartao, e-mail, CPF e device id, para que nada disso
    /// apareca em log nem em motivo de recusa devolvido ao chamador.
    /// </summary>
    public static class MercadoPagoCaucaoRequest
    {
        /// <summary>POST /v1/payments com capture=false: reserva o valor. Repetivel com a chave de idempotencia.</summary>
        public static RequestInfo PreAutorizar(ConfiguracaoIntegracao configuracao, string accessToken, CaucaoSolicitacao solicitacao, string chaveIdempotencia)
        {
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Post, ConstantsCaucao.MercadoPagoRecursoPagamentos);

            requisicao.Headers[ConstantsCaucao.CabecalhoIdempotenciaMercadoPago] = chaveIdempotencia;
            requisicao.IdempotencyKey = chaveIdempotencia;
            requisicao.AllowRetry = true;

            if (!string.IsNullOrWhiteSpace(solicitacao.IdentificadorDispositivo))
            {
                requisicao.Headers[ConstantsCaucao.CabecalhoSessaoMercadoPago] = solicitacao.IdentificadorDispositivo;
            }

            requisicao.Body = MontarCorpoPreAutorizacao(solicitacao);

            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.TokenCartao);
            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.Pagador.Email);
            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.Pagador.Documento);
            AdicionarSeInformado(requisicao.SensitiveValues, MontarCpfFormatado(solicitacao.Pagador.Documento));
            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.IdentificadorDispositivo);

            return requisicao;
        }

        /// <summary>GET /v1/payments/{id}: consulta o estado da caucao. Sem efeito colateral.</summary>
        public static RequestInfo Consultar(ConfiguracaoIntegracao configuracao, string accessToken, string identificadorProvedor)
        {
            string recurso = ConstantsCaucao.MercadoPagoRecursoPagamentos + "/" + Uri.EscapeDataString(identificadorProvedor);
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Get, recurso);
            requisicao.AllowRetry = true;

            return requisicao;
        }

        /// <summary>PUT /v1/payments/{id}: captura. Valor nulo = integral (sem transaction_amount); valor = parcial.</summary>
        public static RequestInfo Capturar(ConfiguracaoIntegracao configuracao, string accessToken, string identificadorProvedor, decimal? valor)
        {
            string recurso = ConstantsCaucao.MercadoPagoRecursoPagamentos + "/" + Uri.EscapeDataString(identificadorProvedor);
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Put, recurso);
            requisicao.AllowRetry = true;

            MercadoPagoCapturaCaucaoRequest corpo = new MercadoPagoCapturaCaucaoRequest { Capture = true };

            if (valor.HasValue)
            {
                corpo.TransactionAmount = FormatarValorDuasCasas(valor.Value);
            }

            requisicao.Body = corpo;

            return requisicao;
        }

        /// <summary>PUT /v1/payments/{id} com {"status":"cancelled"}. Idempotente, pode repetir.</summary>
        public static RequestInfo Cancelar(ConfiguracaoIntegracao configuracao, string accessToken, string identificadorProvedor)
        {
            string recurso = ConstantsCaucao.MercadoPagoRecursoPagamentos + "/" + Uri.EscapeDataString(identificadorProvedor);
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Put, recurso);
            requisicao.AllowRetry = true;
            requisicao.Body = new MercadoPagoCancelamentoCaucaoRequest { Status = ConstantsCaucao.MercadoPagoStatusCancelado };

            return requisicao;
        }

        private static RequestInfo MontarBase(ConfiguracaoIntegracao configuracao, string accessToken, HttpMethod metodo, string recurso)
        {
            RequestInfo requisicao = new RequestInfo
            {
                BaseUrl = configuracao.BaseUrl,
                Resource = recurso,
                Method = metodo,
                TimeoutMs = configuracao.TimeoutMs,
                SensitiveValues = MontarValoresSensiveis(configuracao, accessToken)
            };

            requisicao.Headers[ConstantsCaucao.CabecalhoAutorizacao] = ConstantsCaucao.PrefixoBearer + accessToken;

            return requisicao;
        }

        private static List<string> MontarValoresSensiveis(ConfiguracaoIntegracao configuracao, string accessToken)
        {
            List<string> valoresSensiveis = new List<string>(configuracao.ProtectedValues);

            if (!valoresSensiveis.Contains(accessToken))
            {
                valoresSensiveis.Add(accessToken);
            }

            return valoresSensiveis;
        }

        private static void AdicionarSeInformado(List<string> valoresSensiveis, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor) && !valoresSensiveis.Contains(valor))
            {
                valoresSensiveis.Add(valor);
            }
        }

        private static MercadoPagoCriarCaucaoRequest MontarCorpoPreAutorizacao(CaucaoSolicitacao solicitacao)
        {
            MercadoPagoCriarCaucaoRequest corpo = new MercadoPagoCriarCaucaoRequest
            {
                TransactionAmount = FormatarValorDuasCasas(solicitacao.Valor),
                Token = solicitacao.TokenCartao,
                Description = solicitacao.Descricao,
                Installments = ConstantsCaucao.MercadoPagoParcelasUnicas,
                PaymentMethodId = solicitacao.MetodoPagamentoId,
                IssuerId = solicitacao.EmissorId,
                Capture = false,
                ExternalReference = solicitacao.ReferenciaExterna,
                Payer = new MercadoPagoPagadorCaucaoRequest
                {
                    Email = solicitacao.Pagador.Email,
                    Identification = MontarIdentificacao(solicitacao.Pagador.Documento)
                }
            };

            return corpo;
        }

        /// <summary>CPF no formato 123.456.789-01, para que a variante formatada tambem seja mascarada no log.</summary>
        private static string? MontarCpfFormatado(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento) || !Regex.IsMatch(documento, ConstantsCaucao.PadraoCpf))
            {
                return null;
            }

            return documento.Substring(0, 3) + "." + documento.Substring(3, 3) + "." + documento.Substring(6, 3) + "-" + documento.Substring(9, 2);
        }

        private static MercadoPagoIdentificacaoCaucaoRequest? MontarIdentificacao(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                return null;
            }

            return new MercadoPagoIdentificacaoCaucaoRequest
            {
                Type = ConstantsCaucao.MercadoPagoTipoDocumentoCpf,
                Number = documento
            };
        }

        /// <summary>
        /// Garante 2 casas decimais na escala do numero (120 vira 120.00 no JSON).
        /// Ponto decimal fixo (InvariantCulture) para o provedor.
        /// </summary>
        private static decimal FormatarValorDuasCasas(decimal valor)
        {
            string texto = valor.ToString("F2", CultureInfo.InvariantCulture);
            return decimal.Parse(texto, NumberStyles.Number, CultureInfo.InvariantCulture);
        }
    }
}
