namespace AutoVia.Integrations.Communication.Armazenamento
{
    /// <summary>
    /// Falha nao prevista do servico de leitura, sanitizada: so o nome do tipo da excecao original, sem inner. Lancada
    /// DEPOIS de o servico registrar a falha; quem a captura (a acao da rota) nao registra de novo.
    /// </summary>
    public sealed class ArmazenamentoLeituraException : Exception
    {
        public ArmazenamentoLeituraException(string nomeDoTipoOriginal)
            : base(nomeDoTipoOriginal)
        {
        }
    }
}
