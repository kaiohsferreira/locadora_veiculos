using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de saida (GET e retorno do POST) do cliente. Traz os dados do cadastro com
    /// CPF, datas e data de cadastro formatados (preenchidos no ClienteProfile).
    /// Nao tem nenhuma propriedade de senha nem de auditoria.
    /// </summary>
    public class ReturnClienteVO
    {
        /// <summary>Identificador do cliente.</summary>
        public int IdCliente { get; set; }

        /// <summary>Nome do cliente.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>CPF somente com digitos (11 caracteres).</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>CPF formatado como 000.000.000-00.</summary>
        public string CpfStr { get; set; } = string.Empty;

        /// <summary>Data de nascimento.</summary>
        public DateOnly DataNascimento { get; set; }

        /// <summary>Data de nascimento no formato dd/MM/yyyy.</summary>
        public string DataNascimentoStr { get; set; } = string.Empty;

        /// <summary>E-mail do cliente.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Telefone somente com digitos.</summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>Numero da CNH.</summary>
        public string CnhNumero { get; set; } = string.Empty;

        /// <summary>Categoria da CNH.</summary>
        public CategoriaCnh CnhCategoria { get; set; }

        /// <summary>Validade da CNH.</summary>
        public DateOnly CnhValidade { get; set; }

        /// <summary>Validade da CNH no formato dd/MM/yyyy.</summary>
        public string CnhValidadeStr { get; set; } = string.Empty;

        /// <summary>Logradouro do endereco, quando informado.</summary>
        public string? Logradouro { get; set; }

        /// <summary>Numero do endereco, quando informado.</summary>
        public string? Numero { get; set; }

        /// <summary>Complemento do endereco, quando informado.</summary>
        public string? Complemento { get; set; }

        /// <summary>Bairro do endereco, quando informado.</summary>
        public string? Bairro { get; set; }

        /// <summary>Cidade do endereco, quando informada.</summary>
        public string? Cidade { get; set; }

        /// <summary>UF do endereco, quando informada.</summary>
        public string? Uf { get; set; }

        /// <summary>CEP somente com digitos, quando informado.</summary>
        public string? Cep { get; set; }

        /// <summary>Data e hora do cadastro.</summary>
        public DateTime DataCadastro { get; set; }

        /// <summary>Data e hora do cadastro no formato dd/MM/yyyy HH:mm.</summary>
        public string DataCadastroStr { get; set; } = string.Empty;

        /// <summary>Status do cadastro (PENDENTE, APROVADO ou RECUSADO).</summary>
        public StatusCadastroCliente StatusCadastro { get; set; }
    }
}
