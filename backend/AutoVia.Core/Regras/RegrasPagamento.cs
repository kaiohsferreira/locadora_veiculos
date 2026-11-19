using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Linq.Expressions;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras de negocio da cobranca (RF-36 e RF-31): situacao calculada, dias para vencer, inicio do periodo e os predicados
    /// de cada aba, de cada total e da listagem. Os predicados sao expressoes que o EF traduz para SQL (no repositorio) e
    /// que os testes executam em memoria, assim a mesma regra e provada sem banco. Sem acesso a dados.
    /// </summary>
    public static class RegrasPagamento
    {
        /// <summary>
        /// Situacao mostrada ao cliente: PAGO, CANCELADO e ATRASADO ficam como estao; PENDENTE com vencimento anterior a hoje
        /// vira ATRASADO; PENDENTE com vencimento hoje ou depois continua PENDENTE.
        /// </summary>
        public static StatusPagamento Situacao(StatusPagamento status, DateOnly vencimento, DateOnly hoje)
        {
            if (status == StatusPagamento.PENDENTE && vencimento < hoje)
            {
                return StatusPagamento.ATRASADO;
            }

            return status;
        }

        /// <summary>
        /// Situacao mostrada ao cliente, com o tipo: CAUCAO nunca vira ATRASADO pela data (e bloqueio no cartao, nao divida),
        /// entao devolve o status gravado. Os demais tipos seguem a regra da situacao sem tipo.
        /// </summary>
        public static StatusPagamento Situacao(TipoPagamento tipo, StatusPagamento status, DateOnly vencimento, DateOnly hoje)
        {
            if (tipo == TipoPagamento.CAUCAO)
            {
                return status;
            }

            return Situacao(status, vencimento, hoje);
        }

        /// <summary>Dias do vencimento ate hoje: zero vence hoje, positivo ainda falta, negativo ja venceu.</summary>
        public static int DiasParaVencer(DateOnly vencimento, DateOnly hoje)
        {
            return vencimento.DayNumber - hoje.DayNumber;
        }

        /// <summary>
        /// Inicio do periodo pelo vencimento: DOZE_MESES e SEIS_MESES voltam a partir de hoje; ANO_ATUAL comeca em primeiro de
        /// janeiro do ano corrente; TUDO devolve nulo (sem limite). O periodo ja foi validado pelo validador.
        /// </summary>
        public static DateOnly? InicioDoPeriodo(string periodo, DateOnly hoje)
        {
            if (periodo == ConstantsValues.PeriodoDozeMeses)
            {
                return hoje.AddMonths(-ConstantsValues.MesesPeriodoDozeMeses);
            }

            if (periodo == ConstantsValues.PeriodoSeisMeses)
            {
                return hoje.AddMonths(-ConstantsValues.MesesPeriodoSeisMeses);
            }

            if (periodo == ConstantsValues.PeriodoAnoAtual)
            {
                return new DateOnly(hoje.Year, 1, 1);
            }

            return null;
        }

        /// <summary>Pendente: PENDENTE ou ATRASADO, sem caucao. Vale para a aba, a contagem e o total pendente, sem periodo.</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoPendente()
        {
            return x => x.Tipo != TipoPagamento.CAUCAO
                && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO);
        }

        /// <summary>Pago no periodo: PAGO, sem caucao, com vencimento a partir do inicio do periodo (nulo = sem limite).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoPagoNoPeriodo(DateOnly? inicio)
        {
            bool semLimite = !inicio.HasValue;
            DateOnly limite = inicio ?? DateOnly.MinValue;

            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && (semLimite || x.DataVencimento >= limite);
        }

        /// <summary>
        /// Predicado da aba. PENDENTES: so pendentes (o periodo nao corta). PAGOS: pagos no periodo. TODOS: pendentes sempre
        /// mais o resto dentro do periodo (pagos, cancelados e caucoes); como todo pendente ja entra, vale pendente OU
        /// vencimento a partir do inicio.
        /// </summary>
        public static Expression<Func<Pagamento, bool>> PredicadoDaAba(string aba, DateOnly? inicio)
        {
            if (aba == ConstantsValues.AbaPendentes)
            {
                return PredicadoPendente();
            }

            if (aba == ConstantsValues.AbaPagos)
            {
                return PredicadoPagoNoPeriodo(inicio);
            }

            bool semLimite = !inicio.HasValue;
            DateOnly limite = inicio ?? DateOnly.MinValue;

            return x => (x.Tipo != TipoPagamento.CAUCAO && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO))
                || semLimite
                || x.DataVencimento >= limite;
        }

        /// <summary>Quitado no ano: PAGO, sem caucao, com data de pagamento em [inicioAno, inicioProximoAno).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoQuitadoNoAno(DateOnly inicioAno, DateOnly inicioProximoAno)
        {
            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && x.DataPagamento.HasValue
                && x.DataPagamento.Value >= inicioAno
                && x.DataPagamento.Value < inicioProximoAno;
        }

        /// <summary>
        /// Recusas da baixa, nesta ordem: CAUCAO (409, nao se baixa por esta rota), PAGO (409) e CANCELADO (409). Sem recusa,
        /// devolve nulo e o pagamento pode virar PAGO.
        /// </summary>
        public static string? ErroDaBaixa(TipoPagamento tipo, StatusPagamento status)
        {
            if (tipo == TipoPagamento.CAUCAO)
            {
                return ConstantsMessage.ErroPagamentoBaixaCaucao;
            }

            if (status == StatusPagamento.PAGO)
            {
                return ConstantsMessage.ErroPagamentoJaPago;
            }

            if (status == StatusPagamento.CANCELADO)
            {
                return ConstantsMessage.ErroPagamentoCancelado;
            }

            return null;
        }

        /// <summary>Listagem da locadora: status exato (se houver) e intervalo de vencimento (cada limite opcional, inclusive).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoDaListagem(StatusPagamento? status, DateOnly? de, DateOnly? ate)
        {
            bool semStatus = !status.HasValue;
            StatusPagamento valorStatus = status ?? StatusPagamento.PENDENTE;
            bool semDe = !de.HasValue;
            DateOnly valorDe = de ?? DateOnly.MinValue;
            bool semAte = !ate.HasValue;
            DateOnly valorAte = ate ?? DateOnly.MaxValue;

            return x => (semStatus || x.Status == valorStatus)
                && (semDe || x.DataVencimento >= valorDe)
                && (semAte || x.DataVencimento <= valorAte);
        }
    }
}
