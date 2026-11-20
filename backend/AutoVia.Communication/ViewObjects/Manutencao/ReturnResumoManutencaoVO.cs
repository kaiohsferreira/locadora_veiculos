namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// VO de saida do resumo da manutencao (GET /api/manutencoes/resumo): ordens abertas da locadora (sem periodo) e custo das
    /// ordens encerradas com data de saida no mes corrente. O periodo e informativo (primeiro e ultimo dia do mes do relogio).
    /// </summary>
    public class ReturnResumoManutencaoVO
    {
        /// <summary>Ordens abertas (sem data de saida) da locadora; nao depende do periodo.</summary>
        public int OrdensAbertas { get; set; }

        /// <summary>Primeiro dia do periodo (mes corrente).</summary>
        public DateOnly PeriodoInicio { get; set; }

        /// <summary>Primeiro dia do periodo no formato dd/MM/yyyy.</summary>
        public string PeriodoInicioStr { get; set; } = string.Empty;

        /// <summary>Ultimo dia do periodo (mes corrente).</summary>
        public DateOnly PeriodoFim { get; set; }

        /// <summary>Ultimo dia do periodo no formato dd/MM/yyyy.</summary>
        public string PeriodoFimStr { get; set; } = string.Empty;

        /// <summary>Soma do custo das ordens encerradas com data de saida no periodo; zero quando nao ha.</summary>
        public decimal CustoPeriodo { get; set; }

        /// <summary>Custo do periodo formatado em reais (R$ 1.234,56).</summary>
        public string CustoPeriodoStr { get; set; } = string.Empty;
    }
}
