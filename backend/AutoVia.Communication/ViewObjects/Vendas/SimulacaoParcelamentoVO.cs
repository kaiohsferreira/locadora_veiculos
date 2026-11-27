namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// Entrada da simulacao do parcelamento (GET /api/veiculos/{idVeiculo}/simulacao, query string). O veiculo vem da rota.
    /// Validado por SimulacaoParcelamentoVOValidator. Nao grava nada.
    /// </summary>
    public class SimulacaoParcelamentoVO
    {
        /// <summary>Entrada, em reais. Maior ou igual a zero, ate 2 casas decimais e menor que o valor de venda.</summary>
        public decimal ValorEntrada { get; set; }

        /// <summary>Numero de parcelas, de 1 a 60.</summary>
        public int NumeroParcelas { get; set; }
    }
}
