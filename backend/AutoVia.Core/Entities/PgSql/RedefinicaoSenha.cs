using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela redefinicao_senha: link de recuperacao de senha (RF-49). Guarda so o hash SHA-256
    /// do token enviado por e-mail (nunca o token), a validade e o titular (cliente OU funcionario).
    /// Uso unico: ao redefinir a senha, o registro e desativado com Disable(). Sem indice unico no
    /// banco: a unicidade e validada no servico.
    /// </summary>
    public class RedefinicaoSenha : BaseEntities
    {
        [Column("id_redefinicao_senha")]
        public int IdRedefinicaoSenha { get; set; }

        /// <summary>Titular cliente. Exatamente um entre IdCliente e IdFuncionario (ck_redefinicao_titular).</summary>
        [Column("id_cliente")]
        public int? IdCliente { get; set; }

        /// <summary>Titular funcionario. Exatamente um entre IdCliente e IdFuncionario (ck_redefinicao_titular).</summary>
        [Column("id_funcionario")]
        public int? IdFuncionario { get; set; }

        /// <summary>CHAR(64): SHA-256 em hexadecimal do token. O token em claro nunca vai para o banco.</summary>
        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>Fim da validade do link. Coluna timestamp sem fuso: gravar com DateTime.Now.</summary>
        [Column("expira_em")]
        public DateTime ExpiraEm { get; set; }

        // --- Navegacao ---
        public virtual Cliente? Cliente { get; set; }
        public virtual Funcionario? Funcionario { get; set; }
    }
}
