using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>Dados publicos de uma locacao.</summary>
    public class ReturnLocacaoVO
    {
        public int IdLocacao { get; set; }
        public int IdReserva { get; set; }
        public int IdCliente { get; set; }
        public int IdVeiculo { get; set; }
        public int IdFuncionario { get; set; }
        public DateTime DataRetirada { get; set; }
        public DateTime DataDevolucaoPrevista { get; set; }
        public int KmSaida { get; set; }
        public decimal ValorDiaria { get; set; }
        public decimal ValorCaucao { get; set; }
        public StatusLocacao Status { get; set; }
    }
}
