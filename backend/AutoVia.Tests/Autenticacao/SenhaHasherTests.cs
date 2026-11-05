using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Globalization;
using System.Security.Cryptography;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do SenhaHasher (PBKDF2-SHA256): formato v1 gravado, verificacao correta e
    /// errada, Verify sem excecao para hash invalido e senha nunca registrada no log.
    /// </summary>
    public class SenhaHasherTests
    {
        private const string SenhaDeTeste = "Senha#Forte2026";

        [Fact]
        public void Hash_GeraFormatoV1ComIteracoesSaltEChave()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            string hash = hasher.Hash(SenhaDeTeste);

            string[] partes = hash.Split('.');
            Assert.Equal(5, partes.Length);
            Assert.Equal(ConstantsValues.HashVersao, partes[0]);
            Assert.Equal(ConstantsValues.HashAlgoritmo, partes[1]);
            Assert.Equal(ConstantsValues.HashIteracoesPbkdf2, int.Parse(partes[2]));
            Assert.Equal(ConstantsValues.HashTamanhoSalt, Convert.FromBase64String(partes[3]).Length);
            Assert.Equal(ConstantsValues.HashTamanhoChave, Convert.FromBase64String(partes[4]).Length);
            Assert.DoesNotContain(SenhaDeTeste, hash);
        }

        [Fact]
        public void Verify_SenhaCorreta_DevolveVerdadeiro()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());
            string hash = hasher.Hash(SenhaDeTeste);

            bool confere = hasher.Verify(SenhaDeTeste, hash);

            Assert.True(confere);
        }

        [Fact]
        public void Verify_SenhaErrada_DevolveFalso()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());
            string hash = hasher.Hash(SenhaDeTeste);

            bool confere = hasher.Verify("outra-senha", hash);

            Assert.False(confere);
        }

        [Fact]
        public void Hash_MesmaSenhaDuasVezes_GeraSaltsDiferentes()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            string primeiro = hasher.Hash(SenhaDeTeste);
            string segundo = hasher.Hash(SenhaDeTeste);

            Assert.NotEqual(primeiro, segundo);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("lixo")]
        [InlineData("v2.PBKDF2-SHA256.600000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("v1.PBKDF2-SHA512.600000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("v1.PBKDF2-SHA256.abc.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("v1.PBKDF2-SHA256.0.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("v1.PBKDF2-SHA256.600000.!!!.!!!")]
        [InlineData("v1.PBKDF2-SHA256.600000.AAAAAAAAAAAAAAAAAAAAAA==.AAAA")]
        public void Verify_HashNuloVazioOuMalformado_DevolveFalsoSemLancar(string senhaHash)
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            bool confere = hasher.Verify(SenhaDeTeste, senhaHash);

            Assert.False(confere);
        }

        [Fact]
        public void Verify_HashNulo_DevolveFalsoSemLancar()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            bool confere = hasher.Verify(SenhaDeTeste, null!);

            Assert.False(confere);
        }

        [Fact]
        public void Verify_IteracoesAcimaDoTeto_DevolveFalsoMesmoComASenhaCorreta()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());
            byte[] salt = RandomNumberGenerator.GetBytes(ConstantsValues.HashTamanhoSalt);
            int iteracoesAcimaDoTeto = ConstantsValues.HashIteracoesMaximasVerificacao + 1;
            byte[] chave = Rfc2898DeriveBytes.Pbkdf2(SenhaDeTeste, salt, iteracoesAcimaDoTeto, HashAlgorithmName.SHA256, ConstantsValues.HashTamanhoChave);
            string hashAcimaDoTeto = string.Join(
                ".",
                ConstantsValues.HashVersao,
                ConstantsValues.HashAlgoritmo,
                iteracoesAcimaDoTeto.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(chave));

            bool confere = hasher.Verify(SenhaDeTeste, hashAcimaDoTeto);

            Assert.False(confere);
        }

        [Fact]
        public void SimularVerificacao_NaoLancaNemRegistraNoLog()
        {
            FakeLogService log = new FakeLogService();
            SenhaHasher hasher = new SenhaHasher(log);

            hasher.SimularVerificacao(SenhaDeTeste);

            Assert.Empty(log.Entries);
        }

        [Fact]
        public void OperacoesComSenha_NaoRegistramSenhaNemHashNoLog()
        {
            FakeLogService log = new FakeLogService();
            SenhaHasher hasher = new SenhaHasher(log);
            string hash = hasher.Hash(SenhaDeTeste);

            hasher.Verify(SenhaDeTeste, hash);
            hasher.Verify("errada", hash);

            Assert.Empty(log.Entries);
        }
    }
}
