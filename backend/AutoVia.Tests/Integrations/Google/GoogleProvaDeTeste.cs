using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Integrations.Google
{
    /// <summary>
    /// Apoio dos testes do login com Google. CHAVES RSA DE TESTE, geradas em memoria: nao sao do Google e
    /// nunca saem deste processo. Os JWKS e as provas assinadas sao montados aqui e servidos pelo
    /// ComunicacaoGoogleFalsa, que substitui a rede. Nenhum teste chama o Google.
    /// </summary>
    public static class GoogleProvaDeTeste
    {
        public const string ClientId = "123456-teste.apps.googleusercontent.com";
        public const string Emissor = "https://accounts.google.com";
        public const string KidPrincipal = "kid-teste-principal";
        public const string KidSecundaria = "kid-teste-secundaria";
        public const string Sub = "109876543210";
        public const string EmailMisto = "Maria.Teste@Exemplo.com";
        public const string EmailMinusculo = "maria.teste@exemplo.com";
        public const string NomeTeste = "Maria Teste";

        /// <summary>Chave que assina as provas validas e cujo JWKS o teste serve.</summary>
        public static readonly RSA ChavePrincipal = RSA.Create(2048);

        /// <summary>Chave de outro emissor: usada para provas com assinatura que nao bate com o JWKS.</summary>
        public static readonly RSA ChaveSecundaria = RSA.Create(2048);

        /// <summary>
        /// Trava de TODA operacao com as chaves acima (exportar, assinar). As classes de teste rodam em paralelo e
        /// compartilham estas instancias RSA; sem a trava, uma assinatura concorrente com a exportacao do JWKS
        /// produzia prova reprovada por assinatura de forma intermitente. A verificacao nao usa a instancia de
        /// assinatura (so o JWKS exportado), por isso nao precisa desta trava.
        /// </summary>
        private static readonly object TravaChaves = new object();

        /// <summary>Claims de uma prova valida. O teste altera o que quer quebrar (remove, troca o valor).</summary>
        public static Dictionary<string, object> ClaimsPadrao()
        {
            long agora = DateTimeOffset.Now.ToUnixTimeSeconds();

            return new Dictionary<string, object>
            {
                { "iss", Emissor },
                { "aud", ClientId },
                { "sub", Sub },
                { "email", EmailMisto },
                { "email_verified", true },
                { "name", NomeTeste },
                { "iat", agora },
                { "exp", agora + 3600 }
            };
        }

        /// <summary>JWKS (conjunto de chaves publicas) com as chaves informadas, no formato do Google.</summary>
        public static string MontarJwks(params (RSA Chave, string Kid)[] chaves)
        {
            List<Dictionary<string, string>> keys = new List<Dictionary<string, string>>();

            foreach ((RSA chave, string kid) in chaves)
            {
                RSAParameters parametros;
                lock (TravaChaves)
                {
                    parametros = chave.ExportParameters(false);
                }

                keys.Add(new Dictionary<string, string>
                {
                    { "kty", "RSA" },
                    { "kid", kid },
                    { "use", "sig" },
                    { "alg", "RS256" },
                    { "n", Base64UrlEncoder.Encode(parametros.Modulus!) },
                    { "e", Base64UrlEncoder.Encode(parametros.Exponent!) }
                });
            }

            return JsonSerializer.Serialize(new Dictionary<string, object> { { "keys", keys } });
        }

        /// <summary>Assina a prova com RS256 e o kid informado (o cabecalho leva o kid da chave).</summary>
        public static string AssinarRs256(RSA chave, string kid, IDictionary<string, object> claims)
        {
            SigningCredentials credenciais = new SigningCredentials(
                new RsaSecurityKey(chave) { KeyId = kid },
                SecurityAlgorithms.RsaSha256);

            lock (TravaChaves)
            {
                return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(claims), credenciais);
            }
        }

        /// <summary>Assina com HS256 usando a chave publica como segredo (ataque classico de troca de algoritmo).</summary>
        public static string AssinarHs256(RSA chavePublica, string kid, IDictionary<string, object> claims)
        {
            byte[] segredo;
            lock (TravaChaves)
            {
                segredo = chavePublica.ExportRSAPublicKey();
            }

            SigningCredentials credenciais = new SigningCredentials(
                new SymmetricSecurityKey(segredo) { KeyId = kid },
                SecurityAlgorithms.HmacSha256);

            return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(claims), credenciais);
        }

        /// <summary>Prova com alg 'none' e sem assinatura.</summary>
        public static string MontarSemAssinatura(string kid, IDictionary<string, object> claims)
        {
            string cabecalho = JsonSerializer.Serialize(new Dictionary<string, string> { { "alg", "none" }, { "typ", "JWT" }, { "kid", kid } });
            string corpo = JsonSerializer.Serialize(claims);

            return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(cabecalho)) + "." + Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(corpo)) + ".";
        }

        /// <summary>Configuracao de integracao GOOGLE para os testes de provedor (so codigo, ambiente, URL e timeout importam).</summary>
        public static ConfiguracaoIntegracao ConfiguracaoGoogle(AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Google,
                "Login com Google",
                ambiente,
                "https://www.googleapis.com",
                15000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Integracao GOOGLE do banco (em memoria) com os settings informados.</summary>
        public static Integration IntegracaoGoogle(string? provedor, string? clientId, bool ativo = true, AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX)
        {
            Integration integracao = new Integration
            {
                Codigo = ConstantsIntegration.Google,
                Nome = "Login com Google",
                Ambiente = ambiente,
                BaseUrl = "https://www.googleapis.com",
                TimeoutMs = 15000,
                Ativo = ativo
            };

            if (provedor != null)
            {
                integracao.Settings.Add(new IntegrationSetting { Chave = ConstantsGoogle.ChaveProvedor, Valor = provedor, Protegido = false });
            }

            if (clientId != null)
            {
                integracao.Settings.Add(new IntegrationSetting { Chave = ConstantsGoogle.ChaveClientId, Valor = clientId, Protegido = false });
            }

            return integracao;
        }

        /// <summary>IComunicacaoService falso: devolve o JWKS configurado ou falha de rede, e conta as chamadas.</summary>
        public sealed class ComunicacaoGoogleFalsa : IComunicacaoService
        {
            public string Jwks { get; set; } = string.Empty;

            public bool Falhar { get; set; }

            public int Chamadas { get; private set; }

            public List<RequestInfo> Requisicoes { get; } = new List<RequestInfo>();

            public Task<RequestResponse<T>> SendAsync<T>(RequestInfo request, CancellationToken cancellationToken = default)
            {
                Chamadas++;
                Requisicoes.Add(request);

                if (Falhar)
                {
                    return Task.FromResult(new RequestResponse<T> { StatusCode = 0, IsNetworkFailure = true });
                }

                RequestResponse<T> resposta = new RequestResponse<T>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    RawContent = Jwks,
                    Data = (T?)(object?)Jwks,
                    Attempts = 1
                };

                return Task.FromResult(resposta);
            }
        }

        /// <summary>IHostEnvironment de teste: ambiente escolhido pelo teste.</summary>
        public sealed class AmbienteFalso : IHostEnvironment
        {
            public AmbienteFalso(string nomeAmbiente)
            {
                EnvironmentName = nomeAmbiente;
                ContentRootPath = Path.GetTempPath();
                ContentRootFileProvider = new NullFileProvider();
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "AutoVia.Tests";

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
        }
    }
}
