using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Emissao do token JWT (HS256) apos o login. As claims sao sub, role, nome, cargo
    /// (so funcionario), sv (versao da sessao), jti, iss, aud, iat, nbf e exp. Nunca inclui
    /// CPF, senha ou hash.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>Gera o token do usuario autenticado. Cargo so para funcionario. lembrar=true usa Jwt:ExpiracaoLembrarDias; false usa Jwt:ExpiracaoMinutos.</summary>
        TokenGeradoVO GerarToken(PerfilUsuario perfil, int idUsuario, string nome, CargoFuncionario? cargo, string versaoSessao, bool lembrar);

        /// <summary>Calcula a versao da sessao (claim sv): Base64Url dos 16 primeiros bytes de HMAC-SHA256(Jwt:Chave, senhaHash). Muda quando a senha muda.</summary>
        string CalcularVersaoSessao(string senhaHash);
    }
}
