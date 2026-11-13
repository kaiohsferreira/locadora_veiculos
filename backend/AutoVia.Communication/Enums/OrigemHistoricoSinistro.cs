namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Valores da coluna sinistro_historico.origem (restricao ck_sinistro_historico_origem):
    /// quem mudou o status do sinistro. Persistido como texto (HasConversion&lt;string&gt;):
    /// o NOME do membro e o valor gravado no banco. Nao reordene.
    /// Tabela fora da especificacao original: desvio aprovado pelo Kaio em 13/11/2026.
    /// </summary>
    public enum OrigemHistoricoSinistro
    {
        FUNCIONARIO,
        SEGURADORA
    }
}
