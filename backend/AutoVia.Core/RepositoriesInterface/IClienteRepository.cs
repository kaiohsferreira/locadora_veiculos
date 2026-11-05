using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Contrato de persistencia do cliente. Todas as leituras consideram somente os
    /// registros ativos (DisabledAt nulo). A unicidade de CPF e de CNH e checada aqui
    /// para o servico decidir; o banco nao tem indice unico para isso.
    /// </summary>
    public interface IClienteRepository
    {
        /// <summary>Lista os clientes ativos, filtrados pelo status quando informado, ordenados por DataCadastro e IdCliente, na pagina pedida.</summary>
        Task<List<Cliente>> FindAllAsync(StatusCadastroCliente? status, int deslocamento, int quantidade);

        /// <summary>Conta os clientes ativos, com o mesmo filtro de status da listagem.</summary>
        Task<int> CountAsync(StatusCadastroCliente? status);

        /// <summary>Busca um cliente ativo pelo id; null se nao existir ou estiver desativado.</summary>
        Task<Cliente?> FindByIdAsync(int idCliente);

        /// <summary>Verifica se existe cliente ativo com o CPF informado (somente digitos).</summary>
        Task<bool> ExistsByCpfAsync(string cpf);

        /// <summary>Verifica se existe cliente ativo com o numero de CNH informado (somente digitos).</summary>
        Task<bool> ExistsByCnhNumeroAsync(string cnhNumero);

        /// <summary>Grava um novo cliente com id gerado, datas de auditoria em DateTime.Now e DisabledAt nulo.</summary>
        Task<Cliente> InsertAsync(Cliente cliente);
    }
}
