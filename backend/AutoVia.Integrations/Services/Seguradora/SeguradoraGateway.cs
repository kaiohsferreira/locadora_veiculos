using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Seguradora;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Seguradora;
using AutoVia.Integrations.Interfaces;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Seguradora
{
    /// <summary>
    /// Gateway da integracao SEGURADORA (P9-INT-01). Valida o pedido (BusinessException, 409),
    /// resolve a integracao SEGURADORA, le 'provedor', escolhe o ISeguradoraProvider pelo Nome
    /// (FAKE so em SANDBOX) e repassa a chamada. Nao persiste nada.
    /// </summary>
    public class SeguradoraGateway : ISeguradoraGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<ISeguradoraProvider> _providers;
        private readonly ILogService _logService;

        public SeguradoraGateway(IIntegrationResolver integrationResolver, IEnumerable<ISeguradoraProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        /// <summary>Informa se a apolice estava vigente na data.</summary>
        public async Task<VigenciaApoliceResultado> ConsultarVigenciaAsync(string numeroApolice, DateOnly dataReferencia, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarApolice(numeroApolice);

                if (dataReferencia == default(DateOnly))
                {
                    throw new BusinessException(ConstantsMessage.ErroSeguradoraDataReferenciaObrigatoria);
                }

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Seguradora, cancellationToken);
                ISeguradoraProvider provider = SelecionarProvedor(configuracao);

                return await provider.ConsultarVigenciaAsync(configuracao, numeroApolice.Trim(), dataReferencia, cancellationToken);
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
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraConsultarVigencia, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Seguradora, ConstantsMessage.ErroSeguradoraConsultarVigencia, ex);
            }
        }

        /// <summary>Abre o sinistro na seguradora e devolve o protocolo (idempotente pela ReferenciaExterna).</summary>
        public async Task<SinistroSeguradoraResultado> AbrirSinistroAsync(SinistroAberturaSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(solicitacao);

            try
            {
                ValidarSolicitacao(solicitacao);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Seguradora, cancellationToken);
                ISeguradoraProvider provider = SelecionarProvedor(configuracao);

                return await provider.AbrirSinistroAsync(configuracao, solicitacao, cancellationToken);
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
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraAbrirSinistro, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Seguradora, ConstantsMessage.ErroSeguradoraAbrirSinistro, ex);
            }
        }

        /// <summary>Consulta o status e a linha do tempo do sinistro pelo protocolo.</summary>
        public async Task<SinistroSeguradoraResultado> ConsultarSinistroAsync(string protocolo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(protocolo) || protocolo.Trim().Length > ConstantsSeguradora.ProtocoloTamanhoMaximo)
                {
                    throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraProtocoloObrigatorio, ConstantsSeguradora.ProtocoloTamanhoMaximo));
                }

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Seguradora, cancellationToken);
                ISeguradoraProvider provider = SelecionarProvedor(configuracao);

                return await provider.ConsultarSinistroAsync(configuracao, protocolo.Trim(), cancellationToken);
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
                _logService.Write(ex, ConstantsMessage.ErroSeguradoraConsultarSinistro, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Seguradora, ConstantsMessage.ErroSeguradoraConsultarSinistro, ex);
            }
        }

        // ------------------------------------------------------------------
        // Validacao dos pedidos (funcoes puras; BusinessException -> 409)
        // ------------------------------------------------------------------

        private static void ValidarSolicitacao(SinistroAberturaSolicitacao solicitacao)
        {
            string referencia = solicitacao.ReferenciaExterna;

            if (string.IsNullOrEmpty(referencia)
                || referencia.Length > ConstantsSeguradora.ReferenciaTamanhoMaximo
                || !Regex.IsMatch(referencia, ConstantsSeguradora.PadraoReferenciaExterna))
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraReferenciaInvalida, ConstantsSeguradora.ReferenciaTamanhoMaximo));
            }

            ValidarApolice(solicitacao.NumeroApolice);

            if (string.IsNullOrWhiteSpace(solicitacao.Placa) || solicitacao.Placa.Trim().Length > ConstantsSeguradora.PlacaTamanhoMaximo)
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraPlacaObrigatoria, ConstantsSeguradora.PlacaTamanhoMaximo));
            }

            DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);

            if (solicitacao.DataOcorrencia == default(DateOnly) || solicitacao.DataOcorrencia > hoje)
            {
                throw new BusinessException(ConstantsMessage.ErroSeguradoraDataOcorrenciaInvalida);
            }

            if (!Enum.IsDefined(solicitacao.Tipo))
            {
                throw new BusinessException(ConstantsMessage.ErroSeguradoraTipoInvalido);
            }

            if (solicitacao.Descricao != null && solicitacao.Descricao.Length > ConstantsSeguradora.DescricaoTamanhoMaximo)
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraDescricaoTamanho, ConstantsSeguradora.DescricaoTamanhoMaximo));
            }

            if (solicitacao.ValorPrejuizo.HasValue && solicitacao.ValorPrejuizo.Value <= 0m)
            {
                throw new BusinessException(ConstantsMessage.ErroSeguradoraValorPrejuizo);
            }
        }

        private static void ValidarApolice(string numeroApolice)
        {
            if (string.IsNullOrWhiteSpace(numeroApolice) || numeroApolice.Trim().Length > ConstantsSeguradora.NumeroApoliceTamanhoMaximo)
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraApoliceObrigatoria, ConstantsSeguradora.NumeroApoliceTamanhoMaximo));
            }
        }

        // ------------------------------------------------------------------
        // Configuracao da integracao SEGURADORA
        // ------------------------------------------------------------------

        /// <summary>Escolhe o provedor pela chave 'provedor'; FAKE fora do SANDBOX e provedor desconhecido viram IntegrationException (502).</summary>
        private ISeguradoraProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string nomeProvedor = configuracao.GetRequired(ConstantsSeguradora.ChaveProvedor).Trim();

            ISeguradoraProvider? provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsSeguradora.ChaveProvedor, ConstantsMessage.ErroSeguradoraProvedorNaoSuportado);
            }

            bool isFake = string.Equals(provider.Nome, ConstantsSeguradora.ProvedorFake, StringComparison.OrdinalIgnoreCase);

            if (isFake && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsSeguradora.ChaveProvedor, ConstantsMessage.ErroSeguradoraFakeForaDoSandbox);
            }

            return provider;
        }
    }
}
