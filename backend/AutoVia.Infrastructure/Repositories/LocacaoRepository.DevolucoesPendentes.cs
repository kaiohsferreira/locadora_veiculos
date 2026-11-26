using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Devolucoes pendentes (GET /api/locacoes; parte do LocacaoRepository). Leitura sem SemEscopoEmpresa: o filtro global da
    /// locadora do contexto isola. O predicado da atrasada vem de RegrasDevolucoesPendentes.PredicadoAtrasada.
    /// </summary>
    public partial class LocacaoRepository
    {
        /// <summary>
        /// Consulta nomeada das devolucoes pendentes: locacoes EM_ANDAMENTO ou ATRASADA, nao desativadas, de qualquer data.
        /// Com somenteAtrasadas, so as que tem status ATRASADA ou devolucao prevista efetiva antes de agora. Sem Include.
        /// </summary>
        public IQueryable<Locacao> ConsultaDevolucoesPendentes(bool somenteAtrasadas, DateTime agora)
        {
            try
            {
                StatusLocacao[] statusAbertos = RegrasStatusVeiculo.StatusLocacoesEmAberto();

                IQueryable<Locacao> consulta = _db.Locacoes
                    .AsNoTracking()
                    .Where(locacao => locacao.DisabledAt == null && statusAbertos.Contains(locacao.Status));

                if (somenteAtrasadas)
                {
                    Expression<Func<Locacao, bool>> predicadoAtrasada = RegrasDevolucoesPendentes.PredicadoAtrasada(agora);
                    consulta = consulta.Where(predicadoAtrasada);
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDevolucoesPendentes, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Pagina ordenada das devolucoes pendentes, com Include de veiculo e cliente: ordem pela devolucao prevista efetiva
        /// crescente e, no empate, pelo id da locacao crescente; depois pula e pega. Sem rastreamento; ainda nao executada.
        /// </summary>
        public IQueryable<Locacao> ConsultaDevolucoesPendentesPagina(bool somenteAtrasadas, DateTime agora, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<Locacao> pagina = ConsultaDevolucoesPendentes(somenteAtrasadas, agora)
                    .Include(locacao => locacao.Veiculo)
                    .Include(locacao => locacao.Cliente)
                    .OrderBy(RegrasDevolucoesPendentes.ExpressaoDevolucaoPrevistaEfetiva)
                    .ThenBy(locacao => locacao.IdLocacao)
                    .Skip(deslocamento)
                    .Take(quantidade);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDevolucoesPendentes, GetType().ToString());
                throw;
            }
        }

        /// <summary>Total das devolucoes pendentes (mesmo filtro da listagem), sem rastreamento.</summary>
        public async Task<int> ContarDevolucoesPendentesAsync(bool somenteAtrasadas, DateTime agora)
        {
            try
            {
                int total = await ConsultaDevolucoesPendentes(somenteAtrasadas, agora).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDevolucoesPendentes, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Pagina das devolucoes pendentes com veiculo (placa, marca, modelo) e cliente (nome). Ordem: devolucao prevista efetiva
        /// crescente e, no empate, id da locacao crescente. Sem rastreamento.
        /// </summary>
        public async Task<List<Locacao>> ListarDevolucoesPendentesAsync(bool somenteAtrasadas, DateTime agora, int deslocamento, int quantidade)
        {
            try
            {
                List<Locacao> locacoes = await ConsultaDevolucoesPendentesPagina(somenteAtrasadas, agora, deslocamento, quantidade)
                    .ToListAsync();

                return locacoes;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDevolucoesPendentes, GetType().ToString());
                throw;
            }
        }
    }
}
