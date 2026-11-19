using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using System.Globalization;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>Resolve ARMAZENAMENTO e delega ao provider suportado.</summary>
    public sealed class ArmazenamentoGateway : IArmazenamentoGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IArmazenamentoProvider> _providers;
        private readonly ILogService _logService;

        public ArmazenamentoGateway(IIntegrationResolver integrationResolver, IEnumerable<IArmazenamentoProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        public async Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracaoIntegracao configuracao = await ResolverAsync(cancellationToken);
                return await Selecionar(configuracao).EnviarAsync(configuracao, ObterBucket(configuracao), chave, conteudo, contentType, ObterExpiracao(configuracao), cancellationToken);
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ex.Message, GetType().ToString());
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoEnviar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoEnviar);
            }
        }

        public async Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracaoIntegracao configuracao = await ResolverAsync(cancellationToken);
                return await Selecionar(configuracao).ObterUrlAsync(configuracao, ObterBucket(configuracao), chave, ObterExpiracao(configuracao), cancellationToken);
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ex.Message, GetType().ToString());
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoObterUrl, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        /// <summary>Assina em lote as URLs das chaves: resolve a integracao uma vez, sem chamada de rede por chave; lista vazia nao resolve nada.</summary>
        public async Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default)
        {
            try
            {
                if (chaves.Count == 0) return new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
                ConfiguracaoIntegracao configuracao = await ResolverAsync(cancellationToken);
                return await Selecionar(configuracao).AssinarUrlsAsync(configuracao, ObterBucket(configuracao), chaves, ObterExpiracao(configuracao), cancellationToken);
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ex.Message, GetType().ToString());
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoObterUrl, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        public async Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracaoIntegracao configuracao = await ResolverAsync(cancellationToken);
                await Selecionar(configuracao).RemoverAsync(configuracao, ObterBucket(configuracao), chave, cancellationToken);
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ex.Message, GetType().ToString());
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoRemover, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoRemover);
            }
        }

        private async Task<ConfiguracaoIntegracao> ResolverAsync(CancellationToken cancellationToken)
        {
            ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Armazenamento, cancellationToken);
            return configuracao;
        }

        /// <summary>Escolhe o provedor pelo nome da chave 'provedor' (ausente = S3_COMPATIVEL; com Trim, sem diferenciar maiusculas; valor vazio ou desconhecido = EA-05).</summary>
        private IArmazenamentoProvider Selecionar(ConfiguracaoIntegracao configuracao)
        {
            string nome = (configuracao.GetOptional(ConstantsArmazenamento.ChaveProvedor) ?? ConstantsArmazenamento.ProvedorS3Compativel).Trim();
            IArmazenamentoProvider? provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase));
            if (provider == null) throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveProvedor, ConstantsMessage.ErroArmazenamentoProvedorNaoSuportado);
            return provider;
        }

        private static string ObterBucket(ConfiguracaoIntegracao configuracao)
        {
            return configuracao.GetOptional(ConstantsArmazenamento.ChaveBucket) ?? ConstantsArmazenamento.BucketPadrao;
        }

        private static int ObterExpiracao(ConfiguracaoIntegracao configuracao)
        {
            string? valor = configuracao.GetOptional(ConstantsArmazenamento.ChaveExpiracao);
            if (string.IsNullOrWhiteSpace(valor)) return ConstantsArmazenamento.ExpiracaoPadraoSegundos;
            int expiracao;
            if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out expiracao) || expiracao <= 0 || expiracao > ConstantsArmazenamento.ExpiracaoMaximaSegundos)
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveExpiracao, ConstantsMessage.ErroArmazenamentoExpiracaoInvalida);
            return expiracao;
        }

        private static Exception CriarErroSanitizado(Exception exception)
        {
            return new Exception(exception.GetType().Name);
        }
    }
}
