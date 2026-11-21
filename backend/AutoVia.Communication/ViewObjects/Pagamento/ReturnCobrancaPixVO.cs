namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Saida do PIX da cobranca (gerar ou reabrir), para o cliente e para o funcionario. Traz o QR (PNG em base64, sem prefixo
    /// data:), a copia e cola, a validade formatada e os segundos que faltam na hora da resposta. NUNCA traz o identificador do
    /// provedor, o id da transacao, nome, CPF ou e-mail do pagador.
    /// </summary>
    public class ReturnCobrancaPixVO
    {
        /// <summary>Id da cobranca.</summary>
        public int IdPagamento { get; set; }

        /// <summary>Forma do PIX: sempre PIX.</summary>
        public string Forma { get; set; } = string.Empty;

        /// <summary>Valor da cobranca, do banco (nunca do pedido).</summary>
        public decimal Valor { get; set; }

        /// <summary>Valor no formato de moeda do projeto (R$).</summary>
        public string ValorStr { get; set; } = string.Empty;

        /// <summary>QR Code em PNG, em base64 (sem prefixo data:). O front monta data:image/png;base64,.</summary>
        public string QrCodeBase64 { get; set; } = string.Empty;

        /// <summary>Codigo copia e cola do PIX.</summary>
        public string CopiaECola { get; set; } = string.Empty;

        /// <summary>Validade no formato dd/MM/yyyy HH:mm:ss (hora local do servidor).</summary>
        public string ExpiraEmStr { get; set; } = string.Empty;

        /// <summary>Segundos que faltam para a validade, calculados na resposta (zero ou mais).</summary>
        public int SegundosParaExpirar { get; set; }
    }
}
