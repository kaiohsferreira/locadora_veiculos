using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Metodos de cadastro e edicao de veiculos (P1-BE-02). Todas as leituras consideram
    /// somente veiculos ativos (DisabledAt nulo).
    /// </summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>Busca um veiculo ativo pelo id, com a categoria. Null se nao existir ou estiver desativado.</summary>
        Task<Veiculo?> FindByIdAsync(int idVeiculo);

        /// <summary>Verifica se existe veiculo ativo com a mesma placa (sem diferenciar maiusculas e espacos), ignorando o id informado.</summary>
        Task<bool> ExistsByPlacaAsync(string placa, int? idVeiculoIgnorado);

        /// <summary>Verifica se existe veiculo ativo com o mesmo chassi (sem diferenciar maiusculas e espacos), ignorando o id informado.</summary>
        Task<bool> ExistsByChassiAsync(string chassi, int? idVeiculoIgnorado);

        /// <summary>Verifica se existe veiculo ativo com o mesmo renavam (sem diferenciar maiusculas e espacos), ignorando o id informado.</summary>
        Task<bool> ExistsByRenavamAsync(string renavam, int? idVeiculoIgnorado);

        /// <summary>Grava um novo veiculo com id gerado, CreatedAt = UpdatedAt = DateTime.Now e DisabledAt nulo.</summary>
        Task<Veiculo> InsertAsync(Veiculo veiculo);

        /// <summary>Atualiza os dados cadastrais do veiculo ativo. Nao altera Status, CreatedAt nem DisabledAt. False se nao achar.</summary>
        Task<bool> UpdateAsync(Veiculo veiculo);
    }
}
