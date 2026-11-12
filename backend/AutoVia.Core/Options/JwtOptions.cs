using AutoVia.Helpers;

namespace AutoVia.Core.Options
{
    /// <summary>
    /// Configuracao do token JWT (secao "Jwt"). A Chave e segredo: so entra por
    /// variavel de ambiente (Jwt__Chave) ou appsettings.Local.json, nunca no git.
    /// Validada na subida da API: sem chave com 32 caracteres ou sem emissor/audiencia,
    /// a API nao sobe.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>Emissor do token (claim iss). Ex.: AutoVia.Api.</summary>
        public string Emissor { get; set; } = string.Empty;

        /// <summary>Audiencia do token (claim aud). Ex.: AutoVia.Web.</summary>
        public string Audiencia { get; set; } = string.Empty;

        /// <summary>Validade do token em minutos, contados a partir de DateTime.Now.</summary>
        public int ExpiracaoMinutos { get; set; }

        /// <summary>Validade do token com "lembrar de mim", em dias corridos (1 a 90), contados a partir de DateTime.Now. Sem renovacao.</summary>
        public int ExpiracaoLembrarDias { get; set; } = ConstantsValues.ExpiracaoLembrarDiasPadrao;

        /// <summary>Segredo HS256, com no minimo 32 caracteres. Nunca registrar nem devolver.</summary>
        public string Chave { get; set; } = string.Empty;
    }
}
