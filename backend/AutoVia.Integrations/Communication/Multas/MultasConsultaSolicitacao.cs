namespace AutoVia.Integrations.Communication.Multas
{
    /// <summary>Consulta canonica de infracoes para a locacao indicada.</summary>
    public sealed class MultasConsultaSolicitacao
    {
        public string Placa { get; init; } = string.Empty;
        public DateTime InicioLocal { get; init; }
        public DateTime FimLocal { get; init; }
    }
}