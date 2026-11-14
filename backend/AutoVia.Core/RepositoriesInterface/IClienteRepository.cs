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
        /// <summary>
        /// Lista os clientes ativos com vinculo com a empresa (reserva, locacao ou venda em veiculo dela), filtrados
        /// pelo status quando informado, ordenados por DataCadastro e IdCliente, na pagina pedida. Id de empresa
        /// nao positivo devolve lista vazia.
        /// </summary>
        Task<List<Cliente>> FindAllAsync(int idEmpresa, StatusCadastroCliente? status, int deslocamento, int quantidade);

        /// <summary>Conta os clientes ativos com vinculo com a empresa, com o mesmo filtro de status da listagem (total no escopo).</summary>
        Task<int> CountAsync(int idEmpresa, StatusCadastroCliente? status);

        /// <summary>Busca um cliente ativo pelo id, somente se tiver vinculo com a empresa; null se nao existir, estiver desativado ou for de outra empresa.</summary>
        Task<Cliente?> FindByIdAsync(int idEmpresa, int idCliente);

        /// <summary>Verifica se existe cliente ativo com o CPF informado (somente digitos).</summary>
        Task<bool> ExistsByCpfAsync(string cpf);

        /// <summary>Verifica se existe cliente ativo com o numero de CNH informado (somente digitos).</summary>
        Task<bool> ExistsByCnhNumeroAsync(string cnhNumero);

        /// <summary>Verifica se existe cliente ativo com o e-mail informado, sem diferenciar maiusculas nem bordas (PENDENTE e RECUSADO tambem contam).</summary>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>Grava um novo cliente com id gerado, datas de auditoria em DateTime.Now e DisabledAt nulo.</summary>
        Task<Cliente> InsertAsync(Cliente cliente);
    }
}
