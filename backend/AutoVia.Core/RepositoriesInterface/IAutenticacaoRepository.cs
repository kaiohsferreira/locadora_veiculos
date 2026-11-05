using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso aos dados de login de clientes e funcionarios. Buscas filtram registros
    /// ativos (DisabledAt nulo); a regra de Ativo do funcionario fica no servico.
    /// </summary>
    public interface IAutenticacaoRepository
    {
        /// <summary>Busca o cliente ativo pelo CPF (somente digitos); nulo se nao existir. Sem rastreamento.</summary>
        Task<Cliente?> FindClienteByCpfAsync(string cpf);

        /// <summary>Busca o funcionario ativo pelo CPF (somente digitos); nulo se nao existir. Sem rastreamento.</summary>
        Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf);

        /// <summary>Indica se existe funcionario ativo com o CPF informado.</summary>
        Task<bool> ExistsFuncionarioByCpfAsync(string cpf);

        /// <summary>Indica se existe funcionario com o CPF informado, ativo ou desativado (busca sem o filtro de DisabledAt). Usada pelo administrador inicial para nao recriar uma conta desativada.</summary>
        Task<bool> ExistsFuncionarioByCpfWithDisabledsAsync(string cpf);

        /// <summary>Grava um novo funcionario com datas de auditoria e DisabledAt nulo; devolve a entidade gravada.</summary>
        Task<Funcionario> InsertFuncionarioAsync(Funcionario funcionario);
    }
}
