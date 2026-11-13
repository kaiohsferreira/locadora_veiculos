namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos da sessao do usuario: nome da claim de versao, prefixo do cache,
    /// faixas aceitas de configuracao e validade do "lembrar de mim".
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Secao de configuracao da sessao (Autenticacao:Sessao).</summary>
        public const string SecaoSessao = "Autenticacao:Sessao";

        /// <summary>Nome da claim que carrega a versao da sessao (HMAC da senha_hash, nao reversivel).</summary>
        public const string ClaimVersaoSessao = "sv";

        /// <summary>Prefixo da chave do cache de sessao: autovia:sessao:{perfil}:{id}.</summary>
        public const string PrefixoCacheSessao = "autovia:sessao:";

        /// <summary>TTL padrao do cache de sessao, em segundos (Autenticacao:Sessao:CacheSegundos).</summary>
        public const int SessaoCacheSegundosPadrao = 60;

        /// <summary>Menor TTL aceito para o cache de sessao, em segundos.</summary>
        public const int SessaoCacheSegundosMinimo = 1;

        /// <summary>Maior TTL aceito para o cache de sessao, em segundos.</summary>
        public const int SessaoCacheSegundosMaximo = 300;

        /// <summary>Tamanho, em bytes, da versao da sessao antes do Base64Url (16 bytes = 22 caracteres).</summary>
        public const int TamanhoVersaoSessaoBytes = 16;

        /// <summary>Validade padrao do "lembrar de mim", em dias corridos (Jwt:ExpiracaoLembrarDias).</summary>
        public const int ExpiracaoLembrarDiasPadrao = 30;

        /// <summary>Maior validade aceita para o "lembrar de mim", em dias corridos.</summary>
        public const int ExpiracaoLembrarDiasMaximo = 90;
    }
}
