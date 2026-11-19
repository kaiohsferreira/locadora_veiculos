namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da fila de verificacao de cadastros da locadora (P2-BE-05, RF-10): filtros da fila (400), falhas internas
    /// (500, detalhe so no log) e a linha informativa de acesso aos documentos pela equipe. Nenhuma mensagem ecoa CPF, nome,
    /// e-mail, nome de arquivo, chave ou link.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>400: status da fila fora de PENDENTE, APROVADO ou RECUSADO (nome exato).</summary>
        public const string ErroClienteFilaStatusInvalido = "Status inválido. Use PENDENTE, APROVADO ou RECUSADO.";

        /// <summary>400: busca acima do limite de caracteres depois de Trim. {0} = maximo de caracteres.</summary>
        public const string ErroClienteFilaBuscaTamanho = "A busca deve ter no máximo {0} caracteres.";

        /// <summary>500: falha ao listar a fila de verificacao (detalhe so no log).</summary>
        public const string ErroListarFilaVerificacao = "Não foi possível listar a fila de verificação de cadastros.";

        /// <summary>500: falha ao obter o cadastro do cliente para verificacao (detalhe so no log).</summary>
        public const string ErroObterVerificacaoCliente = "Não foi possível consultar o cadastro do cliente para verificação.";

        /// <summary>500: falha ao listar os documentos do cliente pela equipe (detalhe so no log).</summary>
        public const string ErroListarDocumentosVerificacao = "Não foi possível listar os documentos do cliente para verificação.";

        /// <summary>Log informativo do acesso da equipe aos documentos. {0} = id do funcionario, {1} = id do cliente, {2} = quantidade. Sem dado pessoal.</summary>
        public const string InfoAcessoDocumentosVerificacao = "Funcionário {0} listou {2} documento(s) do cliente {1} na verificação de cadastro.";
    }
}
