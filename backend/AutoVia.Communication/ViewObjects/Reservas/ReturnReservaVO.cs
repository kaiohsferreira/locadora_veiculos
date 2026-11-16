using System;

namespace AutoVia.Communication.ViewObjects.Reservas
{
    /// <summary>Dados da reserva do proprio cliente autenticado (GET por id e cada item da listagem). Nunca expoe idCliente, idEmpresa, chassi ou renavam.</summary>
    public class ReturnReservaVO
    {
        public int IdReserva { get; set; }

        /// <summary>Codigo de exibicao (AV- com cinco digitos no minimo). So para exibicao.</summary>
        public string Codigo { get; set; } = string.Empty;

        public int IdVeiculo { get; set; }

        /// <summary>Data e hora em que a reserva foi criada (ISO 8601 sem fuso).</summary>
        public DateTime DataReserva { get; set; }
        public string DataReservaStr { get; set; } = string.Empty;

        public DateOnly DataRetirada { get; set; }
        public string DataRetiradaStr { get; set; } = string.Empty;
        public DateOnly DataDevolucao { get; set; }
        public string DataDevolucaoStr { get; set; } = string.Empty;

        /// <summary>Diarias: dataDevolucao menos dataRetirada, por DayNumber.</summary>
        public int Diarias { get; set; }

        public string Status { get; set; } = string.Empty;
        public decimal? ValorEstimado { get; set; }
        public string ValorEstimadoStr { get; set; } = string.Empty;

        public string CategoriaNome { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoModelo { get; set; }
        public string? Cor { get; set; }

        /// <summary>Placa sem hifen (7 caracteres). So na reserva do proprio cliente.</summary>
        public string Placa { get; set; } = string.Empty;

        public string LocadoraNomeFantasia { get; set; } = string.Empty;
        public string LocadoraLogradouro { get; set; } = string.Empty;
        public string LocadoraNumero { get; set; } = string.Empty;
        public string LocadoraBairro { get; set; } = string.Empty;
        public string LocadoraCidade { get; set; } = string.Empty;

        /// <summary>UF com duas letras.</summary>
        public string LocadoraUf { get; set; } = string.Empty;
    }
}
