using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using System.Collections.Concurrent;

namespace AutoVia.Integrations.Services.Caucao
{
    /// <summary>
    /// Estado em memoria das caucoes do provedor FAKE (singleton). Usado so em SANDBOX e nos testes.
    ///
    /// Imutabilidade: o store guarda registros imutaveis (CaucaoArmazenada) e cada leitura devolve
    /// uma copia nova de CaucaoResultado. Quem recebe a copia pode altera-la sem mexer no que esta guardado.
    ///
    /// Atomicidade: toda transicao (capturar, cancelar) le e grava sob o mesmo bloqueio.
    /// Expiracao: reserva cujo ReservaValidaAte ja passou aparece como EXPIRADA (pelo relogio do store).
    /// </summary>
    public class CaucaoFakeStore
    {
        private readonly ConcurrentDictionary<string, CaucaoArmazenada> _caucoes = new ConcurrentDictionary<string, CaucaoArmazenada>(StringComparer.Ordinal);
        private readonly object _transicoes = new object();
        private readonly Func<DateTime> _relogio;

        public CaucaoFakeStore()
            : this(() => DateTime.Now)
        {
        }

        /// <summary>Construtor para testes: permite controlar o relogio (expiracao pelo tempo).</summary>
        internal CaucaoFakeStore(Func<DateTime> relogio)
        {
            _relogio = relogio;
        }

        /// <summary>
        /// Devolve uma copia da caucao existente pelo identificador ou cria uma com a fabrica
        /// (idempotente). A fabrica recebe o instante atual do store.
        /// </summary>
        public CaucaoResultado ObterOuCriar(string identificador, Func<DateTime, CaucaoResultado> fabrica)
        {
            lock (_transicoes)
            {
                DateTime agora = _relogio();
                CaucaoArmazenada armazenada = _caucoes.GetOrAdd(identificador, (string chave) => CaucaoArmazenada.DeResultado(fabrica(agora)));

                return ParaResultado(armazenada, agora);
            }
        }

        /// <summary>Copia da caucao pelo identificador, ou null se nao existir.</summary>
        public CaucaoResultado? Obter(string identificador)
        {
            lock (_transicoes)
            {
                CaucaoArmazenada? armazenada;

                if (!_caucoes.TryGetValue(identificador, out armazenada))
                {
                    return null;
                }

                return ParaResultado(armazenada, _relogio());
            }
        }

        /// <summary>
        /// Captura a reserva. Valor nulo = integral (valor reservado); valor menor = parcial (o restante
        /// e liberado). So captura RESERVADA; valor maior que o reservado lanca BusinessException.
        /// </summary>
        public CaucaoResultado Capturar(string identificador, decimal? valor)
        {
            lock (_transicoes)
            {
                CaucaoArmazenada armazenada = ObterExistente(identificador);
                DateTime agora = _relogio();
                CaucaoStatus statusAtual = EstadoEfetivo(armazenada, agora);

                if (statusAtual != CaucaoStatus.RESERVADA)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoNaoReservada);
                }

                decimal valorCapturar = valor ?? armazenada.Valor;

