using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de saida de um vinculo do cliente com uma locadora (GET minhas locadoras, T-BE-06). A justificativa da recusa
    /// e INTERNA da locadora: este VO nao a traz. Datas ja formatadas pelo ClienteLocadoraProfile.
    /// </summary>
    public class ReturnClienteLocadoraVO
    {
        /// <summary>Id da locadora do vinculo.</summary>
        public int IdEmpresa { get; set; }

        /// <summary>Nome fantasia da locadora.</summary>
        public string NomeFantasia { get; set; } = string.Empty;

        /// <summary>Status do cadastro do cliente nesta locadora (PENDENTE, APROVADO ou RECUSADO).</summary>
        public StatusCadastroCliente Status { get; set; }

        /// <summary>Data da solicitacao no formato dd/MM/yyyy HH:mm.</summary>
        public string DataSolicitacaoStr { get; set; } = string.Empty;

        /// <summary>Data da decisao no formato dd/MM/yyyy HH:mm; vazia enquanto estiver pendente.</summary>
        public string? DataVerificacaoStr { get; set; }
    }
}
