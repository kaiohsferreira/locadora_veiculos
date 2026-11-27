using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela proposta_compra: proposta de compra de um seminovo feita pelo cliente, com a simulacao de entrada e
    /// parcelamento (P6-BE-02, RF-27). Desvio aprovado por Kaio em 21/11/2026 (backend/docs/especificacao/desvios_aprovados.json,
    /// bloco proposta_compra). Filtro "Empresa" pelo Veiculo, igual a Venda (EscopoEmpresa.cs). Os valores sao calculados
    /// no servidor (RegrasPropostaCompra), nunca aceitos do cliente.
    /// </summary>
    public class PropostaCompra : BaseEntities
    {
        [Column("id_proposta_compra")]
        public int IdPropostaCompra { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        /// <summary>Cliente que enviou a proposta. Vem do token, nunca do corpo da requisicao.</summary>
        [Column("id_cliente")]
        public int IdCliente { get; set; }

        /// <summary>NUMERIC(12,2), maior que zero (ck_proposta_compra_valor). Copia do veiculo.valor_venda no momento da proposta.</summary>
        [Column("valor_venda")]
        public decimal ValorVenda { get; set; }

        /// <summary>NUMERIC(12,2), maior ou igual a zero e menor que o valor de venda (ck_proposta_compra_entrada).</summary>
        [Column("valor_entrada")]
        public decimal ValorEntrada { get; set; }

        /// <summary>De 1 a 60 (ck_proposta_compra_parcelas).</summary>
        [Column("numero_parcelas")]
        public int NumeroParcelas { get; set; }

        /// <summary>NUMERIC(12,2): valor financiado dividido pelas parcelas, arredondado para baixo no centavo.</summary>
        [Column("valor_parcela")]
        public decimal ValorParcela { get; set; }

        /// <summary>NUMERIC(12,2): valor da parcela mais a diferenca de centavos, para a soma fechar no valor financiado.</summary>
        [Column("valor_ultima_parcela")]
        public decimal ValorUltimaParcela { get; set; }

        /// <summary>DEFAULT 'ENVIADA'.</summary>
        [Column("status")]
        public StatusPropostaCompra Status { get; set; } = StatusPropostaCompra.ENVIADA;

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual Cliente Cliente { get; set; } = null!;
    }
}
