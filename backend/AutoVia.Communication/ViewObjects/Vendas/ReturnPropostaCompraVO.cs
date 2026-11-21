using System;

namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// Saida da proposta de compra (POST e GET /api/propostas-compra). Traz o parcelamento gravado e o veiculo resumido;
    /// nao expoe placa, chassi, renavam nem dados do cliente. Preenchido pelo PropostaCompraProfile.
    /// </summary>
    public class ReturnPropostaCompraVO : ReturnSimulacaoParcelamentoVO
    {
        /// <summary>Identificador da proposta.</summary>
        public int IdPropostaCompra { get; set; }

        /// <summary>Marca do veiculo.</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo.</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Ano do modelo do veiculo.</summary>
        public int AnoModelo { get; set; }

        /// <summary>Situacao da proposta: ENVIADA, ACEITA, RECUSADA ou CANCELADA.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Data e hora do envio (CreatedAt).</summary>
        public DateTime DataProposta { get; set; }
    }
}
