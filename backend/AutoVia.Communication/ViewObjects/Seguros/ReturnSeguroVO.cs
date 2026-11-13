namespace AutoVia.Communication.ViewObjects.Seguros
{
    /// <summary>
    /// VO de saida (GET e retorno do POST) da apolice de seguro. Traz os dados da
    /// entidade, a placa do veiculo, valores e datas formatados (SeguroProfile) e o
    /// destaque de vencimento do RF-41 (DiasParaVencer e VenceEm60Dias, calculados no
    /// SeguroService a partir da data de hoje).
    /// </summary>
    public class ReturnSeguroVO
    {
        /// <summary>Identificador da apolice.</summary>
        public int IdSeguro { get; set; }

        /// <summary>Identificador do veiculo segurado.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Placa do veiculo segurado.</summary>
        public string PlacaVeiculo { get; set; } = string.Empty;

        /// <summary>Nome da seguradora.</summary>
        public string Seguradora { get; set; } = string.Empty;

        /// <summary>Numero da apolice.</summary>
        public string NumeroApolice { get; set; } = string.Empty;

        /// <summary>Inicio da vigencia.</summary>
        public DateOnly DataInicio { get; set; }

        /// <summary>Inicio da vigencia no formato dd/MM/yyyy.</summary>
        public string DataInicioStr { get; set; } = string.Empty;

        /// <summary>Fim da vigencia.</summary>
        public DateOnly DataFim { get; set; }

        /// <summary>Fim da vigencia no formato dd/MM/yyyy.</summary>
        public string DataFimStr { get; set; } = string.Empty;

        /// <summary>Valor do premio, em reais.</summary>
        public decimal ValorPremio { get; set; }

        /// <summary>Valor do premio formatado em moeda brasileira.</summary>
        public string ValorPremioStr { get; set; } = string.Empty;

        /// <summary>Descricao da cobertura, quando informada.</summary>
        public string? Cobertura { get; set; }

        /// <summary>Dias de hoje ate o fim da vigencia: 0 = vence hoje; negativo = ja venceu.</summary>
        public int DiasParaVencer { get; set; }

        /// <summary>True quando a apolice vence em ate 60 dias (0 a 60), para a tela destacar.</summary>
        public bool VenceEm60Dias { get; set; }
    }
}
