using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// Detalhe do cadastro do cliente para a equipe que verifica (GET /api/clientes/{id}/verificacao, P2-BE-05). Traz os
    /// campos da fila e os dados de cadastro e de endereco. Sem senha e sem identificador de login externo. Datas no formato
    /// do projeto (dd/MM/yyyy e dd/MM/yyyy HH:mm).
    /// </summary>
    public class ReturnClienteVerificacaoVO : ReturnClienteFilaVO
    {
        /// <summary>Data de nascimento (dd/MM/yyyy).</summary>
        public string DataNascimento { get; set; } = string.Empty;

        /// <summary>E-mail do cliente.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Telefone somente com digitos.</summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>Numero da CNH completo (so ATENDENTE e ADMINISTRADOR chegam aqui).</summary>
        public string CnhNumero { get; set; } = string.Empty;

        /// <summary>Categoria da CNH.</summary>
        public CategoriaCnh CnhCategoria { get; set; }

        /// <summary>Validade da CNH (dd/MM/yyyy).</summary>
        public string CnhValidade { get; set; } = string.Empty;

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
    }
}
