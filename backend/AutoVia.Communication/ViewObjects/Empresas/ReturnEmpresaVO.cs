namespace AutoVia.Communication.ViewObjects.Empresas
{
    /// <summary>
    /// VO de retorno do cadastro publico de locadora (201). Traz os dados da empresa e do responsavel
    /// (sem senha, sem hash e sem CPF completo) e nao devolve token: o login e feito depois em /api/auth/login.
    /// </summary>
    public class ReturnEmpresaVO
    {
        /// <summary>Id da empresa gerado pelo banco.</summary>
        public int IdEmpresa { get; set; }

        /// <summary>Razao social.</summary>
        public string RazaoSocial { get; set; } = string.Empty;

        /// <summary>Nome fantasia.</summary>
        public string NomeFantasia { get; set; } = string.Empty;

        /// <summary>CNPJ so com digitos (14).</summary>
        public string Cnpj { get; set; } = string.Empty;

        /// <summary>CNPJ formatado (00.000.000/0000-00).</summary>
        public string CnpjStr { get; set; } = string.Empty;

        /// <summary>Telefone so com digitos.</summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>E-mail da locadora, em minusculas.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>CEP so com digitos (8).</summary>
        public string Cep { get; set; } = string.Empty;

        /// <summary>Logradouro da sede.</summary>
        public string Logradouro { get; set; } = string.Empty;

        /// <summary>Numero do endereco.</summary>
        public string Numero { get; set; } = string.Empty;

        /// <summary>Complemento, quando informado.</summary>
        public string? Complemento { get; set; }

        /// <summary>Bairro da sede.</summary>
        public string Bairro { get; set; } = string.Empty;

        /// <summary>Cidade da sede.</summary>
        public string Cidade { get; set; } = string.Empty;

        /// <summary>UF em maiusculas.</summary>
        public string Uf { get; set; } = string.Empty;

        /// <summary>Situacao da empresa (nasce verdadeira).</summary>
        public bool Ativo { get; set; }

        /// <summary>Id do funcionario responsavel criado com a empresa.</summary>
        public int IdFuncionarioResponsavel { get; set; }

        /// <summary>Nome do responsavel.</summary>
        public string ResponsavelNome { get; set; } = string.Empty;

        /// <summary>E-mail de acesso e contato do responsavel, em minusculas.</summary>
        public string ResponsavelEmail { get; set; } = string.Empty;

        /// <summary>Matricula do responsavel (MAT-0001).</summary>
        public string MatriculaResponsavel { get; set; } = string.Empty;

        /// <summary>Data e hora do cadastro, no formato dd/MM/yyyy HH:mm.</summary>
        public string DataCadastroStr { get; set; } = string.Empty;
    }
}
