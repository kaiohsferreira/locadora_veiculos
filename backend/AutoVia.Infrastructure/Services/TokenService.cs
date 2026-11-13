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
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Emite o JWT assinado em HS256 com JsonWebTokenHandler. A expiracao e calculada a
    /// partir de DateTime.Now (R5); a biblioteca converte para UTC no token. As claims
    /// sao sub (id), role (perfil), nome, cargo (so funcionario), sv (versao da sessao), jti.
    /// Nunca CPF, senha ou hash. A versao da sessao e um HMAC da senha_hash: nao reversivel.
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

        /// <summary>Gera o token do usuario autenticado. Cargo so para funcionario. lembrar=true usa Jwt:ExpiracaoLembrarDias; false usa Jwt:ExpiracaoMinutos.</summary>
        public TokenGeradoVO GerarToken(PerfilUsuario perfil, int idUsuario, string nome, CargoFuncionario? cargo, string versaoSessao, bool lembrar)
        {
            try
            {
                DateTime agora = DateTime.Now;
                DateTime expiraEm = lembrar
                    ? agora.AddDays(_options.ExpiracaoLembrarDias)
                    : agora.AddMinutes(_options.ExpiracaoMinutos);

                Dictionary<string, object> claims = new Dictionary<string, object>
                {
                    { JwtRegisteredClaimNames.Sub, idUsuario.ToString(CultureInfo.InvariantCulture) },
                    { ConstantsValues.ClaimRole, perfil.ToString() },
                    { ConstantsValues.ClaimNome, nome },
                    { ConstantsValues.ClaimVersaoSessao, versaoSessao },
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

        /// <summary>Calcula a versao da sessao (claim sv): Base64Url dos 16 primeiros bytes de HMAC-SHA256(Jwt:Chave, senhaHash). Nunca e registrada.</summary>
        public string CalcularVersaoSessao(string senhaHash)
        {
            try
            {
                byte[] chave = Encoding.UTF8.GetBytes(_options.Chave);
                byte[] senha = Encoding.UTF8.GetBytes(senhaHash);

                using HMACSHA256 hmac = new HMACSHA256(chave);
                byte[] hash = hmac.ComputeHash(senha);

                byte[] versao = new byte[ConstantsValues.TamanhoVersaoSessaoBytes];
                Array.Copy(hash, versao, versao.Length);

                return ToBase64Url(versao);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularVersaoSessao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Base64 sem preenchimento e com os caracteres seguros para URL (RFC 4648, secao 5).</summary>
        private static string ToBase64Url(byte[] bytes)
        {
            string base64 = Convert.ToBase64String(bytes);

            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
