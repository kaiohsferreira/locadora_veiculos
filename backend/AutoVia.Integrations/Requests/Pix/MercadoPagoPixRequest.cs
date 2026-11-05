using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers.Pix;
using System.Globalization;

namespace AutoVia.Integrations.Requests.Pix
{
    /// <summary>
    /// Monta as RequestInfo do Mercado Pago. Classe pura: nao faz IO e nao captura
    /// excecao; quem chama (MercadoPagoPixProvider) e quem envia e trata o erro.
    ///
    /// Todo request leva SensitiveValues = valores protegidos da integracao + o proprio
    /// access_token, para que nada disso apareca no log. O timeout vem do banco.
    /// </summary>
    public static class MercadoPagoPixRequest
    {
        /// <summary>POST /v1/payments: cria a cobranca PIX. Repetivel, pois a chave de idempotencia e a ReferenciaExterna.</summary>
        public static RequestInfo Criar(ConfiguracaoIntegracao configuracao, string accessToken, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, DateTime agora)
        {
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Post, ConstantsPix.MercadoPagoRecursoPagamentos);

            requisicao.Headers[ConstantsPix.CabecalhoIdempotenciaMercadoPago] = solicitacao.ReferenciaExterna;
            requisicao.IdempotencyKey = solicitacao.ReferenciaExterna;
            requisicao.AllowRetry = true;
            requisicao.Body = MontarCorpoCriacao(solicitacao, expiracaoSegundos, agora);

            // E-mail e CPF do pagador nunca podem aparecer em log nem em motivo de recusa devolvido ao chamador.
            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.Pagador.Email);
            AdicionarSeInformado(requisicao.SensitiveValues, solicitacao.Pagador.Documento);

            return requisicao;
        }

        /// <summary>GET /v1/payments/{id}: consulta o estado da cobranca.</summary>
        public static RequestInfo Consultar(ConfiguracaoIntegracao configuracao, string accessToken, string identificadorProvedor)
        {
            string recurso = ConstantsPix.MercadoPagoRecursoPagamentos + "/" + Uri.EscapeDataString(identificadorProvedor);
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Get, recurso);
            requisicao.AllowRetry = true;

            return requisicao;
        }

        /// <summary>PUT /v1/payments/{id} com {"status":"cancelled"}. Idempotente, pode repetir.</summary>
        public static RequestInfo Cancelar(ConfiguracaoIntegracao configuracao, string accessToken, string identificadorProvedor)
        {
            string recurso = ConstantsPix.MercadoPagoRecursoPagamentos + "/" + Uri.EscapeDataString(identificadorProvedor);
            RequestInfo requisicao = MontarBase(configuracao, accessToken, HttpMethod.Put, recurso);
            requisicao.AllowRetry = true;
            requisicao.Body = new MercadoPagoCancelamentoRequest { Status = ConstantsPix.MercadoPagoStatusCancelado };

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

            requisicao.Headers[ConstantsPix.CabecalhoAutorizacao] = ConstantsPix.PrefixoBearer + accessToken;

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

        private static MercadoPagoCriarPagamentoRequest MontarCorpoCriacao(PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, DateTime agora)
        {
            DateTimeOffset expiraEm = new DateTimeOffset(agora).AddSeconds(expiracaoSegundos);
            string[] partesNome = solicitacao.Pagador.Nome.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            MercadoPagoCriarPagamentoRequest corpo = new MercadoPagoCriarPagamentoRequest
            {
                TransactionAmount = FormatarValorDuasCasas(solicitacao.Valor),
                Description = solicitacao.Descricao,
                PaymentMethodId = ConstantsPix.MercadoPagoMetodoPix,
                ExternalReference = solicitacao.ReferenciaExterna,
                DateOfExpiration = expiraEm.ToString(ConstantsPix.MercadoPagoFormatoDataExpiracao, CultureInfo.InvariantCulture),
                Payer = new MercadoPagoPagadorRequest
                {
                    Email = solicitacao.Pagador.Email,
                    FirstName = partesNome.Length > 0 ? partesNome[0] : string.Empty,
                    LastName = partesNome.Length > 1 ? partesNome[1] : null,
                    Identification = MontarIdentificacao(solicitacao.Pagador.Documento)
                }
            };

            return corpo;
        }

        private static MercadoPagoIdentificacaoRequest? MontarIdentificacao(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                return null;
            }

            return new MercadoPagoIdentificacaoRequest
            {
                Type = ConstantsPix.MercadoPagoTipoDocumentoCpf,
                Number = documento
            };
        }

        /// <summary>
        /// Garante 2 casas decimais na escala do numero (10.5 vira 10.50 no JSON).
        /// Ponto decimal fixo (InvariantCulture) para o provedor.
        /// </summary>
        private static decimal FormatarValorDuasCasas(decimal valor)
        {
            string texto = valor.ToString("F2", CultureInfo.InvariantCulture);
            return decimal.Parse(texto, NumberStyles.Number, CultureInfo.InvariantCulture);
        }
    }
}
