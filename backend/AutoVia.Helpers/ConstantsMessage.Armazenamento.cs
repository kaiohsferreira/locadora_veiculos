namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da integracao ARMAZENAMENTO (contrato integracao-armazenamento v1). Arquivo parcial de ConstantsMessage.
    /// Nenhum valor de chave, URL assinada ou mensagem do SDK entra em mensagem: {0} de ErroArmazenamentoChaveNaoProtegida
    /// e o NOME da chave (access_key ou secret_key), nunca o valor.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Configuracao e selecao do provedor (IntegrationException, 502) ---
        public const string ErroArmazenamentoProvedorNaoSuportado = "O provedor de armazenamento configurado não é suportado.";
        public const string ErroArmazenamentoExpiracaoInvalida = "A configuração url_expiracao_segundos da integração de armazenamento é inválida.";
        public const string ErroArmazenamentoBaseUrlSemHttps = "A base_url da integração de armazenamento deve usar HTTPS.";
        public const string ErroArmazenamentoChaveNaoProtegida = "A chave {0} da integração de armazenamento deve estar marcada como protegida.";
        public const string ErroArmazenamentoUrlAssinadaInvalida = "O armazenamento gerou uma URL temporária inválida.";

        // --- Falha da operacao (IntegrationException, 502) ---
        public const string ErroArmazenamentoEnviar = "Não foi possível enviar o arquivo para o armazenamento.";
        public const string ErroArmazenamentoObterUrl = "Não foi possível gerar a URL temporária do arquivo.";
        public const string ErroArmazenamentoRemover = "Não foi possível remover o arquivo do armazenamento.";

        // --- Rota publica de leitura de arquivo (textos de resposta aprovados pelo Kaio em 18/11/2026) ---
        // 404 de todas as recusas da rota.
        public const string ErroArquivoArmazenadoNaoEncontrado = "Arquivo não encontrado.";
        // 400 de link malformado.
        public const string ErroArmazenamentoLinkInvalido = "O link do arquivo é inválido.";

        // --- So log: falha nao prevista e limpeza (segue o precedente de 17/11/2026, aceito pelo Kaio em 18/11/2026) ---
        public const string ErroArmazenamentoAbrirArquivo = "Não foi possível abrir o arquivo do armazenamento.";
        public const string ErroArmazenamentoLimparTemporarios = "Não foi possível limpar os arquivos temporários do armazenamento.";

        // --- So log: motivo de cada recusa da rota de leitura ---
        public const string LogArmazenamentoLeituraLinkMalformado = "Leitura de arquivo recusada: link malformado.";
        public const string LogArmazenamentoLeituraChaveInsegura = "Leitura de arquivo recusada: chave insegura.";
        public const string LogArmazenamentoLeituraAssinaturaInvalida = "Leitura de arquivo recusada: assinatura inválida.";
        public const string LogArmazenamentoLeituraPrazoVencido = "Leitura de arquivo recusada: prazo do link vencido.";
        public const string LogArmazenamentoLeituraPrazoAcimaDoTeto = "Leitura de arquivo recusada: prazo do link acima do teto.";
        public const string LogArmazenamentoLeituraArquivoAusente = "Leitura de arquivo recusada: arquivo ausente.";
        public const string LogArmazenamentoLeituraTipoNaoSuportado = "Leitura de arquivo recusada: conteúdo que não é JPEG nem PNG.";
        public const string LogArmazenamentoLeituraTamanhoAcimaDoMaximo = "Leitura de arquivo recusada: arquivo acima do tamanho máximo.";
        public const string LogArmazenamentoLeituraFalhaInterna = "Leitura de arquivo recusada: falha ao acessar a pasta ou o segredo.";

        // --- So log: registros agregados do 429 e cancelamento (contagem em {0}) ---
        public const string LogArmazenamentoLeituraLimitePorIp = "Leitura de arquivo recusada: limite de requisições por IP. Recusas desde o último registro: {0}.";
        public const string LogArmazenamentoLeituraTetoDaRota = "Leitura de arquivo recusada: teto de requisições da rota. Recusas desde o último registro: {0}.";
        public const string LogArmazenamentoLeituraCancelada = "Leitura de arquivo cancelada pelo cliente.";

        // --- So log: provedor (lote, emissao sem segredo e motivos de configuracao) ---
        public const string LogArmazenamentoLoteChavesOmitidas = "Lote de links de arquivo: {0} chave(s) omitida(s) por arquivo ausente e {1} por chave inválida.";
        public const string LogArmazenamentoEmissaoSemSegredo = "Emissão de link de arquivo: {0} arquivo(s) sem link porque esta máquina não tem o segredo de assinatura.";
        public const string LogArmazenamentoDiscoDiretorioObrigatorio = "Armazenamento em pasta: a pasta (Armazenamento:Diretorio) é obrigatória quando a API roda em container ou fora do Windows e deve apontar para um disco permanente.";
        public const string LogArmazenamentoDiscoDiretorioEmLocalProibido = "Armazenamento em pasta: a pasta não pode ficar dentro da pasta de publicação da API nem dentro de um repositório Git.";
        public const string LogArmazenamentoDiscoDiretorioInvalido = "Armazenamento em pasta: a pasta é inválida ou não permite gravação.";
        public const string LogArmazenamentoDiscoUrlPublicaInvalida = "Armazenamento em pasta: o endereço público da API para os links de arquivo (Armazenamento:UrlPublicaApi) está ausente ou é inválido.";
        public const string LogArmazenamentoDiscoSegredoAssinatura = "Armazenamento em pasta: não foi possível ler ou criar o segredo de assinatura dos links de arquivo.";
        public const string LogArmazenamentoDiscoChaveInvalida = "Armazenamento em pasta: a chave do arquivo é inválida.";
        public const string LogArmazenamentoDiscoTipoNaoSuportado = "Armazenamento em pasta: o arquivo não é imagem JPEG nem PNG.";
        public const string LogArmazenamentoDiscoTamanhoAcimaDoMaximo = "Armazenamento em pasta: arquivo acima do tamanho máximo.";
    }
}
