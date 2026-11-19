using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Servico de leitura da rota publica (contrato armazenamento-disco v1, ordem_de_avaliacao 2 a 6). Confere chave, assinatura e
    /// prazo ANTES de tocar no arquivo; le o arquivo inteiro para a memoria e o fecha antes de devolver. Toda recusa registra o
    /// motivo e lanca NotFoundException. Falha nao prevista registra UMA vez (so o nome do tipo) e lanca ArmazenamentoLeituraException.
    /// </summary>
    public sealed class ArmazenamentoLeituraService : IArmazenamentoLeituraService
    {
        private readonly IRaizArmazenamentoDisco _raiz;
        private readonly ISegredoAssinaturaArmazenamento _segredo;
        private readonly TimeProvider _relogio;
        private readonly ILogService _logService;

        public ArmazenamentoLeituraService(
            IRaizArmazenamentoDisco raiz,
            ISegredoAssinaturaArmazenamento segredo,
            TimeProvider relogio,
            ILogService logService)
        {
            _raiz = raiz;
            _segredo = segredo;
            _relogio = relogio;
            _logService = logService;
        }

        public async Task<ArquivoParaLeitura> AbrirAsync(string chave, long expiraEmUnix, string assinatura, IReadOnlyCollection<string> etagsDoCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!RegrasArmazenamentoDisco.ChaveSegura(chave))
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraChaveInsegura);
                }

                byte[]? segredo = _segredo.ObterSeExistir();
                if (segredo == null || !AssinaturaLinkArmazenamento.Conferir(segredo, chave, expiraEmUnix, assinatura))
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraAssinaturaInvalida);
                }

                long agora = _relogio.GetUtcNow().ToUnixTimeSeconds();
                if (agora >= expiraEmUnix)
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraPrazoVencido);
                }

                if (expiraEmUnix > agora + ConstantsArmazenamento.ExpiracaoMaximaSegundos)
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraPrazoAcimaDoTeto);
                }

                if (!_raiz.TentarObterSemCriar(out string pastaDosArquivos, out _))
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraArquivoAusente);
                }

                string? caminho = RegrasArmazenamentoDisco.ResolverCaminho(pastaDosArquivos, chave);
                if (caminho == null)
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraChaveInsegura);
                }

                if (!RaizArmazenamentoDisco.EhArquivoRegular(caminho))
                {
                    throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraArquivoAusente);
                }

                return await LerArquivoAsync(caminho, etagsDoCliente, cancellationToken);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (EhFalhaDeAcesso(ex))
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.LogArmazenamentoLeituraFalhaInterna, GetType().ToString());
                throw new NotFoundException(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado);
            }
            catch (Exception ex)
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroArmazenamentoAbrirArquivo, GetType().ToString());
                throw new ArmazenamentoLeituraException(ex.GetType().Name);
            }
        }

        /// <summary>
        /// Abre o arquivo, confere tamanho e tipo pelos primeiros bytes, monta o ETag com os metadados do mesmo identificador e
        /// decide: NaoModificado sem ler os bytes, ou leitura inteira. O arquivo fecha antes de a funcao devolver.
        /// </summary>
        private async Task<ArquivoParaLeitura> LerArquivoAsync(string caminho, IReadOnlyCollection<string> etagsDoCliente, CancellationToken cancellationToken)
        {
            using FileStream fluxo = new FileStream(caminho, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous
            });

            SafeFileHandle identificador = fluxo.SafeFileHandle;
            long tamanho = RandomAccess.GetLength(identificador);
            DateTime ultimaGravacao = File.GetLastWriteTimeUtc(identificador);
            if (tamanho > ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes)
            {
                throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraTamanhoAcimaDoMaximo);
            }

            byte[] cabecalho = new byte[ConstantsValues.AssinaturaPng.Length];
            int tamanhoDoCabecalho = (int)Math.Min(tamanho, cabecalho.Length);
            int lidosDoCabecalho = await LerBlocoAsync(identificador, cabecalho, tamanhoDoCabecalho, cancellationToken);
            string? tipo = RegrasArmazenamentoDisco.TipoPeloConteudo(cabecalho.AsSpan(0, lidosDoCabecalho));
            if (tipo == null)
            {
                throw RecusarLink(ConstantsMessage.LogArmazenamentoLeituraTipoNaoSuportado);
            }

            string etag = tamanho.ToString(ConstantsValues.FormatoHexadecimalEtag, CultureInfo.InvariantCulture)
                + ConstantsValues.SeparadorEtagArmazenamento
                + ultimaGravacao.Ticks.ToString(ConstantsValues.FormatoHexadecimalEtag, CultureInfo.InvariantCulture);
            if (etagsDoCliente.Any(valor => string.Equals(valor, etag, StringComparison.Ordinal)))
            {
                return new ArquivoParaLeitura(Array.Empty<byte>(), tipo, etag, true);
            }

            int tamanhoInteiro = (int)tamanho;
            byte[] conteudo = new byte[tamanhoInteiro];
            int lidos = await LerBlocoAsync(identificador, conteudo, tamanhoInteiro, cancellationToken);
            if (lidos != tamanhoInteiro)
            {
                throw new IOException();
            }

            return new ArquivoParaLeitura(conteudo, tipo, etag, false);
        }

        /// <summary>Le ate a quantidade pedida a partir do inicio do arquivo; para no fim do arquivo.</summary>
        private static async Task<int> LerBlocoAsync(SafeFileHandle identificador, byte[] destino, int quantidade, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < quantidade)
            {
                int lidos = await RandomAccess.ReadAsync(identificador, destino.AsMemory(total, quantidade - total), total, cancellationToken);
                if (lidos == 0)
                {
                    break;
                }

                total += lidos;
            }

            return total;
        }

        /// <summary>Registra o motivo da recusa (constante) e devolve a NotFoundException do cliente (mensagem unica).</summary>
        private NotFoundException RecusarLink(string motivo)
        {
            _logService.Write(motivo, ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, GetType().ToString());
            return new NotFoundException(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado);
        }

        private static bool EhFalhaDeAcesso(Exception ex)
        {
            return ex is ArmazenamentoDiscoException
                || ex is IOException
                || ex is UnauthorizedAccessException;
        }
    }
}
