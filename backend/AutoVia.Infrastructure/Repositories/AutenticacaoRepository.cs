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

        /// <summary>Busca o cliente ativo pelo id; nulo se nao existir. Sem rastreamento.</summary>
        public async Task<Cliente?> FindClienteByIdAsync(int idCliente)
        {
            try
            {
                Cliente? cliente = await _db.Clientes
                    .AsNoTracking()
                    .Where(x => x.IdCliente == idCliente && x.DisabledAt == null)
                    .FirstOrDefaultAsync();

                return cliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Busca o funcionario ativo pelo id, com a empresa (Include). Ignora o escopo de empresa de
        /// proposito: a validacao da sessao roda antes de existir contexto. A regra de Ativo fica no servico. Sem rastreamento.
        /// </summary>
        public async Task<Funcionario?> FindFuncionarioByIdAsync(int idFuncionario)
        {
            try
            {
                Funcionario? funcionario = await _db.Funcionarios
                    .SemEscopoEmpresa()
                    .AsNoTracking()
                    .Include(x => x.Empresa)
                    .Where(x => x.IdFuncionario == idFuncionario && x.DisabledAt == null)
                    .FirstOrDefaultAsync();

                return funcionario;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Busca o funcionario ativo pelo CPF (somente digitos), com a empresa (Include). Pre-autenticacao:
        /// ignora o escopo de empresa, porque o login ainda nao tem contexto. Sem rastreamento.
        /// </summary>
        public async Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf)
        {
            try
            {
                Funcionario? funcionario = await _db.Funcionarios
                    .SemEscopoEmpresa()
                    .AsNoTracking()
                    .Include(x => x.Empresa)
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

        /// <summary>
        /// Indica se existe funcionario com o CPF, ativo ou desativado, em qualquer empresa (administrador
        /// inicial, sem contexto). Sem rastreamento; nao filtra DisabledAt de proposito.
        /// </summary>
        public async Task<bool> ExistsFuncionarioByCpfWithDisabledsAsync(string cpf)
        {
            try
            {
                bool existe = await _db.Funcionarios
                    .SemEscopoEmpresa()
                    .AsNoTracking()
                    .AnyAsync(x => x.Cpf == cpf);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarUsuarioAcesso, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca a empresa operadora da plataforma (empresa inicial) ativa; nulo se nao existir. Sem rastreamento.</summary>
        public async Task<Empresa?> FindEmpresaOperadoraAsync()
        {
            try
            {
                Empresa? empresa = await _db.Empresas
                    .AsNoTracking()
                    .Where(x => x.Operadora && x.DisabledAt == null)
                    .OrderBy(x => x.IdEmpresa)
                    .FirstOrDefaultAsync();

                return empresa;
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
