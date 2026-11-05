using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Documento;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor de OCR da CNH. Cada implementacao tem um Nome, que deve ser igual ao
    /// valor da chave 'provedor' da integracao DOCUMENTO. Provedores futuros usam
    /// IComunicacaoService; nenhum cria HttpClient.
    /// </summary>
    public interface IDocumentoOcrProvider
    {
        /// <summary>Nome do provedor (ex.: FAKE). Comparado sem diferenciar maiusculas.</summary>
        public string Nome { get; }

        /// <summary>Analisa a imagem e devolve os campos crus, sem normalizacao.</summary>
        public Task<CnhOcrBruto> AnalisarCnhAsync(ConfiguracaoIntegracao configuracao, CnhImagem imagem, CancellationToken cancellationToken = default);
    }
}
