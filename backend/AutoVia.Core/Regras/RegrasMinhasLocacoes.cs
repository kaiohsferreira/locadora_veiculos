using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da lista "Minhas locacoes" do cliente (tela 27). Sem banco, sem log e sem relogio: quem chama passa o
    /// instante. Testadas direto em MinhasLocacoesRegrasTests.
    /// </summary>
    public static class RegrasMinhasLocacoes
    {
        private static readonly StatusLocacao[] StatusEmAndamento = new StatusLocacao[] { StatusLocacao.EM_ANDAMENTO, StatusLocacao.ATRASADA };
        private static readonly StatusLocacao[] StatusHistorico = new StatusLocacao[] { StatusLocacao.FINALIZADA, StatusLocacao.CANCELADA };

        /// <summary>
        /// Status aceitos pela situacao: HISTORICO devolve FINALIZADA e CANCELADA; EM_ANDAMENTO, nulo ou vazio devolvem
        /// EM_ANDAMENTO e ATRASADA. Texto fora da lista nao chega aqui (o validador recusa antes).
        /// </summary>
        public static IReadOnlyCollection<StatusLocacao> SituacaoParaStatus(string? situacao)
        {
            if (situacao == ConstantsValues.SituacaoMinhasLocacoesHistorico)
            {
                return StatusHistorico;
            }

            return StatusEmAndamento;
        }

        /// <summary>
        /// Atrasada quando o status gravado e ATRASADA ou quando a devolucao prevista efetiva ja passou do instante pedido
        /// (comparacao estrita: exatamente na hora ainda nao e atraso).
        /// </summary>
        public static bool EstaAtrasada(DateTime devolucaoPrevistaEfetiva, DateTime agora, StatusLocacao status)
        {
            if (status == StatusLocacao.ATRASADA)
            {
                return true;
            }

            return devolucaoPrevistaEfetiva < agora;
        }
    }
}
