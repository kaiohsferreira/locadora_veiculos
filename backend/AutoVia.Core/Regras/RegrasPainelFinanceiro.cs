using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Linq.Expressions;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras do painel financeiro (P7-BE-05, RF-35): periodo padrao, predicados de cada indicador e de cada aba, ticket medio
    /// e mascara do CPF. Os predicados sao expressoes que o EF traduz para SQL no repositorio e que os testes executam em
    /// memoria, assim a mesma regra e provada sem banco. A caucao (tipo CAUCAO) fica fora de todos os predicados: nao e
    /// divida. Eixo de data (aprovado em 20/11/2026): recebido pela data do pagamento; o resto pelo vencimento. Sem acesso a dados.
    /// </summary>
    public static class RegrasPainelFinanceiro
    {
        /// <summary>
        /// Periodo usado na consulta: o informado quando vieram as duas datas; sem datas, o mes corrente (do dia 1 ao ultimo dia
        /// de hoje). A validacao de um so lado ja foi feita pelo validador (RegrasPeriodoFinanceiro).
        /// </summary>
        public static (DateOnly De, DateOnly Ate) ResolverPeriodo(DateOnly? de, DateOnly? ate, DateOnly hoje)
        {
            if (de.HasValue && ate.HasValue)
            {
                return (de.Value, ate.Value);
            }

            DateOnly inicioDoMes = new DateOnly(hoje.Year, hoje.Month, ConstantsValues.PrimeiroDiaDoMes);
            DateOnly fimDoMes = inicioDoMes.AddMonths(1).AddDays(-1);

            return (inicioDoMes, fimDoMes);
        }

        /// <summary>Recebido: PAGO, sem caucao, com a data de pagamento em [de, ate] (inclusive nos dois lados).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoRecebido(DateOnly de, DateOnly ate)
        {
            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && x.DataPagamento.HasValue
                && x.DataPagamento.Value >= de
                && x.DataPagamento.Value <= ate;
        }

        /// <summary>A receber: PENDENTE sem caucao, vencimento em [de, ate] e ainda nao vencido (vencimento hoje ou depois).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoAReceber(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            return x => x.Tipo != TipoPagamento.CAUCAO
                && x.Status == StatusPagamento.PENDENTE
                && x.DataVencimento >= de
                && x.DataVencimento <= ate
                && x.DataVencimento >= hoje;
        }

        /// <summary>
        /// Em atraso: ATRASADO, ou PENDENTE com vencimento anterior a hoje, sem caucao e com vencimento em [de, ate]. A regra nao
        /// depende de a rotina de atraso ter rodado: o PENDENTE vencido conta do mesmo jeito antes e depois.
        /// </summary>
        public static Expression<Func<Pagamento, bool>> PredicadoEmAtraso(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            return x => x.Tipo != TipoPagamento.CAUCAO
                && (x.Status == StatusPagamento.ATRASADO || (x.Status == StatusPagamento.PENDENTE && x.DataVencimento < hoje))
                && x.DataVencimento >= de
                && x.DataVencimento <= ate;
        }

        /// <summary>Base do ticket medio: PAGO, sem caucao, com vencimento em [de, ate] (o ticket usa o vencimento, nao o pagamento).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoTicketMedio(DateOnly de, DateOnly ate)
        {
            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && x.DataVencimento >= de
                && x.DataVencimento <= ate;
        }

        /// <summary>
        /// Predicado da aba. TODOS: PENDENTE, ATRASADO ou PAGO, sem caucao, vencimento em [de, ate]. EM_ATRASO: a populacao do
        /// indicador em atraso. VENCE_HOJE: PENDENTE com vencimento hoje, so se hoje estiver em [de, ate]. PAGOS_RECENTES: PAGO
        /// com data de pagamento nos ultimos DiasPagosRecentes dias ate hoje (ignora o periodo). Aba desconhecida nao traz nada.
        /// </summary>
        public static Expression<Func<Pagamento, bool>> PredicadoDaAba(string aba, DateOnly de, DateOnly ate, DateOnly hoje)
        {
            if (aba == ConstantsValues.AbaTitulosTodos)
            {
                return PredicadoTodos(de, ate);
            }

            if (aba == ConstantsValues.AbaTitulosEmAtraso)
            {
                return PredicadoEmAtraso(de, ate, hoje);
            }

            if (aba == ConstantsValues.AbaTitulosVenceHoje)
            {
                return PredicadoVenceHoje(de, ate, hoje);
            }

            if (aba == ConstantsValues.AbaTitulosPagosRecentes)
            {
                return PredicadoPagosRecentes(hoje);
            }

            return x => false;
        }

        /// <summary>Calcula o ticket medio: soma dividida pela quantidade, duas casas, AwayFromZero. Zero quando nao ha pagamento.</summary>
        public static decimal CalcularTicketMedio(decimal soma, int quantidade)
        {
            if (quantidade <= 0)
            {
                return 0m;
            }

            decimal ticket = soma / quantidade;

            return Math.Round(ticket, ConstantsValues.CasasDecimaisTicketMedio, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Mascara do CPF: tira tudo que nao for digito; so com exatamente 11 digitos sai NNN.***.***-NN (3 primeiros e 2 ultimos).
        /// Outro tamanho, ou ausencia, devolve nulo. O CPF inteiro nunca sai.
        /// </summary>
        public static string? MascararCpf(string? cpf)
        {
            string digitos = StaticMethods.OnlyDigits(cpf);

            if (digitos.Length != ConstantsValues.TamanhoCpfDigitos)
            {
                return null;
            }

            string inicio = digitos.Substring(0, ConstantsValues.DigitosCpfInicioMascara);
            string fim = digitos.Substring(digitos.Length - ConstantsValues.DigitosCpfFimMascara);

            return string.Format(ConstantsValues.FormatoCpfMascarado, inicio, fim);
        }

        private static Expression<Func<Pagamento, bool>> PredicadoTodos(DateOnly de, DateOnly ate)
        {
            return x => x.Tipo != TipoPagamento.CAUCAO
                && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO || x.Status == StatusPagamento.PAGO)
                && x.DataVencimento >= de
                && x.DataVencimento <= ate;
        }

        private static Expression<Func<Pagamento, bool>> PredicadoVenceHoje(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            bool hojeNoPeriodo = hoje >= de && hoje <= ate;

            return x => hojeNoPeriodo
                && x.Tipo != TipoPagamento.CAUCAO
                && x.Status == StatusPagamento.PENDENTE
                && x.DataVencimento == hoje;
        }

        private static Expression<Func<Pagamento, bool>> PredicadoPagosRecentes(DateOnly hoje)
        {
            DateOnly inicioDaJanela = hoje.AddDays(1 - ConstantsValues.DiasPagosRecentes);

            return x => x.Tipo != TipoPagamento.CAUCAO
                && x.Status == StatusPagamento.PAGO
                && x.DataPagamento.HasValue
                && x.DataPagamento.Value >= inicioDaJanela
                && x.DataPagamento.Value <= hoje;
        }
    }
}
