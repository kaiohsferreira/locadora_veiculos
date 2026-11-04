using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela cliente: pessoa que reserva, aluga ou compra.</summary>
    public class Cliente
    {
        [Column("id_cliente")]
        public int IdCliente { get; set; }

        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>CHAR(11), somente digitos.</summary>
        [Column("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [Column("data_nascimento")]
        public DateOnly DataNascimento { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Column("cnh_numero")]
        public string CnhNumero { get; set; } = string.Empty;

        [Column("cnh_categoria")]
        public CategoriaCnh CnhCategoria { get; set; }

        [Column("cnh_validade")]
        public DateOnly CnhValidade { get; set; }

        [Column("logradouro")]
        public string? Logradouro { get; set; }

        [Column("numero")]
        public string? Numero { get; set; }

        [Column("complemento")]
        public string? Complemento { get; set; }

        [Column("bairro")]
        public string? Bairro { get; set; }

        [Column("cidade")]
        public string? Cidade { get; set; }

        [Column("uf")]
        public string? Uf { get; set; }

        /// <summary>CHAR(8), somente digitos.</summary>
        [Column("cep")]
        public string? Cep { get; set; }

        /// <summary>DEFAULT CURRENT_TIMESTAMP.</summary>
        [Column("data_cadastro")]
        public DateTime DataCadastro { get; set; }

        /// <summary>DEFAULT 'PENDENTE'.</summary>
        [Column("status_cadastro")]
        public StatusCadastroCliente StatusCadastro { get; set; } = StatusCadastroCliente.PENDENTE;

        // --- Navegacao ---
        public virtual List<Reserva> Reservas { get; set; } = new List<Reserva>();
        public virtual List<Locacao> Locacoes { get; set; } = new List<Locacao>();
        public virtual List<Venda> Vendas { get; set; } = new List<Venda>();
    }
}
