namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de documentos do cliente (RF-08). As sete primeiras chegam ao usuario (400 e 429). As demais so vao
    /// para o log (servico e repositorio): nunca levam nome de arquivo, chave, link, CPF nem numero da CNH.
    /// ErroClienteDocumentoTamanho tem {0}, preenchido com ConstantsValues.TamanhoMaximoArquivoDocumentoClienteMb (InvariantCulture).
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Mensagem do 400 quando o campo tipo vem vazio ou ausente.</summary>
        public const string ErroClienteDocumentoTipoObrigatorio = "Informe o tipo do documento.";

        /// <summary>Mensagem do 400 quando o campo tipo nao e CNH nem COMPROVANTE_RESIDENCIA.</summary>
        public const string ErroClienteDocumentoTipoInvalido = "Tipo de documento inválido.";

        /// <summary>Mensagem do 400 quando o formulario nao traz o arquivo.</summary>
        public const string ErroClienteDocumentoArquivoObrigatorio = "Envie o arquivo do documento.";

        /// <summary>Mensagem do 400 quando o arquivo chega com zero bytes.</summary>
        public const string ErroClienteDocumentoArquivoVazio = "O arquivo está vazio.";

        /// <summary>Mensagem do 400 quando o arquivo passa de 10 MB; {0} = ConstantsValues.TamanhoMaximoArquivoDocumentoClienteMb.</summary>
        public const string ErroClienteDocumentoTamanho = "O arquivo deve ter no máximo {0} MB.";

        /// <summary>Mensagem do 400 quando o tipo declarado ou o conteudo nao e PDF, JPG ou PNG, ou quando os dois nao batem.</summary>
        public const string ErroClienteDocumentoFormato = "Formato não aceito. Envie um arquivo PDF, JPG ou PNG.";

        /// <summary>Mensagem do 429 quando o cliente atingiu o teto de envios de documento na janela de 24 horas.</summary>
        public const string ErroLimiteEnviosDocumento = "Você atingiu o limite de envios de documentos. Tente novamente mais tarde.";

        /// <summary>Mensagem do log quando falha o envio de documento do cliente (servico e controller).</summary>
        public const string ErroEnviarClienteDocumento = "Não foi possível enviar o documento do cliente.";

        /// <summary>Mensagem do log quando falha a listagem de documentos do cliente (servico e controller).</summary>
        public const string ErroListarClienteDocumento = "Não foi possível listar os documentos do cliente.";

        /// <summary>Mensagem do log quando falha uma consulta de documentos do cliente (repositorio).</summary>
        public const string ErroConsultarClienteDocumento = "Não foi possível consultar os documentos do cliente.";

        /// <summary>Mensagem do log quando falha a gravacao do documento do cliente (repositorio).</summary>
        public const string ErroGravarClienteDocumento = "Não foi possível gravar o documento do cliente.";

        /// <summary>Mensagem do log quando nao e possivel remover do armazenamento o arquivo antigo substituido; {0} = id do documento (nunca a chave).</summary>
        public const string ErroRemoverArquivoAntigoClienteDocumento = "Não foi possível remover do armazenamento o arquivo antigo do documento {0}.";

        /// <summary>Mensagem do log quando a gravacao falha e nao e possivel remover do armazenamento o arquivo recem-enviado (nunca a chave).</summary>
        public const string ErroRemoverArquivoNovoClienteDocumento = "Não foi possível remover do armazenamento o arquivo recém-enviado do documento do cliente.";

        /// <summary>Mensagem do log quando o armazenamento nao devolveu o link de um ou mais documentos da listagem; {0} = quantidade (InvariantCulture).</summary>
        public const string ErroClienteDocumentoSemUrlAssinada = "O armazenamento não devolveu o link de {0} documento(s) do cliente.";
    }
}
