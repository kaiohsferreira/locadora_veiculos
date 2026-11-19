namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de retorno de um documento do cliente (201 do envio e itens da listagem). Nao traz chave do objeto, id do cliente
    /// nem caminho. A URL e temporaria e vem de uma assinatura do armazenamento; quando o arquivo nao tem link (ausente no
    /// armazenamento), Url e ExpiraEm vem nulos e a lista continua.
    /// </summary>
    public class ReturnClienteDocumentoVO
    {
        /// <summary>Id do documento.</summary>
        public int IdClienteDocumento { get; set; }

        /// <summary>Tipo do documento: CNH ou COMPROVANTE_RESIDENCIA.</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Nome original limpo (so para a tela mostrar).</summary>
        public string NomeArquivo { get; set; } = string.Empty;

        /// <summary>Tipo do conteudo decidido pelo arquivo: image/jpeg, image/png ou application/pdf.</summary>
        public string TipoConteudo { get; set; } = string.Empty;

        /// <summary>Tamanho do arquivo em bytes.</summary>
        public int TamanhoBytes { get; set; }

        /// <summary>Data e hora do envio, no formato de data e hora do projeto (StaticMethods.FormateDateTimeToPTBR).</summary>
        public string DataEnvio { get; set; } = string.Empty;

        /// <summary>URL https temporaria do arquivo; nula quando o arquivo nao tem link assinado.</summary>
        public string? Url { get; set; }

        /// <summary>Instante em que a URL expira, em UTC, no formato de ida e volta do .NET; nulo quando nao ha URL.</summary>
        public string? ExpiraEm { get; set; }
    }
}
