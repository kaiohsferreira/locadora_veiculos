using AutoVia.Communication.Enums;

namespace AutoVia.Integrations.Communication.Seguradora
{
    /// <summary>
    /// Situacao do sinistro na seguradora: protocolo, status atual e a linha do tempo
    /// (eventos em ordem cronologica; o primeiro e a abertura).
    /// </summary>
    public class SinistroSeguradoraResultado
    {
        /// <summary>Protocolo do sinistro na seguradora (gravado em sinistro.protocolo_seguradora).</summary>
        public string Protocolo { get; set; } = string.Empty;

        /// <summary>Referencia da AutoVia enviada na abertura.</summary>
        public string ReferenciaExterna { get; set; } = string.Empty;

        /// <summary>Status atual do sinistro na seguradora.</summary>
        public StatusSinistro Status { get; set; }

        /// <summary>Mudancas de status em ordem cronologica.</summary>
        public List<SinistroEventoSeguradora> Eventos { get; set; } = new List<SinistroEventoSeguradora>();

        /// <summary>Provedor que respondeu (ex.: FAKE).</summary>
        public string Provedor { get; set; } = string.Empty;
    }
}
