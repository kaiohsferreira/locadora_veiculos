namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Campo que a verificacao de cadastro consulta (POST /api/clientes/verificar-cadastro).
    /// Nao e persistido: so existe na entrada, onde o JSON traz o nome do membro como texto
    /// (CPF, EMAIL ou CNH).
    /// </summary>
    public enum CampoVerificacaoCadastro
    {
        CPF,
        EMAIL,
        CNH
    }
}
