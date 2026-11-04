using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela venda: venda de um veiculo da frota (seminovo).</summary>
    public class Venda : BaseEntities
    {
        [Column("id_venda")]
        public int IdVenda { get; set; }

        /// <summary>Uma venda ativa por veiculo: unicidade validada no servico, sem indice unico no banco.</summary>
        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("id_cliente")]
        public int IdCliente { get; set; }

        [Column("id_funcionario")]
        public int IdFuncionario { get; set; }

        /// <summary>DEFAULT CURRENT_DATE.</summary>
        [Column("data_venda")]
        public DateOnly DataVenda { get; set; }

        /// <summary>NUMERIC(12,2), maior que zero (ck_venda_valor).</summary>
        [Column("valor_venda")]
        public decimal ValorVenda { get; set; }

        [Column("forma_pagamento")]
        public FormaPagamentoVenda FormaPagamento { get; set; }

        /// <summary>Anulavel. Unica entre as vendas ativas: validada no servico, sem indice unico no banco.</summary>
        [Column("numero_nota_fiscal")]
        public string? NumeroNotaFiscal { get; set; }

        /// <summary>DEFAULT 'PENDENTE'.</summary>
        [Column("status_documentacao")]
        public StatusDocumentacaoVenda StatusDocumentacao { get; set; } = StatusDocumentacaoVenda.PENDENTE;

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Funcionario Funcionario { get; set; } = null!;
        public virtual List<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
    }
}
