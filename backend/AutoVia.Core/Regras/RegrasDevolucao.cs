using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras do encerramento da locacao (P5-BE-04, RF-25; devolucao-encerramento v4). Sem banco, sem log e sem relogio:
    /// quem chama passa a data. Diarias pela retirada real (N3), igual a apuracao.
    /// </summary>
    public static class RegrasDevolucao
    {
        /// <summary>Locacao em andamento ou atrasada pode ser encerrada (mesma regra da vistoria de saida).</summary>
        public static bool LocacaoPodeEncerrar(StatusLocacao status)
        {
            bool pode = RegrasVistoria.LocacaoAceitaVistoria(status);

            return pode;
        }

        /// <summary>
        /// Status do veiculo depois do encerramento: LOCADO vira RESERVADO quando ha outra reserva ativa ou confirmada, senao
        /// DISPONIVEL. Qualquer outro status de entrada e recusa defensiva (nulo).
        /// </summary>
        public static StatusVeiculo? StatusVeiculoAposDevolucao(StatusVeiculo atual, bool haOutraReservaEmAberto)
        {
            if (atual != StatusVeiculo.LOCADO)
            {
                return null;
            }

            StatusVeiculo? status = haOutraReservaEmAberto ? StatusVeiculo.RESERVADO : StatusVeiculo.DISPONIVEL;

            return status;
        }

        /// <summary>Diarias pela retirada real (DateOnly da retirada ate DateOnly da devolucao prevista), igual a apuracao (N3).</summary>
        public static int CalcularDiarias(DateTime dataRetirada, DateTime dataDevolucaoPrevista)
        {
            int diarias = RegrasReserva.CalcularDiarias(DateOnly.FromDateTime(dataRetirada), DateOnly.FromDateTime(dataDevolucaoPrevista));

            return diarias;
        }

        /// <summary>Valor total da locacao: diarias x valor da diaria mais o total das extras, com 2 casas (meio centavo para cima).</summary>
        public static decimal CalcularValorTotal(int diarias, decimal valorDiaria, decimal totalExtras)
        {
            decimal bruto = (diarias * valorDiaria) + totalExtras;
            decimal total = decimal.Round(bruto, 2, MidpointRounding.AwayFromZero);

            return total;
        }

        /// <summary>Valor da linha DIARIA (diarias x valor da diaria, 2 casas).</summary>
        public static decimal ValorDaDiaria(int diarias, decimal valorDiaria)
        {
            decimal valor = decimal.Round(diarias * valorDiaria, 2, MidpointRounding.AwayFromZero);

            return valor;
        }

        /// <summary>A DIARIA so entra com diarias e valor da diaria maiores que zero (CHECK ck_pagamento_valor).</summary>
        public static bool DiariaDeveLancar(int diarias, decimal valorDiaria)
        {
            bool deve = diarias > 0 && valorDiaria > 0m;

            return deve;
        }

        /// <summary>Total confirmado pela tela (com ate 2 casas, meio centavo para cima) igual ao total recalculado.</summary>
        public static bool TotalConfere(decimal totalConfirmado, decimal valorTotal)
        {
            bool confere = decimal.Round(totalConfirmado, 2, MidpointRounding.AwayFromZero) == valorTotal;

            return confere;
        }

        /// <summary>
        /// Alvo da retencao em aberto (O-10): COBRANCA_EXTRA PENDENTE ou ATRASADO. So ela pode ser quitada pela retencao; PAGO,
        /// CANCELADO ou outro status e recusa (409) antes de gravar.
        /// </summary>
        public static bool AlvoRetencaoEmAberto(StatusPagamento status)
        {
            bool aberto = status == StatusPagamento.PENDENTE || status == StatusPagamento.ATRASADO;

            return aberto;
        }

        /// <summary>
        /// Valor retido e valor liberado da caucao depois da resolucao (2 casas). Retido nao passa do reservado; o liberado e o
        /// restante. Nao confirmada ou baixada: nada retido e o liberado segue o caminho da decisao (pelo chamador).
        /// </summary>
        public static decimal ValorLiberadoDaCaucao(decimal valorReservado, decimal valorRetido)
        {
            decimal liberado = decimal.Round(valorReservado - valorRetido, 2, MidpointRounding.AwayFromZero);

            return liberado;
        }
    }
}
