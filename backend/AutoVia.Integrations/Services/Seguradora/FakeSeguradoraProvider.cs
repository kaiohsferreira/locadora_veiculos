using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Seguradora;
using AutoVia.Integrations.Helpers.Seguradora;
using AutoVia.Integrations.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Services.Seguradora
{
    /// <summary>
    /// Seguradora de teste (Nome = FAKE). Nao chama rede. Protocolo deterministico:
    /// FAKE-SIN- + 12 primeiros hex do SHA-256 da ReferenciaExterna, entao a mesma referencia
    /// devolve o mesmo sinistro. Vigencia: vigente, salvo se o numero da apolice contiver
    /// VENCIDA. O gateway so libera este provedor em SANDBOX.
    /// </summary>
    public class FakeSeguradoraProvider : ISeguradoraProvider
    {
        private readonly SeguradoraFakeStore _store;
        private readonly ILogService _logService;

        public FakeSeguradoraProvider(SeguradoraFakeStore store, ILogService logService)
        {
            _store = store;
            _logService = logService;
        }

        /// <summary>Nome do provedor: FAKE.</summary>
        public string Nome => ConstantsSeguradora.ProvedorFake;

        /// <summary>Vigente, a menos que o numero da apolice contenha o marcador VENCIDA.</summary>
        public Task<VigenciaApoliceResultado> ConsultarVigenciaAsync(ConfiguracaoIntegracao configuracao, string numeroApolice, DateOnly dataReferencia, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool vencida = numeroApolice.Contains(ConstantsSeguradora.FakeMarcadorApoliceVencida, StringComparison.OrdinalIgnoreCase);

                VigenciaApoliceResultado resultado = new VigenciaApoliceResultado
                {
                    NumeroApolice = numeroApolice,
                    DataReferencia = dataReferencia,
                    Vigente = !vencida,
                    Provedor = Nome
                };

                return Task.FromResult(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraConsultarVigencia, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroSeguradoraConsultarVigencia, ex);
            }
        }

        /// <summary>Cria o sinistro ABERTO no store (ou devolve o existente da mesma referencia).</summary>
        public Task<SinistroSeguradoraResultado> AbrirSinistroAsync(ConfiguracaoIntegracao configuracao, SinistroAberturaSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string protocolo = GerarProtocolo(solicitacao.ReferenciaExterna);
                SinistroSeguradoraResultado resultado = _store.ObterOuCriar(protocolo, solicitacao.ReferenciaExterna, Nome);

                return Task.FromResult(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraAbrirSinistro, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroSeguradoraAbrirSinistro, ex);
            }
        }

        /// <summary>Devolve o sinistro do protocolo; NotFoundException se nao existir.</summary>
        public Task<SinistroSeguradoraResultado> ConsultarSinistroAsync(ConfiguracaoIntegracao configuracao, string protocolo, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                SinistroSeguradoraResultado? resultado = _store.Obter(protocolo);

                if (resultado == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroSeguradoraSinistroNaoEncontrado);
                }

                return Task.FromResult(resultado);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraConsultarSinistro, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroSeguradoraConsultarSinistro, ex);
            }
        }

        /// <summary>FAKE-SIN- + 12 primeiros hex (maiusculos) do SHA-256 da referencia. Funcao pura.</summary>
        private static string GerarProtocolo(string referenciaExterna)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(referenciaExterna));
            string hex = Convert.ToHexString(hash);

            return ConstantsSeguradora.PrefixoProtocoloFake + hex.Substring(0, ConstantsSeguradora.TamanhoHashProtocoloFake);
        }
    }
}
