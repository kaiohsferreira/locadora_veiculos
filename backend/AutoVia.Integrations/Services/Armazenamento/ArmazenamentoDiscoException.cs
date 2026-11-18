namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Motivo interno de uma recusa do provedor de disco (ED-02 a ED-08). Message = a constante LogArmazenamentoDisco* ou
    /// LogArmazenamentoLeitura*, sem caminho, chave ou valor de configuracao. Nunca sai da integracao: o consumidor registra
    /// o motivo e responde a mensagem da operacao.
    /// </summary>
    internal sealed class ArmazenamentoDiscoException : Exception
    {
        internal ArmazenamentoDiscoException(string motivo)
            : base(motivo)
        {
        }
    }
}
