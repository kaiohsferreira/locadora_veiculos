using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso ao vinculo entre a conta Google (sub) e o cliente (cliente.google_sub). Buscas e escritas
    /// consideram so clientes ativos (DisabledAt nulo). Sem indice unico: a unicidade fica no servico.
    /// </summary>
    public interface IGoogleVinculoRepository
    {
        /// <summary>Busca o cliente ativo vinculado a esta conta Google (sub); nulo se nao houver. Sem rastreamento.</summary>
        Task<Cliente?> FindClienteByGoogleSubAsync(string googleSub);

        /// <summary>Busca, entre os clientes ativos, os que tem este e-mail (comparacao em minusculas), limitado a quantidade informada. Sem rastreamento.</summary>
        Task<List<Cliente>> FindClientesByEmailAsync(string email, int quantidade);

        /// <summary>Indica se algum cliente ativo, diferente do informado, ja usa esta conta Google (sub).</summary>
        Task<bool> ExistsClienteByGoogleSubAsync(string googleSub, int idClienteIgnorado);

        /// <summary>Grava a conta Google (sub) no cliente ativo e atualiza UpdatedAt com DateTime.Now. Devolve false se o cliente nao existe ou esta desativado.</summary>
        Task<bool> UpdateGoogleSubAsync(int idCliente, string googleSub);
    }
}
