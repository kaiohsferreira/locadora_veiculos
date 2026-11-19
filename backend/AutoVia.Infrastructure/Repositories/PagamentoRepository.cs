using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Acesso a dados das cobrancas (RF-31 e RF-36). O lancamento confere a locacao ou a venda com o filtro de locadora
    /// ligado. A baixa trava a linha da cobranca (consulta de trava com FOR UPDATE, com o filtro de locadora valendo) na
    /// mesma transacao que confere o status. As consultas do cliente tem a posse pelo cliente do contexto e nao dependem
    /// do filtro de locadora. Todo catch registra no log e relanca; 404 e 409 lancados dentro da transacao sao registrados aqui.
    /// </summary>
    public class PagamentoRepository : IPagamentoRepository
    {
        /// <summary>
        /// Trava da cobranca pelo id, com disabled_at nulo e FOR UPDATE. A locadora do contexto entra DENTRO do SQL ({1}): a
        /// cobranca so e travada se a locacao ou a venda de origem for de um veiculo dessa locadora. Assim a linha de outra
        /// locadora nao e bloqueada antes do filtro descartar a linha.
        /// </summary>
        private const string SqlTravaPagamento = "SELECT * FROM pagamento WHERE id_pagamento = {0} AND disabled_at IS NULL AND (EXISTS (SELECT 1 FROM locacao l INNER JOIN veiculo v ON v.id_veiculo = l.id_veiculo WHERE l.id_locacao = pagamento.id_locacao AND v.id_empresa = {1}) OR EXISTS (SELECT 1 FROM venda vd INNER JOIN veiculo vv ON vv.id_veiculo = vd.id_veiculo WHERE vd.id_venda = pagamento.id_venda AND vv.id_empresa = {1})) FOR UPDATE";

        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public PagamentoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>
        /// Lanca a cobranca: confere a locacao ou a venda (existe, nao desativada e da locadora do contexto; senao 404),
        /// grava PENDENTE, sem forma, sem data de pagamento e sem id da transacao, e devolve a cobranca gravada.
        /// </summary>
        public async Task<Pagamento> LancarAtomicamenteAsync(Pagamento pagamento, DateTime agora)
        {
            try
            {
                if (pagamento.IdLocacao.HasValue)
                {
                    Locacao? locacao = await ConsultaLocacaoDaCobranca(pagamento.IdLocacao.Value).FirstOrDefaultAsync();

                    if (locacao == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                    }
                }

                if (pagamento.IdVenda.HasValue)
                {
                    Venda? venda = await ConsultaVendaDaCobranca(pagamento.IdVenda.Value).FirstOrDefaultAsync();

                    if (venda == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroPagamentoVendaNaoEncontrada);
                    }
                }

                pagamento.IdPagamento = 0;
                pagamento.Status = StatusPagamento.PENDENTE;
                pagamento.DataPagamento = null;
                pagamento.Forma = null;
                pagamento.IdTransacao = null;
                pagamento.CreatedAt = agora;
                pagamento.UpdatedAt = agora;
                pagamento.DisabledAt = null;

                _db.Pagamentos.Add(pagamento);
                await _db.SaveChangesAsync();

                return pagamento;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLancarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Da baixa em transacao: trava a cobranca, recusa caucao, pago e cancelado (409) e inexistente (404), e grava PAGO
        /// com a data e a forma. Duas baixas ao mesmo tempo: a segunda espera a trava e responde 409.
        /// </summary>
        public async Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Pagamento? pagamento = await ConsultaTravaPagamento(idPagamento).FirstOrDefaultAsync();

                    if (pagamento == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
                    }

                    string? erroDaBaixa = RegrasPagamento.ErroDaBaixa(pagamento.Tipo, pagamento.Status);

                    if (erroDaBaixa != null)
                    {
                        throw new BusinessException(erroDaBaixa);
                    }

                    pagamento.Status = StatusPagamento.PAGO;
                    pagamento.DataPagamento = dataPagamento;
                    pagamento.Forma = forma;
                    pagamento.UpdatedAt = agora;

                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();

                    return pagamento;
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBaixarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista da locadora: predicado, ordem de vencimento e id, com a pagina e o total (filtro de locadora ligado).</summary>
        public async Task<(List<Pagamento> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaPagamentosDaLocadora().Where(predicado);

                int total = await consulta.CountAsync();

                List<Pagamento> itens = await ConsultaPaginaDaLocadora(consulta, deslocamento, tamanho).ToListAsync();

                return (itens, total);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista do cliente: predicado sobre a consulta base do cliente, ordem de vencimento decrescente e id, com a pagina e o total.</summary>
        public async Task<(List<Pagamento> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaMeusPagamentos(idCliente).Where(predicado);

                int total = await consulta.CountAsync();

                List<Pagamento> itens = await ConsultaPaginaDoCliente(consulta, deslocamento, tamanho).ToListAsync();

                return (itens, total);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>Resumo do cliente: conta e soma a consulta base em cada um dos tres predicados.</summary>
        public async Task<ResumoPagamentosClienteRegistro> ResumirMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> pendentes, Expression<Func<Pagamento, bool>> pagosNoPeriodo, Expression<Func<Pagamento, bool>> quitadosNoAno)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaMeusPagamentos(idCliente);

                int quantidadePendentes = await consulta.Where(pendentes).CountAsync();
                decimal totalPendente = await consulta.Where(pendentes).SumAsync(x => x.Valor);
                int quantidadePagosNoPeriodo = await consulta.Where(pagosNoPeriodo).CountAsync();
                int quantidadeQuitadasAno = await consulta.Where(quitadosNoAno).CountAsync();
                decimal totalLiquidadoAno = await consulta.Where(quitadosNoAno).SumAsync(x => x.Valor);

                return new ResumoPagamentosClienteRegistro(quantidadePendentes, totalPendente, quantidadeQuitadasAno, totalLiquidadoAno, quantidadePagosNoPeriodo);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroResumirMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Ordem e pagina da lista da locadora: vencimento crescente e id para desempatar, com o deslocamento e a quantidade.
        /// Publica para o teste de SQL por ToQueryString; quem executa e ListarPagamentosDaLocadoraAsync.
        /// </summary>
        public IQueryable<Pagamento> ConsultaPaginaDaLocadora(IQueryable<Pagamento> consulta, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> pagina = consulta
                    .OrderBy(x => x.DataVencimento)
                    .ThenBy(x => x.IdPagamento)
                    .Skip(deslocamento)
                    .Take(tamanho);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Ordem e pagina da lista do cliente: vencimento decrescente e id decrescente, com o deslocamento e a quantidade.
        /// Publica para o teste de SQL por ToQueryString; quem executa e ListarMeusPagamentosAsync.
        /// </summary>
        public IQueryable<Pagamento> ConsultaPaginaDoCliente(IQueryable<Pagamento> consulta, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> pagina = consulta
                    .OrderByDescending(x => x.DataVencimento)
                    .ThenByDescending(x => x.IdPagamento)
                    .Skip(deslocamento)
                    .Take(tamanho);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: cobrancas da locadora do contexto, nao desativadas e com a locacao ou a venda de origem nao
        /// desativada. O filtro de locadora fica ligado (sem bypass). Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Pagamento> ConsultaPagamentosDaLocadora()
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && ((x.Locacao != null && x.Locacao.DisabledAt == null)
                            || (x.Venda != null && x.Venda.DisabledAt == null)));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada e base unica do cliente: cobrancas nao desativadas cuja locacao ou venda de origem e do cliente
        /// do contexto e nao esta desativada. A posse e calculada fora da consulta: sem cliente no contexto, ou com outro
        /// cliente, a consulta devolve vazio. Sem filtro de locadora (a posse e o limite). Traz o veiculo para a descricao.
        /// </summary>
        public IQueryable<Pagamento> ConsultaMeusPagamentos(int idCliente)
        {
            try
            {
                int idClienteContexto = _db.IdClienteContexto;
                bool posseDoCliente = idClienteContexto > 0 && idClienteContexto == idCliente;

                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .SemEscopoEmpresa()
                    .AsNoTracking()
                    .Where(x => posseDoCliente
                        && x.DisabledAt == null
                        && ((x.Locacao != null && x.Locacao.DisabledAt == null && x.Locacao.IdCliente == idCliente)
                            || (x.Venda != null && x.Venda.DisabledAt == null && x.Venda.IdCliente == idCliente)))
                    .Include(x => x.Locacao!).ThenInclude(x => x.Veiculo)
                    .Include(x => x.Venda!).ThenInclude(x => x.Veiculo);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: trava da cobranca (FOR UPDATE) pelo id, com disabled_at nulo e a origem nao desativada. O filtro
        /// de locadora fica ligado. Unica consulta com SQL bruto deste repositorio.
        /// </summary>
        public IQueryable<Pagamento> ConsultaTravaPagamento(int idPagamento)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .FromSqlRaw(SqlTravaPagamento, idPagamento, _db.IdEmpresaContexto)
                    .Where(x => (x.Locacao != null && x.Locacao.DisabledAt == null)
                        || (x.Venda != null && x.Venda.DisabledAt == null));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta nomeada: locacao nao desativada pelo id, com o filtro de locadora ligado.</summary>
        public IQueryable<Locacao> ConsultaLocacaoDaCobranca(int idLocacao)
        {
            try
            {
                IQueryable<Locacao> consulta = _db.Locacoes
                    .AsNoTracking()
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta nomeada: venda nao desativada pelo id, com o filtro de locadora ligado.</summary>
        public IQueryable<Venda> ConsultaVendaDaCobranca(int idVenda)
        {
            try
            {
                IQueryable<Venda> consulta = _db.Vendas
                    .AsNoTracking()
                    .Where(x => x.IdVenda == idVenda && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }
    }
}
