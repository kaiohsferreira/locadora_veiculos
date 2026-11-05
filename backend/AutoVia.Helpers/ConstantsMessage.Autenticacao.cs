namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de autenticacao (login, token, autorizacao e limite de requisicoes),
    /// em portugues. Mensagens com {0} sao formatadas com string.Format.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Login ---
        /// <summary>Mesma mensagem para CPF inexistente, senha incorreta, conta sem senha e cadastro desativado (nao revela qual falhou).</summary>
        public const string ErroCredenciaisInvalidas = "CPF ou senha inválidos.";
        public const string ErroLoginPerfilInvalido = "Informe o perfil de acesso: CLIENTE ou FUNCIONARIO.";
        public const string ErroLoginCpfObrigatorio = "Informe o CPF.";
        public const string ErroLoginCpfFormato = "O CPF deve ter 11 dígitos.";
        public const string ErroLoginSenhaObrigatoria = "Informe a senha.";
        public const string ErroLoginSenhaTamanho = "A senha deve ter no máximo {0} caracteres.";

        // --- Autenticacao e autorizacao (ProblemDetails 401 e 403) ---
        public const string TituloNaoAutenticado = "Não autenticado";
        public const string ErroNaoAutenticado = "Autenticação necessária. Entre com um token válido.";
        public const string TituloAcessoNegado = "Acesso negado";
        public const string ErroAcessoNegado = "Seu perfil não tem permissão para acessar este recurso.";

        // --- Limite de requisicoes (429) ---
        public const string TituloLimiteRequisicoes = "Muitas requisições";
        public const string ErroLimiteRequisicoes = "Muitas tentativas. Aguarde alguns instantes e tente novamente.";

        // --- Falhas internas do modulo ---
        public const string ErroAutenticar = "Não foi possível autenticar o usuário.";
        public const string ErroBuscarUsuarioAcesso = "Não foi possível consultar o usuário de acesso.";
        public const string ErroSalvarUsuarioAcesso = "Não foi possível salvar o usuário de acesso.";
        public const string ErroGerarToken = "Não foi possível gerar o token de acesso.";
        public const string ErroGerarHashSenha = "Não foi possível proteger a senha.";
        public const string ErroConfiguracaoJwt = "A configuração do token (Jwt:Chave com no mínimo {0} caracteres, Jwt:Emissor, Jwt:Audiencia) está ausente ou inválida.";

        // --- Administrador inicial (configuracao, idempotente) ---
        public const string ErroCriarAdminInicial = "Não foi possível criar o administrador inicial.";
        public const string InfoAdminInicialCriado = "Administrador inicial criado.";
    }
}
