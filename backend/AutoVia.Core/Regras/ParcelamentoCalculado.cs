namespace AutoVia.Core.Regras
{
    /// <summary>Resultado do calculo do parcelamento da proposta de compra (RegrasPropostaCompra.CalcularParcelamento).</summary>
    public class ParcelamentoCalculado
    {
        /// <summary>Valor de venda do veiculo, em reais.</summary>
        public decimal ValorVenda { get; set; }

        /// <summary>Entrada informada, em reais.</summary>
        public decimal ValorEntrada { get; set; }

        /// <summary>Valor de venda menos a entrada.</summary>
        public decimal ValorFinanciado { get; set; }

        /// <summary>Quantidade de parcelas.</summary>
        public int NumeroParcelas { get; set; }

        /// <summary>Valor financiado dividido pelas parcelas, arredondado para baixo no centavo.</summary>
        public decimal ValorParcela { get; set; }

        /// <summary>Ultima parcela: a parcela mais a diferenca de centavos, para a soma fechar no valor financiado.</summary>
        public decimal ValorUltimaParcela { get; set; }
    }
}
