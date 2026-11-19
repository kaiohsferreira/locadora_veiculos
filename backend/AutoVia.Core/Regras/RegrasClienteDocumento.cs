using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras dos documentos do cliente (sem banco, sem servico, sem relogio): tipo pelo nome exato, chave do objeto,
    /// nome original limpo, documento ativo de maior id por tipo e texto do instante de expiracao do link. Testadas direto em
    /// RegrasClienteDocumentoTests.
    /// </summary>
    public static class RegrasClienteDocumento
    {
        /// <summary>Verdadeiro quando o texto e o nome exato de um tipo de documento, depois de Trim (sem diferenciar nada alem disso).</summary>
        public static bool TipoPermitido(string? nome)
        {
            TipoDocumentoCliente tipo = default;

            return TentarLerTipoPorNome(nome, out tipo);
        }

        /// <summary>
        /// Le o tipo pelo nome exato do enum, depois de Trim. Numero, minuscula e nome parcial sao recusados. Devolve falso e
        /// deixa o tipo no valor padrao quando nao reconhece.
        /// </summary>
        public static bool TentarLerTipoPorNome(string? nome, out TipoDocumentoCliente tipo)
        {
            tipo = default;
            string? aparado = nome?.Trim();

            if (string.IsNullOrEmpty(aparado))
            {
                return false;
            }

            string[] nomes = Enum.GetNames<TipoDocumentoCliente>();
            foreach (string nomeMembro in nomes)
            {
                if (string.Equals(nomeMembro, aparado, StringComparison.Ordinal))
                {
                    tipo = Enum.Parse<TipoDocumentoCliente>(nomeMembro);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chave do objeto no armazenamento: clientes/{idCliente}/documentos/{guid}.{extensao}. A extensao vem do tipo do conteudo
        /// (jpg, png ou pdf). Sem CPF, sem nome do cliente e sem nome original. O GUID chega por parametro para a funcao continuar pura.
        /// </summary>
        public static string MontarChaveObjeto(int idCliente, Guid identificador, string tipoConteudo)
        {
            string extensao = ExtensaoPorTipoConteudo(tipoConteudo);

            return string.Format(
                CultureInfo.InvariantCulture,
                ConstantsValues.FormatoChaveDocumentoCliente,
                idCliente,
                identificador.ToString(ConstantsValues.FormatoGuidChaveDocumentoCliente, CultureInfo.InvariantCulture),
                extensao);
        }

        /// <summary>
        /// Nome original limpo para a tela: fica so o nome (sem pasta, nas barras normais e invertidas), sem caracteres de controle
        /// nem de formato (por exemplo, o de inverter o texto), com Trim e no maximo 120 caracteres sem cortar um par substituto
        /// (emoji). Sem nada sobrando, usa o nome fixo de ConstantsValues.
        /// </summary>
        public static string LimparNomeArquivo(string? nomeOriginal)
        {
            if (string.IsNullOrEmpty(nomeOriginal))
            {
                return ConstantsValues.NomeArquivoDocumentoClientePadrao;
            }

            char[] separadores = new char[] { ConstantsValues.CaractereBarra, ConstantsValues.CaractereBarraInvertidaNomeArquivo };
            int indiceUltimoSeparador = nomeOriginal.LastIndexOfAny(separadores);
            string somenteNome = indiceUltimoSeparador >= 0 ? nomeOriginal.Substring(indiceUltimoSeparador + 1) : nomeOriginal;

            StringBuilder semControle = new StringBuilder(somenteNome.Length);
            foreach (char caractere in somenteNome)
            {
                if (!char.IsControl(caractere) && CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.Format)
                {
                    semControle.Append(caractere);
                }
            }

            string aparado = semControle.ToString().Trim();

            if (aparado.Length > ConstantsValues.TamanhoMaximoNomeArquivoDocumentoCliente)
            {
                int tamanhoCorte = ConstantsValues.TamanhoMaximoNomeArquivoDocumentoCliente;
                if (char.IsHighSurrogate(aparado[tamanhoCorte - 1]))
                {
                    tamanhoCorte--;
                }

                aparado = aparado.Substring(0, tamanhoCorte).Trim();
            }

            if (aparado.Length == 0)
            {
                return ConstantsValues.NomeArquivoDocumentoClientePadrao;
            }

            return aparado;
        }

        /// <summary>
        /// Entre os documentos ativos (DisabledAt nulo) do tipo informado, o de maior id. Nulo quando nao ha nenhum. Os
        /// desativados e os de outro tipo sao ignorados.
        /// </summary>
        public static ClienteDocumento? EscolherAtivoPorTipo(IEnumerable<ClienteDocumento> documentos, TipoDocumentoCliente tipo)
        {
            ClienteDocumento? escolhido = null;

            foreach (ClienteDocumento documento in documentos)
            {
                if (documento.DisabledAt != null || documento.Tipo != tipo)
                {
                    continue;
                }

                if (escolhido == null || documento.IdClienteDocumento > escolhido.IdClienteDocumento)
                {
                    escolhido = documento;
                }
            }

            return escolhido;
        }

        /// <summary>Texto do instante de expiracao do link do documento, em UTC, no mesmo formato de ida e volta das fotos do veiculo.</summary>
        public static string FormatarExpiraEm(DateTimeOffset expiraEm)
        {
            return RegrasVeiculoFoto.FormatarExpiraEm(expiraEm);
        }

        /// <summary>Extensao da chave para o tipo do conteudo. Tipo fora da tabela (PDF, JPEG, PNG) e erro de programacao.</summary>
        private static string ExtensaoPorTipoConteudo(string tipoConteudo)
        {
            if (string.Equals(tipoConteudo, ConstantsValues.TipoConteudoJpeg, StringComparison.Ordinal))
            {
                return ConstantsValues.ExtensaoJpegDocumentoCliente;
            }

            if (string.Equals(tipoConteudo, ConstantsValues.TipoConteudoPng, StringComparison.Ordinal))
            {
                return ConstantsValues.ExtensaoPngDocumentoCliente;
            }

            if (string.Equals(tipoConteudo, ConstantsValues.TipoConteudoPdf, StringComparison.Ordinal))
            {
                return ConstantsValues.ExtensaoPdfDocumentoCliente;
            }

            throw new ArgumentOutOfRangeException(nameof(tipoConteudo));
        }
    }
}
