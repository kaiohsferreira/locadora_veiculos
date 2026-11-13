using AutoVia.Communication.Enums;

namespace AutoVia.Integrations.Communication.Seguradora
{
    /// <summary>
    /// Pedido de abertura de sinistro na seguradora. Validado pelo SeguradoraGateway.
    /// A mesma ReferenciaExterna devolve o mesmo protocolo (abertura idempotente).
    /// </summary>
    public class SinistroAberturaSolicitacao
    {
        /// <summary>Identificador do sinistro na AutoVia (ex.: SIN-123). Ate 64 caracteres, [A-Za-z0-9_-].</summary>
        public string ReferenciaExterna { get; set; } = string.Empty;

        /// <summary>Numero da apolice vigente na data da ocorrencia.</summary>
        public string NumeroApolice { get; set; } = string.Empty;

        /// <summary>Placa do veiculo.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Data da ocorrencia; nao pode ser futura.</summary>
        public DateOnly DataOcorrencia { get; set; }

        /// <summary>Tipo do sinistro.</summary>
        public TipoSinistro Tipo { get; set; }

        /// <summary>Descricao opcional da ocorrencia (ate 1000 caracteres).</summary>
        public string? Descricao { get; set; }

        /// <summary>Valor estimado do prejuizo, quando informado (maior que zero).</summary>
        public decimal? ValorPrejuizo { get; set; }
    }
}
