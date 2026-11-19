using AutoVia.Helpers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Helpers.Armazenamento
{
    /// <summary>
    /// Assinatura do link de arquivo (contrato armazenamento-disco v1, regra 11): HMAC-SHA256 com o segredo sobre o prefixo de
    /// versao, a chave e o instante de validade, em base64url sem preenchimento. Sem estado.
    /// </summary>
    public static class AssinaturaLinkArmazenamento
    {
        /// <summary>Calcula a assinatura canonica (43 caracteres base64url) da chave e do instante de validade.</summary>
        public static string Calcular(byte[] segredo, string chave, long expiraEmUnix)
        {
            string textoAssinado = ConstantsValues.PrefixoAssinaturaLinkArmazenamento
                + ConstantsValues.QuebraDeLinhaCampoAssinatura + chave + ConstantsValues.QuebraDeLinhaCampoAssinatura
                + expiraEmUnix.ToString(CultureInfo.InvariantCulture);
            byte[] hash = HMACSHA256.HashData(segredo, Encoding.UTF8.GetBytes(textoAssinado));
            return Convert.ToBase64String(hash)
                .TrimEnd(ConstantsValues.SinalIgualBase64)
                .Replace(ConstantsValues.SinalMaisBase64, ConstantsValues.SubstitutoMaisBase64Url)
                .Replace(ConstantsValues.SinalBarraBase64, ConstantsValues.SubstitutoBarraBase64Url);
        }

        /// <summary>
        /// Confere a assinatura recebida comparando o TEXTO com o recalculado, em tempo constante. Nao decodifica a recebida:
        /// so a escrita canonica confere.
        /// </summary>
        public static bool Conferir(byte[] segredo, string chave, long expiraEmUnix, string? assinaturaRecebida)
        {
            if (assinaturaRecebida == null || assinaturaRecebida.Length != ConstantsValues.TamanhoAssinaturaLinkCaracteres)
            {
                return false;
            }

            string assinaturaEsperada = Calcular(segredo, chave, expiraEmUnix);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(assinaturaEsperada),
                Encoding.UTF8.GetBytes(assinaturaRecebida));
        }
    }
}
