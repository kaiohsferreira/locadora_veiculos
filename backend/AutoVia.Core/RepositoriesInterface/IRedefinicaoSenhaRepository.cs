using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso aos links de recuperacao de senha (tabela redefinicao_senha). Leituras filtram DisabledAt
    /// nulo, salvo o metodo WithDisableds (contagem por limite). Exclusao sempre logica (Disable()).
    /// Nunca recebe nem devolve o token em claro: so o hash SHA-256.
    /// </summary>
    public interface IRedefinicaoSenhaRepository
    {
        /// <summary>Conta as solicitacoes do titular criadas desde a data, inclusive as desativadas (limite por conta). Sem rastreamento.</summary>
        Task<int> CountSolicitacoesDesdeWithDisabledsAsync(int? idCliente, int? idFuncionario, DateTime desde);

        /// <summary>Desativa os pendentes do titular e grava a nova solicitacao, num unico SaveChanges. Devolve a entidade gravada.</summary>
        Task<RedefinicaoSenha> InsertInvalidandoAnterioresAsync(RedefinicaoSenha nova);

        /// <summary>Busca o link ativo pelo hash do token, com titular ativo carregado (Cliente ou Funcionario). Rastreado; nulo se nao houver.</summary>
        Task<RedefinicaoSenha?> FindPendenteByTokenHashAsync(string tokenHash);

        /// <summary>Troca o hash de senha do titular e desativa este link e os demais pendentes da conta, num unico SaveChanges.</summary>
        Task ConcluirRedefinicaoAsync(RedefinicaoSenha redefinicao, string novoSenhaHash);
    }
}
