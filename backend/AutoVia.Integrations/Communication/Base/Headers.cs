namespace AutoVia.Integrations.Communication.Base
{
    /// <summary>Cabecalhos HTTP de uma chamada externa (nome sem diferenciar maiusculas).</summary>
    public class Headers : Dictionary<string, string>
    {
        public Headers()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
