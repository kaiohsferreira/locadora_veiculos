using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Documento;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Documento;
using AutoVia.Integrations.Interfaces;
using System.Globalization;

namespace AutoVia.Integrations.Services.Documento
{
    /// <summary>
    /// Gateway de leitura da CNH: valida a imagem, le a configuracao DOCUMENTO,
    /// escolhe o provedor pelo Nome, normaliza os campos e aplica a regra de
    /// confianca. Nao fala com provedor por HTTP: quem faz isso e o IDocumentoOcrProvider.
    /// </summary>
    public class DocumentoOcrGateway : IDocumentoOcrGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IDocumentoOcrProvider> _providers;
        private readonly ILogService _logService;

        public DocumentoOcrGateway(IIntegrationResolver integrationResolver, IEnumerable<IDocumentoOcrProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        /// <summary>
        /// Le numero, categoria e validade da CNH a partir da imagem enviada e informa
        /// se a equipe precisa conferir os dados manualmente.
        /// </summary>
        public async Task<CnhLeituraResultado> LerCnhAsync(CnhImagem imagem, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarImagem(imagem);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Documento, cancellationToken);

                string provedor = configuracao.GetRequired(ConstantsDocumento.ChaveProvedor).Trim().ToUpperInvariant();
                string modeloOcr = configuracao.GetRequired(ConstantsDocumento.ChaveModeloOcr);
                decimal confiancaMinima = LerConfiancaMinima(configuracao);

                if (string.Equals(provedor, ConstantsDocumento.ProvedorFake, StringComparison.Ordinal)
                    && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
                {
                    throw new IntegrationException(
                        ConstantsIntegration.Documento,
                        ConstantsDocumento.ChaveProvedor,
                        ConstantsMessage.ErroDocumentoFakeForaDoSandbox);
                }

                IDocumentoOcrProvider? provider = _providers.FirstOrDefault(
                    x => string.Equals(x.Nome, provedor, StringComparison.OrdinalIgnoreCase));

                if (provider == null)
                {
                    throw new IntegrationException(
                        ConstantsIntegration.Documento,
                        ConstantsDocumento.ChaveProvedor,
                        ConstantsMessage.ErroDocumentoProvedorNaoSuportado);
                }

                CnhOcrBruto? bruto = await provider.AnalisarCnhAsync(configuracao, imagem, cancellationToken);

                return MontarResultado(bruto ?? new CnhOcrBruto(), confiancaMinima, modeloOcr, provider.Nome);
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
                _logService.Write(ex, ConstantsMessage.ErroDocumentoLerCnh, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Documento, ConstantsMessage.ErroDocumentoLerCnh, ex);
            }
        }

        /// <summary>Imagem obrigatoria, ate 4 MB, JPG ou PNG, e com bytes que batem com o ContentType.</summary>
        private static void ValidarImagem(CnhImagem imagem)
        {
            if (imagem == null
                || imagem.Conteudo == null
                || imagem.Conteudo.Length == 0
                || string.IsNullOrWhiteSpace(imagem.NomeArquivo))
            {
                throw new BusinessException(ConstantsMessage.ErroDocumentoImagemObrigatoria);
            }

            if (imagem.Conteudo.Length > ConstantsDocumento.TamanhoMaximoBytes)
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroDocumentoImagemTamanho, ConstantsDocumento.TamanhoMaximoMb));
            }

            if (!ConteudoCombinaComTipo(imagem.Conteudo, imagem.ContentType))
            {
                throw new BusinessException(ConstantsMessage.ErroDocumentoImagemTipo);
            }
        }

        private static bool ConteudoCombinaComTipo(byte[] conteudo, string contentType)
        {
            if (string.Equals(contentType, ConstantsDocumento.TipoJpeg, StringComparison.OrdinalIgnoreCase))
                return ComecaCom(conteudo, ConstantsDocumento.AssinaturaJpeg);

            if (string.Equals(contentType, ConstantsDocumento.TipoPng, StringComparison.OrdinalIgnoreCase))
                return ComecaCom(conteudo, ConstantsDocumento.AssinaturaPng);

            return false;
        }

        private static bool ComecaCom(byte[] conteudo, byte[] assinatura)
        {
            if (conteudo.Length < assinatura.Length)
                return false;

            for (int indice = 0; indice < assinatura.Length; indice++)
            {
                if (conteudo[indice] != assinatura[indice])
                    return false;
            }

            return true;
        }

        /// <summary>Le confianca_minima em InvariantCulture, entre 0 e 1. Qualquer outro valor vira IntegrationException.</summary>
        private static decimal LerConfiancaMinima(ConfiguracaoIntegracao configuracao)
        {
            string valor = configuracao.GetRequired(ConstantsDocumento.ChaveConfiancaMinima);

            decimal confiancaMinima;
            bool numerica = decimal.TryParse(
                valor,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture,
                out confiancaMinima);

            if (!numerica
                || confiancaMinima < ConstantsDocumento.ConfiancaMinimaLimiteInferior
                || confiancaMinima > ConstantsDocumento.ConfiancaMinimaLimiteSuperior)
            {
                throw new IntegrationException(
                    ConstantsIntegration.Documento,
                    ConstantsDocumento.ChaveConfiancaMinima,
                    ConstantsMessage.ErroDocumentoConfiancaMinimaInvalida);
            }

            return confiancaMinima;
        }

        /// <summary>
        /// Normaliza os campos e calcula a confianca. Campo ausente conta 0 e exige
        /// conferencia. Confianca igual ao minimo nao exige conferencia.
        /// </summary>
        private static CnhLeituraResultado MontarResultado(CnhOcrBruto bruto, decimal confiancaMinima, string modeloOcr, string nomeProvedor)
        {
            string? numero = CnhFieldNormalizer.NormalizarNumero(bruto.Numero);
            CategoriaCnh? categoria = CnhFieldNormalizer.NormalizarCategoria(bruto.Categoria);
            DateOnly? validade = CnhFieldNormalizer.NormalizarValidade(bruto.Validade);

            // Confianca do provedor e limitada a 0..1 antes da media: valor fora da faixa nao vira regra.
            decimal confiancaNumero = numero == null ? 0m : Math.Clamp(bruto.ConfiancaNumero, 0m, 1m);
            decimal confiancaCategoria = categoria == null ? 0m : Math.Clamp(bruto.ConfiancaCategoria, 0m, 1m);
            decimal confiancaValidade = validade == null ? 0m : Math.Clamp(bruto.ConfiancaValidade, 0m, 1m);

            decimal confiancaGeral = Math.Min(confiancaNumero, Math.Min(confiancaCategoria, confiancaValidade));

            bool requerConferencia = numero == null
                || categoria == null
                || validade == null
                || numero.Length != ConstantsDocumento.TamanhoNumeroCnh
                || confiancaGeral < confiancaMinima;

            CnhLeituraResultado resultado = new CnhLeituraResultado
            {
                NumeroCnh = numero,
                ConfiancaNumero = confiancaNumero,
                Categoria = categoria,
                ConfiancaCategoria = confiancaCategoria,
                Validade = validade,
                ConfiancaValidade = confiancaValidade,
                ConfiancaGeral = confiancaGeral,
                ConfiancaMinima = confiancaMinima,
                RequerConferenciaManual = requerConferencia,
                ModeloOcr = modeloOcr,
                Provedor = nomeProvedor
            };

            return resultado;
        }
    }
}
