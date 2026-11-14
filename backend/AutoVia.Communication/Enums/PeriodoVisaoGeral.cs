namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Periodos aceitos pelo parametro "periodo" de GET /api/painel/visao-geral (painel.contract.json). Nao e gravado no
    /// banco: trafega como texto (JsonStringEnumConverter), com o nome do membro. O padrao quando o parametro e omitido
    /// esta no FiltroVisaoGeralVO (TRINTA_DIAS).
    /// </summary>
    public enum PeriodoVisaoGeral
    {
        HOJE,
        SETE_DIAS,
        TRINTA_DIAS,
        MES_ATUAL
    }
}
