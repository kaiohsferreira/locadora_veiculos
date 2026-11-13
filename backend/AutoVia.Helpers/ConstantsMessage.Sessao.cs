namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da sessao do usuario: conferencia do token a cada requisicao e o login
    /// com "lembrar de mim" (RF-50). Nenhuma delas revela qual dado falhou.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Falha ao consultar a sessao (banco ou cache). A requisicao e recusada com 401.</summary>
        public const string ErroValidarSessao = "Não foi possível validar a sessão do usuário.";

        /// <summary>Conta desativada, senha trocada desde a emissao do token, ou token sem a versao da sessao.</summary>
        public const string ErroSessaoInvalida = "A sessão do usuário não é mais válida.";

        /// <summary>Falha ao calcular a versao da sessao (claim sv) na emissao do token.</summary>
        public const string ErroCalcularVersaoSessao = "Não foi possível proteger a sessão do usuário.";

        /// <summary>Jwt:ExpiracaoLembrarDias ou Autenticacao:Sessao:CacheSegundos fora da faixa aceita: a API nao sobe.</summary>
        public const string ErroConfiguracaoSessao = "A configuração da sessão (Jwt:ExpiracaoLembrarDias e Autenticacao:Sessao:CacheSegundos) está ausente ou inválida.";
    }
}
