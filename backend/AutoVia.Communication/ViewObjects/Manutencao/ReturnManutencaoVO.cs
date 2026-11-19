using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// VO de saida da ordem de manutencao (retorno da abertura e do encerramento). Traz os dados da ordem, a placa do
    /// veiculo e as datas e o custo formatados (ManutencaoProfile). Aberta = verdadeiro enquanto nao houver data de saida.
    /// </summary>
    public class ReturnManutencaoVO
    {
        /// <summary>Identificador da ordem.</summary>
        public int IdManutencao { get; set; }

        /// <summary>Identificador do veiculo.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Placa do veiculo.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Tipo da manutencao: PREVENTIVA ou CORRETIVA.</summary>
        public TipoManutencao Tipo { get; set; }

        /// <summary>Data de entrada do veiculo na oficina.</summary>
        public DateOnly DataEntrada { get; set; }

        /// <summary>Data de entrada no formato dd/MM/yyyy.</summary>
        public string DataEntradaStr { get; set; } = string.Empty;

        /// <summary>Data de saida; nula enquanto a ordem estiver aberta.</summary>
        public DateOnly? DataSaida { get; set; }

        /// <summary>Data de saida no formato dd/MM/yyyy; vazia enquanto a ordem estiver aberta.</summary>
        public string DataSaidaStr { get; set; } = string.Empty;

        /// <summary>Quilometragem do veiculo registrada na abertura (nao muda no encerramento).</summary>
        public int KmRegistrado { get; set; }

        /// <summary>Oficina, quando informada.</summary>
        public string? Oficina { get; set; }

        /// <summary>Descricao do servico, quando informada.</summary>
        public string? Descricao { get; set; }

        /// <summary>Custo da manutencao; nulo enquanto nao informado.</summary>
        public decimal? Custo { get; set; }

        /// <summary>Custo formatado em moeda brasileira; vazio quando nao informado.</summary>
        public string CustoStr { get; set; } = string.Empty;

        /// <summary>Verdadeiro enquanto a ordem nao foi encerrada.</summary>
        public bool Aberta { get; set; }
    }
}
