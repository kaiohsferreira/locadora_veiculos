using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using System.Collections.Concurrent;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Estado em memoria das cobrancas do provedor FAKE (singleton). Usado so em SANDBOX
    /// e nos testes. MarcarComoPaga simula o pagamento feito pelo pagador.
    ///
    /// Imutabilidade: o store guarda registros imutaveis (CobrancaArmazenada) e cada leitura
    /// devolve uma copia nova de PixCobrancaResultado. Quem recebe a copia pode altera-la
    /// sem mexer no que esta guardado.
    ///
    /// Atomicidade: toda transicao (pagar, cancelar) le e grava sob o mesmo bloqueio, entao
    /// pagamento e cancelamento em paralelo nao deixam a cobranca inconsistente.
    /// </summary>
    public class PixFakeStore
    {
        private readonly ConcurrentDictionary<string, CobrancaArmazenada> _cobrancas = new ConcurrentDictionary<string, CobrancaArmazenada>(StringComparer.Ordinal);
        private readonly object _transicoes = new object();

        /// <summary>Devolve uma copia da cobranca existente pelo identificador ou cria uma com a fabrica (idempotente).</summary>
        public PixCobrancaResultado ObterOuCriar(string identificador, Func<PixCobrancaResultado> fabrica)
        {
            lock (_transicoes)
            {
                CobrancaArmazenada armazenada = _cobrancas.GetOrAdd(identificador, (string chave) => CobrancaArmazenada.DeResultado(fabrica()));
                return ParaResultado(armazenada, DateTime.Now);
            }
        }

        /// <summary>Copia da cobranca pelo identificador, ou null se nao existir.</summary>
        public PixCobrancaResultado? Obter(string identificador)
        {
            lock (_transicoes)
            {
                CobrancaArmazenada? armazenada;

                if (!_cobrancas.TryGetValue(identificador, out armazenada))
                {
                    return null;
                }

                return ParaResultado(armazenada, DateTime.Now);
            }
        }

        /// <summary>
        /// Simula o pagamento. So paga cobranca aguardando pagamento; cobranca ja paga nao muda.
        /// Cancelada ou expirada lanca BusinessException.
        /// </summary>
        public void MarcarComoPaga(string identificador)
        {
            lock (_transicoes)
            {
                CobrancaArmazenada armazenada = ObterExistente(identificador);
                PixStatusCobranca statusAtual = EstadoEfetivo(armazenada, DateTime.Now);

                if (statusAtual == PixStatusCobranca.PAGA)
                {
                    return;
                }

                if (statusAtual != PixStatusCobranca.AGUARDANDO_PAGAMENTO)
                {
                    throw new BusinessException(ConstantsMessage.ErroPixCobrancaNaoAguardaPagamento);
                }

                _cobrancas[identificador] = armazenada with { Status = PixStatusCobranca.PAGA, PagoEm = DateTime.Now };
            }
        }

        /// <summary>
        /// Cancela a cobranca, a menos que ja esteja paga: a verificacao e a gravacao acontecem no mesmo
        /// bloqueio, entao um pagamento concorrente nao pode ser cancelado por engano.
        /// Cobranca ja cancelada, expirada ou recusada devolve o estado atual, sem alterar nada.
        /// </summary>
        public PixCobrancaResultado CancelarSeNaoPaga(string identificador)
        {
            lock (_transicoes)
            {
                CobrancaArmazenada armazenada = ObterExistente(identificador);
                DateTime agora = DateTime.Now;
                PixStatusCobranca statusAtual = EstadoEfetivo(armazenada, agora);

                if (statusAtual == PixStatusCobranca.PAGA)
                {
                    throw new BusinessException(ConstantsMessage.ErroPixCancelarCobrancaPaga);
                }

                if (statusAtual != PixStatusCobranca.AGUARDANDO_PAGAMENTO)
                {
                    return ParaResultado(armazenada, agora);
                }

                CobrancaArmazenada cancelada = armazenada with { Status = PixStatusCobranca.CANCELADA };
                _cobrancas[identificador] = cancelada;

                return ParaResultado(cancelada, agora);
            }
        }

        private CobrancaArmazenada ObterExistente(string identificador)
        {
            CobrancaArmazenada? armazenada;

            if (!_cobrancas.TryGetValue(identificador, out armazenada))
            {
                throw new NotFoundException(ConstantsMessage.ErroPixCobrancaNaoEncontrada);
            }

            return armazenada;
        }

        /// <summary>Estado visto por quem le: cobranca aguardando cujo prazo ja passou vira EXPIRADA.</summary>
        private static PixStatusCobranca EstadoEfetivo(CobrancaArmazenada armazenada, DateTime agora)
        {
            bool prazoPassou = armazenada.ExpiraEm.HasValue && armazenada.ExpiraEm.Value < agora;

            if (armazenada.Status == PixStatusCobranca.AGUARDANDO_PAGAMENTO && prazoPassou)
            {
                return PixStatusCobranca.EXPIRADA;
            }

            return armazenada.Status;
        }

        /// <summary>Copia nova para quem chama: alterar o resultado nao altera o que esta guardado.</summary>
        private static PixCobrancaResultado ParaResultado(CobrancaArmazenada armazenada, DateTime agora)
        {
            return new PixCobrancaResultado
            {
                IdentificadorProvedor = armazenada.IdentificadorProvedor,
                ReferenciaExterna = armazenada.ReferenciaExterna,
                Status = EstadoEfetivo(armazenada, agora),
                Valor = armazenada.Valor,
                CopiaECola = armazenada.CopiaECola,
                QrCodeBase64 = armazenada.QrCodeBase64,
                UrlCobranca = armazenada.UrlCobranca,
                ExpiraEm = armazenada.ExpiraEm,
                PagoEm = armazenada.PagoEm,
                Provedor = armazenada.Provedor
            };
        }

        /// <summary>Registro imutavel guardado no store. Nunca sai dele: quem le recebe um PixCobrancaResultado novo.</summary>
        private sealed record CobrancaArmazenada(
            string IdentificadorProvedor,
            string ReferenciaExterna,
            PixStatusCobranca Status,
            decimal Valor,
            string? CopiaECola,
            string? QrCodeBase64,
            string? UrlCobranca,
            DateTime? ExpiraEm,
            DateTime? PagoEm,
            string Provedor)
        {
            public static CobrancaArmazenada DeResultado(PixCobrancaResultado cobranca)
            {
                return new CobrancaArmazenada(
                    cobranca.IdentificadorProvedor,
                    cobranca.ReferenciaExterna,
                    cobranca.Status,
                    cobranca.Valor,
                    cobranca.CopiaECola,
                    cobranca.QrCodeBase64,
                    cobranca.UrlCobranca,
                    cobranca.ExpiraEm,
                    cobranca.PagoEm,
                    cobranca.Provedor);
            }
        }
    }
}
