namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// Corpo do POST /api/propostas-compra. O cliente vem do token e os valores (preco, parcela e ultima parcela) sao
    /// calculados no servidor; por isso o VO so tem o veiculo, a entrada e as parcelas. Validado por PropostaCompraVOValidator.
    /// </summary>
    public class PropostaCompraVO
    {
        /// <summary>Veiculo do catalogo de seminovos.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Entrada, em reais. Maior ou igual a zero, ate 2 casas decimais e menor que o valor de venda.</summary>
        public decimal ValorEntrada { get; set; }

        /// <summary>Numero de parcelas, de 1 a 60.</summary>
        public int NumeroParcelas { get; set; }
    }
}
