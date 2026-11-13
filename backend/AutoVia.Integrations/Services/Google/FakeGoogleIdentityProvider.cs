using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Google
{
    /// <summary>
    /// Provedor de teste (Nome = FAKE). Nao chama rede e nao verifica nenhuma prova.
    ///
    /// Credenciais: 'FAKE:&lt;email&gt;' devolve a identidade com e-mail verificado; 'FAKE:&lt;email&gt;:naoverificado'
    /// e recusada (401). sub = 'fake-' + 16 hex do SHA-256 do e-mail em minusculas (determinista); nome = parte local.
    ///
    /// TRAVA DUPLA: so funciona com IHostEnvironment.IsDevelopment() E ambiente da integracao SANDBOX. Fora disso
    /// recusa SEMPRE com IntegrationException: um FAKE aceito em producao deixaria qualquer um forjar uma conta.
    /// </summary>
    public class FakeGoogleIdentityProvider : IGoogleIdentityProvider
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogService _logService;

        public FakeGoogleIdentityProvider(IHostEnvironment hostEnvironment, ILogService logService)
        {
            _hostEnvironment = hostEnvironment;
            _logService = logService;
        }

        public string Nome => ConstantsGoogle.ProvedorFake;

        /// <summary>Aceita 'FAKE:&lt;email&gt;' (verificado) e recusa ':naoverificado'. Recusa tudo fora de Development + SANDBOX.</summary>
        public Task<GoogleIdentidade> ValidarAsync(ConfiguracaoIntegracao configuracao, string clientId, string credencial, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuracao);

            try
            {
                ValidarTrava(configuracao);

                if (string.IsNullOrWhiteSpace(credencial) || !credencial.StartsWith(ConstantsGoogle.PrefixoCredencialFake, StringComparison.Ordinal))
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoCredencialMalformada);
                }

                string corpo = credencial.Substring(ConstantsGoogle.PrefixoCredencialFake.Length);
                bool naoVerificado = corpo.EndsWith(ConstantsGoogle.SufixoNaoVerificadoFake, StringComparison.OrdinalIgnoreCase);
                string emailBruto = naoVerificado
                    ? corpo.Substring(0, corpo.Length - ConstantsGoogle.SufixoNaoVerificadoFake.Length)
                    : corpo;
                string email = emailBruto.Trim().ToLowerInvariant();

                if (!EmailValido(email))
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoClaims);
                }

                if (naoVerificado)
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoNaoVerificado);
                }

                GoogleIdentidade identidade = new GoogleIdentidade
                {
                    Sub = ConstantsGoogle.PrefixoSubFake + HashCurto(email),
                    Email = email,
                    EmailVerificado = true,
                    Nome = ParteLocal(email)
                };

                return Task.FromResult(identidade);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGoogleIndisponivel, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel, ex);
            }
        }

        /// <summary>Trava: Development e SANDBOX ao mesmo tempo, senao recusa.</summary>
        private void ValidarTrava(ConfiguracaoIntegracao configuracao)
        {
            if (!_hostEnvironment.IsDevelopment() || configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsGoogle.ChaveProvedor, ConstantsMessage.ErroGoogleFakeForaDoDesenvolvimento);
            }
        }

        private UnauthorizedException RecusarCredencial(string motivo)
        {
            _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleCredencialRecusada, Nome, motivo), GetType().ToString());
            return new UnauthorizedException(ConstantsMessage.ErroGoogleCredencialInvalida);
        }

        private static bool EmailValido(string email)
        {
            return email.Length > 0
                && email.Length <= ConstantsGoogle.TamanhoMaximoEmail
                && Regex.IsMatch(email, ConstantsEmail.PadraoEmail);
        }

        /// <summary>Primeiros 16 caracteres hexadecimais (minusculos) do SHA-256 do e-mail.</summary>
        private static string HashCurto(string email)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(email));
            string hex = Convert.ToHexString(hash).ToLowerInvariant();
            return hex.Substring(0, ConstantsGoogle.TamanhoHashSubFake);
        }

        /// <summary>Parte local do e-mail (antes do @), cortada no limite de 120 caracteres.</summary>
        private static string ParteLocal(string email)
        {
            int posicaoArroba = email.IndexOf(ConstantsGoogle.SeparadorEmail);
            string local = posicaoArroba > 0 ? email.Substring(0, posicaoArroba) : email;
            return local.Length > ConstantsGoogle.TamanhoMaximoNome ? local.Substring(0, ConstantsGoogle.TamanhoMaximoNome) : local;
        }
    }
}
