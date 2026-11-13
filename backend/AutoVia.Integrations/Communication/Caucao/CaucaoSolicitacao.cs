namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>
    /// Pedido de pre-autorizacao de caucao no cartao (P7-INT-01, RF-20). O backend nunca recebe
    /// numero de cartao nem CVV: recebe o token gerado no formulario do front, de uso unico.
    /// A ReferenciaExterna nao e a chave de idempotencia (o gateway deriva a chave da tentativa).
    /// </summary>
    public class CaucaoSolicitacao
    {
        /// <summary>1 a 64 caracteres: letras, numeros, '-' e '_'. Vira external_reference.</summary>
        public string ReferenciaExterna { get; set; } = string.Empty;

        /// <summary>Maior que zero, com no maximo 2 casas decimais.</summary>
        public decimal Valor { get; set; }

        /// <summary>Descricao obrigatoria, ate 140 caracteres.</summary>
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Token do cartao gerado no front (8 a 128 caracteres: letras, numeros, '-' e '_'). Nunca vai para o log.</summary>
        public string TokenCartao { get; set; } = string.Empty;

        /// <summary>Bandeira devolvida pelo front (visa, master, elo...): 2 a 30 caracteres, [a-z0-9_].</summary>
        public string MetodoPagamentoId { get; set; } = string.Empty;

        /// <summary>issuer_id devolvido pelo front (opcional, maior que zero).</summary>
        public long? EmissorId { get; set; }

        public CaucaoPagador Pagador { get; set; } = new CaucaoPagador();

        /// <summary>Device id do MercadoPago.js para antifraude (opcional, ate 128). Nunca vai para o log.</summary>
        public string? IdentificadorDispositivo { get; set; }
    }
}
