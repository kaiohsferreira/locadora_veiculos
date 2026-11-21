using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Leitura de uma linha da lista de cobrancas (da locadora ou do cliente). Projecao SEM a coluna qr_code_base64 (o QR pesa
    /// alguns KB e so e lido na leitura de uma cobranca) e com a descricao do veiculo ja resolvida no banco. Nao tem o id da
    /// transacao do provedor: a lista nunca o devolve.
    /// </summary>
    public class PagamentoListagemRegistro
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

        /// <summary>Marca e modelo do veiculo da locacao ou da venda; nulo quando nao ha veiculo.</summary>
        public string? VeiculoDescricao { get; set; }
    }
}
