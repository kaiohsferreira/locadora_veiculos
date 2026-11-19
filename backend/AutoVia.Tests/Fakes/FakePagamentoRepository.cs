using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de cobrancas em memoria, para os testes de servico e HTTP sem PostgreSQL. As listas de locacoes e de vendas
    /// sao as que existem para a locadora do teste (quem nao esta nelas e de outra locadora ou inexistente). Aplica as mesmas
    /// regras de origem, de status e de posse do repositorio real; as consultas SQL sao provadas por PagamentoConsultasSqlTests.
    /// </summary>
    public class FakePagamentoRepository : IPagamentoRepository
    {
        private int _proximoId = 1;

        /// <summary>Cobrancas semeadas e lancadas (a locadora do teste).</summary>
        public List<Pagamento> Pagamentos { get; } = new List<Pagamento>();

        /// <summary>Locacoes da locadora do teste.</summary>
        public List<Locacao> Locacoes { get; } = new List<Locacao>();

        /// <summary>Vendas da locadora do teste.</summary>
        public List<Venda> Vendas { get; } = new List<Venda>();

        /// <summary>Quantidade de baixas que chegaram ao repositorio (inclusive as recusadas).</summary>
        public int Baixas { get; private set; }

        /// <summary>Grava a cobranca PENDENTE, se a origem existir na locadora do teste; senao recusa com 404.</summary>
        public Task<Pagamento> LancarAtomicamenteAsync(Pagamento pagamento, DateTime agora)
        {
            if (pagamento.IdLocacao.HasValue && !Locacoes.Any(x => x.IdLocacao == pagamento.IdLocacao.Value && x.DisabledAt == null))
            {
                throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            }

            if (pagamento.IdVenda.HasValue && !Vendas.Any(x => x.IdVenda == pagamento.IdVenda.Value && x.DisabledAt == null))
            {
                throw new NotFoundException(ConstantsMessage.ErroPagamentoVendaNaoEncontrada);
            }

            pagamento.IdPagamento = _proximoId;
            _proximoId++;
            pagamento.Status = StatusPagamento.PENDENTE;
            pagamento.DataPagamento = null;
            pagamento.Forma = null;
            pagamento.IdTransacao = null;
            pagamento.CreatedAt = agora;
            pagamento.UpdatedAt = agora;
            pagamento.DisabledAt = null;

            Pagamentos.Add(pagamento);

            return Task.FromResult(pagamento);
        }

        /// <summary>Da baixa como o repositorio real: inexistente ou de origem desativada (404), depois as recusas de status (409).</summary>
        public Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora)
        {
            Baixas++;

            Pagamento? pagamento = ConsultaBaixa(idPagamento).FirstOrDefault();

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

            return Task.FromResult(pagamento);
        }

        /// <summary>Lista da locadora: predicado, ordem de vencimento e id, com pagina e total.</summary>
        public Task<(List<Pagamento> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            List<Pagamento> filtrados = Pagamentos
                .Where(x => x.DisabledAt == null && OrigemAtiva(x))
                .AsQueryable()
                .Where(predicado)
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.IdPagamento)
                .ToList();

            List<Pagamento> pagina = filtrados.Skip(deslocamento).Take(tamanho).ToList();

            return Task.FromResult((pagina, filtrados.Count));
        }

        /// <summary>Lista do cliente: predicado sobre a consulta base do cliente, ordem decrescente de vencimento e id.</summary>
        public Task<(List<Pagamento> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            List<Pagamento> filtrados = ConsultaMeusPagamentos(idCliente)
                .Where(predicado)
                .OrderByDescending(x => x.DataVencimento)
                .ThenByDescending(x => x.IdPagamento)
                .ToList();

            List<Pagamento> pagina = filtrados.Skip(deslocamento).Take(tamanho).ToList();

            return Task.FromResult((pagina, filtrados.Count));
        }

        /// <summary>Resumo do cliente: conta e soma a consulta base em cada um dos tres predicados.</summary>
        public Task<ResumoPagamentosClienteRegistro> ResumirMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> pendentes, Expression<Func<Pagamento, bool>> pagosNoPeriodo, Expression<Func<Pagamento, bool>> quitadosNoAno)
        {
            List<Pagamento> consulta = ConsultaMeusPagamentos(idCliente).ToList();

            List<Pagamento> listaPendentes = consulta.AsQueryable().Where(pendentes).ToList();
            List<Pagamento> listaPagos = consulta.AsQueryable().Where(pagosNoPeriodo).ToList();
            List<Pagamento> listaQuitadas = consulta.AsQueryable().Where(quitadosNoAno).ToList();

            ResumoPagamentosClienteRegistro registro = new ResumoPagamentosClienteRegistro(
                listaPendentes.Count,
                listaPendentes.Sum(x => x.Valor),
                listaQuitadas.Count,
                listaQuitadas.Sum(x => x.Valor),
                listaPagos.Count);

            return Task.FromResult(registro);
        }

        /// <summary>Consulta base do cliente, como o repositorio real: nao desativada, origem do cliente e nao desativada.</summary>
        public IQueryable<Pagamento> ConsultaMeusPagamentos(int idCliente)
        {
            return Pagamentos
                .Where(x => x.DisabledAt == null && OrigemAtiva(x) && OrigemDoCliente(x, idCliente))
                .AsQueryable();
        }

        /// <summary>Consulta da baixa: a cobranca nao desativada e com origem ativa.</summary>
        private List<Pagamento> ConsultaBaixa(int idPagamento)
        {
            return Pagamentos
                .Where(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x))
                .ToList();
        }

        /// <summary>Verdadeiro quando a locacao ou a venda de origem existe e nao esta desativada.</summary>
        private static bool OrigemAtiva(Pagamento pagamento)
        {
            bool locacaoAtiva = pagamento.Locacao != null && pagamento.Locacao.DisabledAt == null;
            bool vendaAtiva = pagamento.Venda != null && pagamento.Venda.DisabledAt == null;

            return locacaoAtiva || vendaAtiva;
        }

        /// <summary>Verdadeiro quando a locacao ou a venda de origem e do cliente.</summary>
        private static bool OrigemDoCliente(Pagamento pagamento, int idCliente)
        {
            bool locacaoDoCliente = pagamento.Locacao != null && pagamento.Locacao.DisabledAt == null && pagamento.Locacao.IdCliente == idCliente;
            bool vendaDoCliente = pagamento.Venda != null && pagamento.Venda.DisabledAt == null && pagamento.Venda.IdCliente == idCliente;

            return locacaoDoCliente || vendaDoCliente;
        }
    }
}
