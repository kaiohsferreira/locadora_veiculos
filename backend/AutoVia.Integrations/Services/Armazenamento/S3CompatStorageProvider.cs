using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroIntegracaoExterna, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroIntegracaoExterna);
            }
        }

        public async Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroIntegracaoExterna, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroIntegracaoExterna);
            }
        }

        public async Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
        {
            try
            {
                using AmazonS3Client cliente = CriarCliente(configuracao);
                await cliente.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = chave }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroIntegracaoExterna, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroIntegracaoExterna);
            }
        }

        private static AmazonS3Client CriarCliente(ConfiguracaoIntegracao configuracao)
        {
            Uri? endpoint;
            if (!Uri.TryCreate(configuracao.BaseUrl, UriKind.Absolute, out endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroIntegracaoExterna);
            if (!configuracao.IsProtected(ConstantsArmazenamento.ChaveAccessKey))
                throw new IntegrationException(configuracao.Codigo, ConstantsArmazenamento.ChaveAccessKey, ConstantsMessage.ErroIntegracaoExterna);
            if (!configuracao.IsProtected(ConstantsArmazenamento.ChaveSecretKey))
                throw new IntegrationException(configuracao.Codigo, ConstantsArmazenamento.ChaveSecretKey, ConstantsMessage.ErroIntegracaoExterna);
            string accessKey = configuracao.GetRequired(ConstantsArmazenamento.ChaveAccessKey);
            string secretKey = configuracao.GetRequired(ConstantsArmazenamento.ChaveSecretKey);
            string regiao = configuracao.GetOptional(ConstantsArmazenamento.ChaveRegiao) ?? ConstantsArmazenamento.RegiaoPadrao;
            AmazonS3Config s3Config = new AmazonS3Config { ServiceURL = configuracao.BaseUrl, ForcePathStyle = true, AuthenticationRegion = regiao };
            return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
        }

        private static ArquivoArmazenado Assinar(AmazonS3Client cliente, string bucket, string chave, int expiracaoSegundos)
        {
            DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos);
            GetPreSignedUrlRequest requisicao = new GetPreSignedUrlRequest { BucketName = bucket, Key = chave, Verb = HttpVerb.GET, Expires = expiraEm.UtcDateTime };
            string url = cliente.GetPreSignedURL(requisicao);
            Uri? urlUri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out urlUri) || urlUri.Scheme != Uri.UriSchemeHttps)
                throw new IntegrationException(ConstantsArmazenamento.Codigo, ConstantsMessage.ErroIntegracaoExterna);
            return new ArquivoArmazenado(url, expiraEm);
        }

        private static Exception CriarErroSanitizado(Exception exception)
        {
            return new Exception(exception.GetType().Name);
        }
    }
}
