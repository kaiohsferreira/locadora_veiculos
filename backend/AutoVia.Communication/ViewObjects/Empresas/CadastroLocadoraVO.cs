namespace AutoVia.Communication.ViewObjects.Empresas
{
    /// <summary>
    /// VO de entrada do cadastro publico de locadora (POST /api/empresas, anonimo). Estrutura plana. NAO tem
    /// operadora, ativo, cargo, id de empresa nem datas: a empresa e o cargo do responsavel sao definidos no
    /// servico. A validacao e feita pelo CadastroLocadoraVOValidator. Senha e CPF nunca vao para o log.
    /// </summary>
    public class CadastroLocadoraVO
    {
        /// <summary>Razao social da locadora (obrigatorio, ate 150).</summary>
        public string RazaoSocial { get; set; } = string.Empty;

        /// <summary>Nome fantasia, mostrado no catalogo (obrigatorio, ate 100).</summary>
        public string NomeFantasia { get; set; } = string.Empty;

        /// <summary>CNPJ da locadora, com ou sem mascara (14 digitos com digito verificador).</summary>
        public string Cnpj { get; set; } = string.Empty;

        /// <summary>Telefone da locadora, com ou sem mascara (10 ou 11 digitos).</summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>E-mail da locadora (obrigatorio, valido, ate 120).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>CEP da sede, com ou sem mascara (8 digitos).</summary>
        public string Cep { get; set; } = string.Empty;

        /// <summary>Logradouro da sede (obrigatorio, ate 120).</summary>
        public string Logradouro { get; set; } = string.Empty;

        /// <summary>Numero do endereco (obrigatorio, ate 10).</summary>
        public string Numero { get; set; } = string.Empty;

        /// <summary>Complemento (opcional, ate 60).</summary>
        public string? Complemento { get; set; }

        /// <summary>Bairro da sede (obrigatorio, ate 60).</summary>
        public string Bairro { get; set; } = string.Empty;

        /// <summary>Cidade da sede (obrigatorio, ate 60).</summary>
        public string Cidade { get; set; } = string.Empty;

        /// <summary>UF com 2 letras (gravada em maiusculas).</summary>
        public string Uf { get; set; } = string.Empty;

        /// <summary>Nome do responsavel, que entra como ADMINISTRADOR (obrigatorio, ate 120).</summary>
        public string ResponsavelNome { get; set; } = string.Empty;

        /// <summary>CPF do responsavel, com ou sem mascara (11 digitos com digito verificador). Serve de login.</summary>
        public string ResponsavelCpf { get; set; } = string.Empty;

        /// <summary>E-mail de contato e recuperacao do responsavel (obrigatorio, valido, ate 120).</summary>
        public string ResponsavelEmail { get; set; } = string.Empty;

        /// <summary>Senha do responsavel: 8 a 64 caracteres, com ao menos uma letra maiuscula e um numero. Gravada so como hash.</summary>
        public string Senha { get; set; } = string.Empty;

        /// <summary>Aceite dos termos: deve ser verdadeiro. Validado e nao gravado.</summary>
        public bool AceiteTermos { get; set; }
    }
}
