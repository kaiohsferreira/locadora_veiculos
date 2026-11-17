using AutoVia.Core.ServicesInterface;
using AutoVia.Integrations.Helpers.Armazenamento;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Processador de foto falso para os testes de servico: conta as chamadas, guarda o token recebido e, quando
    /// recebe Falha, lanca essa excecao (recusa de imagem ou fila cheia). Sem Falha devolve uma foto JPEG fixa.
    /// </summary>
    public sealed class FakeProcessadorFotoService : IProcessadorFotoService
    {
        /// <summary>Conteudo que o processador falso devolve: diferente da entrada dos testes, para provar que o servico envia o recodificado.</summary>
        public static readonly byte[] ConteudoRecodificado = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x52, 0x45, 0x43, 0x4F };

        /// <summary>Quantas vezes ProcessarAsync foi chamado.</summary>
        public int ChamadasCount { get; private set; }

        /// <summary>Ultimo token recebido por ProcessarAsync.</summary>
        public CancellationToken UltimoToken { get; private set; }

        /// <summary>Excecao a lancar em toda chamada; nula = devolve a foto fixa.</summary>
        public Exception? Falha { get; set; }

        public Task<FotoProcessada> ProcessarAsync(byte[]? conteudo, string? contentType, CancellationToken cancellationToken)
        {
            ChamadasCount++;
            UltimoToken = cancellationToken;

            if (Falha != null)
            {
                throw Falha;
            }

            return Task.FromResult(new FotoProcessada(ConteudoRecodificado, ConstantsArmazenamento.MimeJpeg, 800, 600));
        }
    }
}
