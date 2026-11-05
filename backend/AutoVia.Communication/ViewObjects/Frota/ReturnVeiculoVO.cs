using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de saida (GET e retorno do POST) do veiculo. Traz os dados da entidade, o nome
    /// da categoria e os valores e datas formatados (preenchidos no VeiculoProfile).
    /// </summary>
    public class ReturnVeiculoVO
    {
        /// <summary>Identificador do veiculo.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Identificador da categoria do veiculo.</summary>
        public int IdCategoria { get; set; }

        /// <summary>Nome da categoria do veiculo.</summary>
        public string CategoriaNome { get; set; } = string.Empty;

        /// <summary>Placa sem hifen (7 caracteres).</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Numero do chassi.</summary>
        public string Chassi { get; set; } = string.Empty;

        /// <summary>Numero do renavam.</summary>
        public string Renavam { get; set; } = string.Empty;

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

        /// <summary>Valor da diaria, em reais.</summary>
        public decimal ValorDiaria { get; set; }

        /// <summary>Valor da diaria formatado em moeda brasileira.</summary>
        public string ValorDiariaStr { get; set; } = string.Empty;

        /// <summary>Valor de venda, em reais, quando o veiculo esta a venda.</summary>
        public decimal? ValorVenda { get; set; }

        /// <summary>Valor de venda formatado em moeda; vazio quando nulo.</summary>
        public string ValorVendaStr { get; set; } = string.Empty;

        /// <summary>Status atual do veiculo.</summary>
        public StatusVeiculo Status { get; set; }

        /// <summary>Data de aquisicao, quando informada.</summary>
        public DateOnly? DataAquisicao { get; set; }

        /// <summary>Data de aquisicao no formato dd/MM/yyyy; vazio quando nula.</summary>
        public string DataAquisicaoStr { get; set; } = string.Empty;
    }
}
