using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using System.Globalization;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Provedor DISCO_LOCAL (contrato armazenamento-disco v1): grava o arquivo numa pasta da maquina, assina o link da rota
    /// publica e confere a existencia dos arquivos do lote. Falha responde a mensagem ja aprovada da operacao (IntegrationException);
    /// o motivo especifico vai so para o log, uma vez.
    /// </summary>
    public sealed class DiscoArmazenamentoProvider : IArmazenamentoProvider
    {
        private readonly IRaizArmazenamentoDisco _raiz;
        private readonly ISegredoAssinaturaArmazenamento _segredo;
        private readonly IEnderecoPublicoApi _enderecoPublico;
        private readonly TimeProvider _relogio;
        private readonly ILogService _logService;

        public DiscoArmazenamentoProvider(
            IRaizArmazenamentoDisco raiz,
            ISegredoAssinaturaArmazenamento segredo,
            IEnderecoPublicoApi enderecoPublico,
            TimeProvider relogio,
            ILogService logService)
        {
            _raiz = raiz;
            _segredo = segredo;
            _enderecoPublico = enderecoPublico;
            _relogio = relogio;
            _logService = logService;
        }

        public string Nome => ConstantsArmazenamento.ProvedorDiscoLocal;

        public async Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidarChave(chave);
                ValidarConteudo(conteudo, contentType);
                string pastaDosArquivos = _raiz.ObterPastaDosArquivos();
                string pastaTemporaria = _raiz.ObterPastaTemporaria();
                string destino = ResolverDestino(pastaDosArquivos, chave);
                string enderecoPublico = _enderecoPublico.Obter();
                byte[] segredo = _segredo.ObterOuCriar();
                await GravarAsync(pastaTemporaria, destino, conteudo, cancellationToken);
                return MontarArquivoAssinado(enderecoPublico, segredo, chave, CalcularExpiracao(expiracaoSegundos));
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
                throw RegistrarFalha(ex, configuracao.Codigo, ConstantsMessage.ErroArmazenamentoEnviar);
            }
        }

        public Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ObterUrl(chave, expiracaoSegundos));
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
                throw RegistrarFalha(ex, configuracao.Codigo, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        public async Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidarChave(chave);
                if (!_raiz.TentarObterSemCriar(out string pastaDosArquivos, out _))
                {
                    return;
                }

                string destino = ResolverDestino(pastaDosArquivos, chave);
                await ExecutarComTentativasAsync(() => ApagarArquivo(destino), cancellationToken);
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
                throw RegistrarFalha(ex, configuracao.Codigo, ConstantsMessage.ErroArmazenamentoRemover);
            }
        }

        public Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(ConfiguracaoIntegracao configuracao, string bucket, IReadOnlyCollection<string> chaves, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyDictionary<string, ArquivoArmazenado>>(AssinarLote(chaves, expiracaoSegundos));
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
                throw RegistrarFalha(ex, configuracao.Codigo, ConstantsMessage.ErroArmazenamentoObterUrl);
            }
        }

        /// <summary>Consulta de URL: sem arquivo devolve nulo; sem segredo na maquina devolve nulo com registro, sem criar segredo.</summary>
        private ArquivoArmazenado? ObterUrl(string chave, int expiracaoSegundos)
        {
            ValidarChave(chave);
            if (!_raiz.TentarObterSemCriar(out string pastaDosArquivos, out _))
            {
                return null;
            }

            string destino = ResolverDestino(pastaDosArquivos, chave);
            if (!RaizArmazenamentoDisco.EhArquivoRegular(destino))
            {
                return null;
            }

            byte[]? segredo = _segredo.ObterSeExistir();
            if (segredo == null)
            {
                RegistrarSemSegredo(ConstantsValues.UmArquivoSemLinkArmazenamento);
                return null;
            }

            return MontarArquivoAssinado(_enderecoPublico.Obter(), segredo, chave, CalcularExpiracao(expiracaoSegundos));
        }

        /// <summary>Lote: confere cada chave sem criar nada; omite a invalida e a ausente; uma expiracao para o lote inteiro.</summary>
        private IReadOnlyDictionary<string, ArquivoArmazenado> AssinarLote(IReadOnlyCollection<string> chaves, int expiracaoSegundos)
        {
            Dictionary<string, ArquivoArmazenado> arquivos = new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
            if (chaves.Count == 0)
            {
                return arquivos;
            }

            bool pastaExiste = _raiz.TentarObterSemCriar(out string pastaDosArquivos, out _);
            int ausentes = 0;
            int invalidas = 0;
            List<string> marcadas = new List<string>();
            foreach (string chave in chaves.Distinct(StringComparer.Ordinal))
            {
                if (!RegrasArmazenamentoDisco.ChaveSegura(chave))
                {
                    invalidas++;
                    continue;
                }

                if (!pastaExiste)
                {
                    ausentes++;
                    continue;
                }

                string? destino = RegrasArmazenamentoDisco.ResolverCaminho(pastaDosArquivos, chave);
                if (destino == null)
                {
                    invalidas++;
                    continue;
                }

                if (!RaizArmazenamentoDisco.EhArquivoRegular(destino))
                {
                    ausentes++;
                    continue;
                }

                marcadas.Add(chave);
            }

            if (marcadas.Count > 0)
            {
                byte[]? segredo = _segredo.ObterSeExistir();
                if (segredo == null)
                {
                    RegistrarSemSegredo(marcadas.Count);
                }
                else
                {
                    string enderecoPublico = _enderecoPublico.Obter();
                    long expiraEmUnix = CalcularExpiracao(expiracaoSegundos);
                    foreach (string chave in marcadas)
                    {
                        arquivos[chave] = MontarArquivoAssinado(enderecoPublico, segredo, chave, expiraEmUnix);
                    }
                }
            }

            if (ausentes > 0 || invalidas > 0)
            {
                _logService.Write(
                    string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLoteChavesOmitidas, ausentes, invalidas),
                    ConstantsMessage.ErroArmazenamentoObterUrl,
                    GetType().ToString());
            }

            return arquivos;
        }

        private async Task GravarAsync(string pastaTemporaria, string destino, byte[] conteudo, CancellationToken cancellationToken)
        {
            string temporario = Path.Combine(pastaTemporaria, Guid.NewGuid().ToString(ConstantsValues.FormatoGuidArquivoArmazenamento) + ConstantsValues.ExtensaoTemporarioArmazenamento);
            bool movido = false;
            try
            {
                FileStreamOptions opcoes = new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous,
                    BufferSize = ConstantsValues.TamanhoBufferArquivoArmazenamento
                };

                using (FileStream fluxo = new FileStream(temporario, opcoes))
                {
                    await fluxo.WriteAsync(conteudo, cancellationToken);
                }

                string? pastaDoDestino = Path.GetDirectoryName(destino);
                if (pastaDoDestino != null)
                {
                    Directory.CreateDirectory(pastaDoDestino);
                }

                await ExecutarComTentativasAsync(() => File.Move(temporario, destino, overwrite: true), cancellationToken);
                movido = true;
            }
            finally
            {
                if (!movido)
                {
                    ApagarTemporarioMelhorEsforco(temporario);
                }
            }
        }

        /// <summary>Repete a operacao de arquivo ate ConstantsValues.TentativasOperacaoArquivo vezes; a ultima falha sobe.</summary>
        private async Task ExecutarComTentativasAsync(Action operacao, CancellationToken cancellationToken)
        {
            for (int tentativa = 1; ; tentativa++)
            {
                try
                {
                    operacao();
                    return;
                }
                catch (Exception ex) when (EhFalhaDeArquivo(ex) && tentativa < ConstantsValues.TentativasOperacaoArquivo)
                {
                    // Falha intermediaria nao e erro: a nova tentativa acontece. Fica so o nome do tipo, como registro informativo.
                    // A falha final sobe e e registrada pelo chamador.
                    _logService.WriteInfo(ex.GetType().Name, GetType().ToString());
                }

                await Task.Delay(TimeSpan.FromMilliseconds(ConstantsValues.EsperaNovaTentativaArquivoMs), _relogio, cancellationToken);
            }
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

        /// <summary>Apaga o arquivo. Arquivo, pasta do arquivo (ou pasta nula) inexistente e sucesso (idempotente): nao gasta tentativa e nao registra erro.</summary>
        private void ApagarArquivo(string destino)
        {
            // Pasta nula ou inexistente: nada a apagar (regra 6 do contrato armazenamento-disco); sem tentativa e sem registro.
            string? pastaDoDestino = Path.GetDirectoryName(destino);
            if (pastaDoDestino == null || !Directory.Exists(pastaDoDestino))
            {
                return;
            }

            if (!File.Exists(destino))
            {
                return;
            }

            try
            {
                File.Delete(destino);
            }
            catch (DirectoryNotFoundException ex)
            {
                // A pasta sumiu entre a conferencia e a remocao: o resultado e o mesmo de arquivo ausente.
                _logService.WriteInfo(ex.GetType().Name, GetType().ToString());
            }
        }

        private void RegistrarSemSegredo(int quantidade)
        {
            _logService.Write(
                string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoEmissaoSemSegredo, quantidade),
                ConstantsMessage.ErroArmazenamentoObterUrl,
                GetType().ToString());
        }

        /// <summary>Registra a falha UMA vez: motivo (constante) para recusa interna; so o nome do tipo para o resto. Devolve a excecao da operacao.</summary>
        private IntegrationException RegistrarFalha(Exception ex, string codigo, string mensagemOperacao)
        {
            string motivo = ex is ArmazenamentoDiscoException ? ex.Message : ex.GetType().Name;
            _logService.Write(motivo, mensagemOperacao, GetType().ToString());
            return new IntegrationException(codigo, mensagemOperacao);
        }

        private long CalcularExpiracao(int expiracaoSegundos)
        {
            return RegrasArmazenamentoDisco.CalcularExpiracaoUnix(_relogio.GetUtcNow().ToUnixTimeSeconds(), expiracaoSegundos);
        }

        private static ArquivoArmazenado MontarArquivoAssinado(string enderecoPublico, byte[] segredo, string chave, long expiraEmUnix)
        {
            string assinatura = AssinaturaLinkArmazenamento.Calcular(segredo, chave, expiraEmUnix);
            return new ArquivoArmazenado(
                MontarUrl(enderecoPublico, chave, expiraEmUnix, assinatura),
                DateTimeOffset.FromUnixTimeSeconds(expiraEmUnix));
        }

        private static string MontarUrl(string enderecoPublico, string chave, long expiraEmUnix, string assinatura)
        {
            return enderecoPublico
                + ConstantsValues.CaminhoRaizEnderecoPublico
                + ConstantsValues.RotaLeituraArquivoArmazenado
                + ConstantsValues.CaractereQueryEnderecoPublico
                + ConstantsValues.ParametroChaveLink + ConstantsValues.IgualParametroLink + Uri.EscapeDataString(chave)
                + ConstantsValues.SeparadorParametrosLink
                + ConstantsValues.ParametroExpiracaoLink + ConstantsValues.IgualParametroLink + expiraEmUnix.ToString(CultureInfo.InvariantCulture)
                + ConstantsValues.SeparadorParametrosLink
                + ConstantsValues.ParametroAssinaturaLink + ConstantsValues.IgualParametroLink + assinatura;
        }

        private static void ValidarChave(string chave)
        {
            if (!RegrasArmazenamentoDisco.ChaveSegura(chave))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoChaveInvalida);
            }
        }

        /// <summary>Conteudo nulo ou vazio cai na falha de E/S da operacao (ED-09); tamanho e tipo tem motivo proprio (ED-08).</summary>
        private static void ValidarConteudo(byte[] conteudo, string contentType)
        {
            if (conteudo == null || conteudo.Length == 0)
            {
                throw new ArgumentException();
            }

            if (conteudo.Length > ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes)
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoTamanhoAcimaDoMaximo);
            }

            int tamanhoDoCabecalho = Math.Min(conteudo.Length, ConstantsValues.AssinaturaPng.Length);
            string? tipo = RegrasArmazenamentoDisco.TipoPeloConteudo(conteudo.AsSpan(0, tamanhoDoCabecalho));
            if (tipo == null || !string.Equals(tipo, contentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoTipoNaoSuportado);
            }
        }

        private static string ResolverDestino(string pastaDosArquivos, string chave)
        {
            string? destino = RegrasArmazenamentoDisco.ResolverCaminho(pastaDosArquivos, chave);
            if (destino == null)
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoChaveInvalida);
            }

            return destino;
        }

        private static bool EhFalhaDeArquivo(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException;
        }
    }
}
