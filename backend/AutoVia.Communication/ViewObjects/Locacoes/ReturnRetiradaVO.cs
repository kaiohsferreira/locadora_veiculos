using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>Contexto de retirada da reserva, sem dados internos do cliente (nada de link, chave ou nome de arquivo).</summary>
    public class ReturnRetiradaVO : IComFotoCapa
    {
        public int IdReserva { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public StatusReserva StatusReserva { get; set; }
        public DateOnly DataRetiradaPrevista { get; set; }
        public string DataRetiradaPrevistaStr { get; set; } = string.Empty;
        public DateOnly DataDevolucaoPrevista { get; set; }
        public string DataDevolucaoPrevistaStr { get; set; } = string.Empty;
        public decimal? ValorEstimado { get; set; }
        public string ValorEstimadoStr { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteCpfMascarado { get; set; } = string.Empty;
        public string CnhNumero { get; set; } = string.Empty;
        public CategoriaCnh CnhCategoria { get; set; }
        public DateOnly CnhValidade { get; set; }
        public string CnhValidadeStr { get; set; } = string.Empty;
        public bool CnhVencida { get; set; }
        public DateTime? CnhEnviadaEm { get; set; }
        public string CnhEnviadaEmStr { get; set; } = string.Empty;
        public DateTime? ComprovanteResidenciaEnviadoEm { get; set; }
        public string ComprovanteResidenciaEnviadoEmStr { get; set; } = string.Empty;
        public StatusCadastroCliente? StatusCadastroLocadora { get; set; }
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string CategoriaNome { get; set; } = string.Empty;
        public int AnoModelo { get; set; }
        public FotoCapaVeiculoVO? FotoCapa { get; set; }
        public string? ErroFotoCapa { get; set; }
        public StatusVeiculo StatusVeiculo { get; set; }
        public int KmAtual { get; set; }
        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;
        public int DiariasPrevistas { get; set; }
        public string LocalRetiradaNome { get; set; } = string.Empty;
        public string LocalRetiradaEndereco { get; set; } = string.Empty;
        public int? IdLocacaoExistente { get; set; }

        // --- Caucao na retirada (contrato caucao-retirada v1.1, leitura_da_retirada): preenchidos pelo servico, nunca pelo profile.
        // Falam da configuracao do servidor, nao da locacao; nunca levam id_transacao, chave publica, provedor nem dado de cartao.

        /// <summary>Verdadeiro quando o servidor exige caucao na retirada (Pagamentos:ValorCaucao maior que 0).</summary>
        public bool CaucaoExigida { get; set; }

        /// <summary>Valor da caucao configurado no servidor (0 quando nao exigida). Nunca vem do pedido.</summary>
        public decimal ValorCaucao { get; set; }

        /// <summary>Valor da caucao formatado (StaticMethods.FormateToMonetary), sempre preenchido; com 0, o texto do zero.</summary>
        public string ValorCaucaoStr { get; set; } = string.Empty;

        /// <summary>A abertura com caucao pode ser tentada pela tela: caucao exigida e simulacao liberada (so a simulacao existe na tela).</summary>
        public bool CaucaoDisponivel { get; set; }

        /// <summary>O provedor configurado e o FAKE com a trava liberada: a mesma regra da trava do POST, para o GET e o POST concordarem.</summary>
        public bool CaucaoModoSimulacao { get; set; }
    }
}