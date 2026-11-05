using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Emite o JWT assinado em HS256 com JsonWebTokenHandler. A expiracao e calculada a
    /// partir de DateTime.Now (R5); a biblioteca converte para UTC no token. As claims
    /// sao sub (id), role (perfil), nome, cargo (so funcionario) e jti; nunca CPF, senha ou hash.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _options;
        private readonly ILogService _logService;
        private readonly JsonWebTokenHandler _tokenHandler;

        public TokenService(JwtOptions options, ILogService logService)
        {
            _options = options;
            _logService = logService;
            _tokenHandler = new JsonWebTokenHandler();
        }

        /// <summary>Gera o token do usuario autenticado; cargo deve ser informado so para funcionario.</summary>
        public TokenGeradoVO GerarToken(PerfilUsuario perfil, int idUsuario, string nome, CargoFuncionario? cargo)
        {
            try
            {
                DateTime agora = DateTime.Now;
                DateTime expiraEm = agora.AddMinutes(_options.ExpiracaoMinutos);

                Dictionary<string, object> claims = new Dictionary<string, object>
                {
                    { JwtRegisteredClaimNames.Sub, idUsuario.ToString(CultureInfo.InvariantCulture) },
                    { ConstantsValues.ClaimRole, perfil.ToString() },
                    { ConstantsValues.ClaimNome, nome },
                    { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
                };

                if (cargo.HasValue)
                {
                    claims[ConstantsValues.ClaimCargo] = cargo.Value.ToString();
                }

                SigningCredentials credenciais = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Chave)),
                    SecurityAlgorithms.HmacSha256);

                SecurityTokenDescriptor descritor = new SecurityTokenDescriptor
                {
                    Issuer = _options.Emissor,
                    Audience = _options.Audiencia,
                    IssuedAt = agora,
                    NotBefore = agora,
                    Expires = expiraEm,
                    Claims = claims,
                    SigningCredentials = credenciais
                };

                string token = _tokenHandler.CreateToken(descritor);

                TokenGeradoVO tokenGerado = new TokenGeradoVO
                {
                    Token = token,
                    ExpiraEm = expiraEm
                };

                return tokenGerado;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGerarToken, GetType().ToString());
                throw;
            }
        }
    }
}
