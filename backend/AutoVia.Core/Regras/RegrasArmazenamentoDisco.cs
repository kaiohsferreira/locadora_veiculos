using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras do armazenamento em pasta (provedor DISCO_LOCAL, contrato armazenamento-disco v1): chave segura, caminho sob a
    /// pasta, prazo do link em janela, endereco publico, loopback, tipo pelo conteudo e pasta raiz. Sem banco, sem disco de
    /// gravacao e sem relogio (o instante entra por parametro). Testadas direto em RegrasArmazenamentoDiscoTests.
    /// </summary>
    public static class RegrasArmazenamentoDisco
    {
        private static readonly HashSet<string> NomesReservados = new HashSet<string>(
            ConstantsValues.NomesReservadosDeArquivo.Split(ConstantsValues.CaractereSeparadorListaNomesReservados),
            StringComparer.Ordinal);

        /// <summary>
        /// Verdadeiro quando a chave pode abrir ou gravar arquivo: 1 a 200 caracteres da lista branca, sem barra nas pontas, sem
        /// segmento vazio, sem segmento que comece ou termine com ponto, sem dois pontos seguidos e sem nome reservado do Windows.
        /// Conferida caractere a caractere, sem expressao regular.
        /// </summary>
        public static bool ChaveSegura(string? chave)
        {
            if (string.IsNullOrEmpty(chave))
            {
                return false;
            }

            if (chave.Length > ConstantsValues.TamanhoMaximoChaveArmazenamento)
            {
                return false;
            }

            foreach (char caractere in chave)
            {
                if (ConstantsValues.CaracteresPermitidosChaveArmazenamento.IndexOf(caractere) < 0)
                {
                    return false;
                }
            }

            if (chave[0] == ConstantsValues.CaractereSeparadorChaveArmazenamento
                || chave[chave.Length - 1] == ConstantsValues.CaractereSeparadorChaveArmazenamento)
            {
                return false;
            }

            string[] segmentos = chave.Split(ConstantsValues.CaractereSeparadorChaveArmazenamento);
            foreach (string segmento in segmentos)
            {
                if (segmento.Length == 0)
                {
                    return false;
                }

                if (segmento[0] == ConstantsValues.CaracterePontoFinal
                    || segmento[segmento.Length - 1] == ConstantsValues.CaracterePontoFinal)
                {
                    return false;
                }

                if (segmento.Contains(ConstantsValues.SequenciaPontoDuplo, StringComparison.Ordinal))
                {
                    return false;
                }

                int indicePonto = segmento.IndexOf(ConstantsValues.CaracterePontoFinal);
                string nomeBase = indicePonto >= 0 ? segmento.Substring(0, indicePonto) : segmento;
                if (NomesReservados.Contains(nomeBase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Caminho completo do arquivo dentro da pasta, ou null quando ele nao fica sob a pasta (inclusive caminho invalido).
        /// Nao confere a lista branca: e a segunda barreira, independente da primeira.
        /// </summary>
        public static string? ResolverCaminho(string pastaDosArquivos, string chave)
        {
            string? pastaCompleta = NormalizarCaminho(pastaDosArquivos);
            string? caminhoCompleto = NormalizarCaminho(Path.Combine(pastaDosArquivos, chave));
            if (pastaCompleta == null || caminhoCompleto == null)
            {
                return null;
            }

            return EstaDentro(caminhoCompleto, pastaCompleta) ? caminhoCompleto : null;
        }

        /// <summary>
        /// Instante de validade do link, em segundos Unix. A janela tem metade do prazo (no minimo 1 s): o link recebido vale entre
        /// prazo menos a janela e o prazo inteiro, nunca mais que o prazo. Dois instantes da mesma janela recebem o mesmo instante.
        /// </summary>
        public static long CalcularExpiracaoUnix(long agoraUnix, int prazoSegundos)
        {
            long janela = Math.Max(ConstantsValues.JanelaMinimaLinkArmazenamentoSegundos, prazoSegundos / ConstantsValues.DivisorJanelaLinkArmazenamento);
            long inicioDaJanela = (agoraUnix / janela) * janela;
            return inicioDaJanela + prazoSegundos;
        }

        /// <summary>
        /// Verdadeiro quando o endereco publico pode entrar nos links: URL absoluta, so com origem (sem caminho, query, fragmento
        /// nem usuario), https, ou http quando o host e loopback.
        /// </summary>
        public static bool EnderecoPublicoAceito(string? endereco)
        {
            if (string.IsNullOrWhiteSpace(endereco))
            {
                return false;
            }

            if (!Uri.TryCreate(endereco, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            if (uri.AbsolutePath != ConstantsValues.CaminhoRaizEnderecoPublico || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            if (endereco.Contains(ConstantsValues.CaractereQueryEnderecoPublico) || endereco.Contains(ConstantsValues.CaractereFragmentoEnderecoPublico))
            {
                return false;
            }

            if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                return HostEhLoopback(uri.Host);
            }

            return false;
        }

        /// <summary>
        /// Verdadeiro para localhost, localhost. (com ponto final), *.localhost e enderecos de loopback (127.0.0.0/8 e ::1), com ou
        /// sem colchetes. Recebe o host SEM porta. Mesma regra de AddRecuperacaoSenhaStartup.EhLoopback (copia, nao unificada).
        /// </summary>
        public static bool HostEhLoopback(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            string nomeSemPontoFinal = host.TrimEnd(ConstantsValues.CaracterePontoFinal);
            if (string.Equals(nomeSemPontoFinal, ConstantsValues.HostLocalhost, StringComparison.OrdinalIgnoreCase)
                || nomeSemPontoFinal.EndsWith(ConstantsValues.SufixoSubdominioLocalhost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string semColchetes = host.Trim(ConstantsValues.CaractereColcheteAbre, ConstantsValues.CaractereColcheteFecha);
            bool enderecoValido = IPAddress.TryParse(semColchetes, out IPAddress? endereco);

            return enderecoValido && endereco != null && IPAddress.IsLoopback(endereco);
        }

        /// <summary>
        /// Tipo do conteudo pelos primeiros bytes, conforme a tabela tipos_aceitos (JPEG e PNG), ou null quando nao e aceito.
        /// E a unica decisao de tipo do provedor de disco.
        /// </summary>
        public static string? TipoPeloConteudo(ReadOnlySpan<byte> primeirosBytes)
        {
            if (ComecaCom(primeirosBytes, ConstantsValues.AssinaturaJpeg))
            {
                return ConstantsValues.TipoConteudoJpeg;
            }

            if (ComecaCom(primeirosBytes, ConstantsValues.AssinaturaPng))
            {
                return ConstantsValues.TipoConteudoPng;
            }

            return null;
        }

        /// <summary>
        /// Verdadeiro quando a pasta raiz pode ser usada: nao e raiz de disco, nao e igual a, dentro de nem contendo a pasta de
        /// publicacao da API, e nem ela nem uma pasta acima tem entrada .git (informada por existeEntradaGit). O chamador ja
        /// exige caminho absoluto.
        /// </summary>
        public static bool PastaRaizAceita(string raiz, string pastaDePublicacao, Func<string, bool> existeEntradaGit)
        {
            string? raizCompleta = NormalizarCaminho(raiz);
            string? publicacaoCompleta = NormalizarCaminho(pastaDePublicacao);
            if (raizCompleta == null || publicacaoCompleta == null)
            {
                return false;
            }

            if (EhRaizDeDisco(raizCompleta))
            {
                return false;
            }

            if (string.Equals(raizCompleta, publicacaoCompleta, ComparacaoCaminho())
                || EstaDentro(raizCompleta, publicacaoCompleta)
                || EstaDentro(publicacaoCompleta, raizCompleta))
            {
                return false;
            }

            return !TemEntradaGitNaRaizOuAcima(raizCompleta, existeEntradaGit);
        }

        private static bool ComecaCom(ReadOnlySpan<byte> bytes, byte[] assinatura)
        {
            if (bytes.Length < assinatura.Length)
            {
                return false;
            }

            for (int indice = 0; indice < assinatura.Length; indice++)
            {
                if (bytes[indice] != assinatura[indice])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TemEntradaGitNaRaizOuAcima(string raizCompleta, Func<string, bool> existeEntradaGit)
        {
            string? atual = raizCompleta;
            while (atual != null)
            {
                if (existeEntradaGit(Path.Combine(atual, ConstantsValues.NomeEntradaGit)))
                {
                    return true;
                }

                atual = Path.GetDirectoryName(atual);
            }

            return false;
        }

        private static bool EhRaizDeDisco(string caminhoCompleto)
        {
            string? raizDoCaminho = Path.GetPathRoot(caminhoCompleto);
            return raizDoCaminho != null
                && string.Equals(Path.TrimEndingDirectorySeparator(raizDoCaminho), Path.TrimEndingDirectorySeparator(caminhoCompleto), ComparacaoCaminho());
        }

        private static bool EstaDentro(string caminho, string pasta)
        {
            string prefixo = pasta + Path.DirectorySeparatorChar;
            return caminho.StartsWith(prefixo, ComparacaoCaminho());
        }

        private static StringComparison ComparacaoCaminho()
        {
            return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        /// <summary>
        /// Caminho absoluto sem separador final, ou null quando nao e um caminho valido. Excecao de caminho invalido vira null
        /// (sem log: funcao pura, sem ILogService; quem chama registra o motivo e responde a recusa).
        /// </summary>
        private static string? NormalizarCaminho(string caminho)
        {
            try
            {
                return Path.TrimEndingDirectorySeparator(Path.GetFullPath(caminho));
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
