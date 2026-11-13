namespace AutoVia.Integrations.Communication.Seguradora
{
    /// <summary>Resposta da seguradora sobre a vigencia de uma apolice numa data (RF-42 / RP-16).</summary>
    public class VigenciaApoliceResultado
    {
        /// <summary>Numero da apolice consultada.</summary>
        public string NumeroApolice { get; set; } = string.Empty;

        /// <summary>Data consultada (normalmente a data da ocorrencia).</summary>
        public DateOnly DataReferencia { get; set; }

        /// <summary>True quando a apolice cobre a data consultada.</summary>
        public bool Vigente { get; set; }

        /// <summary>Provedor que respondeu (ex.: FAKE).</summary>
        public string Provedor { get; set; } = string.Empty;
    }
}
