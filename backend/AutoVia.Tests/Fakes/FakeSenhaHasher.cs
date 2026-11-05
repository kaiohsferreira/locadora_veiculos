using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ISenhaHasher de teste: hash deterministico ("HASH:" + senha), so para os testes
    /// conferirem que a senha nao e gravada em texto puro. Nao e PBKDF2 e nao serve
    /// fora dos testes.
    /// </summary>
    public class FakeSenhaHasher : ISenhaHasher
    {
        private const string PrefixoHash = "HASH:";

        /// <summary>Gera o hash de teste: prefixo fixo seguido da senha.</summary>
        public string Hash(string senha)
        {
            string hash = PrefixoHash + senha;

            return hash;
        }

        /// <summary>Confere a senha com o hash de teste; hash nulo ou vazio devolve false.</summary>
        public bool Verify(string senha, string senhaHash)
        {
            if (string.IsNullOrEmpty(senhaHash))
            {
                return false;
            }

            return senhaHash == PrefixoHash + senha;
        }

        /// <summary>Nada a simular no fake.</summary>
        public void SimularVerificacao(string senha)
        {
        }
    }
}
