using AutoVia.Communication.Enums;
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
    /// Persistencia da equipe da locadora (P10-BE-03) sobre o AutoViaDbContext. O filtro global "Empresa" limita tudo a
    /// locadora do contexto, sem nenhum bypass aqui. Leituras sem rastreamento e filtradas por DisabledAt nulo. Nenhum
    /// metodo remove a linha: a situacao muda pelo campo Ativo.
    /// </summary>
    public class FuncionarioRepository : IFuncionarioRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public FuncionarioRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Indica se existe funcionario ativo (DisabledAt nulo) da locadora com a matricula, sem diferenca de maiusculas nem bordas.</summary>
        public async Task<bool> ExistsByMatriculaAsync(string matricula, int? idFuncionarioIgnorado)
        {
            try
            {
                string matriculaNormalizada = matricula.Trim().ToUpperInvariant();

                IQueryable<Funcionario> consulta = _db.Funcionarios
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null && x.Matricula.ToUpper() == matriculaNormalizada);

                if (idFuncionarioIgnorado.HasValue)
                {
                    consulta = consulta.Where(x => x.IdFuncionario != idFuncionarioIgnorado.Value);
                }

                bool existe = await consulta.AnyAsync();

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca o funcionario da locadora pelo id (filtro de empresa e DisabledAt nulo), sem rastreamento.</summary>
        public async Task<Funcionario?> FindByIdAsync(int idFuncionario)
        {
            try
            {
                Funcionario? funcionario = await _db.Funcionarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdFuncionario == idFuncionario && x.DisabledAt == null);

                return funcionario;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista a pagina da equipe, ordenada por nome e id, com os filtros aplicados.</summary>
        public async Task<List<Funcionario>> FindAllAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo, int deslocamento, int quantidade)
        {
            try
            {
                List<Funcionario> funcionarios = await AplicarFiltros(busca, digitosCpf, cargo, ativo)
                    .OrderBy(x => x.Nome)
                    .ThenBy(x => x.IdFuncionario)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return funcionarios;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarEquipe, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta a equipe que atende aos mesmos filtros da listagem (total da paginacao).</summary>
        public async Task<int> CountAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo)
        {
            try
            {
                int total = await AplicarFiltros(busca, digitosCpf, cargo, ativo).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarEquipe, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta os ADMINISTRADOR ativos da locadora, sem o id informado.</summary>
        public async Task<int> CountAdministradoresAtivosAsync(int? idFuncionarioIgnorado)
        {
            try
            {
                IQueryable<Funcionario> consulta = _db.Funcionarios
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null && x.Ativo && x.Cargo == CargoFuncionario.ADMINISTRADOR);

                if (idFuncionarioIgnorado.HasValue)
                {
                    consulta = consulta.Where(x => x.IdFuncionario != idFuncionarioIgnorado.Value);
                }

                int total = await consulta.CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarEquipe, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava o funcionario com id gerado, CreatedAt e UpdatedAt com o mesmo DateTime.Now e DisabledAt nulo.</summary>
        public async Task<Funcionario> InsertAsync(Funcionario funcionario)
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>Atualiza so os campos editaveis (nome, matricula, e-mail, admissao e cargo). CPF, senha e empresa ficam como estao.</summary>
        public async Task<bool> UpdateAsync(Funcionario funcionario)
        {
            try
            {
                Funcionario? salvo = await _db.Funcionarios
                    .FirstOrDefaultAsync(x => x.IdFuncionario == funcionario.IdFuncionario && x.DisabledAt == null);

                if (salvo == null)
                {
                    return false;
                }

                salvo.Nome = funcionario.Nome;
                salvo.Matricula = funcionario.Matricula;
                salvo.Email = funcionario.Email;
                salvo.DataAdmissao = funcionario.DataAdmissao;
                salvo.Cargo = funcionario.Cargo;
                salvo.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava a situacao do acesso (Ativo) e UpdatedAt. Nunca mexe em DisabledAt: a exclusao logica fica reservada.</summary>
        public async Task<bool> SetSituacaoAsync(int idFuncionario, bool ativo)
        {
            try
            {
                Funcionario? salvo = await _db.Funcionarios
                    .FirstOrDefaultAsync(x => x.IdFuncionario == idFuncionario && x.DisabledAt == null);

                if (salvo == null)
                {
                    return false;
                }

                salvo.Ativo = ativo;
                salvo.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAlterarSituacaoFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta da equipe: ativos (DisabledAt nulo), busca por nome ou matricula (texto) e por CPF (digitos),
        /// cargo e situacao. Busca e texto ja normalizados pelo servico. Sem Skip/Take: quem pagina e a listagem.
        /// </summary>
        private IQueryable<Funcionario> AplicarFiltros(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo)
        {
            IQueryable<Funcionario> consulta = _db.Funcionarios
                .AsNoTracking()
                .Where(x => x.DisabledAt == null);

            string texto = busca ?? string.Empty;
            string digitos = digitosCpf ?? string.Empty;
            bool temTexto = texto.Length > 0;
            bool temDigitos = digitos.Length > 0;

            if (temTexto && temDigitos)
            {
                consulta = consulta.Where(x => x.Nome.ToLower().Contains(texto)
                    || x.Matricula.ToLower().Contains(texto)
                    || x.Cpf.Contains(digitos));
            }
            else if (temTexto)
            {
                consulta = consulta.Where(x => x.Nome.ToLower().Contains(texto) || x.Matricula.ToLower().Contains(texto));
            }
            else if (temDigitos)
            {
                consulta = consulta.Where(x => x.Cpf.Contains(digitos));
            }

            if (cargo.HasValue)
            {
                consulta = consulta.Where(x => x.Cargo == cargo.Value);
            }

            if (ativo.HasValue)
            {
                consulta = consulta.Where(x => x.Ativo == ativo.Value);
            }

            return consulta;
        }
    }
}
