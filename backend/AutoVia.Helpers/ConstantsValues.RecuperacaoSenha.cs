namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos da recuperacao de senha por link (N1, RF-49): secao de configuracao, caminho e
    /// parametro do link no front, tamanhos do token, limites padrao e faixas aceitas de configuracao.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Secao de configuracao da recuperacao (Autenticacao:Recuperacao).</summary>
        public const string SecaoRecuperacao = "Autenticacao:Recuperacao";

        /// <summary>Caminho da tela de redefinicao no front. O link vai no fragmento (#token=), que nao sai no Referer.</summary>
        public const string CaminhoRedefinirSenhaFront = "/redefinir-senha";

        /// <summary>Nome do parametro do token no fragmento do link.</summary>
        public const string ParametroTokenFront = "token";

        /// <summary>Modelo do link: {0} base do front, {1} caminho, {2} parametro, {3} token.</summary>
        public const string ModeloLinkRedefinicao = "{0}{1}#{2}={3}";

        /// <summary>Barra final removida da URL publica do front antes de montar o link.</summary>
        public const char CaractereBarra = '/';

        /// <summary>Bytes aleatorios do token (256 bits). Em base64url viram 43 caracteres.</summary>
        public const int TamanhoTokenBytes = 32;

        /// <summary>Tamanho do hash SHA-256 em hexadecimal (coluna token_hash, CHAR(64)).</summary>
        public const int TamanhoTokenHash = 64;

        /// <summary>Maior tamanho aceito para o token recebido na redefinicao.</summary>
        public const int TamanhoMaximoToken = 128;

        /// <summary>Validade padrao do link, em minutos (Autenticacao:Recuperacao:ValidadeMinutos).</summary>
        public const int ValidadeMinutosPadrao = 30;

        /// <summary>Menor validade aceita, em minutos.</summary>
        public const int ValidadeMinutosMinimo = 1;

        /// <summary>Limite padrao de solicitacoes por conta na janela (Autenticacao:Recuperacao:LimitePorHora).</summary>
        public const int LimitePorHoraPadrao = 3;

        /// <summary>Menor limite aceito por conta.</summary>
        public const int LimitePorHoraMinimo = 1;

        /// <summary>Janela do limite por conta, em minutos (1 hora).</summary>
        public const int JanelaLimiteMinutos = 60;

        /// <summary>Piso padrao de tempo da resposta do pedido, em ms (Autenticacao:Recuperacao:TempoMinimoRespostaMs).</summary>
        public const int TempoMinimoRespostaMsPadrao = 400;

        /// <summary>Menor piso aceito, em ms (0 desliga a espera; so usado em teste).</summary>
        public const int TempoMinimoRespostaMsMinimo = 0;

        /// <summary>Maior piso aceito, em ms: teto de configuracao (uma espera acima de 5 s nao e resposta de login).</summary>
        public const int TempoMinimoRespostaMsMaximo = 5000;

        /// <summary>Maior validade aceita para o link, em minutos (1 dia).</summary>
        public const int ValidadeMinutosMaximo = 1440;

        /// <summary>Maior limite por conta na janela de 1 hora aceito na configuracao.</summary>
        public const int LimitePorHoraMaximo = 100;

        /// <summary>Nome de host local aceito como loopback fora de Development (regra de UrlPublicaFront).</summary>
        public const string HostLocalhost = "localhost";

        /// <summary>Colchete que envolve o endereco IPv6 no host da URI (ex.: [::1]).</summary>
        public const char CaractereColcheteAbre = '[';

        /// <summary>Colchete de fechamento do endereco IPv6 no host da URI.</summary>
        public const char CaractereColcheteFecha = ']';

        /// <summary>Etiqueta da mensagem de recuperacao, so para log (sem dado pessoal).</summary>
        public const string EtiquetaEmailRecuperacao = "recuperacao-senha";

        /// <summary>Capacidade da fila de pedidos de recuperacao (em memoria). Fila cheia: a resposta continua neutra e o pedido e descartado.</summary>
        public const int CapacidadeFilaPedidosRecuperacao = 200;

        /// <summary>Texto que substitui o pedido em ToString: o CPF nunca sai do objeto por log ou por texto.</summary>
        public const string TextoPedidoRecuperacaoOculto = "pedido de recuperação de senha (dados omitidos)";

        /// <summary>Ponto final de um host absoluto (ex.: localhost. e aceito como loopback fora de Development).</summary>
        public const char CaracterePontoFinal = '.';

        /// <summary>Sufixo de subdominio local (ex.: app.localhost), tratado como loopback fora de Development.</summary>
        public const string SufixoSubdominioLocalhost = ".localhost";
    }
}
