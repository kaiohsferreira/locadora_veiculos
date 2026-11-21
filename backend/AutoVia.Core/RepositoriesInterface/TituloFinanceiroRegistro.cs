using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Uma linha da lista de titulos do painel financeiro. Projecao SEM a coluna qr_code_base64 e sem o id da transacao do
    /// provedor, com o nome e o CPF do cliente dono da origem (o servico mascara o CPF antes de devolver).
    /// </summary>
    public class TituloFinanceiroRegistro
    {
        public int IdPagamento { get; set; }

        public int? IdLocacao { get; set; }

        public int? IdVenda { get; set; }

        public TipoPagamento Tipo { get; set; }

        public decimal Valor { get; set; }

        public DateOnly DataVencimento { get; set; }

        public DateOnly? DataPagamento { get; set; }

        public string? Forma { get; set; }

        public StatusPagamento Status { get; set; }

        /// <summary>Nome do cliente dono da locacao ou da venda de origem.</summary>
        public string? ClienteNome { get; set; }

        /// <summary>CPF do cliente dono da origem, ainda sem mascara (o servico aplica RegrasPainelFinanceiro.MascararCpf).</summary>
        public string? ClienteCpf { get; set; }
    }
}
