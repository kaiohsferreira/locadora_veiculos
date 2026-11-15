using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface IPainelOperacionalRepository
    {
        Task<List<Reserva>> FindReservasDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia);
        Task<List<Locacao>> FindLocacoesDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia);
    }
}
