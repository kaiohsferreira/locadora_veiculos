using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
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

        /// <summary>Trava a cobranca e da baixa: PENDENTE ou ATRASADO vira PAGO com a data e a forma. Recusa caucao, pago e cancelado (409) e inexistente (404).</summary>
        Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora);

        /// <summary>Lista as cobrancas da locadora do contexto que atendem ao predicado, na ordem de vencimento e id, com a pagina e o total.</summary>
        Task<(List<Pagamento> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho);

        /// <summary>Lista as cobrancas do cliente que atendem ao predicado, da mais recente para a mais antiga, com a pagina e o total.</summary>
        Task<(List<Pagamento> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho);

        /// <summary>Conta e soma as cobrancas do cliente em cada um dos tres predicados do resumo.</summary>
        Task<ResumoPagamentosClienteRegistro> ResumirMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> pendentes, Expression<Func<Pagamento, bool>> pagosNoPeriodo, Expression<Func<Pagamento, bool>> quitadosNoAno);
    }
}
