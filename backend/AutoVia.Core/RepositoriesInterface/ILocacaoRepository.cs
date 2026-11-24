using AutoVia.Core.Entities.PgSql;
using System;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Persistencia e transicoes atomicas do fluxo de locacao.</summary>
    public interface ILocacaoRepository
    {
        Task<Reserva?> FindReservaParaRetiradaAsync(int idReserva);

        /// <summary>Reserva ativa da locadora do contexto, com o veiculo e o cliente (pagador da caucao), sem rastreamento.</summary>
        Task<Reserva?> FindReservaByIdAsync(int idReserva);

        Task<Funcionario?> FindActiveFuncionarioByIdAsync(int idFuncionario);
        Task<bool> ExistsActiveByReservaAsync(int idReserva);

        /// <summary>
        /// Abre a locacao na transacao com as travas (veiculo, depois reserva) e as reconferencias. Com caucao, grava no MESMO
        /// SaveChanges a locacao com valor_caucao e a linha CAUCAO (pela navegacao Locacao.Pagamentos). Caucao nula = RF-17.
        /// </summary>
        Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada, CaucaoAberturaRegistro? caucao);

        Task<Locacao?> FindByIdAsync(int idLocacao);

        /// <summary>
        /// Verdadeiro quando existe locacao ativa da reserva com pagamento CAUCAO nao desativado de mesmo id_transacao. Leitura
        /// SEM trava e com o filtro da locadora do contexto; usada pela compensacao para nunca cancelar a caucao gravada.
        /// </summary>
        Task<bool> ExisteCaucaoGravadaAsync(int idReserva, string idTransacao);
    }
}
