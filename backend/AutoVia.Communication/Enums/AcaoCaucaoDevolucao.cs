namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Acao sobre a caucao no encerramento da locacao (caucao-devolucao v4). Nao e persistida: o banco guarda so o status do
    /// pagamento CAUCAO. Aparece na resposta do encerramento como texto.
    /// </summary>
    public enum AcaoCaucaoDevolucao
    {
        SEM_CAUCAO,
        BAIXADA,
        RETIDA,
        RETIDA_PARCIAL,
        NAO_CONFIRMADA_NO_PROVEDOR
    }
}
