using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Decisoes ja tomadas pelo servico de encerramento, gravadas pelo repositorio na transacao final (devolucao-encerramento v4,
    /// passo 10). Nada aqui e lido do cliente: vem da apuracao, da caucao e do relogio.
    /// </summary>
    public sealed class EncerramentoRegistro
    {
        /// <summary>Locacao a encerrar.</summary>
        public int IdLocacao { get; set; }

        /// <summary>Reserva de origem (trava de linha quando houver).</summary>
        public int? IdReserva { get; set; }

        /// <summary>Veiculo da locacao (trava de linha, ordem global).</summary>
        public int IdVeiculo { get; set; }

        /// <summary>Hora da vistoria de saida (data_devolucao_efetiva).</summary>
        public DateTime DataDevolucaoEfetiva { get; set; }

        /// <summary>Quilometragem da vistoria de saida (km_chegada e km_atual do veiculo).</summary>
        public int KmChegada { get; set; }

        /// <summary>Valor total da locacao, ja conferido com o total confirmado.</summary>
        public decimal ValorTotal { get; set; }

        /// <summary>Momento do registro (updated_at e created_at dos lancamentos novos).</summary>
        public DateTime Agora { get; set; }

        /// <summary>Data de hoje para vencimentos e pagamentos.</summary>
        public DateOnly Hoje { get; set; }

        /// <summary>Linha CAUCAO da locacao (quando existe e estava PENDENTE na leitura).</summary>
        public int? IdPagamentoCaucao { get; set; }

        /// <summary>Novo status da caucao; nulo = nao regravar.</summary>
        public StatusPagamento? NovoStatusCaucao { get; set; }

        /// <summary>Valor gravado na caucao quando ha regravacao.</summary>
        public decimal? ValorCaucaoGravado { get; set; }

        /// <summary>Grava data_pagamento = hoje na caucao (retencao ou captura); falso mantem a data (ESTORNADA).</summary>
        public bool DataHojeNaCaucao { get; set; }

        /// <summary>COBRANCA_EXTRA que a retencao quita (PAGO, forma CARTAO, id_transacao nulo).</summary>
        public int? IdPagamentoAlvo { get; set; }

        /// <summary>Verdadeiro quando a retencao quita a COBRANCA_EXTRA alvo.</summary>
        public bool QuitarAlvo { get; set; }

        /// <summary>Cobranca extra que faltava (apuracao antiga com T maior que zero) entra nesta gravacao.</summary>
        public bool LancarCobrancaFaltante { get; set; }

        /// <summary>Valor da cobranca extra que faltava (T).</summary>
        public decimal ValorCobrancaFaltante { get; set; }

        /// <summary>Quando verdadeiro, a cobranca que faltava nasce PAGO (retencao); falso nasce PENDENTE (PIX).</summary>
        public bool CobrancaFaltanteRetida { get; set; }

        /// <summary>Lanca a DIARIA (se nao houver DIARIA ativa da locacao, conferido sob trava).</summary>
        public bool LancarDiaria { get; set; }

        /// <summary>Valor da DIARIA (diarias x valor da diaria).</summary>
        public decimal ValorDiaria { get; set; }
    }

    /// <summary>Resultado da transacao final do encerramento.</summary>
    public sealed class ResultadoEncerramento
    {
        /// <summary>Locacao gravada (ou a lida, quando a repeticao nao gravou).</summary>
        public Locacao Locacao { get; set; } = null!;

        /// <summary>Verdadeiro quando esta chamada gravou o encerramento; falso na repeticao (nada gravado).</summary>
        public bool Criada { get; set; }

        /// <summary>Status final do veiculo.</summary>
        public StatusVeiculo StatusVeiculo { get; set; }

        /// <summary>Quilometragem atual do veiculo gravada (igual a km_chegada).</summary>
        public int KmAtualVeiculo { get; set; }

        /// <summary>Id da DIARIA lancada nesta gravacao; nulo quando nao lancou.</summary>
        public int? IdPagamentoDiaria { get; set; }

        /// <summary>Id da cobranca extra que faltava, quando foi lancada nesta gravacao.</summary>
        public int? IdPagamentoCobrancaFaltante { get; set; }
    }
}
