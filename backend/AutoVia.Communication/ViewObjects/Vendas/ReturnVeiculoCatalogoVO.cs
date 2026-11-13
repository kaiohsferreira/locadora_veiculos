namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// VO de saida do catalogo de seminovos (rota publica). Traz so o que o comprador
    /// precisa ver: nao expoe placa, chassi, renavam, diaria nem status. O preco formatado
    /// e preenchido no VeiculoCatalogoProfile.
    /// </summary>
    public class ReturnVeiculoCatalogoVO
    {
        /// <summary>Identificador do veiculo (usado na proposta de compra).</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Marca do veiculo.</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo.</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Ano de fabricacao.</summary>
        public int AnoFabricacao { get; set; }

        /// <summary>Ano do modelo.</summary>
        public int AnoModelo { get; set; }

        /// <summary>Cor do veiculo, quando informada.</summary>
        public string? Cor { get; set; }

        /// <summary>Quilometragem atual.</summary>
        public int KmAtual { get; set; }

        /// <summary>Nome da categoria do veiculo.</summary>
        public string CategoriaNome { get; set; } = string.Empty;

        /// <summary>Valor de venda, em reais.</summary>
        public decimal ValorVenda { get; set; }

        /// <summary>Valor de venda formatado em moeda brasileira.</summary>
        public string ValorVendaStr { get; set; } = string.Empty;
    }
}
