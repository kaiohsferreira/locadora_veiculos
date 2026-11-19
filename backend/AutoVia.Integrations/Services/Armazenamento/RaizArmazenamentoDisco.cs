using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Pasta raiz do armazenamento em disco (contrato armazenamento-disco v1, regra 2). Resolve e confere a pasta na primeira
    /// chamada, guarda o resultado aprovado em memoria, cria as subpastas so nos metodos Obter* e limpa temporarios orfaos uma
    /// vez por processo. Falha de configuracao vira ArmazenamentoDiscoException com o motivo (constante LogArmazenamentoDisco*).
    /// </summary>
    public sealed class RaizArmazenamentoDisco : IRaizArmazenamentoDisco
    {
        private readonly IOptions<ArmazenamentoDiscoOptions> _opcoes;
        private readonly ILogService _logService;
        private readonly bool _emContainer;
        private readonly string _pastaDeDadosDoUsuario;
        private readonly string _pastaDePublicacao;
        private readonly bool _windows;
        private readonly object _trava = new object();
        private string? _raizAprovada;
        private bool _temporariosVerificados;

        public RaizArmazenamentoDisco(IOptions<ArmazenamentoDiscoOptions> opcoes, IConfiguration configuracao, ILogService logService)
            : this(
                opcoes,
                LerIndicadorContainer(configuracao),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppContext.BaseDirectory,
                logService,
                OperatingSystem.IsWindows())
        {
        }

        internal RaizArmazenamentoDisco(
            IOptions<ArmazenamentoDiscoOptions> opcoes,
            bool emContainer,
            string pastaDeDadosDoUsuario,
            string pastaDePublicacao,
            ILogService logService,
            bool windows)
        {
            _opcoes = opcoes;
            _emContainer = emContainer;
            _pastaDeDadosDoUsuario = pastaDeDadosDoUsuario;
            _pastaDePublicacao = pastaDePublicacao;
            _logService = logService;
            _windows = windows;
        }

        public string ObterPastaDosArquivos()
        {
            return Path.Combine(PrepararRaiz(), ConstantsValues.SubpastaArquivosArmazenamento);
        }

        public string ObterPastaTemporaria()
        {
            return Path.Combine(PrepararRaiz(), ConstantsValues.SubpastaTemporariosArmazenamento);
        }

        public string ObterPastaDoSegredo()
        {
            return Path.Combine(PrepararRaiz(), ConstantsValues.SubpastaSegredoArmazenamento);
        }

        public bool TentarObterSemCriar(out string pastaDosArquivos, out string pastaDoSegredo)
        {
            string raiz = ResolverRaiz();
            pastaDosArquivos = Path.Combine(raiz, ConstantsValues.SubpastaArquivosArmazenamento);
            pastaDoSegredo = Path.Combine(raiz, ConstantsValues.SubpastaSegredoArmazenamento);
            return Directory.Exists(pastaDosArquivos);
        }

        /// <summary>Verdadeiro quando o caminho e um arquivo comum (nao pasta, nao link). Nao cria nada.</summary>
        internal static bool EhArquivoRegular(string caminho)
        {
            if (!File.Exists(caminho))
            {
                return false;
            }

            return (File.GetAttributes(caminho) & FileAttributes.ReparsePoint) == 0;
        }

        private string PrepararRaiz()
        {
            string raiz = ResolverRaiz();
            CriarPastas(raiz);
            LimparTemporariosUmaVez(raiz);
            return raiz;
        }

        private string ResolverRaiz()
        {
            lock (_trava)
            {
                if (_raizAprovada != null)
                {
                    return _raizAprovada;
                }
            }

            string raiz = CalcularRaiz();
            lock (_trava)
            {
                _raizAprovada = raiz;
            }

            return raiz;
        }

        private string CalcularRaiz()
        {
            string configurado = _opcoes.Value.Diretorio.Trim();
            if (configurado.Length > 0)
            {
                if (!Path.IsPathFullyQualified(configurado))
                {
                    throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido);
                }

                return ValidarRaiz(NormalizarOuFalhar(configurado));
            }

            if (_emContainer || !_windows)
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioObrigatorio);
            }

            if (_pastaDeDadosDoUsuario.Length == 0)
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido);
            }

            string padrao = Path.Combine(_pastaDeDadosDoUsuario, ConstantsValues.PastaAutoViaDadosLocais, ConstantsValues.PastaArmazenamentoPadrao);
            return ValidarRaiz(NormalizarOuFalhar(padrao));
        }

        private string ValidarRaiz(string raiz)
        {
            string? raizDoDisco = Path.GetPathRoot(raiz);
            if (raizDoDisco != null && string.Equals(Path.TrimEndingDirectorySeparator(raizDoDisco), raiz, ComparacaoCaminho()))
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido);
            }

            if (!RegrasArmazenamentoDisco.PastaRaizAceita(raiz, _pastaDePublicacao, ExisteEntradaGit))
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioEmLocalProibido);
            }

            return raiz;
        }

        private void LimparTemporariosUmaVez(string raiz)
        {
            bool executar;
            lock (_trava)
            {
                executar = !_temporariosVerificados;
                _temporariosVerificados = true;
            }

            if (!executar)
            {
                return;
            }

            string pastaTemporaria = Path.Combine(raiz, ConstantsValues.SubpastaTemporariosArmazenamento);
            try
            {
                DateTime limite = DateTime.Now - TimeSpan.FromMinutes(ConstantsValues.IdadeMinimaTemporarioOrfaoMinutos);
                foreach (string arquivo in Directory.EnumerateFiles(pastaTemporaria))
                {
                    if (File.GetLastWriteTime(arquivo) < limite)
                    {
                        File.Delete(arquivo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroArmazenamentoLimparTemporarios, GetType().ToString());
            }
        }

        private static void CriarPastas(string raiz)
        {
            string pastaTemporaria = Path.Combine(raiz, ConstantsValues.SubpastaTemporariosArmazenamento);
            try
            {
                CriarPasta(raiz);
                CriarPasta(Path.Combine(raiz, ConstantsValues.SubpastaArquivosArmazenamento));
                CriarPasta(pastaTemporaria);
                CriarPasta(Path.Combine(raiz, ConstantsValues.SubpastaSegredoArmazenamento));
                ConferirGravacao(pastaTemporaria);
            }
            catch (Exception ex) when (EhFalhaDeAcessoAoDisco(ex))
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido);
            }
        }

        private static void CriarPasta(string caminho)
        {
            if (OperatingSystem.IsWindows())
            {
                Directory.CreateDirectory(caminho);
                return;
            }

            Directory.CreateDirectory(caminho, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private static void ConferirGravacao(string pastaTemporaria)
        {
            string caminhoSonda = Path.Combine(pastaTemporaria, Guid.NewGuid().ToString(ConstantsValues.FormatoGuidArquivoArmazenamento) + ConstantsValues.ExtensaoTemporarioArmazenamento);
            using (FileStream sonda = File.Create(caminhoSonda))
            {
            }

            File.Delete(caminhoSonda);
        }

        private static string NormalizarOuFalhar(string caminho)
        {
            try
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(caminho));
            }
            catch (Exception ex) when (EhFalhaDeAcessoAoDisco(ex))
            {
                throw Falha(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido);
            }
        }

        private static bool ExisteEntradaGit(string caminho)
        {
            return File.Exists(caminho) || Directory.Exists(caminho);
        }

        private static bool LerIndicadorContainer(IConfiguration configuracao)
        {
            string? valor = configuracao[ConstantsValues.ChaveExecucaoEmContainer];
            return bool.TryParse(valor, out bool emContainer) && emContainer;
        }

        private static bool EhFalhaDeAcessoAoDisco(Exception ex)
        {
            return ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException;
        }

        private static StringComparison ComparacaoCaminho()
        {
            return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        private static ArmazenamentoDiscoException Falha(string motivo)
        {
            return new ArmazenamentoDiscoException(motivo);
        }
    }
}
