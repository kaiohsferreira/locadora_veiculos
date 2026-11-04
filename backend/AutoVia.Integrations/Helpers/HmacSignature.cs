using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Helpers
{
    /// <summary>
    /// Assinatura HMAC-SHA256, usada para validar webhooks (ex.: confirmacao de
    /// pagamento com o webhook_secret da integracao PAGAMENTO).
    /// </summary>
    public static class HmacSignature
    {
        /// <summary>Calcula a assinatura HMAC-SHA256 do conteudo, em hexadecimal minusculo.</summary>
        public static string ComputeHex(string payload, string secret)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] hash = HMACSHA256.HashData(keyBytes, payloadBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Compara a assinatura recebida com a calculada em tempo constante, para
        /// nao vazar informacao por diferenca de tempo de resposta.
        /// </summary>
        public static bool Verify(string payload, string secret, string? receivedSignatureHex)
        {
            if (string.IsNullOrWhiteSpace(receivedSignatureHex))
                return false;

            string expected = ComputeHex(payload, secret);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            byte[] receivedBytes = Encoding.UTF8.GetBytes(receivedSignatureHex.Trim().ToLowerInvariant());

            return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
        }
    }
}
