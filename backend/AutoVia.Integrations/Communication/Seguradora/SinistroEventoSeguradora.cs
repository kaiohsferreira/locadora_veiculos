using AutoVia.Communication.Enums;

namespace AutoVia.Integrations.Communication.Seguradora
{
    /// <summary>
    /// Uma mudanca de status do sinistro informada pela seguradora. A P9-BE-03 copia cada
    /// evento novo para sinistro_historico com origem SEGURADORA.
    /// </summary>
    public class SinistroEventoSeguradora
    {
        /// <summary>Status antes da mudanca; nulo no evento de abertura.</summary>
        public StatusSinistro? StatusAnterior { get; set; }

        /// <summary>Status depois da mudanca.</summary>
        public StatusSinistro StatusNovo { get; set; }

        /// <summary>Quando a seguradora registrou a mudanca.</summary>
        public DateTime DataAlteracao { get; set; }

        /// <summary>Observacao da seguradora, quando houver.</summary>
        public string? Observacao { get; set; }
    }
}
