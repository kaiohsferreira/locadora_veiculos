using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de saida do login bem-sucedido. Traz o token Bearer e os dados que o front
    /// precisa para a sessao. Nao traz CPF, senha, hash nem dados de contato.
    /// </summary>
    public class ReturnLoginVO
    {
        /// <summary>Token JWT (HS256) a ser enviado no header Authorization: Bearer.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Tipo do token: sempre Bearer.</summary>
        public string TipoToken { get; set; } = string.Empty;

        /// <summary>Instante em que o token expira.</summary>
        public DateTime ExpiraEm { get; set; }

        /// <summary>Instante de expiracao formatado em dd/MM/yyyy HH:mm.</summary>
        public string ExpiraEmStr { get; set; } = string.Empty;

        /// <summary>Perfil autenticado: CLIENTE ou FUNCIONARIO.</summary>
        public PerfilUsuario Perfil { get; set; }

        /// <summary>Identificador do usuario (id do cliente ou do funcionario).</summary>
        public int IdUsuario { get; set; }

        /// <summary>Nome do usuario.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Cargo do funcionario; nulo para cliente.</summary>
        public CargoFuncionario? Cargo { get; set; }

        /// <summary>Status do cadastro do cliente (PENDENTE, APROVADO, RECUSADO); nulo para funcionario.</summary>
        public StatusCadastroCliente? StatusCadastro { get; set; }
    }
}
