namespace AutoVia.Integrations.Communication.Multas
{
    /// <summary>Infracao de transito normalizada, pronta para consumo pelo fluxo de locacao.</summary>
    public sealed class MultaResultado
    {
        public string IdentificadorProvedor { get; init; } = string.Empty;
        public DateTime DataHoraLocal { get; init; }
        public string? CodigoInfracao { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public decimal Valor { get; init; }
    }
}