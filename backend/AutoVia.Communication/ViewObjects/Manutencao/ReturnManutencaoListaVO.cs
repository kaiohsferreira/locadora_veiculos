using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;

namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// Item da lista de ordens de manutencao (GET /api/manutencoes). Traz a ordem, o veiculo achatado (placa, marca e modelo),
    /// a foto de capa (fotoCapa ou erroFotoCapa, preenchidos pelo IFotoCapaVeiculoService), o tipo, as datas, o km e o custo
    /// (formatados pelo ManutencaoProfile). Aberta = verdadeiro enquanto nao houver data de saida.
    /// </summary>
    public class ReturnManutencaoListaVO : IComFotoCapa
    {
        /// <summary>Identificador da ordem (o codigo OS da tela).</summary>
        public int IdManutencao { get; set; }

        /// <summary>Identificador do veiculo; e a chave com que a capa e casada.</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Placa do veiculo.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Marca do veiculo.</summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>Modelo do veiculo.</summary>
        public string Modelo { get; set; } = string.Empty;

        /// <summary>Foto de capa do veiculo; nula quando nao ha foto ou quando o armazenamento falhou (ver erroFotoCapa).</summary>
        public FotoCapaVeiculoVO? FotoCapa { get; set; }

        /// <summary>Mensagem quando a capa de um item com foto cadastrada nao pode ser montada agora; nula quando nao ha erro.</summary>
        public string? ErroFotoCapa { get; set; }

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

        /// <summary>Quilometragem do veiculo registrada na abertura.</summary>
        public int KmRegistrado { get; set; }

        /// <summary>Oficina, quando informada.</summary>
        public string? Oficina { get; set; }

        /// <summary>Descricao do servico, quando informada.</summary>
        public string? Descricao { get; set; }

        /// <summary>Custo da manutencao; nulo enquanto a ordem estiver aberta ou sem custo informado.</summary>
        public decimal? Custo { get; set; }

        /// <summary>Custo formatado em moeda brasileira; vazio quando nao informado.</summary>
        public string CustoStr { get; set; } = string.Empty;

        /// <summary>Verdadeiro enquanto a ordem nao foi encerrada.</summary>
        public bool Aberta { get; set; }
    }
}
