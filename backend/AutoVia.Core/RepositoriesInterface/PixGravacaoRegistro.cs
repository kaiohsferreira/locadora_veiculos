using System;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Dados do PIX que o provedor devolveu e que a cobranca grava: identificador no provedor (id_transacao), QR (PNG em base64),
    /// copia e cola e validade (ja na hora local do servidor). Nunca vem do pedido do cliente ou do funcionario.
    /// </summary>
    public class PixGravacaoRegistro
    {
        public PixGravacaoRegistro(string identificadorProvedor, string qrCodeBase64, string copiaECola, DateTime expiraEm)
        {
            IdentificadorProvedor = identificadorProvedor;
            QrCodeBase64 = qrCodeBase64;
            CopiaECola = copiaECola;
            ExpiraEm = expiraEm;
        }

        public string IdentificadorProvedor { get; }

        public string QrCodeBase64 { get; }

        public string CopiaECola { get; }

        public DateTime ExpiraEm { get; }
    }
}
