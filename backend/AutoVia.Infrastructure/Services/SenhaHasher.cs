using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Globalization;
using System.Security.Cryptography;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Hash de senha com PBKDF2-SHA256 (600000 iteracoes, salt de 16 bytes, chave de 32).
    /// Formato gravado: v1.PBKDF2-SHA256.{iteracoes}.{salt base64}.{hash base64}.
    /// A senha em texto nunca e gravada, registrada ou devolvida. Verify compara em
    /// tempo constante e devolve false para hash nulo, vazio ou malformado.
    /// </summary>
    public class SenhaHasher : ISenhaHasher
    {
        private const string SeparadorHash = ".";
        private const int QuantidadeDePartesDoHash = 5;

        private readonly ILogService _logService;

        public SenhaHasher(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>Gera o hash PBKDF2 da senha, com salt aleatorio, no formato v1.PBKDF2-SHA256.</summary>
        public string Hash(string senha)
        {
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(ConstantsValues.HashTamanhoSalt);
                byte[] chave = Rfc2898DeriveBytes.Pbkdf2(senha, salt, ConstantsValues.HashIteracoesPbkdf2, HashAlgorithmName.SHA256, ConstantsValues.HashTamanhoChave);

                string hash = string.Join(
                    SeparadorHash,
                    ConstantsValues.HashVersao,
                    ConstantsValues.HashAlgoritmo,
                    ConstantsValues.HashIteracoesPbkdf2.ToString(CultureInfo.InvariantCulture),
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(chave));

                return hash;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGerarHashSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Confere a senha com o hash em tempo constante. Hash nulo, vazio ou malformado devolve false (nunca lanca).</summary>
        public bool Verify(string senha, string senhaHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(senhaHash))
                {
                    return false;
                }

                string[] partes = senhaHash.Split(SeparadorHash);

                if (partes.Length != QuantidadeDePartesDoHash)
                {
                    return false;
                }

                if (partes[0] != ConstantsValues.HashVersao || partes[1] != ConstantsValues.HashAlgoritmo)
                {
                    return false;
                }

                if (!int.TryParse(partes[2], NumberStyles.None, CultureInfo.InvariantCulture, out int iteracoes) || iteracoes <= 0)
                {
                    return false;
                }

                // Teto: um hash com custo acima de 2x o padrao e recusado antes de qualquer derivacao.
                if (iteracoes > ConstantsValues.HashIteracoesMaximasVerificacao)
                {
                    return false;
                }

                byte[]? salt = DecodificarBase64(partes[3]);
                byte[]? hashEsperado = DecodificarBase64(partes[4]);

                if (salt == null || salt.Length == 0)
                {
                    return false;
                }

                if (hashEsperado == null || hashEsperado.Length != ConstantsValues.HashTamanhoChave)
                {
                    return false;
                }

                byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

                return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
            }
            catch (Exception ex)
            {
                // Falha inesperada nao libera o acesso: a verificacao falha fechada.
                _logService.Write(ex, ConstantsMessage.ErroGerarHashSenha, GetType().ToString());
                return false;
            }
        }

        /// <summary>Executa uma derivacao com o mesmo custo, sem resultado, para o login nao revelar se o CPF existe.</summary>
        public void SimularVerificacao(string senha)
        {
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(ConstantsValues.HashTamanhoSalt);

                Rfc2898DeriveBytes.Pbkdf2(senha, salt, ConstantsValues.HashIteracoesPbkdf2, HashAlgorithmName.SHA256, ConstantsValues.HashTamanhoChave);
            }
            catch (Exception ex)
            {
                // A simulacao nunca muda o resultado do login: qualquer falha so e registrada.
                _logService.Write(ex, ConstantsMessage.ErroGerarHashSenha, GetType().ToString());
            }
        }

        /// <summary>Decodifica base64 sem lancar excecao; devolve nulo se o texto nao for base64 valido.</summary>
        private static byte[]? DecodificarBase64(string valor)
        {
            Span<byte> buffer = new byte[valor.Length];

            bool decodificou = Convert.TryFromBase64String(valor, buffer, out int escritos);

            if (!decodificou)
            {
                return null;
            }

            return buffer.Slice(0, escritos).ToArray();
        }
    }
}
