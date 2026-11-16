using AutoVia.Core.Entities.PgSql;
using AutoVia.Communication.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface IReservaRepository
    {
        /// <summary>Le o veiculo ativo de locadora ativa para a pre-checagem da reserva (sem trava).</summary>
        Task<Veiculo?> FindVeiculoParaReservaAsync(int idVeiculo);

        /// <summary>Cria a reserva em transacao, com a trava do veiculo, a locadora ativa e o conflito de periodo.</summary>
        Task<Reserva> InsertAtomicallyAsync(Reserva reserva);

        /// <summary>Reserva do proprio cliente, com veiculo, categoria e locadora; null quando nao acha.</summary>
        Task<Reserva?> FindByIdAsync(int idReserva, int idCliente);

        /// <summary>Total do cliente com o filtro da listagem. Colecao de status nula significa todos os status.</summary>
        Task<int> CountAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca);

        /// <summary>Pagina do cliente com o filtro da listagem e a ordem pedida (crescente ou decrescente pela retirada).</summary>
        Task<List<Reserva>> FindAllAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca, bool ordemCrescente, int deslocamento, int quantidade);

        /// <summary>Cancela logicamente (status CANCELADA) a reserva do proprio cliente, em transacao.</summary>
        Task CancelAtomicallyAsync(int idReserva, int idCliente);
    }
}
