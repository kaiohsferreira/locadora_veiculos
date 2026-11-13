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
    /// Testes do TokenService: claims do token (inclusive sv), expiracao de 120 minutos ou de
    /// 30 dias (lembrar de mim), calculo da versao da sessao, validacao com os parametros da API
    /// (chave, emissor, audiencia, validade) e ida e volta. A chave e gerada no proprio teste.
    /// </summary>
    public class TokenServiceTests
    {
        private static readonly string ChaveDeTeste = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        private const string EmissorDeTeste = "AutoVia.Api";
        private const string AudienciaDeTeste = "AutoVia.Web";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string SenhaHashA = "v1.PBKDF2-SHA256.600000.salt-a.hash-a";
        private const string SenhaHashB = "v1.PBKDF2-SHA256.600000.salt-b.hash-b";
        private const int TamanhoVersaoEsperado = 22;

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

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 42, "Maria Cliente", null, VersaoSessaoDeTeste, false);
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

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.FUNCIONARIO, 7, "Ana Funcionaria", CargoFuncionario.GESTOR_FROTA, VersaoSessaoDeTeste, false);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            Assert.Equal("FUNCIONARIO", resultado.ClaimsIdentity.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("GESTOR_FROTA", resultado.ClaimsIdentity.FindFirst(ConstantsValues.ClaimCargo)?.Value);
        }

        [Fact]
        public async Task IdaEVolta_NameIdentifierDoTokenEIgualAoIdDoUsuario()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.FUNCIONARIO, 1234, "Carlos", CargoFuncionario.ATENDENTE, VersaoSessaoDeTeste, false);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            Assert.Equal("1234", resultado.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public async Task Expiracao_CalculadaApartirDoRelogioLocal()
        {
            TokenService servico = new TokenService(OpcoesPadrao(120), new FakeLogService());
            DateTime antes = DateTime.Now;

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null, VersaoSessaoDeTeste, false);
            DateTime depois = DateTime.Now;
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            JsonWebToken jwt = Assert.IsType<JsonWebToken>(resultado.SecurityToken);
            DateTime esperadoMinimo = antes.AddMinutes(120).ToUniversalTime();
            DateTime esperadoMaximo = depois.AddMinutes(120).ToUniversalTime();
            Assert.InRange(jwt.ValidTo, esperadoMinimo.AddSeconds(-5), esperadoMaximo.AddSeconds(5));
            Assert.InRange(gerado.ExpiraEm, antes.AddMinutes(120).AddSeconds(-5), depois.AddMinutes(120).AddSeconds(5));
        }

        [Fact]
        public async Task LembrarDeMim_ExpiracaoDe30DiasCorridos()
        {
            TokenService servico = new TokenService(OpcoesPadrao(120), new FakeLogService());
            DateTime antes = DateTime.Now;

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null, VersaoSessaoDeTeste, true);
            DateTime depois = DateTime.Now;
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.True(resultado.IsValid);
            JsonWebToken jwt = Assert.IsType<JsonWebToken>(resultado.SecurityToken);
            Assert.InRange(jwt.ValidTo, antes.AddDays(30).ToUniversalTime().AddSeconds(-5), depois.AddDays(30).ToUniversalTime().AddSeconds(5));
            Assert.InRange(gerado.ExpiraEm, antes.AddDays(30).AddSeconds(-5), depois.AddDays(30).AddSeconds(5));
        }

        [Fact]
        public async Task Sv_PresenteNoTokenEIgualAVersaoInformada()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());
            string versao = servico.CalcularVersaoSessao(SenhaHashA);

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null, versao, false);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.Equal(versao, resultado.ClaimsIdentity.FindFirst(ConstantsValues.ClaimVersaoSessao)?.Value);
        }

        [Fact]
        public void CalcularVersaoSessao_MesmaSenhaHash_DevolveMesmoValor()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            string primeira = servico.CalcularVersaoSessao(SenhaHashA);
            string segunda = servico.CalcularVersaoSessao(SenhaHashA);

            Assert.Equal(primeira, segunda);
        }

        [Fact]
        public void CalcularVersaoSessao_SenhaHashDiferente_MudaAVersao()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            string antiga = servico.CalcularVersaoSessao(SenhaHashA);
            string nova = servico.CalcularVersaoSessao(SenhaHashB);

            Assert.NotEqual(antiga, nova);
        }

        [Fact]
        public void CalcularVersaoSessao_EmBase64UrlDe16Bytes_NaoExpoeOHash()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());

            string versao = servico.CalcularVersaoSessao(SenhaHashA);

            Assert.Equal(TamanhoVersaoEsperado, versao.Length);
            Assert.DoesNotContain('+', versao);
            Assert.DoesNotContain('/', versao);
            Assert.DoesNotContain('=', versao);
            Assert.DoesNotContain(SenhaHashA, versao);
        }

        [Fact]
        public async Task Token_AssinadoComOutraChave_NaoValida()
        {
            TokenService servico = new TokenService(OpcoesPadrao(), new FakeLogService());
            string outraChave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null, VersaoSessaoDeTeste, false);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, outraChave);

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public async Task Token_Expirado_NaoValida()
        {
            TokenService servico = new TokenService(OpcoesPadrao(-10), new FakeLogService());

            TokenGeradoVO gerado = servico.GerarToken(PerfilUsuario.CLIENTE, 1, "Maria", null, VersaoSessaoDeTeste, false);
            TokenValidationResult resultado = await ValidarAsync(gerado.Token, ChaveDeTeste);

            Assert.False(resultado.IsValid);
        }
    }
}
