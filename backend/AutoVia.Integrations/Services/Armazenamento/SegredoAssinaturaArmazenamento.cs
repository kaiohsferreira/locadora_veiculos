using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Interfaces;
using System.Security.Cryptography;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Segredo de assinatura dos links de arquivo (contrato armazenamento-disco v1, configuration.segredo). Fica em
    /// &lt;raiz&gt;/segredo/assinatura.key, fora da pasta servida. Le uma vez por processo e guarda em memoria. So ObterOuCriar cria;
    /// ObterSeExistir nunca cria. Arquivo ilegivel ou de tamanho errado vira ArmazenamentoDiscoException (ED-06).
    /// </summary>
    public sealed class SegredoAssinaturaArmazenamento : ISegredoAssinaturaArmazenamento
    {
        private readonly IRaizArmazenamentoDisco _raiz;
        private readonly ILogService _logService;
        private readonly object _trava = new object();
        private byte[]? _segredo;

        public SegredoAssinaturaArmazenamento(IRaizArmazenamentoDisco raiz, ILogService logService)
        {
            _raiz = raiz;
            _logService = logService;
        }

        public byte[] ObterOuCriar()
        {
            lock (_trava)
            {
                if (_segredo != null)
                {
                    return _segredo;
                }

                string caminho = Path.Combine(_raiz.ObterPastaDoSegredo(), ConstantsValues.ArquivoSegredoAssinatura);
                if (!RaizArmazenamentoDisco.EhArquivoRegular(caminho))
                {
                    CriarSegredo(caminho);
                }

                byte[] segredo = LerSegredo(caminho);
                _segredo = segredo;
                return segredo;
            }
        }

        public byte[]? ObterSeExistir()
        {
            lock (_trava)
            {
                if (_segredo != null)
                {
                    return _segredo;
                }

                _raiz.TentarObterSemCriar(out _, out string pastaDoSegredo);
                string caminho = Path.Combine(pastaDoSegredo, ConstantsValues.ArquivoSegredoAssinatura);
                if (!RaizArmazenamentoDisco.EhArquivoRegular(caminho))
                {
                    return null;
                }

                byte[] segredo = LerSegredo(caminho);
                _segredo = segredo;
                return segredo;
            }
        }

        /// <summary>
        /// Gera 32 bytes, grava num temporario e move SEM sobrescrever. Se outro processo criou antes, o temporario e apagado e
        /// o segredo que existe e lido depois.
        /// </summary>
        private void CriarSegredo(string caminho)
        {
            byte[] novo = RandomNumberGenerator.GetBytes(ConstantsValues.TamanhoSegredoAssinaturaBytes);
            string temporario = Path.Combine(_raiz.ObterPastaTemporaria(), Guid.NewGuid().ToString(ConstantsValues.FormatoGuidArquivoArmazenamento) + ConstantsValues.ExtensaoTemporarioArmazenamento);
            bool movido = false;
            try
            {
                GravarTemporario(temporario, novo);
                File.Move(temporario, caminho, overwrite: false);
                movido = true;
            }
            catch (Exception ex) when (EhFalhaDeArquivo(ex))
            {
                if (!File.Exists(caminho))
                {
                    throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura);
                }

                // Outro processo criou o segredo antes: a falha e a corrida perdida; fica so o nome do tipo, como registro informativo.
                _logService.WriteInfo(ex.GetType().Name, GetType().ToString());
            }
            finally
            {
                if (!movido)
                {
                    ApagarTemporarioMelhorEsforco(temporario);
                }
            }
        }

        private static void GravarTemporario(string caminho, byte[] conteudo)
        {
            FileStreamOptions opcoes = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None
            };

            if (!OperatingSystem.IsWindows())
            {
                opcoes.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (FileStream fluxo = new FileStream(caminho, opcoes))
            {
                fluxo.Write(conteudo, 0, conteudo.Length);
            }
        }

        private static byte[] LerSegredo(string caminho)
        {
            byte[] conteudo;
            try
            {
                conteudo = File.ReadAllBytes(caminho);
            }
            catch (Exception ex) when (EhFalhaDeArquivo(ex))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura);
            }

            if (conteudo.Length != ConstantsValues.TamanhoSegredoAssinaturaBytes)
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura);
            }

            return conteudo;
        }

        private void ApagarTemporarioMelhorEsforco(string caminho)
        {
            try
            {
                File.Delete(caminho);
            }
            catch (Exception ex) when (EhFalhaDeArquivo(ex))
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroArmazenamentoLimparTemporarios, GetType().ToString());
            }
        }

        private static bool EhFalhaDeArquivo(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException;
        }
    }
}
