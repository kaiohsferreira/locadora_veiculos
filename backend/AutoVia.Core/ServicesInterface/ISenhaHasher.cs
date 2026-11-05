namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Protecao de senha com PBKDF2-SHA256. Guarda e compara somente o hash, nunca a
    /// senha em texto. Formato do hash (auth.contract.json, v1):
    /// v1.PBKDF2-SHA256.{iteracoes}.{salt base64}.{hash base64}.
    /// </summary>
    public interface ISenhaHasher
    {
        /// <summary>Gera o hash PBKDF2 da senha, com salt aleatorio, no formato v1.PBKDF2-SHA256.</summary>
        string Hash(string senha);

        /// <summary>Confere a senha com o hash em tempo constante. Hash nulo, vazio ou malformado devolve false.</summary>
        bool Verify(string senha, string senhaHash);

        /// <summary>Executa uma verificacao sem resultado, com o mesmo custo, para o login nao revelar se o CPF existe.</summary>
        void SimularVerificacao(string senha);
    }
}
