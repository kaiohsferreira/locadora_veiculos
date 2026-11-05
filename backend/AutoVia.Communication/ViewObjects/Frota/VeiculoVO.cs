namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de entrada (POST e PUT) do veiculo. Estrutura plana: a categoria entra so pelo id,
    /// sem status e sem campos de auditoria (o status e definido pelo servico). A validacao
    /// e feita somente pelo VeiculoVOValidator.
    /// </summary>
    public class VeiculoVO
    {
        /// <summary>Identificador da categoria do veiculo (obrigatorio, maior que zero).</summary>
        public int IdCategoria { get; set; }

        /// <summary>Placa, no padrao antigo (ABC1234) ou Mercosul (ABC1D23). Aceita hifen e minusculas.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Chassi: 17 caracteres alfanumericos, sem as letras I, O e Q.</summary>
        public string Chassi { get; set; } = string.Empty;

        /// <summary>Renavam: exatamente 11 digitos.</summary>
        public string Renavam { get; set; } = string.Empty;

        /// <summary>Marca do veiculo (obrigatoria, ate 40 caracteres).</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo (obrigatorio, ate 40 caracteres).</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Ano de fabricacao: entre 1980 e o ano seguinte ao atual.</summary>
        public int AnoFabricacao { get; set; }

        /// <summary>Ano do modelo: nao pode ser anterior ao ano de fabricacao nem posterior ao ano seguinte ao atual.</summary>
        public int AnoModelo { get; set; }

        /// <summary>Cor do veiculo (opcional, ate 30 caracteres). Vazia vira nula.</summary>
        public string? Cor { get; set; }

        /// <summary>Quilometragem atual (zero ou mais).</summary>
        public int KmAtual { get; set; }

        /// <summary>Valor da diaria, em reais; deve ser maior que zero, com ate 2 casas decimais.</summary>
        public decimal ValorDiaria { get; set; }

        /// <summary>Valor de venda, em reais (opcional). Quando informado, deve ser maior que zero, com ate 2 casas decimais.</summary>
        public decimal? ValorVenda { get; set; }

        /// <summary>Data de aquisicao (opcional). Nao pode ser posterior a hoje.</summary>
        public DateOnly? DataAquisicao { get; set; }
    }
}
