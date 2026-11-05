using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using System.Collections.Concurrent;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Estado em memoria das cobrancas do provedor FAKE (singleton). Usado so em SANDBOX
    /// e nos testes. MarcarComoPaga simula o pagamento feito pelo pagador.
    /// </summary>
    public class PixFakeStore
    {
        private readonly ConcurrentDictionary<string, PixCobrancaResultado> _cobrancas = new ConcurrentDictionary<string, PixCobrancaResultado>(StringComparer.Ordinal);

        /// <summary>Devolve a cobranca existente pelo identificador ou cria uma com a fabrica (idempotente).</summary>
        public PixCobrancaResultado ObterOuCriar(string identificador, Func<PixCobrancaResultado> fabrica)
        {
            return _cobrancas.GetOrAdd(identificador, (string chave) => fabrica());
        }

        /// <summary>Cobranca pelo identificador, ou null se nao existir.</summary>
        public PixCobrancaResultado? Obter(string identificador)
        {
            PixCobrancaResultado? cobranca;
            _cobrancas.TryGetValue(identificador, out cobranca);
            return cobranca;
        }

        /// <summary>Simula o pagamento da cobranca (usado nos testes e no sandbox).</summary>
        public void MarcarComoPaga(string identificador)
        {
            PixCobrancaResultado cobranca = ObterExistente(identificador);
            cobranca.Status = PixStatusCobranca.PAGA;
            cobranca.PagoEm = DateTime.Now;
        }

        /// <summary>Marca a cobranca como cancelada. Nao verifica se ja foi paga (quem chama verifica).</summary>
        public void MarcarComoCancelada(string identificador)
        {
            PixCobrancaResultado cobranca = ObterExistente(identificador);
            cobranca.Status = PixStatusCobranca.CANCELADA;
        }

        private PixCobrancaResultado ObterExistente(string identificador)
        {
            PixCobrancaResultado? cobranca = Obter(identificador);

            if (cobranca == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroPixCobrancaNaoEncontrada);
            }

            return cobranca;
        }
    }
}
