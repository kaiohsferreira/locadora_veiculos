namespace AutoVia.Integrations.Communication.Pix
{
    /// <summary>
    /// Pedido de cobranca PIX feito pelo modulo de pagamento (P7-BE-02).
    /// A ReferenciaExterna e a chave de idempotencia: mesma referencia = mesma cobranca.
    /// </summary>
    public class PixCobrancaSolicitacao
    {
        /// <summary>1 a 64 caracteres: letras, numeros, '-' e '_'. Vira external_reference e X-Idempotency-Key.</summary>
        public string ReferenciaExterna { get; set; } = string.Empty;

        /// <summary>Maior que zero, com no maximo 2 casas decimais.</summary>
        public decimal Valor { get; set; }

        /// <summary>Descricao obrigatoria, ate 140 caracteres.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Segundos ate expirar. Se nulo, usa a chave pix_expiracao_segundos (padrao 3600). Maximo 2592000.</summary>
        public int? ExpiracaoSegundos { get; set; }

        public PixPagador Pagador { get; set; } = new PixPagador();
    }
}
