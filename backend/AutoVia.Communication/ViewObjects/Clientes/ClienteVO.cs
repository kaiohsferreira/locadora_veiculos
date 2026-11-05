using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de entrada (POST publico) do cadastro do cliente. Estrutura plana, sem id,
    /// sem status e sem campos de auditoria: o cliente nasce PENDENTE, definido no
    /// servico. A validacao e feita somente pelo ClienteVOValidator.
    /// </summary>
    public class ClienteVO
    {
        /// <summary>Nome completo do cliente (obrigatorio, sem so espacos, ate 120 caracteres).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>CPF com ou sem mascara; 11 digitos com digito verificador valido.</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Data de nascimento: nao pode ser futura e o cliente deve ter 18 anos ou mais.</summary>
        public DateOnly DataNascimento { get; set; }

        /// <summary>E-mail do cliente (obrigatorio, valido, ate 120 caracteres).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Telefone com DDD, com ou sem mascara: 10 ou 11 digitos.</summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>Numero da CNH: 11 digitos apos remover mascara.</summary>
        public string CnhNumero { get; set; } = string.Empty;

        /// <summary>Categoria da CNH (A, B, AB, C, D ou E). Anulavel no VO para que o campo omitido seja recusado (400) em vez de virar A, o zero do enum.</summary>
        public CategoriaCnh? CnhCategoria { get; set; }

        /// <summary>Validade da CNH: deve ser hoje ou uma data futura.</summary>
        public DateOnly CnhValidade { get; set; }

        /// <summary>Logradouro do endereco, quando informado (ate 120 caracteres).</summary>
        public string? Logradouro { get; set; }

        /// <summary>Numero do endereco, quando informado (ate 10 caracteres).</summary>
        public string? Numero { get; set; }

        /// <summary>Complemento do endereco, quando informado (ate 60 caracteres).</summary>
        public string? Complemento { get; set; }

        /// <summary>Bairro do endereco, quando informado (ate 60 caracteres).</summary>
        public string? Bairro { get; set; }

        /// <summary>Cidade do endereco, quando informada (ate 60 caracteres).</summary>
        public string? Cidade { get; set; }

        /// <summary>UF do endereco, com 2 letras, quando informada.</summary>
        public string? Uf { get; set; }

        /// <summary>CEP com ou sem mascara: 8 digitos, quando informado.</summary>
        public string? Cep { get; set; }

        /// <summary>Senha de acesso: 8 a 64 caracteres, com ao menos uma letra e um numero. Nunca devolvida nem gravada em texto.</summary>
        public string Senha { get; set; } = string.Empty;
    }
}
