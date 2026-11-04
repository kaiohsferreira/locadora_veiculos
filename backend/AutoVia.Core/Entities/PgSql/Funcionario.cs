using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela funcionario: equipe da locadora que usa o back office.</summary>
    public class Funcionario : BaseEntities
    {
        [Column("id_funcionario")]
        public int IdFuncionario { get; set; }

        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [Column("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [Column("matricula")]
        public string Matricula { get; set; } = string.Empty;

        [Column("cargo")]
        public CargoFuncionario Cargo { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("data_admissao")]
        public DateOnly DataAdmissao { get; set; }

        /// <summary>DEFAULT TRUE. Iniciado como true no C# (ver FuncionarioConfiguration).</summary>
        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        // --- Navegacao ---
        public virtual List<Locacao> Locacoes { get; set; } = new List<Locacao>();
        public virtual List<Vistoria> Vistorias { get; set; } = new List<Vistoria>();
        public virtual List<Venda> Vendas { get; set; } = new List<Venda>();
    }
}
