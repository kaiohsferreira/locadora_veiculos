using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Persistencia dos links de recuperacao de senha (redefinicao_senha). Escrita sempre por Disable()
    /// e SaveChangesAsync, sem Remove. Leituras de link sao rastreadas, porque a redefinicao altera o titular.
    /// Nunca registra token, hash, CPF ou e-mail no log.
    /// </summary>
    public class RedefinicaoSenhaRepository : IRedefinicaoSenhaRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public RedefinicaoSenhaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Conta as solicitacoes do titular criadas desde a data, inclusive as desativadas (limite por conta). Sem rastreamento.</summary>
        public async Task<int> CountSolicitacoesDesdeWithDisabledsAsync(int? idCliente, int? idFuncionario, DateTime desde)
        {
            try
            {
                if (!idCliente.HasValue && !idFuncionario.HasValue)
                {
                    return 0;
                }

                int total = await ConsultaDoTitular(idCliente, idFuncionario)
                    .AsNoTracking()
                    .Where(x => x.CreatedAt >= desde)
                    .CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarRedefinicaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Desativa os pendentes do titular e grava a nova solicitacao (CreatedAt e UpdatedAt = DateTime.Now), num unico SaveChanges.</summary>
        public async Task<RedefinicaoSenha> InsertInvalidandoAnterioresAsync(RedefinicaoSenha nova)
        {
            try
            {
                DateTime agora = DateTime.Now;

                List<RedefinicaoSenha> pendentes = await ConsultaDoTitular(nova.IdCliente, nova.IdFuncionario)
                    .Where(x => x.DisabledAt == null)
                    .ToListAsync();

                foreach (RedefinicaoSenha pendente in pendentes)
                {
                    pendente.Disable();
                }

                nova.IdRedefinicaoSenha = 0;
                nova.CreatedAt = agora;
                nova.UpdatedAt = agora;
                nova.DisabledAt = null;

                _db.RedefinicaoSenhas.Add(nova);
                await _db.SaveChangesAsync();

                return nova;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarRedefinicaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca o link ativo pelo hash do token, com o titular ativo carregado. Rastreado. Nulo se nao houver link valido para uso.</summary>
        public async Task<RedefinicaoSenha?> FindPendenteByTokenHashAsync(string tokenHash)
        {
            try
            {
                RedefinicaoSenha? redefinicao = await _db.RedefinicaoSenhas
                    .Include(x => x.Cliente)
                    .Include(x => x.Funcionario)
                    .Where(x => x.TokenHash == tokenHash
                        && x.DisabledAt == null
                        && ((x.Cliente != null && x.Cliente.DisabledAt == null)
                            || (x.Funcionario != null && x.Funcionario.DisabledAt == null)))
                    .FirstOrDefaultAsync();

                return redefinicao;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarRedefinicaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Troca o hash de senha do titular carregado no link e desativa este link e os demais pendentes da conta, num unico SaveChanges.</summary>
        public async Task ConcluirRedefinicaoAsync(RedefinicaoSenha redefinicao, string novoSenhaHash)
        {
            try
            {
                DateTime agora = DateTime.Now;

                if (redefinicao.Cliente != null)
                {
                    redefinicao.Cliente.SenhaHash = novoSenhaHash;
                    // Decisao do Kaio (C2): a troca de senha apaga o vinculo com o Google (sem exclusao; so zera a coluna).
                    // Quem invadiu a conta Google da vitima nao volta a entrar; o proximo login Google pede CPF e senha.
                    redefinicao.Cliente.GoogleSub = null;
                    redefinicao.Cliente.UpdatedAt = agora;
                }
                else if (redefinicao.Funcionario != null)
                {
                    redefinicao.Funcionario.SenhaHash = novoSenhaHash;
                    redefinicao.Funcionario.UpdatedAt = agora;
                }
                else
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroSalvarRedefinicaoSenha);
                }

                redefinicao.Disable();

                List<RedefinicaoSenha> pendentes = await ConsultaDoTitular(redefinicao.IdCliente, redefinicao.IdFuncionario)
                    .Where(x => x.DisabledAt == null)
                    .ToListAsync();

                foreach (RedefinicaoSenha pendente in pendentes)
                {
                    if (pendente.DisabledAt == null)
                    {
                        pendente.Disable();
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarRedefinicaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta dos links do titular (cliente ou funcionario). Sem titular, nao ha consulta valida: nenhum registro.</summary>
        private IQueryable<RedefinicaoSenha> ConsultaDoTitular(int? idCliente, int? idFuncionario)
        {
            if (idCliente.HasValue)
            {
                return _db.RedefinicaoSenhas.Where(x => x.IdCliente == idCliente.Value);
            }

            if (idFuncionario.HasValue)
            {
                return _db.RedefinicaoSenhas.Where(x => x.IdFuncionario == idFuncionario.Value);
            }

            return _db.RedefinicaoSenhas.Where(x => false);
        }
    }
}
