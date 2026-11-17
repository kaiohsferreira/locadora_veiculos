namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO publico da busca de veiculos disponiveis (P3-BE-01, RF-11). Nunca traz placa, chassi, renavam, idEmpresa,
    /// CNPJ nem endereco completo da locadora (D-14): da locadora, so nome fantasia, cidade e UF (RF-72).
    /// </summary>
    public class ReturnVeiculoDisponivelVO : IComFotoCapa
    {
        public int IdVeiculo { get; set; }
        public int IdCategoria { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoModelo { get; set; }
        public string? Cor { get; set; }
        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;

        /// <summary>Nome fantasia da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraNomeFantasia { get; set; } = string.Empty;

        /// <summary>Cidade da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraCidade { get; set; } = string.Empty;

        /// <summary>UF da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraUf { get; set; } = string.Empty;

        /// <summary>
        /// Capa do veiculo (foto ativa, primeira por ordem). Nula quando o veiculo nao tem foto ou quando o armazenamento falhou;
        /// o AutoMapper nao preenche (VeiculoDisponivelProfile ignora); quem preenche e o IFotoCapaVeiculoService.
        /// </summary>
        public FotoCapaVeiculoVO? FotoCapa { get; set; }

        /// <summary>Texto de erro da capa (ConstantsMessage) quando o armazenamento falhou para um veiculo com foto cadastrada.</summary>
        public string? ErroFotoCapa { get; set; }
    }
}
