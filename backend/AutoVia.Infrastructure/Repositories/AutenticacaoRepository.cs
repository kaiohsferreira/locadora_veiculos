using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Consultas de login sobre cliente e funcionario. Leituras usam AsNoTracking e
    /// filtram registros ativos (DisabledAt nulo). Nunca registra CPF ou senha no log.
    /// </summary>
    public class AutenticacaoRepository : IAutenticacaoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public AutenticacaoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Busca o cliente ativo pelo CPF (somente digitos); nulo se nao existir. Sem rastreamento.</summary>
        public async Task<Cliente?> FindClienteByCpfAsync(string cpf)
        {
            try
            {
                Cliente? cliente = await _db.Clientes
                    .AsNoTracking()
                    .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                    .OrderBy(x => x.IdCliente)
                    .FirstOrDefaultAsync();

                return cliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca o funcionario ativo pelo CPF (somente digitos); nulo se nao existir. Sem rastreamento.</summary>
        public async Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf)
        {
            try
            {
                Funcionario? funcionario = await _db.Funcionarios
                    .AsNoTracking()
                    .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                    .OrderBy(x => x.IdFuncionario)
                    .FirstOrDefaultAsync();

                return funcionario;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>Indica se existe funcionario ativo com o CPF informado.</summary>
        public async Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
        {
            try
            {
                bool existe = await _db.Funcionarios
                    .AnyAsync(x => x.Cpf == cpf && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava um novo funcionario com datas de auditoria (DateTime.Now) e DisabledAt nulo.</summary>
        public async Task<Funcionario> InsertFuncionarioAsync(Funcionario funcionario)
        {
            try
            {
                DateTime agora = DateTime.Now;

                funcionario.IdFuncionario = 0;
                funcionario.CreatedAt = agora;
                funcionario.UpdatedAt = agora;
                funcionario.DisabledAt = null;

                _db.Funcionarios.Add(funcionario);
                await _db.SaveChangesAsync();

                return funcionario;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }
    }
}
