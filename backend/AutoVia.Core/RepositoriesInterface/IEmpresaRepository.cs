using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso a empresa (locadora) no cadastro publico (P10-BE-01). Empresa e global (sem filtro de locadora),
    /// entao as consultas de unicidade alcancam a plataforma inteira. Todas filtram DisabledAt nulo.
    /// </summary>
    public interface IEmpresaRepository
    {
        /// <summary>Indica se existe empresa ativa (DisabledAt nulo, ativa ou nao) com o CNPJ informado (so digitos).</summary>
        Task<bool> ExistsByCnpjAsync(string cnpj);

        /// <summary>Indica se existe empresa ativa (DisabledAt nulo) com o e-mail informado, ja aparado e em minusculas.</summary>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>Indica se o CNPJ informado e o da empresa operadora da plataforma (empresa inicial).</summary>
        Task<bool> ExistsOperadoraByCnpjAsync(string cnpj);

        /// <summary>Indica se existe funcionario ativo (DisabledAt nulo, com ou sem Ativo) com o CPF informado, de QUALQUER empresa (bypass do escopo, declarado).</summary>
        Task<bool> ExistsFuncionarioByCpfAsync(string cpf);

        /// <summary>Grava a empresa e o responsavel na MESMA gravacao (um unico SaveChangesAsync) e devolve a empresa com os ids gerados.</summary>
        Task<Empresa> InsertComResponsavelAsync(Empresa empresa, Funcionario responsavel);
    }
}
