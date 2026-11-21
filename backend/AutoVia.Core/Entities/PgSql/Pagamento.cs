using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela pagamento: cobranca de uma locacao OU de uma venda, nunca das duas
    /// (ck_pagamento_origem). Exatamente um entre IdLocacao e IdVenda e preenchido.
    /// </summary>
    public class Pagamento : BaseEntities
    {
        [Column("id_pagamento")]
        public int IdPagamento { get; set; }

        [Column("id_locacao")]
        public int? IdLocacao { get; set; }

        [Column("id_venda")]
        public int? IdVenda { get; set; }

        [Column("tipo")]
        public TipoPagamento Tipo { get; set; }

        /// <summary>NUMERIC(12,2), maior que zero (ck_pagamento_valor).</summary>
        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("data_vencimento")]
        public DateOnly DataVencimento { get; set; }

        [Column("data_pagamento")]
        public DateOnly? DataPagamento { get; set; }

        /// <summary>
        /// VARCHAR(20) livre: a especificacao nao define CHECK para esta coluna,
        /// por isso continua string (PIX, CARTAO, BOLETO...) e nao enum.
        /// </summary>
        [Column("forma")]
        public string? Forma { get; set; }

        /// <summary>DEFAULT 'PENDENTE'.</summary>
        [Column("status")]
        public StatusPagamento Status { get; set; } = StatusPagamento.PENDENTE;

        /// <summary>
        /// VARCHAR(40), anulavel: identificador da transacao no provedor de pagamento, preenchido por quem grava a caucao ou a
        /// cobranca. Sem indice e sem CHECK. Nunca sai em resposta de API para o cliente.
        /// </summary>
        [Column("id_transacao")]
        public string? IdTransacao { get; set; }

        /// <summary>
        /// TEXT, anulavel: imagem PNG do QR Code do PIX em base64, sem prefixo data:. Sem indice e sem CHECK.
        /// As listagens nao devem ler esta coluna (regra do contrato pagamento-pix; a projecao entra com as rotas).
        /// </summary>
        [Column("qr_code_base64")]
        public string? QrCodeBase64 { get; set; }

        /// <summary>
        /// VARCHAR(512), anulavel: codigo copia e cola do PIX. Sem indice e sem CHECK.
        /// </summary>
        [Column("pix_copia_e_cola")]
        public string? PixCopiaECola { get; set; }

        /// <summary>
        /// TIMESTAMP sem fuso, anulavel: validade do PIX gravada na hora local do servidor (o gateway converte antes de gravar).
        /// Sem indice e sem CHECK.
        /// </summary>
        [Column("pix_expira_em")]
        public DateTime? PixExpiraEm { get; set; }

        // --- Navegacao ---
        public virtual Locacao? Locacao { get; set; }
        public virtual Venda? Venda { get; set; }
    }
}
