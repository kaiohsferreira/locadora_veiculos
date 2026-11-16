using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso a dados das apolices (tabela seguro). Todas as buscas ignoram as apolices
    /// desativadas (DisabledAt == null). Nao ha exclusao fisica.
    /// </summary>
    public interface ISeguroRepository
    {
        /// <summary>Lista as apolices ativas que atendem aos filtros, ordenadas por fim da vigencia e id, na pagina pedida, com o veiculo.</summary>
        Task<List<Seguro>> FindAllAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte, int deslocamento, int quantidade);

        /// <summary>Conta as apolices ativas que atendem aos mesmos filtros de FindAllAsync.</summary>
        Task<int> CountAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte);

        /// <summary>Busca uma apolice ativa pelo id, com o veiculo; null se nao existir.</summary>
        Task<Seguro?> FindByIdAsync(int idSeguro);

        /// <summary>Verifica se existe apolice ativa com o mesmo numero (Trim, sem diferenciar maiusculas), ignorando o id informado.</summary>
        Task<bool> ExistsByNumeroApoliceAsync(string numeroApolice, int? idSeguroIgnorado);

        /// <summary>Grava uma nova apolice e devolve com o id gerado.</summary>
        Task<Seguro> InsertAsync(Seguro seguro);

        /// <summary>Atualiza os dados de uma apolice ativa; false se nao achar.</summary>
        Task<bool> UpdateAsync(Seguro seguro);
    }
}
