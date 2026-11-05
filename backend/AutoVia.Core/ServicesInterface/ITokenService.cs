using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Emissao do token JWT (HS256) apos o login. As claims sao sub, role, nome, cargo
    /// (so funcionario), jti, iss, aud, iat, nbf e exp. Nunca inclui CPF, senha ou hash.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>Gera o token do usuario autenticado; cargo deve ser informado so para funcionario.</summary>
        TokenGeradoVO GerarToken(PerfilUsuario perfil, int idUsuario, string nome, CargoFuncionario? cargo);
    }
}
