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
    /// Persistencia do cliente sobre o AutoViaDbContext. Leituras sem rastreamento e
    /// sempre filtradas pelos registros ativos (DisabledAt nulo). Nenhum metodo remove
    /// a linha fisicamente.
    /// </summary>
    public class ClienteRepository : IClienteRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ClienteRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>
        /// Consulta dos clientes ativos com vinculo com a empresa (RF-59): ao menos uma reserva, locacao OU venda em
        /// veiculo da empresa. Cliente e global, entao nao ha filtro "Empresa" na propria entidade: o escopo e este
        /// EXISTS explicito. Reserva, Locacao e Venda ja tem o filtro global "Empresa", que entra em cada subconsulta
        /// com o mesmo id do contexto; a condicao explicita repete esse valor (sem anular nem somar outra regra).
        /// Cada subconsulta so conta registro ativo (DisabledAt nulo): reserva, locacao ou venda excluida logicamente
        /// nao gera vinculo. Status cancelado continua contando (o cliente ja negociou com a locadora).
        /// Id nao positivo gera a condicao falsa (fail-closed). Sem rastreamento.
        /// </summary>
        public IQueryable<Cliente> ConsultaDeClientesDaEmpresa(int idEmpresa)
        {
            try
            {
                IQueryable<Cliente> consulta = _db.Clientes
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && idEmpresa > 0
                        && (_db.Reservas.Any(r => r.DisabledAt == null && r.IdCliente == x.IdCliente && r.Veiculo.IdEmpresa == idEmpresa)
                            || _db.Locacoes.Any(l => l.DisabledAt == null && l.IdCliente == x.IdCliente && l.Veiculo.IdEmpresa == idEmpresa)
                            || _db.Vendas.Any(v => v.DisabledAt == null && v.IdCliente == x.IdCliente && v.Veiculo.IdEmpresa == idEmpresa)));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista os clientes ativos com vinculo com a empresa, com filtro de status opcional, ordenados por DataCadastro e IdCliente, paginados.</summary>
        public async Task<List<Cliente>> FindAllAsync(int idEmpresa, StatusCadastroCliente? status, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<Cliente> consulta = ConsultaDeClientesDaEmpresa(idEmpresa);

                if (status.HasValue)
                {
                    consulta = consulta.Where(x => x.StatusCadastro == status.Value);
                }

                List<Cliente> clientes = await consulta
                    .OrderBy(x => x.DataCadastro)
                    .ThenBy(x => x.IdCliente)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return clientes;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarClientes, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta os clientes ativos com vinculo com a empresa, com o mesmo filtro de status da listagem.</summary>
        public async Task<int> CountAsync(int idEmpresa, StatusCadastroCliente? status)
        {
            try
            {
                IQueryable<Cliente> consulta = ConsultaDeClientesDaEmpresa(idEmpresa);

                if (status.HasValue)
                {
                    consulta = consulta.Where(x => x.StatusCadastro == status.Value);
                }

                int total = await consulta.CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarClientes, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca um cliente ativo pelo id, somente se tiver vinculo com a empresa; null se nao existir, estiver desativado ou for de outra empresa.</summary>
        public async Task<Cliente?> FindByIdAsync(int idEmpresa, int idCliente)
        {
            try
            {
                Cliente? cliente = await ConsultaDeClientesDaEmpresa(idEmpresa)
                    .FirstOrDefaultAsync(x => x.IdCliente == idCliente);

                return cliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe cliente ativo com o CPF informado.</summary>
        public async Task<bool> ExistsByCpfAsync(string cpf)
        {
            try
            {
                bool existe = await _db.Clientes
                    .AnyAsync(x => x.Cpf == cpf && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe cliente ativo com o numero de CNH informado.</summary>
        public async Task<bool> ExistsByCnhNumeroAsync(string cnhNumero)
        {
            try
            {
                bool existe = await _db.Clientes
                    .AnyAsync(x => x.CnhNumero == cnhNumero && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe cliente ativo com o e-mail informado. A comparacao ignora maiusculas e bordas: o e-mail chega aparado em minusculas (lower(email) no SQL).</summary>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                string emailMinusculo = email.Trim().ToLowerInvariant();

                bool existe = await _db.Clientes
                    .AnyAsync(x => x.DisabledAt == null && x.Email.ToLower() == emailMinusculo);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava um novo cliente: id gerado pelo banco, DataCadastro, CreatedAt e UpdatedAt com o mesmo DateTime.Now, DisabledAt nulo.</summary>
        public async Task<Cliente> InsertAsync(Cliente cliente)
        {
            try
            {
                DateTime agora = DateTime.Now;

                cliente.IdCliente = 0;
                cliente.DataCadastro = agora;
                cliente.CreatedAt = agora;
                cliente.UpdatedAt = agora;
                cliente.DisabledAt = null;

                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync();

                return cliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarCliente, GetType().ToString());
                throw;
            }
        }
    }
}
