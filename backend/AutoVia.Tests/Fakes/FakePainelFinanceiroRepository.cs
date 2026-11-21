using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IPainelFinanceiroRepository de teste, sem banco. Guarda as cobrancas de cada locadora e aplica os MESMOS predicados do
    /// repositorio real (RegrasPainelFinanceiro), em memoria, pela empresa do contexto (sem empresa, nada). FalharConsulta faz
    /// qualquer consulta lancar excecao, para o teste conferir o log e o relancamento.
    /// </summary>
    public class FakePainelFinanceiroRepository : IPainelFinanceiroRepository
    {
        private readonly IContextoEmpresa _contexto;
        private readonly Dictionary<int, List<Pagamento>> _pagamentosPorEmpresa;

        public FakePainelFinanceiroRepository(IContextoEmpresa contexto, Dictionary<int, List<Pagamento>> pagamentosPorEmpresa)
        {
            _contexto = contexto;
            _pagamentosPorEmpresa = pagamentosPorEmpresa;
        }

        /// <summary>Quando verdadeiro, toda consulta lanca InvalidOperationException (falha simulada).</summary>
        public bool FalharConsulta { get; set; }

        /// <summary>Cobrancas da locadora do contexto, com a origem nao desativada (a regra da consulta base do repositorio real).</summary>
        private List<Pagamento> Base()
        {
            if (FalharConsulta)
            {
                throw new InvalidOperationException("falha simulada da consulta");
            }

            List<Pagamento> pagamentos = new List<Pagamento>();

            if (_pagamentosPorEmpresa.ContainsKey(_contexto.IdEmpresa))
            {
                pagamentos = _pagamentosPorEmpresa[_contexto.IdEmpresa];
            }

            return pagamentos
                .Where(x => x.DisabledAt == null
                    && ((x.Locacao != null && x.Locacao.DisabledAt == null) || (x.Venda != null && x.Venda.DisabledAt == null)))
                .ToList();
        }

        public Task<IndicadorFinanceiroRegistro> ObterIndicadorRecebidoAsync(DateOnly de, DateOnly ate)
        {
            Func<Pagamento, bool> predicado = RegrasPainelFinanceiro.PredicadoRecebido(de, ate).Compile();

            return Task.FromResult(Indicador(Base().Where(predicado)));
        }

        public Task<IndicadorFinanceiroRegistro> ObterIndicadorAReceberAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            Func<Pagamento, bool> predicado = RegrasPainelFinanceiro.PredicadoAReceber(de, ate, hoje).Compile();

            return Task.FromResult(Indicador(Base().Where(predicado)));
        }

        public Task<IndicadorFinanceiroRegistro> ObterIndicadorEmAtrasoAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            Func<Pagamento, bool> predicado = RegrasPainelFinanceiro.PredicadoEmAtraso(de, ate, hoje).Compile();

            return Task.FromResult(Indicador(Base().Where(predicado)));
        }

        public Task<IndicadorFinanceiroRegistro> ObterIndicadorTicketMedioAsync(DateOnly de, DateOnly ate)
        {
            Func<Pagamento, bool> predicado = RegrasPainelFinanceiro.PredicadoTicketMedio(de, ate).Compile();

            return Task.FromResult(Indicador(Base().Where(predicado)));
        }

        public Task<(List<TituloFinanceiroRegistro> Itens, int Total)> ListarTitulosAsync(string aba, DateOnly de, DateOnly ate, DateOnly hoje, int deslocamento, int tamanho)
        {
            Func<Pagamento, bool> predicado = RegrasPainelFinanceiro.PredicadoDaAba(aba, de, ate, hoje).Compile();
            List<Pagamento> filtrados = Base().Where(predicado).ToList();
            IEnumerable<Pagamento> ordenados = OrdenarTitulos(aba, filtrados);
            List<TituloFinanceiroRegistro> itens = ordenados.Skip(deslocamento).Take(tamanho).Select(Projetar).ToList();

            return Task.FromResult((itens, filtrados.Count));
        }

        public Task<ContadoresTitulosFinanceirosRegistro> ContarTitulosAsync(DateOnly de, DateOnly ate, DateOnly hoje)
        {
            List<Pagamento> pagamentosDaLocadora = Base();

            ContadoresTitulosFinanceirosRegistro contadores = new ContadoresTitulosFinanceirosRegistro
            {
                QuantidadeTodos = pagamentosDaLocadora.Count(RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosTodos, de, ate, hoje).Compile()),
                QuantidadeEmAtraso = pagamentosDaLocadora.Count(RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosEmAtraso, de, ate, hoje).Compile()),
                QuantidadeVenceHoje = pagamentosDaLocadora.Count(RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosVenceHoje, de, ate, hoje).Compile()),
                QuantidadePagosRecentes = pagamentosDaLocadora.Count(RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosPagosRecentes, de, ate, hoje).Compile())
            };

            return Task.FromResult(contadores);
        }

        private static IndicadorFinanceiroRegistro Indicador(IEnumerable<Pagamento> itens)
        {
            List<Pagamento> lista = itens.ToList();

            return new IndicadorFinanceiroRegistro
            {
                Valor = lista.Sum(x => x.Valor),
                Quantidade = lista.Count
            };
        }

        private static IEnumerable<Pagamento> OrdenarTitulos(string aba, List<Pagamento> filtrados)
        {
            if (aba == ConstantsValues.AbaTitulosPagosRecentes)
            {
                return filtrados.OrderByDescending(x => x.DataPagamento).ThenByDescending(x => x.IdPagamento);
            }

            if (aba == ConstantsValues.AbaTitulosVenceHoje)
            {
                return filtrados.OrderBy(x => x.IdPagamento);
            }

            return filtrados.OrderBy(x => x.DataVencimento).ThenBy(x => x.IdPagamento);
        }

        private static TituloFinanceiroRegistro Projetar(Pagamento x)
        {
            return new TituloFinanceiroRegistro
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
                ClienteNome = x.Locacao != null ? x.Locacao.Cliente.Nome : x.Venda?.Cliente.Nome,
                ClienteCpf = x.Locacao != null ? x.Locacao.Cliente.Cpf : x.Venda?.Cliente.Cpf
            };
        }
    }

    /// <summary>Monta cobrancas de teste (origem: locacao ou venda com cliente), sem banco.</summary>
    public static class FabricaPagamentoFinanceiroDeTeste
    {
        /// <summary>Cria uma cobranca: origem locacao (idLocacao) ou venda (quando idVenda tem valor). Forma so quando paga.</summary>
        public static Pagamento Criar(
            int id,
            TipoPagamento tipo,
            StatusPagamento status,
            decimal valor,
            DateOnly vencimento,
            DateOnly? pagamento,
            string nome = "Maria Souza",
            string cpf = "19412345612",
            int? idVenda = null)
        {
            Cliente cliente = new Cliente { IdCliente = 1, Nome = nome, Cpf = cpf };
            Pagamento cobranca = new Pagamento
            {
                IdPagamento = id,
                Tipo = tipo,
                Status = status,
                Valor = valor,
                DataVencimento = vencimento,
                DataPagamento = pagamento,
                Forma = status == StatusPagamento.PAGO ? "PIX" : null
            };

            if (idVenda.HasValue)
            {
                cobranca.IdVenda = idVenda.Value;
                cobranca.Venda = new Venda { IdVenda = idVenda.Value, IdCliente = cliente.IdCliente, Cliente = cliente };
            }
            else
            {
                cobranca.IdLocacao = 1;
                cobranca.Locacao = new Locacao { IdLocacao = 1, IdCliente = cliente.IdCliente, Cliente = cliente };
            }

            return cobranca;
        }
    }
}