                if (valorCapturar > armazenada.Valor)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoCapturaAcimaReservado);
                }

                CaucaoArmazenada capturada = armazenada with
                {
                    Status = CaucaoStatus.CAPTURADA,
                    ValorCapturado = valorCapturar,
                    CapturadoEm = agora
                };

                _caucoes[identificador] = capturada;

                return ParaResultado(capturada, agora);
            }
        }

        /// <summary>
        /// Libera a reserva. So cancela RESERVADA. Capturada lanca BusinessException; qualquer outro
        /// estado encerrado (cancelada, expirada, recusada, em analise) devolve o estado atual, sem alterar nada.
        /// </summary>
        public CaucaoResultado Cancelar(string identificador)
        {
            return CancelarComIndicador(identificador, out _);
        }

        /// <summary>
        /// Igual a Cancelar, mas informa se ESTA chamada fez a transicao (RESERVADA/EM_ANALISE -> CANCELADA).
        /// Usado pelos testes de concorrencia; quem chama com efetivou = false recebeu o estado ja existente.
        /// </summary>
        internal CaucaoResultado CancelarComIndicador(string identificador, out bool efetivou)
        {
            efetivou = false;

            lock (_transicoes)
            {
                CaucaoArmazenada armazenada = ObterExistente(identificador);
                DateTime agora = _relogio();
                CaucaoStatus statusAtual = EstadoEfetivo(armazenada, agora);

                if (statusAtual == CaucaoStatus.CAPTURADA || statusAtual == CaucaoStatus.ESTORNADA)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoCancelarCapturada);
                }

                // RESERVADA e EM_ANALISE viram CANCELADA; CANCELADA, EXPIRADA e RECUSADA devolvem o estado atual.
                if (statusAtual != CaucaoStatus.RESERVADA && statusAtual != CaucaoStatus.EM_ANALISE)
                {
                    return ParaResultado(armazenada, agora);
                }

                CaucaoArmazenada cancelada = armazenada with { Status = CaucaoStatus.CANCELADA };
                _caucoes[identificador] = cancelada;
                efetivou = true;

                return ParaResultado(cancelada, agora);
            }
        }

        private CaucaoArmazenada ObterExistente(string identificador)
        {
            CaucaoArmazenada? armazenada;

            if (!_caucoes.TryGetValue(identificador, out armazenada))
            {
                throw new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada);
            }

            return armazenada;
        }

        /// <summary>Estado visto por quem le: reserva cujo prazo ja passou vira EXPIRADA.</summary>
        private static CaucaoStatus EstadoEfetivo(CaucaoArmazenada armazenada, DateTime agora)
        {
            bool prazoPassou = armazenada.ReservaValidaAte.HasValue && armazenada.ReservaValidaAte.Value < agora;

            if (armazenada.Status == CaucaoStatus.RESERVADA && prazoPassou)
            {
                return CaucaoStatus.EXPIRADA;
            }

            return armazenada.Status;
        }

        /// <summary>Copia nova para quem chama: alterar o resultado nao altera o que esta guardado.</summary>
        private static CaucaoResultado ParaResultado(CaucaoArmazenada armazenada, DateTime agora)
        {
            CaucaoStatus statusEfetivo = EstadoEfetivo(armazenada, agora);

            return new CaucaoResultado
            {
                IdentificadorProvedor = armazenada.IdentificadorProvedor,
                ReferenciaExterna = armazenada.ReferenciaExterna,
                Status = statusEfetivo,
                Valor = armazenada.Valor,
                ValorCapturado = armazenada.ValorCapturado,
                CartaoFinal = armazenada.CartaoFinal,
                CriadoEm = armazenada.CriadoEm,
                // ReservaValidaAte so existe enquanto a caucao esta RESERVADA (e nao expirou).
                ReservaValidaAte = statusEfetivo == CaucaoStatus.RESERVADA ? armazenada.ReservaValidaAte : null,
                CapturadoEm = armazenada.CapturadoEm,
                Provedor = armazenada.Provedor
            };
        }

        /// <summary>Registro imutavel guardado no store. Nunca sai dele: quem le recebe um CaucaoResultado novo.</summary>
        private sealed record CaucaoArmazenada(
            string IdentificadorProvedor,
            string ReferenciaExterna,
            CaucaoStatus Status,
            decimal Valor,
            decimal? ValorCapturado,
            string? CartaoFinal,
            DateTime? CriadoEm,
            DateTime? ReservaValidaAte,
            DateTime? CapturadoEm,
            string Provedor)
        {
            public static CaucaoArmazenada DeResultado(CaucaoResultado caucao)
            {
                return new CaucaoArmazenada(
                    caucao.IdentificadorProvedor,
                    caucao.ReferenciaExterna,
                    caucao.Status,
                    caucao.Valor,
                    caucao.ValorCapturado,
                    caucao.CartaoFinal,
                    caucao.CriadoEm,
                    caucao.ReservaValidaAte,
                    caucao.CapturadoEm,
                    caucao.Provedor);
            }
        }
    }
}
