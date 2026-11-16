using AutoVia.Core.Entities.PgSql;
using System;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Persistencia e transicoes atomicas do fluxo de locacao.</summary>
    public interface ILocacaoRepository
    {
        Task<Reserva?> FindReservaByIdAsync(int idReserva);
        Task<Funcionario?> FindActiveFuncionarioByIdAsync(int idFuncionario);
        Task<bool> ExistsActiveByReservaAsync(int idReserva);
        Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada);
        Task<Locacao?> FindByIdAsync(int idLocacao);
    }
}
