using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Vinculo da conta Google com o cliente (cliente.google_sub). Buscas so de clientes ativos e com
    /// AsNoTracking nas leituras; a escrita rastreia o cliente e grava UpdatedAt = DateTime.Now.
    /// Nunca registra sub, e-mail ou nome no log: so o erro generico da operacao.
    /// </summary>
    public class GoogleVinculoRepository : IGoogleVinculoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public GoogleVinculoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Busca o cliente ativo vinculado a esta conta Google (sub); nulo se nao houver. Sem rastreamento.</summary>
        public async Task<Cliente?> FindClienteByGoogleSubAsync(string googleSub)
        {
            try
            {
                Cliente? cliente = await _db.Clientes
                    .AsNoTracking()
                    .Where(x => x.GoogleSub == googleSub && x.DisabledAt == null)
                    .OrderBy(x => x.IdCliente)
                    .FirstOrDefaultAsync();

                return cliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca, entre os clientes ativos, os que tem este e-mail (minusculas), limitado a quantidade. Sem rastreamento.</summary>
        public async Task<List<Cliente>> FindClientesByEmailAsync(string email, int quantidade)
        {
            try
            {
                string emailMinusculo = email.Trim().ToLowerInvariant();

                List<Cliente> clientes = await _db.Clientes
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null && x.Email.ToLower() == emailMinusculo)
                    .OrderBy(x => x.IdCliente)
                    .Take(quantidade)
                    .ToListAsync();

                return clientes;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>Indica se algum cliente ativo, diferente do informado, ja usa esta conta Google (sub). Sem rastreamento.</summary>
        public async Task<bool> ExistsClienteByGoogleSubAsync(string googleSub, int idClienteIgnorado)
        {
            try
            {
                bool existe = await _db.Clientes
                    .AsNoTracking()
                    .AnyAsync(x => x.GoogleSub == googleSub && x.IdCliente != idClienteIgnorado && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava a conta Google (sub) no cliente ativo e atualiza UpdatedAt com DateTime.Now. Devolve false se o cliente nao existe ou esta desativado.</summary>
        public async Task<bool> UpdateGoogleSubAsync(int idCliente, string googleSub)
        {
            try
            {
                Cliente? cliente = await _db.Clientes
                    .Where(x => x.IdCliente == idCliente && x.DisabledAt == null)
                    .FirstOrDefaultAsync();

                if (cliente == null)
                {
                    return false;
                }

                cliente.GoogleSub = googleSub;
                cliente.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVinculoGoogle, GetType().ToString());
                throw;
            }
        }
    }
}
