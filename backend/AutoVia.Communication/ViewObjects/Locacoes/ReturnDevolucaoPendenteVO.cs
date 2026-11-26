namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>
    /// Item da lista de devolucoes pendentes da locadora do funcionario. So o nome do cliente (sem CPF, e-mail, telefone ou CNH),
    /// sem chassi, renavam, valor de caucao ou identificador de transacao.
    /// </summary>
    public class ReturnDevolucaoPendenteVO
    {
        /// <summary>Identificador da locacao.</summary>
        public int IdLocacao { get; set; }

        /// <summary>Codigo da reserva de origem; nulo quando a locacao nao tem reserva.</summary>
        public string? CodigoReserva { get; set; }

        /// <summary>Nome do cliente (pagador da locacao).</summary>
        public string NomeCliente { get; set; } = string.Empty;

        /// <summary>Placa do veiculo.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Marca do veiculo.</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo.</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Status da locacao: EM_ANDAMENTO ou ATRASADA.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Verdadeiro quando o status e ATRASADA ou a devolucao prevista efetiva ja passou.</summary>
        public bool Atrasada { get; set; }

        /// <summary>Devolucao prevista efetiva: o dia combinado na hora da retirada (RegrasCobrancasExtras).</summary>
        public DateTime DevolucaoPrevistaEfetiva { get; set; }

        /// <summary>Devolucao prevista efetiva formatada em pt-BR.</summary>
        public string DevolucaoPrevistaEfetivaStr { get; set; } = string.Empty;
    }
}
