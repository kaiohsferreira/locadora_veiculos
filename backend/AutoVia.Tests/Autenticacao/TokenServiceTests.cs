using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do TokenService: claims do token, expiracao a partir de DateTime.Now, validacao
    /// com os parametros da API (chave, emissor, audiencia, validade) e ida e volta
    /// (token gerado, validado e lido como NameIdentifier). A chave e gerada no proprio teste.
    /// </summary>
    public class TokenServiceTests
    {
        private static readonly string ChaveDeTeste = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        private const string EmissorDeTeste = "AutoVia.Api";
        private const string AudienciaDeTeste = "AutoVia.Web";

        private static JwtOptions OpcoesPadrao(int expiracaoMinutos = 120)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = EmissorDeTeste,
                Audiencia = AudienciaDeTeste,
                ExpiracaoMinutos = expiracaoMinutos,
                Chave = ChaveDeTeste
            };

            return opcoes;
        }

        private static async Task<TokenValidationResult> ValidarAsync(string token, string chave)
        {
            JsonWebTokenHandler handler = new JsonWebTokenHandler
            {
                MapInboundClaims = true
            };

            string[] algoritmos = new string[] { SecurityAlgorithms.HmacSha256 };

            TokenValidationParameters parametros = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = EmissorDeTeste,
                ValidateAudience = true,
                ValidAudience = AudienciaDeTeste,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                ValidAlgorithms = algoritmos,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            TokenValidationResult resultado = await handler.ValidateTokenAsync(token, parametros);

            return resultado;
        }

        [Fact]
        public async Task GerarToken_Cliente_TemClaimsEsperadasESemDadosSensiveis()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 42, "Maria Cliente", null);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            ClaimsIdentity identidade = resultado.ClaimsIdentity;
            Assert.Equal("CLIENTE", identidade.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("Maria Cliente", identidade.FindFirst(ConstantsValues.ClaimNome)?.Value);
            Assert.Null(identidade.FindFirst(ConstantsValues.ClaimCargo));
            Assert.False(identidade.HasClaim(claim => claim.Type.Contains("cpf", StringComparison.OrdinalIgnoreCase)));
            Assert.False(identidade.HasClaim(claim => claim.Type.Contains("senha", StringComparison.OrdinalIgnoreCase)));
            Assert.False(identidade.HasClaim(claim => claim.Type.Contains("hash", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task GerarToken_Funcionario_IncluiCargo()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.FUNCIONARIO, 7, "Ana Funcionaria", CargoFuncionario.GESTOR_FROTA);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            Assert.Equal("FUNCIONARIO", resultado.ClaimsIdentity.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("GESTOR_FROTA", resultado.ClaimsIdentity.FindFirst(ConstantsValues.ClaimCargo)?.Value);
        }

        [Fact]
        public async Task IdaEVolta_NameIdentifierDoTokenEIgualAoIdDoUsuario()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.FUNCIONARIO, 1234, "Carlos", CargoFuncionario.ATENDENTE);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            Assert.Equal("1234", resultado.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public async Task Expiracao_CalculadaApartirDoRelogioLocal()
        {
            TokenService servico = new TokenService(OpcoesPadrao(120), new FakeLogService());
            DateTime antes = DateTime.Now;

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null);
            DateTime depois = DateTime.Now;
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            JsonWebToken jwt = Assert.IsType<JsonWebToken>(resultado.SecurityToken);
            DateTime esperadoMinimo = antes.AddMinutes(120).ToUniversalTime();
            DateTime esperadoMaximo = depois.AddMinutes(120).ToUniversalTime();
            Assert.InRange(jwt.ValidTo, esperadoMinimo.AddSeconds(-5), esperadoMaximo.AddSeconds(5));
            Assert.InRange(gerado.ExpiraEm, antes.AddMinutes(120).AddSeconds(-5), depois.AddMinutes(120).AddSeconds(5));
        }

        [Fact]
        public async Task Token_AssinadoComOutraChave_NaoValida()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());
            string outraChave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, outraChave);

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public async Task Token_Expirado_NaoValida()
        {
            TokenService servico = new TokenService(OpcoesPadrao(-10), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.False(resultado.IsValid);
        }
    }
}
