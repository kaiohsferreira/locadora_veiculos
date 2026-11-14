using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela empresa: cada locadora cadastrada na plataforma AutoVia. Os veiculos e os
    /// funcionarios pertencem a uma empresa (id_empresa); as demais tabelas herdam o
    /// isolamento pelo veiculo.
    /// </summary>
    public class Empresa : BaseEntities
    {
        [Column("id_empresa")]
        public int IdEmpresa { get; set; }

        [Column("razao_social")]
        public string RazaoSocial { get; set; } = string.Empty;

        [Column("nome_fantasia")]
        public string NomeFantasia { get; set; } = string.Empty;

        /// <summary>CHAR(14), so digitos (ck_empresa_cnpj).</summary>
        [Column("cnpj")]
        public string Cnpj { get; set; } = string.Empty;

        [Column("telefone")]
        public string Telefone { get; set; } = string.Empty;

        /// <summary>Gravado em minusculas, para a igualdade usar o indice idx_empresa_email.</summary>
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>CHAR(8), so digitos.</summary>
        [Column("cep")]
        public string Cep { get; set; } = string.Empty;

        [Column("logradouro")]
        public string Logradouro { get; set; } = string.Empty;

        [Column("numero")]
        public string Numero { get; set; } = string.Empty;

        [Column("complemento")]
        public string? Complemento { get; set; }

        [Column("bairro")]
        public string Bairro { get; set; } = string.Empty;

        [Column("cidade")]
        public string Cidade { get; set; } = string.Empty;

        /// <summary>CHAR(2), em maiusculas.</summary>
        [Column("uf")]
        public string Uf { get; set; } = string.Empty;

        /// <summary>DEFAULT TRUE. Suspensao reversivel da locadora, separada de disabled_at.</summary>
        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        /// <summary>DEFAULT FALSE. Verdadeiro so para a empresa operadora da plataforma (empresa inicial).</summary>
        [Column("operadora")]
        public bool Operadora { get; set; }

        // --- Navegacao ---
        public virtual List<Funcionario> Funcionarios { get; set; } = new List<Funcionario>();
        public virtual List<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    }
}
