namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da leitura do IP do cliente (limite por IP): chave e padrao do cabecalho que traz o IP real atras do
    /// proxy, chave que o host do ASP.NET le (guarda), quantidade de saltos confiados e formato do prefixo IPv6 usado
    /// como chave do limite.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Chave do cabecalho que traz o IP real do cliente. Vazia ou so com espacos: nenhum cabecalho de IP e lido.</summary>
        public const string ChaveCabecalhoIpCliente = "Rede:CabecalhoIpCliente";

        /// <summary>
        /// Cabecalho padrao do IP real: X-Real-IP. Com ele o limite fica exato no teste publicado no Railway (10 de 10) e o
        /// cabecalho forjado pelo cliente nao burla, porque a borda sobrescreve o valor. Com X-Forwarded-For passavam 20
        /// com limite 10. Em hospedagem cuja borda repasse o X-Real-IP do cliente sem sobrescrever, o cabecalho e forjavel:
        /// ajustar a variavel Rede__CabecalhoIpCliente.
        /// </summary>
        public const string CabecalhoIpClientePadrao = "X-Real-IP";

        /// <summary>
        /// Chave que o host do ASP.NET le (variavel ASPNETCORE_FORWARDEDHEADERS_ENABLED). Verdadeira significa que o host
        /// insere um UseForwardedHeaders proprio e a API nao sobe (guarda logo apos o Build).
        /// </summary>
        public const string ChaveForwardedHeadersHabilitado = "FORWARDEDHEADERS_ENABLED";

        /// <summary>Caracteres, alem de letras e digitos, aceitos num nome de cabecalho HTTP (token do RFC 9110).</summary>
        public const string CaracteresTokenCabecalho = "!#$%&'*+-.^_`|~";

        /// <summary>Quantos saltos de encaminhamento o cabecalho aceita: 1, so o salto da borda.</summary>
        public const int LimiteSaltosEncaminhados = 1;

        /// <summary>Tamanho do prefixo IPv6 usado como chave do limite (dois enderecos do mesmo prefixo caem na mesma chave).</summary>
        public const int PrefixoIpv6Bits = 64;

        /// <summary>Quantidade de bytes finais do IPv6 zerados para formar o prefixo (64 bits de interface).</summary>
        public const int BytesInterfaceIpv6 = 8;

        /// <summary>Separador entre o prefixo IPv6 e o tamanho do prefixo na chave do limite.</summary>
        public const string SeparadorPrefixoIpv6 = "/";
    }
}
