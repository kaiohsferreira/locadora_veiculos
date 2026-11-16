namespace AutoVia.Integrations.Communication.Multas
{
    /// <summary>Resultado canonico de uma consulta de infracoes.</summary>
    public sealed class MultasConsultaResultado
    {
        public IReadOnlyList<MultaResultado> Infracoes { get; init; } = Array.Empty<MultaResultado>();
    }
}