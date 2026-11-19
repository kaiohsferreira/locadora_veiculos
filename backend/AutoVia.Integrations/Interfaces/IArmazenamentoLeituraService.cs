using AutoVia.Integrations.Communication.Armazenamento;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Leitura de arquivo pela rota publica, com link assinado: confere o link e devolve o arquivo ja fechado.</summary>
    public interface IArmazenamentoLeituraService
    {
        /// <summary>
        /// Confere chave, assinatura e prazo, depois o arquivo. Recusa: NotFoundException (com registro do motivo).
        /// Falha nao prevista: ArmazenamentoLeituraException (ja registrada).
        /// </summary>
        Task<ArquivoParaLeitura> AbrirAsync(string chave, long expiraEmUnix, string assinatura, IReadOnlyCollection<string> etagsDoCliente, CancellationToken cancellationToken = default);
    }
}
