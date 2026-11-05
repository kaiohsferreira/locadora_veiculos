using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Documento;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Documento;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Integrations.Services.Documento
{
    /// <summary>
    /// Provedor de teste da leitura da CNH. Devolve uma CNH sintetica fixa com
    /// confianca 0.97. Se o nome do arquivo contiver 'baixa-confianca', devolve 0.60.
    /// Uso somente em SANDBOX: a regra de ambiente fica no DocumentoOcrGateway.
    /// </summary>
    public class FakeDocumentoOcrProvider : IDocumentoOcrProvider
    {
        private readonly ILogService _logService;

        public FakeDocumentoOcrProvider(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>Nome do provedor: FAKE.</summary>
        public string Nome => ConstantsDocumento.ProvedorFake;

        /// <summary>Devolve a CNH sintetica. Nao faz chamada externa.</summary>
        public Task<CnhOcrBruto> AnalisarCnhAsync(ConfiguracaoIntegracao configuracao, CnhImagem imagem, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool baixaConfianca = !string.IsNullOrEmpty(imagem.NomeArquivo)
                    && imagem.NomeArquivo.Contains(ConstantsDocumento.FakeMarcadorBaixaConfianca, StringComparison.OrdinalIgnoreCase);

                decimal confianca = baixaConfianca ? ConstantsDocumento.FakeConfiancaBaixa : ConstantsDocumento.FakeConfianca;

                CnhOcrBruto bruto = new CnhOcrBruto
                {
                    Numero = ConstantsDocumento.FakeNumeroCnh,
                    ConfiancaNumero = confianca,
                    Categoria = ConstantsDocumento.FakeCategoria,
                    ConfiancaCategoria = confianca,
                    Validade = ConstantsDocumento.FakeValidade,
                    ConfiancaValidade = confianca
                };

                return Task.FromResult<CnhOcrBruto>(bruto);
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
    }
}
