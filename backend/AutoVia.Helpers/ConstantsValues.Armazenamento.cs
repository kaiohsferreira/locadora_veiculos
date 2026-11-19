namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do armazenamento em pasta (provedor DISCO_LOCAL, contrato armazenamento-disco v1): chaves de configuracao da
    /// maquina, limites da rota publica de leitura, chave segura, assinatura e prazo do link, tipos aceitos e tamanhos.
    /// AutoVia.Helpers nao enxerga AutoVia.Integrations; por isso alguns numeros repetem ConstantsArmazenamento e um teste prende a igualdade.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Configuracao da maquina (appsettings ou variavel de ambiente; nunca no banco) ---

        /// <summary>Secao do appsettings com a configuracao do armazenamento em pasta.</summary>
        public const string SecaoArmazenamento = "Armazenamento";

        /// <summary>Chave da pasta raiz do armazenamento (vazia = pasta de dados do usuario no Windows fora de container).</summary>
        public const string ChaveArmazenamentoDiretorio = "Armazenamento:Diretorio";

        /// <summary>Chave do endereco publico da API usado nos links de arquivo.</summary>
        public const string ChaveArmazenamentoUrlPublicaApi = "Armazenamento:UrlPublicaApi";

        /// <summary>Chave do limite de leituras por IP por minuto na rota publica de arquivo.</summary>
        public const string ChaveLimiteLeituraArquivo = "Armazenamento:LimiteLeituraPorMinuto";

        /// <summary>Padrao do limite por IP por minuto da rota publica de arquivo.</summary>
        public const int LimiteLeituraArquivoPorMinutoPadrao = 600;

        /// <summary>Chave do teto de leituras por minuto da rota publica de arquivo (contador unico da rota).</summary>
        public const string ChaveLimiteLeituraArquivoGlobal = "Armazenamento:LimiteLeituraGlobalPorMinuto";

        /// <summary>Padrao do teto por minuto da rota publica de arquivo.</summary>
        public const int LimiteLeituraArquivoGlobalPorMinutoPadrao = 3000;

        /// <summary>Nome da politica de limite por IP da rota publica de arquivo.</summary>
        public const string PoliticaLimiteLeituraArquivo = "AutoViaLeituraArquivo";

        /// <summary>Intervalo minimo, em segundos, entre dois registros agregados de recusa do limite.</summary>
        public const int IntervaloRegistroRecusaLimiteSegundos = 60;

        /// <summary>Chave de configuracao que indica execucao em container (variavel do .NET).</summary>
        public const string ChaveExecucaoEmContainer = "DOTNET_RUNNING_IN_CONTAINER";

        // --- Disposicao da pasta ---

        /// <summary>Pasta AutoVia dentro da pasta de dados locais do usuario (padrao no Windows fora de container).</summary>
        public const string PastaAutoViaDadosLocais = "AutoVia";

        /// <summary>Pasta de armazenamento dentro da pasta AutoVia.</summary>
        public const string PastaArmazenamentoPadrao = "armazenamento";

        /// <summary>Unica subpasta servida pela rota de leitura.</summary>
        public const string SubpastaArquivosArmazenamento = "arquivos";

        /// <summary>Subpasta dos temporarios de gravacao.</summary>
        public const string SubpastaTemporariosArmazenamento = "tmp";

        /// <summary>Subpasta do segredo de assinatura (irma da pasta de arquivos; nunca servida).</summary>
        public const string SubpastaSegredoArmazenamento = "segredo";

        /// <summary>Nome do arquivo do segredo de assinatura.</summary>
        public const string ArquivoSegredoAssinatura = "assinatura.key";

        /// <summary>Extensao dos temporarios de gravacao.</summary>
        public const string ExtensaoTemporarioArmazenamento = ".tmp";

        /// <summary>Entrada que marca um repositorio Git (a pasta de armazenamento nao pode ficar dentro dele).</summary>
        public const string NomeEntradaGit = ".git";

        /// <summary>Tamanho, em bytes, do segredo de assinatura (256 bits).</summary>
        public const int TamanhoSegredoAssinaturaBytes = 32;

        /// <summary>Idade, em minutos, a partir da qual um temporario orfao e apagado na primeira resolucao da raiz.</summary>
        public const int IdadeMinimaTemporarioOrfaoMinutos = 60;

        /// <summary>Tamanho do buffer de gravacao e de leitura dos arquivos.</summary>
        public const int TamanhoBufferArquivoArmazenamento = 81920;

        /// <summary>Espera, em milissegundos, entre tentativas de mover ou apagar um arquivo.</summary>
        public const int EsperaNovaTentativaArquivoMs = 100;

        /// <summary>Tentativas no total para mover ou apagar um arquivo antes de desistir.</summary>
        public const int TentativasOperacaoArquivo = 3;

        // --- Chave segura (lista branca, conferida caractere a caractere) ---

        /// <summary>Tamanho maximo, em caracteres, da chave do arquivo.</summary>
        public const int TamanhoMaximoChaveArmazenamento = 200;

        /// <summary>Tamanho maximo, em bytes, de um arquivo gravado e de um arquivo lido (igual a ConstantsArmazenamento.TamanhoMaximoFotoBytes).</summary>
        public const int TamanhoMaximoArquivoArmazenamentoBytes = 10485760;

        /// <summary>Caracteres permitidos na chave. Sem maiusculas, espacos, dois-pontos, percentual nem quebra de linha.</summary>
        public const string CaracteresPermitidosChaveArmazenamento = "abcdefghijklmnopqrstuvwxyz0123456789/_.-";

        /// <summary>Nomes reservados do Windows que nao podem ser o nome de um segmento da chave (separados por virgula).</summary>
        public const string NomesReservadosDeArquivo = "con,prn,aux,nul,com1,com2,com3,com4,com5,com6,com7,com8,com9,lpt1,lpt2,lpt3,lpt4,lpt5,lpt6,lpt7,lpt8,lpt9";

        /// <summary>Separador de segmentos da chave.</summary>
        public const char CaractereSeparadorChaveArmazenamento = '/';

        /// <summary>Separador da lista de nomes reservados.</summary>
        public const char CaractereSeparadorListaNomesReservados = ',';

        /// <summary>Sequencia de dois pontos seguidos, proibida em qualquer segmento da chave.</summary>
        public const string SequenciaPontoDuplo = "..";

        // --- Link assinado (prefixo, prazo em janela e formato dos parametros) ---

        /// <summary>Prefixo de versao gravado antes da chave na assinatura do link.</summary>
        public const string PrefixoAssinaturaLinkArmazenamento = "autovia-arquivo-v1";

        /// <summary>Tamanho, em caracteres, da assinatura do link (HMAC-SHA256 em base64url sem preenchimento).</summary>
        public const int TamanhoAssinaturaLinkCaracteres = 43;

        /// <summary>Maximo de digitos do instante de validade do link.</summary>
        public const int MaximoDigitosExpiracaoLink = 10;

        /// <summary>
        /// Formato do instante de validade: so digitos, sem zero a esquerda. Ancorado por \A e \z (inicio e fim absolutos do texto),
        /// sem RegexOptions: em .NET o $ casaria antes de uma quebra de linha final.
        /// </summary>
        public const string PadraoExpiracaoLink = @"\A[1-9][0-9]{0,9}\z";

        /// <summary>Divisor do prazo que da o tamanho da janela de estabilidade do link (prazo / 2).</summary>
        public const int DivisorJanelaLinkArmazenamento = 2;

        /// <summary>Menor janela, em segundos, para o prazo em janela (evita divisao por zero com prazo 1).</summary>
        public const int JanelaMinimaLinkArmazenamentoSegundos = 1;

        /// <summary>Rota publica de leitura de arquivo, relativa ao endereco publico da API.</summary>
        public const string RotaLeituraArquivoArmazenado = "api/armazenamento/arquivos";

        /// <summary>Nome do parametro da chave na URL do link.</summary>
        public const string ParametroChaveLink = "chave";

        /// <summary>Nome do parametro do instante de validade na URL do link.</summary>
        public const string ParametroExpiracaoLink = "exp";

        /// <summary>Nome do parametro da assinatura na URL do link.</summary>
        public const string ParametroAssinaturaLink = "sig";

        /// <summary>Caminho da raiz no endereco publico (o endereco nao pode ter outro caminho).</summary>
        public const string CaminhoRaizEnderecoPublico = "/";

        /// <summary>Caractere que inicia a query no endereco publico (proibido no endereco).</summary>
        public const char CaractereQueryEnderecoPublico = '?';

        /// <summary>Caractere que inicia o fragmento no endereco publico (proibido no endereco).</summary>
        public const char CaractereFragmentoEnderecoPublico = '#';

        // --- Resposta da rota de leitura (cabecalhos) ---

        /// <summary>Cache-Control da resposta de arquivo: o navegador confere a cada uso.</summary>
        public const string CacheControlLeituraArquivo = "private, no-cache";

        /// <summary>Valor de X-Content-Type-Options da resposta de arquivo.</summary>
        public const string ValorNaoFarejarTipo = "nosniff";

        /// <summary>Content-Security-Policy da resposta de arquivo.</summary>
        public const string PoliticaConteudoLeituraArquivo = "default-src 'none'";

        // --- Tipos aceitos (tabela unica do provedor de disco) ---

        /// <summary>Tipo JPEG aceito pelo provedor de disco.</summary>
        public const string TipoConteudoJpeg = "image/jpeg";

        /// <summary>Tipo PNG aceito pelo provedor de disco.</summary>
        public const string TipoConteudoPng = "image/png";

        /// <summary>Primeiros bytes de um JPEG (FF D8 FF).</summary>
        public static readonly byte[] AssinaturaJpeg = new byte[] { 0xFF, 0xD8, 0xFF };

        /// <summary>Primeiros bytes de um PNG (89 50 4E 47 0D 0A 1A 0A).</summary>
        public static readonly byte[] AssinaturaPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // --- Disposicao das constantes movidas das classes do armazenamento (mesmos valores) ---

        /// <summary>Formato do GUID dos nomes de arquivos temporarios e de sonda (32 digitos, sem hifens).</summary>
        public const string FormatoGuidArquivoArmazenamento = "N";

        /// <summary>Quantidade de arquivos sem link no lote (contagem usada no registro de emissao sem segredo).</summary>
        public const int UmArquivoSemLinkArmazenamento = 1;

        /// <summary>Separador dos parametros do link (query string).</summary>
        public const string SeparadorParametrosLink = "&";

        /// <summary>Sinal de igual entre o nome e o valor de cada parametro do link.</summary>
        public const string IgualParametroLink = "=";

        /// <summary>Numero de janela do teto da rota publica de leitura, em minutos.</summary>
        public const int JanelaEmMinutosTetoLeituraArquivo = 1;

        /// <summary>Permissoes consumidas por cada entrada no teto da rota publica de leitura.</summary>
        public const int PermissoesPorEntradaTetoLeitura = 1;

        /// <summary>Separador entre o esquema e o host do endereco publico da API.</summary>
        public const string SeparadorEsquemaHost = "://";

        /// <summary>Menor porta aceita no endereco publico da API.</summary>
        public const int PortaMinimaEnderecoPublico = 1;

        /// <summary>Maior porta aceita no endereco publico da API.</summary>
        public const int PortaMaximaEnderecoPublico = 65535;

        /// <summary>Separador entre o tamanho e o instante no ETag do arquivo.</summary>
        public const string SeparadorEtagArmazenamento = "-";

        /// <summary>Formato hexadecimal minusculo do tamanho e do instante do ETag.</summary>
        public const string FormatoHexadecimalEtag = "x";

        /// <summary>Aspa dupla que cerca o ETag no cabecalho e que e tirada do If-None-Match.</summary>
        public const char CaractereAspasEtag = '"';

        /// <summary>Curinga do If-None-Match (nao e um ETag, responde com os bytes).</summary>
        public const string CuringaIfNoneMatch = "*";

        /// <summary>Nome do cabecalho X-Content-Type-Options da resposta de arquivo.</summary>
        public const string NomeCabecalhoXContentTypeOptions = "X-Content-Type-Options";

        /// <summary>Nome do cabecalho Content-Security-Policy da resposta de arquivo.</summary>
        public const string NomeCabecalhoContentSecurityPolicy = "Content-Security-Policy";

        /// <summary>Quantidade exata de cada parametro do link (chave, exp e sig): uma vez so.</summary>
        public const int QuantidadeEsperadaDeCadaParametroLink = 1;

        /// <summary>Tamanho minimo, em caracteres, da chave do arquivo na rota de leitura.</summary>
        public const int TamanhoMinimoChaveArmazenamento = 1;

        /// <summary>Alfabeto base64url (A-Z, a-z, 0-9, - e _) da assinatura do link, conferido caractere a caractere.</summary>
        public const string AlfabetoBase64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        /// <summary>Quebra de linha que separa os campos do texto assinado do link.</summary>
        public const char QuebraDeLinhaCampoAssinatura = '\n';

        /// <summary>Sinal de mais do base64 padrao (trocado no base64url).</summary>
        public const char SinalMaisBase64 = '+';

        /// <summary>Sinal de barra do base64 padrao (trocado no base64url).</summary>
        public const char SinalBarraBase64 = '/';

        /// <summary>Sinal de igual de preenchimento do base64 (removido no base64url).</summary>
        public const char SinalIgualBase64 = '=';

        /// <summary>Substituto do sinal de mais no base64url.</summary>
        public const char SubstitutoMaisBase64Url = '-';

        /// <summary>Substituto do sinal de barra no base64url.</summary>
        public const char SubstitutoBarraBase64Url = '_';
    }
}
