using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede. Identificador deterministico:
    /// FAKE- + 12 primeiros hex do SHA-256 da ReferenciaExterna, entao a mesma referencia
    /// devolve a mesma cobranca. O gateway so o libera em SANDBOX.
    /// </summary>
    public class FakePixProvider : IPixProvider
    {
        private readonly PixFakeStore _store;
        private readonly ILogService _logService;

        public FakePixProvider(PixFakeStore store, ILogService logService)
        {
            _store = store;
            _logService = logService;
        }

        public string Nome => ConstantsPix.ProvedorFake;

        public Task<PixCobrancaResultado> CriarAsync(ConfiguracaoIntegracao configuracao, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                string identificador = GerarIdentificador(solicitacao.ReferenciaExterna);
                PixCobrancaResultado cobranca = _store.ObterOuCriar(identificador, () => CriarCobranca(identificador, solicitacao, expiracaoSegundos));

                return Task.FromResult(cobranca);
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
                _logService.Write(ex, ConstantsMessage.ErroPixCriarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixCriarCobranca, ex);
            }
        }

        public Task<PixCobrancaResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                PixCobrancaResultado? cobranca = _store.Obter(identificadorProvedor);

                if (cobranca == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPixCobrancaNaoEncontrada);
                }

                return Task.FromResult(cobranca);
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
                _logService.Write(ex, ConstantsMessage.ErroPixConsultarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixConsultarCobranca, ex);
            }
        }

        public Task<PixCobrancaResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                // A verificacao de PAGA e a gravacao do cancelamento acontecem juntas no store (atomico).
                PixCobrancaResultado cancelada = _store.CancelarSeNaoPaga(identificadorProvedor);

                return Task.FromResult(cancelada);
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
                _logService.Write(ex, ConstantsMessage.ErroPixCancelarCobranca, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixCancelarCobranca, ex);
            }
        }

        /// <summary>Identificador FAKE- + 12 primeiros hex do SHA-256 da referencia (minusculo).</summary>
        private static string GerarIdentificador(string referenciaExterna)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(referenciaExterna));
            string hex = Convert.ToHexString(hash).ToLowerInvariant();

            return ConstantsPix.PrefixoIdentificadorFake + hex.Substring(0, ConstantsPix.TamanhoHashIdentificadorFake);
        }

        private static PixCobrancaResultado CriarCobranca(string identificador, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos)
        {
            PixCobrancaResultado cobranca = new PixCobrancaResultado
            {
                IdentificadorProvedor = identificador,
                ReferenciaExterna = solicitacao.ReferenciaExterna,
                Status = PixStatusCobranca.AGUARDANDO_PAGAMENTO,
                Valor = solicitacao.Valor,
                CopiaECola = ConstantsPix.PrefixoCopiaEColaFake + identificador,
                QrCodeBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ConstantsPix.PrefixoQrCodeFake + identificador)),
                ExpiraEm = DateTime.Now.AddSeconds(expiracaoSegundos),
                Provedor = ConstantsPix.ProvedorFake
            };

            return cobranca;
        }
    }
}
