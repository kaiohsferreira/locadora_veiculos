namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da biblioteca de integracoes (AutoVia.Integrations).
    /// {0} = codigo da integracao, {1} = chave do IntegrationSetting.
    /// Nunca incluir VALOR de chave nas mensagens: valores protegidos nao podem
    /// aparecer em log nem em resposta de API.
    /// </summary>
    public static class ConstantsMessageIntegration
    {
        public const string ErroCodigoNaoInformado = "O código da integração não foi informado.";
        public const string ErroIntegracaoNaoEncontrada = "A integração {0} não foi encontrada: não está cadastrada, foi desativada ou não pôde ser lida do banco (ver log).";
        public const string ErroIntegracaoInativa = "A integração {0} está inativa. Ative-a na tabela integration antes de usá-la.";
        public const string ErroChaveObrigatoriaAusente = "A integração {0} não possui a chave obrigatória {1} em integration_setting.";
        public const string ErroChaveNaoConfigurada = "A chave {1} da integração {0} ainda não foi configurada (valor DEFINIR_NO_BANCO). Preencha o valor real direto no banco.";
        public const string ErroBaseUrlInvalida = "A integração {0} está com a base_url inválida.";
        public const string ErroCarregarIntegracao = "Não foi possível carregar a configuração da integração {0}.";

        public const string ErroComunicacaoFalhaRede = "Falha de rede ao chamar o serviço externo.";
        public const string ErroComunicacaoTimeout = "O serviço externo não respondeu dentro do tempo limite.";
        public const string ErroComunicacaoDesserializar = "A resposta do serviço externo veio em um formato inesperado.";
        public const string ErroComunicacaoRequisicaoInvalida = "A requisição para o serviço externo está incompleta (URL ou método ausente).";
        public const string ErroComunicacaoInesperado = "Erro inesperado na comunicação com o serviço externo.";
    }
}
