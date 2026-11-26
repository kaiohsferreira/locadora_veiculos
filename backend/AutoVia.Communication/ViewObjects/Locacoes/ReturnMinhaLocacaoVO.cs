using AutoVia.Communication.ViewObjects.Frota;
using System;

namespace AutoVia.Communication.ViewObjects.Locacoes
{
    /// <summary>
    /// Locacao do proprio cliente autenticado (tela 27, item da lista). Nunca expoe idCliente, idFuncionario, chassi, renavam,
    /// idVeiculo, idEmpresa, CPF, e-mail, telefone, CNH, valor da caucao, id de transacao ou km de chegada.
    /// </summary>
    public class ReturnMinhaLocacaoVO : IComFotoCapa
    {
        /// <summary>Identificador do veiculo, so para casar a capa. Nao e campo de saida: o JSON nao o traz.</summary>
        private int _idVeiculo;

        public int IdLocacao { get; set; }

        /// <summary>Codigo de exibicao da reserva de origem (AV-); nulo quando a locacao nao tem reserva.</summary>
        public string? CodigoReserva { get; set; }

        /// <summary>Nome de StatusLocacao: EM_ANDAMENTO, ATRASADA, FINALIZADA ou CANCELADA.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Verdadeiro quando o status e ATRASADA ou a devolucao prevista efetiva ja passou do instante da consulta.</summary>
        public bool Atrasada { get; set; }

        public DateTime DataRetirada { get; set; }
        public string DataRetiradaStr { get; set; } = string.Empty;

        public DateTime DataDevolucaoPrevista { get; set; }
        public string DataDevolucaoPrevistaStr { get; set; } = string.Empty;

        /// <summary>Data combinada na hora da retirada (regra B1). E a data que a contagem regressiva usa.</summary>
        public DateTime DevolucaoPrevistaEfetiva { get; set; }
        public string DevolucaoPrevistaEfetivaStr { get; set; } = string.Empty;

        /// <summary>Preenchida pelo encerramento; nula enquanto a locacao esta aberta.</summary>
        public DateTime? DataDevolucaoEfetiva { get; set; }
        public string? DataDevolucaoEfetivaStr { get; set; }

        /// <summary>Diarias pela retirada real: DataRetirada ate DataDevolucaoPrevista, pela mesma conta da reserva.</summary>
        public int Diarias { get; set; }

        public decimal ValorDiaria { get; set; }
        public string ValorDiariaStr { get; set; } = string.Empty;

        /// <summary>Valor total apurado no encerramento; nulo enquanto a locacao esta em andamento.</summary>
        public decimal? ValorTotal { get; set; }
        public string? ValorTotalStr { get; set; }

        public int KmSaida { get; set; }

        public string CategoriaNome { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoModelo { get; set; }
        public string? Cor { get; set; }

        /// <summary>Placa sem hifen. O cliente ja a ve na reserva.</summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>Capa do veiculo, preenchida pelo IFotoCapaVeiculoService; o AutoMapper nao preenche.</summary>
        public FotoCapaVeiculoVO? FotoCapa { get; set; }

        /// <summary>Texto de erro da capa quando o armazenamento falhou para um veiculo com foto cadastrada.</summary>
        public string? ErroFotoCapa { get; set; }

        public string LocadoraNomeFantasia { get; set; } = string.Empty;
        public string LocadoraLogradouro { get; set; } = string.Empty;
        public string LocadoraNumero { get; set; } = string.Empty;
        public string LocadoraBairro { get; set; } = string.Empty;
        public string LocadoraCidade { get; set; } = string.Empty;
        public string LocadoraUf { get; set; } = string.Empty;

        /// <summary>Tolerancia de atraso, em minutos, da configuracao de cobrancas da categoria; nula sem politica.</summary>
        public int? ToleranciaAtrasoMinutos { get; set; }

        /// <summary>Nivel de combustivel da retirada (0 a 1, duas casas); nulo sem vistoria de entrada.</summary>
        public decimal? CombustivelRetirada { get; set; }

        /// <summary>CombustivelRetirada com duas casas, para exibicao; nulo quando CombustivelRetirada e nulo.</summary>
        public string? CombustivelRetiradaStr { get; set; }

        int IComFotoCapa.IdVeiculo => _idVeiculo;

        /// <summary>Define o veiculo da locacao para casar a capa. Uso do servico; o valor nao sai no JSON.</summary>
        public void DefinirIdVeiculo(int idVeiculo)
        {
            _idVeiculo = idVeiculo;
        }
    }
}
