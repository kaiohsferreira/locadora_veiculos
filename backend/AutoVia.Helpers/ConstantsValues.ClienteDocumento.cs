namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do modulo de documentos do cliente (RF-08: CNH e comprovante de residencia): tamanhos, formato da chave do
    /// objeto, nome limpo, rota, limites por IP (envio e listagem), teto de envios por cliente em 24 horas e nomes das
    /// politicas de limite. Os 10.485.760 bytes vem de TamanhoMaximoArquivoArmazenamentoBytes (ConstantsValues.Armazenamento).
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Tamanho maximo do arquivo do documento, em megabytes (so para a mensagem de erro; o limite em bytes e TamanhoMaximoArquivoArmazenamentoBytes).</summary>
        public const int TamanhoMaximoArquivoDocumentoClienteMb = 10;

        /// <summary>Teto do corpo do POST de documento, em bytes: o arquivo mais a folga multipart (a mesma folga do envio de foto do veiculo).</summary>
        public const int TamanhoMaximoCorpoDocumentoCliente = TamanhoMaximoArquivoArmazenamentoBytes + FolgaCorpoMultipartFotoVeiculo;

        /// <summary>Tamanho maximo, em caracteres, do nome original guardado (so para a tela mostrar).</summary>
        public const int TamanhoMaximoNomeArquivoDocumentoCliente = 120;

        /// <summary>Nome usado quando o nome original nao sobra nada depois da limpeza.</summary>
        public const string NomeArquivoDocumentoClientePadrao = "documento";

        /// <summary>
        /// Chave do objeto: clientes/{idCliente}/documentos/{guid}.{extensao}. {0} = id do cliente, {1} = GUID sem hifen, {2} = extensao.
        /// Formatada com CultureInfo.InvariantCulture. Sem CPF, sem nome do cliente e sem nome original.
        /// </summary>
        public const string FormatoChaveDocumentoCliente = PrefixoChaveDocumentoCliente + "{0}/documentos/{1}.{2}";

        /// <summary>Formato do GUID dentro da chave do documento: 32 digitos hexadecimais, sem hifen.</summary>
        public const string FormatoGuidChaveDocumentoCliente = "N";

        /// <summary>Extensao da chave para image/jpeg.</summary>
        public const string ExtensaoJpegDocumentoCliente = "jpg";

        /// <summary>Extensao da chave para image/png.</summary>
        public const string ExtensaoPngDocumentoCliente = "png";

        /// <summary>Extensao da chave para application/pdf.</summary>
        public const string ExtensaoPdfDocumentoCliente = "pdf";

        /// <summary>Rota dos documentos do cliente (envio e listagem), relativa ao endereco da API.</summary>
        public const string RotaClienteDocumento = "api/clientes/{idCliente:int}/documentos";

        /// <summary>Chave de configuracao do limite por minuto e por IP do envio de documento.</summary>
        public const string ChaveLimiteEnvioDocumentoCliente = "Clientes:LimiteEnvioDocumentoPorMinuto";

        /// <summary>Limite padrao, por minuto e por IP, do envio de documento.</summary>
        public const int LimiteEnvioDocumentoClientePorMinutoPadrao = 20;

        /// <summary>Chave de configuracao do limite por minuto e por IP da listagem de documentos.</summary>
        public const string ChaveLimiteListaDocumentoCliente = "Clientes:LimiteListaDocumentoPorMinuto";

        /// <summary>Limite padrao, por minuto e por IP, da listagem de documentos.</summary>
        public const int LimiteListaDocumentoClientePorMinutoPadrao = 60;

        /// <summary>Nome da politica de limite por IP do envio de documento (janela de 1 minuto).</summary>
        public const string PoliticaLimiteEnvioDocumentoCliente = "AutoViaEnvioDocumentoCliente";

        /// <summary>Nome da politica de limite por IP da listagem de documentos (janela de 1 minuto).</summary>
        public const string PoliticaLimiteListaDocumentoCliente = "AutoViaListaDocumentoCliente";

        /// <summary>Padrao do teto de envios de documento por cliente na janela (ClientesOptions.LimiteEnviosDocumentoPorDia).</summary>
        public const int LimiteEnviosDocumentoPorDiaPadrao = 20;

        /// <summary>Tamanho, em horas, da janela do teto de envios de documento por cliente.</summary>
        public const int JanelaEnviosDocumentoClienteHoras = 24;

        /// <summary>Nome do campo tipo do formulario (usado como chave nas falhas de validacao).</summary>
        public const string CampoTipoDocumentoCliente = "tipo";

        /// <summary>Nome do campo arquivo do formulario (usado como chave nas falhas de validacao).</summary>
        public const string CampoArquivoDocumentoCliente = "arquivo";

        /// <summary>Barra invertida: separador de pasta do Windows, tirado do nome original antes de guardar.</summary>
        public const char CaractereBarraInvertidaNomeArquivo = '\\';
    }
}
