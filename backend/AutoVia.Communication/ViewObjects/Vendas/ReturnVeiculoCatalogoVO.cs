using AutoVia.Communication.ViewObjects.Frota;

namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// VO de saida do catalogo de seminovos (rota publica). Traz so o que o comprador
    /// precisa ver: nao expoe placa (nem mascarada), chassi, renavam, idEmpresa, CNPJ nem endereco completo da
    /// locadora (D-14); da locadora, so nome fantasia, cidade e UF (RF-72). Mantem kmAtual. O preco formatado
    /// e preenchido no VeiculoCatalogoProfile.
    /// </summary>
    public class ReturnVeiculoCatalogoVO : IComFotoCapa
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

        /// <summary>Nome fantasia da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraNomeFantasia { get; set; } = string.Empty;

        /// <summary>Cidade da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraCidade { get; set; } = string.Empty;

        /// <summary>UF da locadora dona do veiculo (vazio se a navegacao nao vier carregada).</summary>
        public string LocadoraUf { get; set; } = string.Empty;

        /// <summary>
        /// Capa do veiculo (foto ativa, primeira por ordem). Nula quando o veiculo nao tem foto ou quando o armazenamento falhou;
        /// o AutoMapper nao preenche (VeiculoCatalogoProfile ignora); quem preenche e o IFotoCapaVeiculoService.
        /// </summary>
        public FotoCapaVeiculoVO? FotoCapa { get; set; }

        /// <summary>Texto de erro da capa (ConstantsMessage) quando o armazenamento falhou para um veiculo com foto cadastrada.</summary>
        public string? ErroFotoCapa { get; set; }
    }
}
