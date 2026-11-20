using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>Contexto de retirada da reserva, sem dados internos do cliente.</summary>
    public class ReturnRetiradaVO
    {
        public int IdReserva { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public StatusReserva StatusReserva { get; set; }
        public string DataRetiradaPrevistaStr { get; set; } = string.Empty;
        public string DataDevolucaoPrevistaStr { get; set; } = string.Empty;
        public string ValorEstimadoStr { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteCpfMascarado { get; set; } = string.Empty;
        public string CnhNumero { get; set; } = string.Empty;
        public CategoriaCnh CnhCategoria { get; set; }
        public string CnhValidadeStr { get; set; } = string.Empty;
        public StatusCadastroCliente? StatusCadastroLocadora { get; set; }
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public StatusVeiculo StatusVeiculo { get; set; }
        public int KmAtual { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;
        public int? IdLocacaoExistente { get; set; }
    }
}