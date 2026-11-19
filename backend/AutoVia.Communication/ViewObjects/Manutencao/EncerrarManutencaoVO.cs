namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// VO de entrada do encerramento da ordem de manutencao (PATCH /api/manutencoes/{id}/encerrar). Estrutura plana. A
    /// validacao de formato e de faixa e feita pelo EncerrarManutencaoVOValidator; as regras que dependem da ordem e do
    /// veiculo (data, quilometragem atual) ficam no repositorio, na transacao.
    /// </summary>
    public class EncerrarManutencaoVO
    {
        /// <summary>Quilometragem do veiculo na saida (obrigatoria, zero ou mais). Vira o km atual do veiculo.</summary>
        public int? KmAtual { get; set; }

        /// <summary>Data de saida (yyyy-MM-dd). Ausente = hoje; data futura e recusada pelo servico.</summary>
        public DateOnly? DataSaida { get; set; }

        /// <summary>Custo da manutencao (opcional, zero ou mais, ate duas casas decimais).</summary>
        public decimal? Custo { get; set; }
    }
}
