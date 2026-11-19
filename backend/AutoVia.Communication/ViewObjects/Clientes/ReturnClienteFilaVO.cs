using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// Item da fila de verificacao de cadastros da locadora (P2-BE-05). Status, datas e justificativa vem do vinculo com a
    /// locadora, nunca do status geral do cliente. Datas no formato de data e hora do projeto (dd/MM/yyyy HH:mm) e o CPF
    /// formatado (000.000.000-00), preenchidos pelo ClienteVerificacaoProfile. A justificativa da recusa sai nesta fila
    /// porque a equipe da propria locadora a registrou.
    /// </summary>
    public class ReturnClienteFilaVO
    {
        /// <summary>Identificador do cliente.</summary>
        public int IdCliente { get; set; }

        /// <summary>Nome do cliente.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>CPF formatado como 000.000.000-00.</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Data e hora do cadastro do cliente (dd/MM/yyyy HH:mm).</summary>
        public string DataCadastro { get; set; } = string.Empty;

        /// <summary>Status do cadastro nesta locadora (PENDENTE, APROVADO ou RECUSADO).</summary>
        public StatusCadastroCliente Status { get; set; }

        /// <summary>Data e hora do pedido de cadastro nesta locadora (dd/MM/yyyy HH:mm).</summary>
        public string DataSolicitacao { get; set; } = string.Empty;

        /// <summary>Data e hora da ultima decisao da locadora (dd/MM/yyyy HH:mm); nulo enquanto nao houve decisao.</summary>
        public string? DataVerificacao { get; set; }

        /// <summary>Justificativa da ultima recusa; nulo quando nao ha recusa registrada.</summary>
        public string? JustificativaRecusa { get; set; }
    }
}
