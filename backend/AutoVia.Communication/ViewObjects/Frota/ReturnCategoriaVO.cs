namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de saida (GET) da categoria de veiculos. Traz os dados da entidade e o
    /// valor da diaria base formatado em moeda (preenchido no CategoriaProfile).
    /// </summary>
    public class ReturnCategoriaVO
    {
        /// <summary>Identificador da categoria.</summary>
        public int IdCategoria { get; set; }

        /// <summary>Nome da categoria.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Descricao da categoria, quando informada.</summary>
        public string? Descricao { get; set; }

        /// <summary>Valor da diaria base, em reais.</summary>
        public decimal ValorDiariaBase { get; set; }

        /// <summary>Valor da diaria base formatado em moeda brasileira (ex.: R$ 150,00).</summary>
        public string ValorDiariaBaseStr { get; set; } = string.Empty;
    }
}
