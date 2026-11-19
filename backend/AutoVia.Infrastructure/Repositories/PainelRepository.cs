using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Agregacoes da visao geral (P11-BE-01 e P11-BE-02) sobre o AutoViaDbContext. Cada indicador e uma consulta com o
    /// filtro global "Empresa" do contexto (sem bypass) e DisabledAt nulo, executada no banco com AsNoTracking. As consultas
    /// publicas (IQueryable) ficam expostas para a conferencia do SQL gerado nos testes, sem banco.
    /// </summary>
    public class PainelRepository : IPainelRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public PainelRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Pagamentos recebidos no periodo: PAGO, tipo diferente de CAUCAO, data de pagamento em [inicio, fimExclusivo).</summary>
        public IQueryable<Pagamento> ConsultaReceitaDoPeriodo(DateOnly inicio, DateOnly fimExclusivo)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Status == StatusPagamento.PAGO
                        && x.Tipo != TipoPagamento.CAUCAO
                        && x.DataPagamento.HasValue
                        && x.DataPagamento.Value >= inicio
                        && x.DataPagamento.Value < fimExclusivo);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagamentos a receber: PENDENTE com vencimento em hoje ou depois (nao vencidos), sem a caucao (tipo CAUCAO nao e divida).</summary>
        public IQueryable<Pagamento> ConsultaAReceber(DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Tipo != TipoPagamento.CAUCAO
                        && x.Status == StatusPagamento.PENDENTE
                        && x.DataVencimento >= hoje);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagamentos em atraso: ATRASADO, ou PENDENTE com vencimento anterior a hoje; a caucao (tipo CAUCAO) nunca entra.</summary>
        public IQueryable<Pagamento> ConsultaEmAtraso(DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Tipo != TipoPagamento.CAUCAO
                        && (x.Status == StatusPagamento.ATRASADO
                            || (x.Status == StatusPagamento.PENDENTE && x.DataVencimento < hoje)));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Veiculos ativos agrupados por status: uma linha por situacao presente, com a quantidade (GROUP BY no banco).</summary>
        public IQueryable<FrotaSituacaoContagem> ConsultaFrotaPorSituacao()
        {
            try
            {
                IQueryable<FrotaSituacaoContagem> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null)
                    .GroupBy(x => x.Status)
                    .Select(grupo => new FrotaSituacaoContagem
                    {
                        Situacao = grupo.Key,
                        Quantidade = grupo.Count()
                    });

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Locacoes em andamento (EM_ANDAMENTO) ativas.</summary>
        public IQueryable<Locacao> ConsultaLocacoesEmAndamento()
        {
            try
            {
                IQueryable<Locacao> consulta = _db.Locacoes
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null && x.Status == StatusLocacao.EM_ANDAMENTO);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Locacoes em andamento com devolucao prevista em [inicioDia, fimDia).</summary>
        public IQueryable<Locacao> ConsultaDevolucoesPrevistas(DateTime inicioDia, DateTime fimDia)
        {
            try
            {
                IQueryable<Locacao> consulta = _db.Locacoes
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Status == StatusLocacao.EM_ANDAMENTO
                        && x.DataDevolucaoPrevista >= inicioDia
                        && x.DataDevolucaoPrevista < fimDia);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Soma da receita do periodo (SUM no banco).</summary>
        public async Task<decimal> SomarReceitaPeriodoAsync(DateOnly inicio, DateOnly fimExclusivo)
        {
            try
            {
                decimal soma = await ConsultaReceitaDoPeriodo(inicio, fimExclusivo)
                    .Select(x => x.Valor)
                    .SumAsync();

                return soma;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Soma dos pagamentos a receber (SUM no banco).</summary>
        public async Task<decimal> SomarAReceberAsync(DateOnly hoje)
        {
            try
            {
                decimal soma = await ConsultaAReceber(hoje)
                    .Select(x => x.Valor)
                    .SumAsync();

                return soma;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantidade de pagamentos a receber (COUNT no banco).</summary>
        public async Task<int> ContarAReceberAsync(DateOnly hoje)
        {
            try
            {
                int total = await ConsultaAReceber(hoje).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Soma dos pagamentos em atraso (SUM no banco).</summary>
        public async Task<decimal> SomarEmAtrasoAsync(DateOnly hoje)
        {
            try
            {
                decimal soma = await ConsultaEmAtraso(hoje)
                    .Select(x => x.Valor)
                    .SumAsync();

                return soma;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantidade de pagamentos em atraso (COUNT no banco).</summary>
        public async Task<int> ContarEmAtrasoAsync(DateOnly hoje)
        {
            try
            {
                int total = await ConsultaEmAtraso(hoje).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Veiculos por situacao (GROUP BY no banco; no maximo seis linhas).</summary>
        public async Task<List<FrotaSituacaoContagem>> ContarVeiculosPorSituacaoAsync()
        {
            try
            {
                List<FrotaSituacaoContagem> contagens = await ConsultaFrotaPorSituacao().ToListAsync();

                return contagens;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantidade de locacoes em andamento (COUNT no banco).</summary>
        public async Task<int> ContarLocacoesEmAndamentoAsync()
        {
            try
            {
                int total = await ConsultaLocacoesEmAndamento().CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantidade de devolucoes previstas no dia (COUNT no banco).</summary>
        public async Task<int> ContarDevolucoesPrevistasAsync(DateTime inicioDia, DateTime fimDia)
        {
            try
            {
                int total = await ConsultaDevolucoesPrevistas(inicioDia, fimDia).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Veiculos cadastrados na janela: CreatedAt em [inicioMes, fimExclusivoMes), em qualquer status, DisabledAt nulo.</summary>
        public IQueryable<Veiculo> ConsultaVeiculosCadastradosNoMes(DateTime inicioMes, DateTime fimExclusivoMes)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.CreatedAt >= inicioMes
                        && x.CreatedAt < fimExclusivoMes);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantidade de veiculos cadastrados no mes (COUNT no banco).</summary>
        public async Task<int> ContarVeiculosCadastradosNoMesAsync(DateTime inicioMes, DateTime fimExclusivoMes)
        {
            try
            {
                int total = await ConsultaVeiculosCadastradosNoMes(inicioMes, fimExclusivoMes).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCalcularIndicadores, GetType().ToString());
                throw;
            }
        }
    }
}
