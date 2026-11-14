using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela cliente_locadora: aprovacao do cadastro do cliente por locadora (RF-74 a RF-83). Um vinculo por par
    /// cliente x locadora, com status PENDENTE, APROVADO ou RECUSADO. Nao ha indice unico: a regra do vinculo
    /// ativo de menor id fica no servico.
    /// </summary>
    public class ClienteLocadora : BaseEntities
    {
        [Column("id_cliente_locadora")]
        public int IdClienteLocadora { get; set; }

        [Column("id_cliente")]
        public int IdCliente { get; set; }

        [Column("id_empresa")]
        public int IdEmpresa { get; set; }

        /// <summary>DEFAULT 'PENDENTE'. Enum gravado como texto (CHECK ck_cliente_locadora_status).</summary>
        [Column("status")]
        public StatusCadastroCliente Status { get; set; } = StatusCadastroCliente.PENDENTE;

        /// <summary>Motivo interno da recusa (VARCHAR(500)). Obrigatorio na recusa, validado no servico; nao sai para o cliente.</summary>
        [Column("justificativa_recusa")]
        public string? JustificativaRecusa { get; set; }

        /// <summary>Funcionario que decidiu o vinculo. Nulo enquanto estiver PENDENTE.</summary>
        [Column("id_funcionario_verificador")]
        public int? IdFuncionarioVerificador { get; set; }

        /// <summary>DEFAULT CURRENT_TIMESTAMP.</summary>
        [Column("data_solicitacao")]
        public DateTime DataSolicitacao { get; set; }

        /// <summary>Preenchida na decisao (aprovacao ou recusa).</summary>
        [Column("data_verificacao")]
        public DateTime? DataVerificacao { get; set; }

        // --- Navegacao ---
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Empresa Empresa { get; set; } = null!;
        public virtual Funcionario? FuncionarioVerificador { get; set; }
    }
}
