namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// Item da lista de alertas de revisao. Traz o veiculo, a quilometragem da base (a ultima preventiva, ou zero), a
    /// proxima revisao, quanto falta e a situacao (VENCIDA ou PROXIMA). Calculado por RegrasRevisao no ManutencaoService.
    /// </summary>
    public class ReturnAlertaRevisaoVO
    {
        /// <summary>Identificador do veiculo.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Placa do veiculo.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Marca do veiculo.</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo.</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Nome da categoria do veiculo.</summary>
        public string CategoriaNome { get; set; } = string.Empty;

        /// <summary>Quilometragem atual do veiculo.</summary>
        public int KmAtual { get; set; }

        /// <summary>Quilometragem da base da revisao: maior km das preventivas do veiculo; zero quando nao ha preventiva.</summary>
        public int KmUltimaPreventiva { get; set; }

        /// <summary>Quilometragem em que a proxima revisao vence (base mais o intervalo fixo).</summary>
        public int KmProximaRevisao { get; set; }

        /// <summary>Quilometros que faltam para a proxima revisao; negativo quando ja passou.</summary>
        public int FaltamKm { get; set; }

        /// <summary>Percentual do ciclo ja percorrido, com uma casa decimal; pode passar de 100.</summary>
        public decimal PercentualCiclo { get; set; }

        /// <summary>Situacao do alerta: VENCIDA ou PROXIMA.</summary>
        public string Situacao { get; set; } = string.Empty;
    }
}
