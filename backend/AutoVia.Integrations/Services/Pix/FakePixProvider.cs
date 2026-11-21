using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede. Identificador deterministico:
    /// FAKE- + 12 primeiros hex do SHA-256 da ReferenciaExterna, entao a mesma referencia
    /// devolve a mesma cobranca. QR de teste: PNG quadriculado gerado por PngQrCodeFake (nao e QR lido por banco).
    /// TRAVA UNICA (decisao 1 de 21/11/2026): so responde com a chave Pagamentos:PermitirProvedorFake verdadeira, IHostEnvironment
    /// Development e ambiente da integracao SANDBOX. A regra esta em RegrasProvedorFakePix.PodeResponder e e checada aqui.
    /// </summary>
    public class FakePixProvider : IPixProvider
    {
        private readonly PixFakeStore _store;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;

        public FakePixProvider(PixFakeStore store, IHostEnvironment hostEnvironment, IConfiguration configuration, ILogService logService)
        {
            _store = store;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _logService = logService;
        }

        public string Nome => ConstantsPix.ProvedorFake;

        public Task<PixCobrancaResultado> CriarAsync(ConfiguracaoIntegracao configuracao, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                ConferirTravaDupla(configuracao);
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
                ConferirTravaDupla(configuracao);
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
                ConferirTravaDupla(configuracao);
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

        /// <summary>
        /// Trava unica do FAKE: sem a chave, fora de Development ou fora do SANDBOX nao responde (502, com a mensagem do FAKE
        /// fora do sandbox). Recusa antes de tocar no PixFakeStore: nada e criado nem cancelado.
        /// </summary>
        private void ConferirTravaDupla(ConfiguracaoIntegracao configuracao)
        {
            bool chavePermitida = RegrasProvedorFakePix.LerChave(_configuration);

            if (!RegrasProvedorFakePix.PodeResponder(_hostEnvironment.IsDevelopment(), configuracao.Ambiente, chavePermitida))
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroPixFakeForaDoSandbox);
            }
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
                QrCodeBase64 = Convert.ToBase64String(PngQrCodeFake.Gerar(identificador)),
                ExpiraEm = DateTime.Now.AddSeconds(expiracaoSegundos),
                Provedor = ConstantsPix.ProvedorFake
            };

            return cobranca;
        }
    }
}
