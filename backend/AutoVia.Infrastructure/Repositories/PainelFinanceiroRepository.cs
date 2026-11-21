using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Acesso a dados do painel financeiro (P7-BE-05, RF-35) sobre o AutoViaDbContext. A base e a cobranca nao desativada cuja
    /// origem (locacao ou venda) tambem nao esta desativada, com o filtro global "Empresa" do contexto (sem bypass). Os
    /// predicados vem de RegrasPainelFinanceiro (os mesmos que os testes executam em memoria). As listas projetam sem
    /// qr_code_base64. Todo catch registra no log e relanca.
    /// </summary>
    public class PainelFinanceiroRepository : IPainelFinanceiroRepository
    {
        /// <summary>Projecao da linha da lista de titulos: so as colunas da lista e o nome e o CPF do cliente dono da origem.</summary>
        private static readonly Expression<Func<Pagamento, TituloFinanceiroRegistro>> ProjecaoTitulo = x => new TituloFinanceiroRegistro
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
            ClienteNome = x.Locacao != null ? x.Locacao.Cliente.Nome : (x.Venda != null ? x.Venda.Cliente.Nome : null),
            ClienteCpf = x.Locacao != null ? x.Locacao.Cliente.Cpf : (x.Venda != null ? x.Venda.Cliente.Cpf : null)
        };

        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public PainelFinanceiroRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Recebido (consulta publica para o teste de SQL): PAGO, sem caucao, com data de pagamento em [de, ate].</summary>
        public IQueryable<Pagamento> ConsultaIndicadorRecebido(DateOnly de, DateOnly ate)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaBaseDaOrigemAtiva()
                    .Where(RegrasPainelFinanceiro.PredicadoRecebido(de, ate));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>A receber (consulta publica para o teste de SQL): PENDENTE, sem caucao, vencimento em [de, ate] e nao vencido.</summary>
        public IQueryable<Pagamento> ConsultaIndicadorAReceber(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaBaseDaOrigemAtiva()
                    .Where(RegrasPainelFinanceiro.PredicadoAReceber(de, ate, hoje));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Em atraso (consulta publica para o teste de SQL): ATRASADO ou PENDENTE vencido, sem caucao, vencimento em [de, ate].</summary>
        public IQueryable<Pagamento> ConsultaIndicadorEmAtraso(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaBaseDaOrigemAtiva()
                    .Where(RegrasPainelFinanceiro.PredicadoEmAtraso(de, ate, hoje));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Base do ticket medio (consulta publica para o teste de SQL): PAGO, sem caucao, vencimento em [de, ate].</summary>
        public IQueryable<Pagamento> ConsultaIndicadorTicketMedio(DateOnly de, DateOnly ate)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaBaseDaOrigemAtiva()
                    .Where(RegrasPainelFinanceiro.PredicadoTicketMedio(de, ate));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Populacao de uma aba (consulta publica): sem ordem e sem projecao; usada na contagem e na lista.</summary>
        public IQueryable<Pagamento> ConsultaTitulosDaAba(string aba, DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaBaseDaOrigemAtiva()
                    .Where(RegrasPainelFinanceiro.PredicadoDaAba(aba, de, ate, hoje));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista de uma aba (consulta publica para o teste de SQL): ordem da aba e projecao sem qr_code_base64. TODOS e EM_ATRASO:
        /// vencimento crescente e id; VENCE_HOJE: id; PAGOS_RECENTES: data de pagamento decrescente e id decrescente.
        /// </summary>
        public IQueryable<TituloFinanceiroRegistro> ConsultaTitulos(string aba, DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaTitulosDaAba(aba, de, ate, hoje);
                IOrderedQueryable<Pagamento> ordenada;

                if (aba == ConstantsValues.AbaTitulosPagosRecentes)
                {
                    ordenada = consulta.OrderByDescending(x => x.DataPagamento).ThenByDescending(x => x.IdPagamento);
                }
                else if (aba == ConstantsValues.AbaTitulosVenceHoje)
                {
                    ordenada = consulta.OrderBy(x => x.IdPagamento);
                }
                else
                {
                    ordenada = consulta.OrderBy(x => x.DataVencimento).ThenBy(x => x.IdPagamento);
                }

                return ordenada.Select(ProjecaoTitulo);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Recebido: soma e quantidade no banco (SUM e COUNT), sem trazer as entidades.</summary>
        public async Task<IndicadorFinanceiroRegistro> ObterIndicadorRecebidoAsync(DateOnly de, DateOnly ate)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaIndicadorRecebido(de, ate);

                return await MontarIndicadorAsync(consulta);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>A receber: soma e quantidade no banco.</summary>
        public async Task<IndicadorFinanceiroRegistro> ObterIndicadorAReceberAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaIndicadorAReceber(de, ate, hoje);

                return await MontarIndicadorAsync(consulta);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Em atraso: soma e quantidade no banco.</summary>
        public async Task<IndicadorFinanceiroRegistro> ObterIndicadorEmAtrasoAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaIndicadorEmAtraso(de, ate, hoje);

                return await MontarIndicadorAsync(consulta);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Base do ticket medio: soma e quantidade no banco; a divisao e feita no servico.</summary>
        public async Task<IndicadorFinanceiroRegistro> ObterIndicadorTicketMedioAsync(DateOnly de, DateOnly ate)
        {
            try
            {
                IQueryable<Pagamento> consulta = ConsultaIndicadorTicketMedio(de, ate);

                return await MontarIndicadorAsync(consulta);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagina da aba e o total da aba (COUNT no banco e LIMIT/OFFSET com a ordem da aba).</summary>
        public async Task<(List<TituloFinanceiroRegistro> Itens, int Total)> ListarTitulosAsync(string aba, DateOnly de, DateOnly ate, DateOnly hoje, int deslocamento, int tamanho)
        {
            try
            {
                int total = await ConsultaTitulosDaAba(aba, de, ate, hoje).CountAsync();

                List<TituloFinanceiroRegistro> itens = await ConsultaTitulos(aba, de, ate, hoje)
                    .Skip(deslocamento)
                    .Take(tamanho)
                    .ToListAsync();

                return (itens, total);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Tamanho das quatro abas: um COUNT por aba, todos com o mesmo periodo e o mesmo hoje.</summary>
        public async Task<ContadoresTitulosFinanceirosRegistro> ContarTitulosAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            try
            {
                int quantidadeTodos = await ConsultaTitulosDaAba(ConstantsValues.AbaTitulosTodos, de, ate, hoje).CountAsync();
                int quantidadeEmAtraso = await ConsultaTitulosDaAba(ConstantsValues.AbaTitulosEmAtraso, de, ate, hoje).CountAsync();
                int quantidadeVenceHoje = await ConsultaTitulosDaAba(ConstantsValues.AbaTitulosVenceHoje, de, ate, hoje).CountAsync();
                int quantidadePagosRecentes = await ConsultaTitulosDaAba(ConstantsValues.AbaTitulosPagosRecentes, de, ate, hoje).CountAsync();

                return new ContadoresTitulosFinanceirosRegistro
                {
                    QuantidadeTodos = quantidadeTodos,
                    QuantidadeEmAtraso = quantidadeEmAtraso,
                    QuantidadeVenceHoje = quantidadeVenceHoje,
                    QuantidadePagosRecentes = quantidadePagosRecentes
                };
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Soma e quantidade de uma consulta (SUM e COUNT no banco).</summary>
        private static async Task<IndicadorFinanceiroRegistro> MontarIndicadorAsync(IQueryable<Pagamento> consulta)
        {
            decimal valor = await consulta.SumAsync(x => x.Valor);
            int quantidade = await consulta.CountAsync();

            return new IndicadorFinanceiroRegistro
            {
                Valor = valor,
                Quantidade = quantidade
            };
        }

        /// <summary>Base unica do painel: cobranca nao desativada cuja locacao ou venda de origem tambem nao esta desativada.</summary>
        private IQueryable<Pagamento> ConsultaBaseDaOrigemAtiva()
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
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }
    }
}
