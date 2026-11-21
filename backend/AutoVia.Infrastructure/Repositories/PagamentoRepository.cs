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

        /// <summary>
        /// Trava da cobranca do cliente pelo id, com disabled_at nulo e FOR UPDATE. A posse do cliente entra DENTRO do SQL ({1}):
        /// a cobranca so e travada se a locacao ou a venda de origem for desse cliente. Sem locadora no contexto (o cliente nao tem).
        /// </summary>
        private const string SqlTravaPagamentoDoCliente = "SELECT * FROM pagamento WHERE id_pagamento = {0} AND disabled_at IS NULL AND (EXISTS (SELECT 1 FROM locacao l WHERE l.id_locacao = pagamento.id_locacao AND l.id_cliente = {1} AND l.disabled_at IS NULL) OR EXISTS (SELECT 1 FROM venda vd WHERE vd.id_venda = pagamento.id_venda AND vd.id_cliente = {1} AND vd.disabled_at IS NULL)) FOR UPDATE";

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
        /// com a data e a forma. Duas baixas ao mesmo tempo: a segunda espera a trava e responde 409. Dentro da trava, reconfere
        /// se surgiu PIX valido que nao foi o cancelado antes no provedor (decisao 2 de 21/11/2026): nesse caso, 409.
        /// </summary>
        public async Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora, bool confirmarBaixaComPixGerado, string? idTransacaoCancelado)
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

                    if (RegrasPagamento.ExigeConfirmacaoBaixaComPix(pagamento.IdTransacao, forma, confirmarBaixaComPixGerado))
                    {
                        throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaComPixGerado);
                    }

                    if (RegrasPagamento.PixValidoSemCancelamento(pagamento.IdTransacao, pagamento.PixExpiraEm, forma, idTransacaoCancelado, agora))
                    {
                        throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaComPixGerado);
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
        public async Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaPagamentosDaLocadora().Where(predicado);

                int total = await consulta.CountAsync();

                List<PagamentoListagemRegistro> itens = await ConsultaPaginaDaLocadora(consulta, deslocamento, tamanho).ToListAsync();

                return (itens, total);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista do cliente: predicado sobre a consulta base do cliente, ordem de vencimento decrescente e id, com a pagina e o total.</summary>
        public async Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaMeusPagamentos(idCliente).Where(predicado);

                int total = await consulta.CountAsync();

                List<PagamentoListagemRegistro> itens = await ConsultaPaginaDoCliente(consulta, deslocamento, tamanho).ToListAsync();

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
        public IQueryable<PagamentoListagemRegistro> ConsultaPaginaDaLocadora(IQueryable<Pagamento> consulta, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<PagamentoListagemRegistro> pagina = consulta
                    .OrderBy(x => x.DataVencimento)
                    .ThenBy(x => x.IdPagamento)
                    .Skip(deslocamento)
                    .Take(tamanho)
                    .Select(x => new PagamentoListagemRegistro
                    {
                        IdPagamento = x.IdPagamento,
                        IdLocacao = x.IdLocacao,
                        IdVenda = x.IdVenda,
                        Tipo = x.Tipo,
                        Valor = x.Valor,
                        DataVencimento = x.DataVencimento,
                        DataPagamento = x.DataPagamento,
                        Forma = x.Forma,
                        Status = x.Status,
                        VeiculoDescricao = x.Locacao != null
                            ? x.Locacao.Veiculo.Marca + " " + x.Locacao.Veiculo.Modelo
                            : (x.Venda != null ? x.Venda.Veiculo.Marca + " " + x.Venda.Veiculo.Modelo : null)
                    });

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
        public IQueryable<PagamentoListagemRegistro> ConsultaPaginaDoCliente(IQueryable<Pagamento> consulta, int deslocamento, int tamanho)
        {
            try
            {
                IQueryable<PagamentoListagemRegistro> pagina = consulta
                    .OrderByDescending(x => x.DataVencimento)
                    .ThenByDescending(x => x.IdPagamento)
                    .Skip(deslocamento)
                    .Take(tamanho)
                    .Select(x => new PagamentoListagemRegistro
                    {
                        IdPagamento = x.IdPagamento,
                        IdLocacao = x.IdLocacao,
                        IdVenda = x.IdVenda,
                        Tipo = x.Tipo,
                        Valor = x.Valor,
                        DataVencimento = x.DataVencimento,
                        DataPagamento = x.DataPagamento,
                        Forma = x.Forma,
                        Status = x.Status,
                        VeiculoDescricao = x.Locacao != null
                            ? x.Locacao.Veiculo.Marca + " " + x.Locacao.Veiculo.Modelo
                            : (x.Venda != null ? x.Venda.Veiculo.Marca + " " + x.Venda.Veiculo.Modelo : null)
                    });

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

        /// <summary>
        /// Consulta nomeada da cobranca da locadora do contexto para gerar ou reabrir o PIX: nao desativada, com a origem nao
        /// desativada e com o cliente da origem (pagador). O filtro de locadora fica ligado (sem bypass). Sem trava.
        /// </summary>
        public IQueryable<Pagamento> ConsultaCobrancaPixDaLocadora(int idPagamento)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.IdPagamento == idPagamento && x.DisabledAt == null
                        && ((x.Locacao != null && x.Locacao.DisabledAt == null)
                            || (x.Venda != null && x.Venda.DisabledAt == null)))
                    .Include(x => x.Locacao!).ThenInclude(x => x.Cliente)
                    .Include(x => x.Venda!).ThenInclude(x => x.Cliente);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: trava da cobranca do cliente (FOR UPDATE) pelo id, com a posse do cliente dentro do SQL e o
        /// disabled_at nulo. Nao tem locadora no contexto (o cliente nao tem), por isso o filtro de locadora fica desligado e a
        /// posse e o limite. Unica consulta do cliente com SQL bruto deste repositorio.
        /// </summary>
        public IQueryable<Pagamento> ConsultaTravaPagamentoDoCliente(int idCliente, int idPagamento)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .FromSqlRaw(SqlTravaPagamentoDoCliente, idPagamento, idCliente)
                    .SemEscopoEmpresa();

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Leitura da cobranca do cliente para o PIX, sem trava: posse pela consulta base do cliente, origem com cliente. 404 se nao achou.</summary>
        public async Task<Pagamento> ConsultarCobrancaDoClienteParaPixAsync(int idCliente, int idPagamento)
        {
            try
            {
                Pagamento? cobranca = await ConsultaMeusPagamentos(idCliente)
                    .Where(x => x.IdPagamento == idPagamento)
                    .Include(x => x.Locacao!).ThenInclude(x => x.Cliente)
                    .Include(x => x.Venda!).ThenInclude(x => x.Cliente)
                    .FirstOrDefaultAsync();

                if (cobranca == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
                }

                return cobranca;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Leitura da cobranca da locadora do contexto para o PIX, sem trava, com o cliente da origem. 404 se nao achou.</summary>
        public async Task<Pagamento> ConsultarCobrancaDaLocadoraParaPixAsync(int idPagamento)
        {
            try
            {
                Pagamento? cobranca = await ConsultaCobrancaPixDaLocadora(idPagamento).FirstOrDefaultAsync();

                if (cobranca == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
                }

                return cobranca;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Trava curta da cobranca para reabrir o PIX: le status, id da transacao e validade na mesma consulta. 404 se nao achou.</summary>
        public async Task<Pagamento> TravarCobrancaParaPixAsync(int? idCliente, int idPagamento)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Pagamento? pagamento = await ConsultaTravaParaPixSemRastreio(idCliente, idPagamento).FirstOrDefaultAsync();

                    if (pagamento == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
                    }

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
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Grava o PIX gerado em transacao com a trava (FOR UPDATE). Repete a regra de geracao (409) e, se ja existe PIX valido,
        /// devolve o gravado sem sobrescrever (Gravou = falso). 404 se a cobranca sumiu.
        /// </summary>
        public async Task<(Pagamento Pagamento, bool Gravou)> GravarPixAtomicamenteAsync(int? idCliente, int idPagamento, PixGravacaoRegistro dados, DateTime agora)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Pagamento? pagamento = await ConsultaTravaParaPix(idCliente, idPagamento).FirstOrDefaultAsync();

                    if (pagamento == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
                    }

                    string? erroDaGeracao = RegrasPagamento.ErroDaGeracaoPix(pagamento.Tipo, pagamento.Status);

                    if (erroDaGeracao != null)
                    {
                        throw new BusinessException(erroDaGeracao);
                    }

                    if (RegrasPagamento.PixAindaValido(pagamento.IdTransacao, pagamento.PixExpiraEm, agora))
                    {
                        await transacao.CommitAsync();

                        return (pagamento, false);
                    }

                    pagamento.IdTransacao = dados.IdentificadorProvedor;
                    pagamento.Forma = ConstantsValues.FormaPix;
                    pagamento.QrCodeBase64 = dados.QrCodeBase64;
                    pagamento.PixCopiaECola = dados.CopiaECola;
                    pagamento.PixExpiraEm = dados.ExpiraEm;
                    pagamento.UpdatedAt = agora;

                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();

                    return (pagamento, true);
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
                _logService.Write(ex, ConstantsMessage.ErroGravarPixPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Zera o id da transacao do PIX cancelado sob trava (FOR UPDATE, a consulta de sempre com o filtro de locadora), so se
        /// ainda for o cancelado. A validade fica gravada. Todo catch registra e relanca.
        /// </summary>
        public async Task LimparPixCanceladoAsync(int idPagamento, string idTransacaoCancelado, DateTime agora)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                try
                {
                    Pagamento? pagamento = await ConsultaTravaPagamento(idPagamento).FirstOrDefaultAsync();

                    if (pagamento != null && pagamento.IdTransacao == idTransacaoCancelado)
                    {
                        pagamento.IdTransacao = null;
                        pagamento.UpdatedAt = agora;

                        await _db.SaveChangesAsync();
                    }

                    await transacao.CommitAsync();
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
                _logService.Write(ex, ConstantsMessage.ErroLimparPixCancelado, GetType().ToString());
                throw;
            }
        }

        /// <summary>Trava da cobranca para o PIX: do cliente (posse no SQL) ou da locadora do contexto (filtro de locadora).</summary>
        private IQueryable<Pagamento> ConsultaTravaParaPix(int? idCliente, int idPagamento)
        {
            if (idCliente.HasValue)
            {
                return ConsultaTravaPagamentoDoCliente(idCliente.Value, idPagamento);
            }

            return ConsultaTravaPagamento(idPagamento);
        }

        /// <summary>
        /// Trava da cobranca para reabrir o PIX, sem rastreamento: a leitura nao fica no contexto, para que a gravacao (que consulta
        /// de novo no mesmo contexto) veja os valores do banco e nao a copia lida antes. Publica para o teste de rastreamento.
        /// </summary>
        public IQueryable<Pagamento> ConsultaTravaParaPixSemRastreio(int? idCliente, int idPagamento)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaTravaParaPix(idCliente, idPagamento).AsNoTracking();

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

        /// <summary>
        /// Consulta nomeada da rotina de atraso (pagamento-atraso v1): cobrancas nao desativadas, PENDENTE, sem caucao e com
        /// vencimento antes de hoje, com o filtro de locadora ligado. So monta a consulta; quem executa e a atualizacao em massa
        /// de cada rotina. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Pagamento> ConsultaAtrasosVencidos(DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Status == StatusPagamento.PENDENTE
                        && x.Tipo != TipoPagamento.CAUCAO
                        && x.DataVencimento < hoje);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Rotina do sistema (P10 aprovado): marca ATRASADO os pagamentos vencidos de TODAS as locadoras, sem o filtro de locadora
        /// (bypass nomeado, so esta rotina; nao ha usuario nem locadora no contexto). Atualiza so o status e o horario, nunca le o
        /// valor, e devolve so o numero de linhas. O WHERE completo protege a baixa simultanea: a linha paga antes nao e tocada.
        /// </summary>
        public async Task<int> AtualizarAtrasosTodasLocadorasAsync(DateOnly hoje, DateTime agora)
        {
            try
            {
                int quantidade = await ConsultaAtrasosVencidos(hoje)
                    .SemEscopoEmpresa()
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, StatusPagamento.ATRASADO)
                        .SetProperty(x => x.UpdatedAt, agora));

                return quantidade;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosTodasLocadoras, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Marca ATRASADO os pagamentos vencidos da locadora do contexto, com o filtro de locadora ligado (sem bypass). Atualiza so
        /// o status e o horario e devolve so o numero de linhas.
        /// </summary>
        public async Task<int> AtualizarAtrasosDaLocadoraAsync(DateOnly hoje, DateTime agora)
        {
            try
            {
                int quantidade = await ConsultaAtrasosVencidos(hoje)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, StatusPagamento.ATRASADO)
                        .SetProperty(x => x.UpdatedAt, agora));

                return quantidade;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosLocadora, GetType().ToString());
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
