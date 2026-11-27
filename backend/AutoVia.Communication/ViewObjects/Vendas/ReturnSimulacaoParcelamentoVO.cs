namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// Saida da simulacao do parcelamento (RF-27). Parcela = (valor de venda - entrada) / parcelas, arredondada para baixo
    /// no centavo; a ultima parcela absorve a diferenca. Cada valor vem tambem formatado em reais (campos Str).
    /// </summary>
    public class ReturnSimulacaoParcelamentoVO
    {
        /// <summary>Veiculo simulado.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Valor de venda do veiculo, em reais.</summary>
        public decimal ValorVenda { get; set; }

        /// <summary>Valor de venda formatado em moeda brasileira.</summary>
        public string ValorVendaStr { get; set; } = string.Empty;

        /// <summary>Entrada, em reais.</summary>
        public decimal ValorEntrada { get; set; }

        /// <summary>Entrada formatada em moeda brasileira.</summary>
        public string ValorEntradaStr { get; set; } = string.Empty;

        /// <summary>Valor de venda menos a entrada.</summary>
        public decimal ValorFinanciado { get; set; }

        /// <summary>Valor financiado formatado em moeda brasileira.</summary>
        public string ValorFinanciadoStr { get; set; } = string.Empty;

        /// <summary>Numero de parcelas.</summary>
        public int NumeroParcelas { get; set; }

        /// <summary>Valor de cada parcela (todas menos a ultima).</summary>
        public decimal ValorParcela { get; set; }

        /// <summary>Valor da parcela formatado em moeda brasileira.</summary>
        public string ValorParcelaStr { get; set; } = string.Empty;

        /// <summary>Valor da ultima parcela (igual ou alguns centavos maior que as demais).</summary>
        public decimal ValorUltimaParcela { get; set; }

        /// <summary>Valor da ultima parcela formatado em moeda brasileira.</summary>
        public string ValorUltimaParcelaStr { get; set; } = string.Empty;
    }
}
