using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Regra pura da lista de devolucoes pendentes, testada direto: os limites da atrasada (na hora, um segundo antes, virada do
    /// dia com a hora da retirada) e a paridade entre a funcao pura e a Expression que o EF traduz.
    /// </summary>
    public class RegrasDevolucoesPendentesTests
    {
        /// <summary>Retirada as 09:30 de 20/11; a previsao gravada e 22/11 com outra hora (14:00), que a regra descarta.</summary>
        private static readonly DateTime DataRetiradaDeTeste = new DateTime(2026, 11, 20, 9, 30, 0);
        private static readonly DateTime DataPrevistaDeTeste = new DateTime(2026, 11, 22, 14, 0, 0);

        /// <summary>Devolucao prevista efetiva do caso: 22/11 as 09:30 (o dia gravado com a hora da retirada).</summary>
        private static readonly DateTime PrevistaEfetivaDeTeste = new DateTime(2026, 11, 22, 9, 30, 0);

        [Fact]
        public void NaHoraExata_NaoEhAtrasada()
        {
            bool atrasada = RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, DataRetiradaDeTeste, DataPrevistaDeTeste, PrevistaEfetivaDeTeste);

            Assert.False(atrasada);
        }

        [Fact]
        public void UmSegundoDepois_EhAtrasada()
        {
            bool atrasada = RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, DataRetiradaDeTeste, DataPrevistaDeTeste, PrevistaEfetivaDeTeste.AddSeconds(1));

            Assert.True(atrasada);
        }

        [Fact]
        public void UmSegundoAntes_NaoEhAtrasada()
        {
            bool atrasada = RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, DataRetiradaDeTeste, DataPrevistaDeTeste, PrevistaEfetivaDeTeste.AddSeconds(-1));

            Assert.False(atrasada);
        }

        [Fact]
        public void ViradaDoDiaComAHoraDaRetirada_SegueADataGravadaNaHoraDaRetirada()
        {
            DateTime retirada = new DateTime(2026, 11, 20, 23, 0, 0);
            DateTime previsao = new DateTime(2026, 11, 21, 8, 0, 0);
            DateTime efetiva = new DateTime(2026, 11, 21, 23, 0, 0);

            Assert.False(RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, retirada, previsao, efetiva.AddSeconds(-1)));
            Assert.True(RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, retirada, previsao, efetiva.AddSeconds(1)));
        }

        [Fact]
        public void StatusAtrasadaSemPrazo_SempreEhAtrasada()
        {
            bool atrasada = RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.ATRASADA, DataRetiradaDeTeste, DataPrevistaDeTeste, DataRetiradaDeTeste);

            Assert.True(atrasada);
        }

        [Theory]
        [InlineData(StatusLocacao.FINALIZADA)]
        [InlineData(StatusLocacao.CANCELADA)]
        public void StatusFinalizadaOuCancelada_NuncaEhAtrasada(StatusLocacao status)
        {
            bool atrasada = RegrasDevolucoesPendentes.EhAtrasada(status, DataRetiradaDeTeste, DataPrevistaDeTeste, PrevistaEfetivaDeTeste.AddDays(30));

            Assert.False(atrasada);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(3600)]
        public void ExpressaoCompanheira_ConfereComAFuncaoPuraNosLimites(int deslocamentoEmSegundos)
        {
            DateTime agora = PrevistaEfetivaDeTeste.AddSeconds(deslocamentoEmSegundos);
            Func<Locacao, DateTime> expressao = RegrasDevolucoesPendentes.ExpressaoDevolucaoPrevistaEfetiva.Compile();
            Locacao locacao = new Locacao { DataRetirada = DataRetiradaDeTeste, DataDevolucaoPrevista = DataPrevistaDeTeste, Status = StatusLocacao.EM_ANDAMENTO };

            DateTime pelaExpressao = expressao(locacao);
            DateTime pelaFuncao = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(DataRetiradaDeTeste, DataPrevistaDeTeste);

            Assert.Equal(pelaFuncao, pelaExpressao);
            Assert.Equal(
                RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, DataRetiradaDeTeste, DataPrevistaDeTeste, agora),
                pelaExpressao < agora);
        }

        [Fact]
        public void ExpressaoCompanheira_ViradaDoDiaTemAMesmaConta()
        {
            DateTime retirada = new DateTime(2026, 11, 20, 23, 0, 0);
            DateTime previsao = new DateTime(2026, 11, 21, 8, 0, 0);
            Func<Locacao, DateTime> expressao = RegrasDevolucoesPendentes.ExpressaoDevolucaoPrevistaEfetiva.Compile();
            Locacao locacao = new Locacao { DataRetirada = retirada, DataDevolucaoPrevista = previsao, Status = StatusLocacao.EM_ANDAMENTO };

            DateTime pelaExpressao = expressao(locacao);

            Assert.Equal(new DateTime(2026, 11, 21, 23, 0, 0), pelaExpressao);
            Assert.Equal(RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(retirada, previsao), pelaExpressao);
        }

        [Theory]
        [InlineData(StatusLocacao.EM_ANDAMENTO, 0)]
        [InlineData(StatusLocacao.EM_ANDAMENTO, -1)]
        [InlineData(StatusLocacao.EM_ANDAMENTO, 1)]
        [InlineData(StatusLocacao.ATRASADA, 0)]
        [InlineData(StatusLocacao.FINALIZADA, 1)]
        [InlineData(StatusLocacao.CANCELADA, 1)]
        public void PredicadoDaConsulta_ConfereComAFuncaoPuraNosLimites(StatusLocacao status, int deslocamentoEmSegundos)
        {
            DateTime agora = PrevistaEfetivaDeTeste.AddSeconds(deslocamentoEmSegundos);
            Func<Locacao, bool> predicado = RegrasDevolucoesPendentes.PredicadoAtrasada(agora).Compile();
            Locacao locacao = new Locacao { DataRetirada = DataRetiradaDeTeste, DataDevolucaoPrevista = DataPrevistaDeTeste, Status = status };

            bool pelaFuncao = RegrasDevolucoesPendentes.EhAtrasada(status, DataRetiradaDeTeste, DataPrevistaDeTeste, agora);

            Assert.Equal(pelaFuncao, predicado(locacao));
        }

        [Fact]
        public void PredicadoDaConsulta_ViradaDoDiaComAHoraDaRetirada_ConfereComAFuncaoPura()
        {
            DateTime retirada = new DateTime(2026, 11, 20, 23, 0, 0);
            DateTime previsao = new DateTime(2026, 11, 21, 8, 0, 0);
            DateTime efetiva = new DateTime(2026, 11, 21, 23, 0, 0);
            Locacao locacao = new Locacao { DataRetirada = retirada, DataDevolucaoPrevista = previsao, Status = StatusLocacao.EM_ANDAMENTO };

            foreach (DateTime agora in new List<DateTime> { efetiva.AddSeconds(-1), efetiva, efetiva.AddSeconds(1) })
            {
                Func<Locacao, bool> predicado = RegrasDevolucoesPendentes.PredicadoAtrasada(agora).Compile();

                Assert.Equal(RegrasDevolucoesPendentes.EhAtrasada(StatusLocacao.EM_ANDAMENTO, retirada, previsao, agora), predicado(locacao));
            }
        }

        [Fact]
        public void PredicadoDaConsulta_GuardaOAgoraEmClosureNaoComoConstanteDeData()
        {
            Expression<Func<Locacao, bool>> predicado = RegrasDevolucoesPendentes.PredicadoAtrasada(PrevistaEfetivaDeTeste);

            string arvore = predicado.Body.ToString();

            Assert.Single(predicado.Parameters);
            Assert.Contains(nameof(RegrasDevolucoesPendentes.ParametroAgora), arvore);
            Assert.DoesNotContain("Constant<System.DateTime>", arvore);
        }
    }
}
