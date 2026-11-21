using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso a dados das cobrancas (RF-31 e RF-36): lancamento, baixa, listagem da locadora e consultas do cliente. A
    /// baixa trava a linha (FOR UPDATE) na mesma transacao que confere o status. As consultas do cliente nao dependem do
    /// filtro de locadora: a posse e pelo cliente do contexto.
    /// </summary>
    public interface IPagamentoRepository
    {
        /// <summary>Grava a cobranca nascida PENDENTE, depois de conferir que a locacao ou a venda existe, nao esta desativada e e da locadora do contexto (404).</summary>
        Task<Pagamento> LancarAtomicamenteAsync(Pagamento pagamento, DateTime agora);

        /// <summary>
        /// Trava a cobranca e da baixa: PENDENTE ou ATRASADO vira PAGO com a data e a forma. Recusa caucao, pago e cancelado (409),
        /// inexistente (404), forma diferente de PIX com PIX gerado sem confirmacao (409, regra P6) e PIX valido que nao foi o
        /// cancelado antes no provedor (409, decisao 2 de 21/11/2026: idTransacaoCancelado, nulo quando nada foi cancelado).
        /// </summary>
        Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora, bool confirmarBaixaComPixGerado, string? idTransacaoCancelado);

        /// <summary>Lista as cobrancas da locadora do contexto que atendem ao predicado, na ordem de vencimento e id, com a pagina e o total (sem o QR).</summary>
        Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho);

        /// <summary>Lista as cobrancas do cliente que atendem ao predicado, da mais recente para a mais antiga, com a pagina e o total (sem o QR).</summary>
        Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho);

        /// <summary>Conta e soma as cobrancas do cliente em cada um dos tres predicados do resumo.</summary>
        Task<ResumoPagamentosClienteRegistro> ResumirMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> pendentes, Expression<Func<Pagamento, bool>> pagosNoPeriodo, Expression<Func<Pagamento, bool>> quitadosNoAno);

        /// <summary>Leitura da cobranca do cliente para gerar ou reabrir PIX, sem trava: so cobranca dele (posse no SQL), com o cliente da origem para o pagador. 404 se nao achou (com log).</summary>
        Task<Pagamento> ConsultarCobrancaDoClienteParaPixAsync(int idCliente, int idPagamento);

        /// <summary>Leitura da cobranca da locadora do contexto para gerar ou reabrir PIX, sem trava, com o cliente da origem para o pagador. 404 se nao achou (com log).</summary>
        Task<Pagamento> ConsultarCobrancaDaLocadoraParaPixAsync(int idPagamento);

        /// <summary>Trava curta (FOR UPDATE) da cobranca para reabrir o PIX: le status, id da transacao e validade juntos. Cliente (idCliente) ou locadora do contexto (nulo). 404 se nao achou.</summary>
        Task<Pagamento> TravarCobrancaParaPixAsync(int? idCliente, int idPagamento);

        /// <summary>
        /// Grava o PIX gerado sob trava (FOR UPDATE): repete a regra de geracao (409) e, se ja existe PIX valido, nao sobrescreve
        /// (devolve o gravado, Gravou = falso). Devolve a cobranca como ficou e se a gravacao foi feita agora.
        /// </summary>
        Task<(Pagamento Pagamento, bool Gravou)> GravarPixAtomicamenteAsync(int? idCliente, int idPagamento, PixGravacaoRegistro dados, DateTime agora);

        /// <summary>
        /// Zera o id da transacao do PIX cancelado, sob trava (FOR UPDATE), quando a baixa falhou depois do cancelamento no provedor.
        /// So zera se o id gravado ainda for o cancelado (corrida: outro PIX valido nao e tocado). A validade fica gravada.
        /// </summary>
        Task LimparPixCanceladoAsync(int idPagamento, string idTransacaoCancelado, DateTime agora);

        /// <summary>Consulta da rotina de atraso (pagamento-atraso v1): nao desativada, PENDENTE, sem caucao e com vencimento antes de hoje. So monta a consulta.</summary>
        IQueryable<Pagamento> ConsultaAtrasosVencidos(DateOnly hoje);

        /// <summary>Marca como ATRASADO os pagamentos vencidos de todas as locadoras, pela rotina do sistema (bypass nomeado, P10). Devolve so a quantidade de linhas.</summary>
        Task<int> AtualizarAtrasosTodasLocadorasAsync(DateOnly hoje, DateTime agora);

        /// <summary>Marca como ATRASADO os pagamentos vencidos da locadora do contexto (com o filtro de locadora). Devolve so a quantidade de linhas.</summary>
        Task<int> AtualizarAtrasosDaLocadoraAsync(DateOnly hoje, DateTime agora);
    }
}
