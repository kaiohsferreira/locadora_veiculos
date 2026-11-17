using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Processador da foto do veiculo: confere o tipo declarado e a assinatura, le as dimensoes antes de decodificar, aplica a orientacao
    /// do EXIF, reduz, pinta fundo branco (PNG com transparencia) e devolve a versao JPEG recodificada, sem metadados.
    /// Singleton: o limite de processamentos simultaneos vale para o processo inteiro.
    /// </summary>
    public interface IProcessadorFotoService
    {
        /// <summary>
        /// Valida e recodifica a foto. Recusas da imagem saem como FluentValidation.ValidationException com a mensagem especifica;
        /// com a fila de processamento cheia, ou com a espera pela vaga estourando o tempo limite, sai LimiteExcedidoException
        /// (sem log); falhas inesperadas sao registradas e relancadas. O cancellationToken e obrigatorio.
        /// </summary>
        Task<FotoProcessada> ProcessarAsync(byte[]? conteudo, string? contentType, CancellationToken cancellationToken);
    }
}
