namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do login com Google (BE-5, gateway de identidade). Arquivo parcial de ConstantsMessage.
    /// Textos exatos do contrato auth-google v1. Esta tarefa usa so as mensagens de validacao da prova e
    /// da integracao GOOGLE; a BE-6 acrescenta as de servico e de VO neste mesmo arquivo, abaixo do bloco.
    /// Mensagens de integracao vao ao cliente pelo middleware (502): nunca incluir chave, valor ou dado pessoal.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Prova do Google recusada (UnauthorizedException, 401) ---
        public const string ErroGoogleCredencialInvalida = "Não foi possível validar sua conta Google. Tente novamente.";

        // --- Integracao GOOGLE (IntegrationException, 502) ---
        public const string ErroGoogleIndisponivel = "Não foi possível entrar com o Google agora. Entre com CPF e senha ou tente mais tarde.";
        public const string ErroGoogleProvedorNaoSuportado = "O provedor de login com Google configurado não é suportado.";
        public const string ErroGoogleFakeForaDoDesenvolvimento = "O provedor Google FAKE só pode ser usado em desenvolvimento.";
        public const string ErroGoogleCarregarChaves = "Não foi possível obter as chaves públicas do Google.";

        // --- Configuracao publica (somente log; a API devolve habilitado = false) ---
        public const string ErroConfiguracaoGoogle = "Não foi possível consultar a configuração do login com Google.";

        // --- Busca das chaves publicas (log tecnico: status ou tipo da excecao; nunca corpo, credencial, URL ou prova) ---
        public const string LogGoogleChavesIndisponiveis = "Chaves públicas do Google indisponíveis: status HTTP {0}.";
        public const string LogGoogleFalhaBuscaChaves = "Falha ao buscar as chaves públicas do Google: tipo de exceção {0}.";
        public const string LogGoogleFalhaValidacao = "Falha inesperada ao validar a prova do Google: tipo de exceção {0}.";
        public const string LogGoogleBaseUrlInvalida = "Base URL da integração Google fora do padrão (https e host do Google): busca de chaves não realizada.";
        public const string LogGoogleChavesReservaUsada = "Chaves públicas do Google indisponíveis: usando o último conjunto válido.";

        // --- Registro tecnico (log). Provedor, motivo generico. Nunca credencial, sub, e-mail ou nome. ---
        public const string LogGoogleIdentidadeValidada = "Identidade Google validada: provedor {0}.";
        public const string LogGoogleCredencialRecusada = "Credencial Google recusada: provedor {0}, motivo {1}.";

        // --- Validacao da entrada (FluentValidation, 400) - BE-6 ---
        public const string ErroGoogleCredencialObrigatoria = "Informe a credencial do Google.";
        public const string ErroGoogleCredencialTamanho = "A credencial do Google é inválida.";

        // --- Regras do login e do vinculo com Google (BusinessException, 409) - BE-6 ---
        public const string ErroGoogleContaJaVinculada = "Esta conta Google já está vinculada a outro cadastro.";
        public const string ErroGoogleClienteJaVinculado = "Este cadastro já está vinculado a outra conta Google. Entre com CPF e senha.";
        public const string ErroGoogleContaAmbigua = "Encontramos mais de um cadastro com este e-mail. Entre com CPF e senha.";

        // --- Falhas internas do servico e do repositorio (log + 500) - BE-6 ---
        public const string ErroLoginGoogle = "Não foi possível entrar com o Google.";
        public const string ErroVincularGoogle = "Não foi possível vincular a conta Google.";
        public const string ErroBuscarVinculoGoogle = "Não foi possível consultar o vínculo da conta Google.";
        public const string ErroSalvarVinculoGoogle = "Não foi possível salvar o vínculo da conta Google.";

        // --- Registro tecnico de sucesso (so o id numerico do cliente). - BE-6 ---
        public const string LogGoogleLoginAutenticado = "Login Google: sessão aberta para o cliente {0}.";
        public const string LogGoogleVinculoGravado = "Vínculo Google gravado para o cliente {0}.";
    }
}
