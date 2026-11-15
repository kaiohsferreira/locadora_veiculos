namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da leitura do IP do cliente (limite por IP). As mensagens de configuracao nao repetem o valor
    /// informado: a chave e lida do ambiente e o valor pode ser qualquer coisa, inclusive dado que nao deve ir ao log.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Erro de subida (InvalidOperationException): Rede:CabecalhoIpCliente nao e um nome de cabecalho HTTP valido.</summary>
        public const string ErroConfiguracaoCabecalhoIpCliente = "A configuração Rede:CabecalhoIpCliente deve ser o nome de um cabeçalho HTTP válido (letras, dígitos e os caracteres ! # $ % & ' * + - . ^ _ ` | ~, sem espaços), ou ficar vazia.";

        /// <summary>Erro de subida (InvalidOperationException): FORWARDEDHEADERS_ENABLED verdadeira faria o host anular o controle do IP do limite.</summary>
        public const string ErroHostComCabecalhosAutomaticos = "A configuração FORWARDEDHEADERS_ENABLED está verdadeira: o host aplicaria cabeçalhos encaminhados por conta própria e anularia o controle do IP do limite. Desligue ASPNETCORE_FORWARDEDHEADERS_ENABLED.";

        /// <summary>Aviso na subida (log de informacao): a leitura do cabecalho de IP esta desligada, todos os clientes caem na chave do socket.</summary>
        public const string AvisoCabecalhoIpDesligado = "Leitura do IP do cliente desligada (cabeçalho de IP vazio): os clientes passam a compartilhar a chave do IP do socket no limite por IP.";
    }
}
