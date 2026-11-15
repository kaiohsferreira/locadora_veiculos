using AutoVia.Helpers;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Chave de particao do limite por IP. E usada somente pelo ponto unico de particao (RegistrarPoliticaPorIp).
    /// Regras: nulo vira ChaveIpDesconhecido; IPv4 fica como esta; IPv4 mapeado em IPv6 vira IPv4; IPv6 zera os 8
    /// ultimos bytes, remove o scope id e vira "prefixo::/64", para que dois enderecos do mesmo /64 caiam na mesma
    /// chave (a rotacao de enderecos IPv6 nao zera o limite) e /64 diferentes caiam em chaves diferentes.
    /// Riscos aceitos: o corte em /64 deixa quem tem prefixo /56 ou /48 delegado girar entre /64 (/56 e decisao
    /// pendente do Kaio). Enderecos IPv6 especiais (IPv4 compativel ::a.b.c.d e SIIT) nao viram IPv4 e caem em
    /// "::/64"; sem efeito pratico fora de cabecalho forjado.
    /// </summary>
    public static class ChaveIpLimite
    {
        /// <summary>Devolve a chave do limite para o endereco informado (nulo quando a requisicao nao tem IP).</summary>
        public static string Normalizar(IPAddress? endereco)
        {
            if (endereco == null)
            {
                return ConstantsValues.ChaveIpDesconhecido;
            }

            IPAddress enderecoAjustado = endereco;

            if (endereco.IsIPv4MappedToIPv6)
            {
                enderecoAjustado = endereco.MapToIPv4();
            }

            if (enderecoAjustado.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return enderecoAjustado.ToString();
            }

            byte[] bytes = enderecoAjustado.GetAddressBytes();
            int inicioInterface = bytes.Length - ConstantsValues.BytesInterfaceIpv6;

            for (int indice = inicioInterface; indice < bytes.Length; indice++)
            {
                bytes[indice] = 0;
            }

            // new IPAddress(byte[]) nao carrega o scope id: a chave nao depende da interface de rede.
            IPAddress prefixo = new IPAddress(bytes);
            string tamanhoPrefixo = ConstantsValues.PrefixoIpv6Bits.ToString(CultureInfo.InvariantCulture);

            return prefixo.ToString() + ConstantsValues.SeparadorPrefixoIpv6 + tamanhoPrefixo;
        }
    }
}
