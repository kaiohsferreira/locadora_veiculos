using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>Implementa o protocolo S3-compatible com AWS SDK v4; o SDK gerencia o transporte HTTP, sem IComunicacaoService.</summary>
    public sealed class S3CompatStorageProvider : IArmazenamentoProvider
    {
        private readonly ILogService _logService;

        public S3CompatStorageProvider(ILogService logService)
        {
            _logService = logService;
        }

        public string Nome => ConstantsArmazenamento.ProvedorS3Compativel;

        public async Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using AmazonS3Client cliente = CriarCliente(configuracao);
                PutObjectRequest requisicao = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = chave,
                    InputStream = new MemoryStream(conteudo, false),
                    ContentType = contentType,
                    CannedACL = S3CannedACL.Private
                };
                requisicao.Headers.ContentDisposition = ConstantsArmazenamento.ContentDispositionInline;
                await cliente.PutObjectAsync(requisicao, cancellationToken);
                return Assinar(cliente, bucket, chave, expiracaoSegundos);
            }
            catch (IntegrationException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoEnviar, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroArmazenamentoEnviar);
            }
        }

        public async Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using AmazonS3Client cliente = CriarCliente(configuracao);
                try
                {
                    await cliente.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucket, Key = chave }, cancellationToken);
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                return Assinar(cliente, bucket, chave, expiracaoSegundos);
            }
            catch (IntegrationException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoObterUrl, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        public async Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using AmazonS3Client cliente = CriarCliente(configuracao);
                await cliente.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = chave }, cancellationToken);
            }
            catch (IntegrationException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoRemover, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroArmazenamentoRemover);
            }
        }

        private static AmazonS3Client CriarCliente(ConfiguracaoIntegracao configuracao)
        {
            Uri? endpoint;
            if (!Uri.TryCreate(configuracao.BaseUrl, UriKind.Absolute, out endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroArmazenamentoBaseUrlSemHttps);
            string accessKey = configuracao.GetRequired(ConstantsArmazenamento.ChaveAccessKey);
            if (!configuracao.IsProtected(ConstantsArmazenamento.ChaveAccessKey))
                throw new IntegrationException(configuracao.Codigo, ConstantsArmazenamento.ChaveAccessKey, string.Format(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, ConstantsArmazenamento.ChaveAccessKey));
            string secretKey = configuracao.GetRequired(ConstantsArmazenamento.ChaveSecretKey);
            if (!configuracao.IsProtected(ConstantsArmazenamento.ChaveSecretKey))
                throw new IntegrationException(configuracao.Codigo, ConstantsArmazenamento.ChaveSecretKey, string.Format(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, ConstantsArmazenamento.ChaveSecretKey));
            string regiao = configuracao.GetOptional(ConstantsArmazenamento.ChaveRegiao) ?? ConstantsArmazenamento.RegiaoPadrao;
            AmazonS3Config s3Config = new AmazonS3Config { ServiceURL = configuracao.BaseUrl, ForcePathStyle = true, AuthenticationRegion = regiao };
            return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
        }

        public async Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(ConfiguracaoIntegracao configuracao, string bucket, IReadOnlyCollection<string> chaves, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Dictionary<string, ArquivoArmazenado> arquivos = new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
                if (chaves.Count == 0) return arquivos;
                using AmazonS3Client cliente = CriarCliente(configuracao);
                DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos);
                foreach (string chave in chaves.Distinct(StringComparer.Ordinal))
                {
                    arquivos[chave] = AssinarComExpiracao(cliente, bucket, chave, expiraEm);
                }
                return arquivos;
            }
            catch (IntegrationException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenamentoObterUrl, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        private static ArquivoArmazenado Assinar(AmazonS3Client cliente, string bucket, string chave, int expiracaoSegundos)
        {
            DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos);
            return AssinarComExpiracao(cliente, bucket, chave, expiraEm);
        }

        private static ArquivoArmazenado AssinarComExpiracao(AmazonS3Client cliente, string bucket, string chave, DateTimeOffset expiraEm)
        {
            GetPreSignedUrlRequest requisicao = new GetPreSignedUrlRequest { BucketName = bucket, Key = chave, Verb = HttpVerb.GET, Expires = expiraEm.UtcDateTime };
            string url = cliente.GetPreSignedURL(requisicao);
            return MontarArquivoAssinado(url, expiraEm);
        }

        internal static ArquivoArmazenado MontarArquivoAssinado(string url, DateTimeOffset expiraEm)
        {
            Uri? urlUri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out urlUri) || urlUri.Scheme != Uri.UriSchemeHttps)
                throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoUrlAssinadaInvalida);
            return new ArquivoArmazenado(url, expiraEm);
        }

        private static Exception CriarErroSanitizado(Exception exception)
        {
            return new Exception(exception.GetType().Name);
        }
    }
}
