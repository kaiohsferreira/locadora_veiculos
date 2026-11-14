namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Campo que a verificacao publica de locadora consulta (POST /api/empresas/verificar-cadastro).
    /// Nao e persistido: so existe na entrada, onde o JSON traz o nome do membro como texto (CNPJ ou EMAIL).
    /// </summary>
    public enum CampoVerificacaoEmpresa
    {
        CNPJ,
        EMAIL
    }
}
