namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Situacao devolvida pelo login com Google (N3). Nao e persistida: vai na resposta como texto.
    /// AUTENTICADO traz a sessao; as demais nao abrem sessao e nao gravam nada.
    /// </summary>
    public enum SituacaoLoginGoogle
    {
        AUTENTICADO,
        CONFIRMACAO_NECESSARIA,
        CADASTRO_NECESSARIO,
        CONTA_AMBIGUA
    }
}
