using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>Dados publicos de uma locacao. Datas e valores saem com o campo bruto e o campo Str ao lado (camposStr v6).</summary>
    public class ReturnLocacaoVO
    {
        public int IdLocacao { get; set; }
        public int IdReserva { get; set; }
        public int IdCliente { get; set; }
        public int IdVeiculo { get; set; }
        public int IdFuncionario { get; set; }
        public DateTime DataRetirada { get; set; }
        public string DataRetiradaStr { get; set; } = string.Empty;
        public DateTime DataDevolucaoPrevista { get; set; }
        public string DataDevolucaoPrevistaStr { get; set; } = string.Empty;
        public int KmSaida { get; set; }
        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;
        public decimal ValorCaucao { get; set; }
        public string ValorCaucaoStr { get; set; } = string.Empty;
        public StatusLocacao Status { get; set; }
    }
}
