using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Uma linha da consulta das locacoes do cliente: dados da locacao, do veiculo, da categoria e da locadora achatados,
    /// mais a tolerancia de atraso e o combustivel da retirada (mesma consulta) e a capa do veiculo. Nao e VO de saida.
    /// </summary>
    public class LocacaoClienteRegistro
    {
        public int IdLocacao { get; set; }

        /// <summary>Reserva de origem; nulo quando a locacao nao tem reserva.</summary>
        public int? IdReserva { get; set; }

        public StatusLocacao Status { get; set; }

        public DateTime DataRetirada { get; set; }

        public DateTime DataDevolucaoPrevista { get; set; }

        public DateTime? DataDevolucaoEfetiva { get; set; }

        public int KmSaida { get; set; }

        public decimal ValorDiaria { get; set; }

        public decimal? ValorTotal { get; set; }

        public int IdVeiculo { get; set; }

        public string Placa { get; set; } = string.Empty;

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public short AnoModelo { get; set; }

        public string? Cor { get; set; }

        public string CategoriaNome { get; set; } = string.Empty;

        /// <summary>Tolerancia de atraso da configuracao de cobrancas da categoria; nulo sem politica.</summary>
        public int? ToleranciaAtrasoMinutos { get; set; }

        /// <summary>Nivel de combustivel da vistoria de entrada (0 a 1); nulo sem vistoria de entrada.</summary>
        public decimal? CombustivelRetirada { get; set; }

        public string LocadoraNomeFantasia { get; set; } = string.Empty;

        public string LocadoraLogradouro { get; set; } = string.Empty;

        public string LocadoraNumero { get; set; } = string.Empty;

        public string LocadoraBairro { get; set; } = string.Empty;

        public string LocadoraCidade { get; set; } = string.Empty;

        public string LocadoraUf { get; set; } = string.Empty;

        /// <summary>Veiculo da locacao, sem navegacoes carregadas (usado so para montar a capa).</summary>
        public Veiculo Veiculo { get; set; } = null!;

        /// <summary>Fotos ativas de capa do veiculo (no maximo a primeira, pela ordem); vazio quando o veiculo nao tem foto.</summary>
        public List<VeiculoFoto> FotosCapa { get; set; } = new List<VeiculoFoto>();
    }
}
