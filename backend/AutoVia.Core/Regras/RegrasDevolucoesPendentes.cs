using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Linq.Expressions;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras da lista de devolucoes pendentes (GET /api/locacoes). A funcao pura EhAtrasada e testada direto; a Expression
    /// ExpressaoDevolucaoPrevistaEfetiva e a mesma conta em forma de arvore, e PredicadoAtrasada monta o predicado que o EF traduz
    /// no PostgreSQL. Os testes de paridade comparam as formas nos limites (virada do dia e hora da retirada). Sem banco, sem log
    /// e sem relogio aqui: o agora entra como parametro.
    /// </summary>
    public static class RegrasDevolucoesPendentes
    {
        /// <summary>
        /// Companheira de RegrasCobrancasExtras.DevolucaoPrevistaEfetiva para a consulta: a data gravada na hora da retirada.
        /// O corpo precisa ficar igual ao da funcao pura (o teste de paridade confere).
        /// </summary>
        public static readonly Expression<Func<Locacao, DateTime>> ExpressaoDevolucaoPrevistaEfetiva =
            locacao => locacao.DataDevolucaoPrevista.Date + locacao.DataRetirada.TimeOfDay;

        /// <summary>
        /// Atrasada = status ATRASADA, ou em andamento com a devolucao prevista efetiva menor que agora. Estar exatamente na
        /// hora nao e atraso; um segundo depois, e.
        /// </summary>
        public static bool EhAtrasada(StatusLocacao status, DateTime dataRetirada, DateTime dataDevolucaoPrevista, DateTime agora)
        {
            if (status == StatusLocacao.ATRASADA)
            {
                return true;
            }

            if (status != StatusLocacao.EM_ANDAMENTO)
            {
                return false;
            }

            DateTime previstaEfetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(dataRetirada, dataDevolucaoPrevista);

            return previstaEfetiva < agora;
        }

        /// <summary>
        /// Predicado da consulta da atrasada: locacao =&gt; (status ATRASADA) ou (status EM_ANDAMENTO e devolucao prevista efetiva
        /// menor que agora). O agora vai dentro de um objeto de closure (ParametroAgora), para o EF extrai-lo como parametro e nao
        /// como literal na arvore (o cache de consultas compiladas nao se multiplica por requisicao).
        /// </summary>
        public static Expression<Func<Locacao, bool>> PredicadoAtrasada(DateTime agora)
        {
            ParametroAgora parametroAgora = new ParametroAgora(agora);
            ParameterExpression parametro = Expression.Parameter(typeof(Locacao), "locacao");

            Expression previsaoEfetiva = new SubstituidorDeParametro(
                ExpressaoDevolucaoPrevistaEfetiva.Parameters[0],
                parametro).Visit(ExpressaoDevolucaoPrevistaEfetiva.Body)!;

            Expression status = Expression.Property(parametro, nameof(Locacao.Status));
            Expression statusAtrasada = Expression.Equal(status, Expression.Constant(StatusLocacao.ATRASADA));
            Expression statusEmAndamento = Expression.Equal(status, Expression.Constant(StatusLocacao.EM_ANDAMENTO));
            Expression agoraDoParametro = Expression.Property(Expression.Constant(parametroAgora), nameof(ParametroAgora.Valor));
            Expression emAndamentoPassouDaPrevisao = Expression.LessThan(previsaoEfetiva, agoraDoParametro);

            Expression corpo = Expression.OrElse(statusAtrasada, Expression.AndAlso(statusEmAndamento, emAndamentoPassouDaPrevisao));

            Expression<Func<Locacao, bool>> predicado = Expression.Lambda<Func<Locacao, bool>>(corpo, parametro);

            return predicado;
        }

        /// <summary>Objeto de closure que carrega o instante de referencia; o EF o transforma em parametro da consulta.</summary>
        public sealed class ParametroAgora
        {
            public ParametroAgora(DateTime valor)
            {
                Valor = valor;
            }

            /// <summary>Instante de referencia (relogio de negocio, IRelogio).</summary>
            public DateTime Valor { get; }
        }

        /// <summary>Troca cada ocorrencia do parametro antigo pelo novo, dentro de uma arvore de Expression.</summary>
        private sealed class SubstituidorDeParametro : ExpressionVisitor
        {
            private readonly ParameterExpression _antigo;
            private readonly ParameterExpression _novo;

            public SubstituidorDeParametro(ParameterExpression antigo, ParameterExpression novo)
            {
                _antigo = antigo;
                _novo = novo;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _antigo ? _novo : base.VisitParameter(node);
            }
        }
    }
}
