namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>Estado de uma caucao devolvido pelo gateway. Datas em hora local.</summary>
    public class CaucaoResultado
    {
        /// <summary>Identificador da caucao no provedor (FAKE-CAU-... ou id numerico do Mercado Pago como texto).</summary>
        public string IdentificadorProvedor { get; set; } = string.Empty;

        public string ReferenciaExterna { get; set; } = string.Empty;

        public CaucaoStatus Status { get; set; } = CaucaoStatus.EM_ANALISE;

        /// <summary>Valor reservado (transaction_amount devolvido pelo provedor).</summary>
        public decimal Valor { get; set; }

        /// <summary>Valor efetivamente capturado. Preenchido somente quando Status = CAPTURADA.</summary>
        public decimal? ValorCapturado { get; set; }

        /// <summary>Codigo curto do provedor (status_detail, mascarado e truncado). Nunca exibir ao cliente.</summary>
        public string? DetalheProvedor { get; set; }

        /// <summary>Quatro ultimos digitos do cartao, para recibo. Nunca vai para o log.</summary>
        public string? CartaoFinal { get; set; }

        /// <summary>Data-hora da pre-autorizacao (hora local).</summary>
        public DateTime? CriadoEm { get; set; }

        /// <summary>Estimativa: CriadoEm + caucao_validade_dias. Preenchido somente quando Status = RESERVADA. Nao renova a reserva (D1).</summary>
        public DateTime? ReservaValidaAte { get; set; }

        /// <summary>Data-hora da captura (hora local), quando houver.</summary>
        public DateTime? CapturadoEm { get; set; }

        public string Provedor { get; set; } = string.Empty;
    }
}
