using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Acesso aos funcionarios da equipe da locadora (P10-BE-03). Todas as consultas e gravacoes passam pelo filtro
    /// "Empresa" do contexto (a locadora do ADMINISTRADOR autenticado): funcionario de outra locadora nao aparece.
    /// Consultas filtram DisabledAt nulo. Nenhum metodo remove a linha; desativar e mudar Ativo. Nao ha bypass aqui.
    /// </summary>
    public interface IFuncionarioRepository
    {
        /// <summary>Indica se existe funcionario ativo (DisabledAt nulo) da locadora com a matricula (sem diferenca de maiusculas), ignorando o id informado.</summary>
        Task<bool> ExistsByMatriculaAsync(string matricula, int? idFuncionarioIgnorado);

        /// <summary>Busca o funcionario da locadora pelo id, sem rastreamento; nulo se nao existir, estiver desativado ou for de outra locadora.</summary>
        Task<Funcionario?> FindByIdAsync(int idFuncionario);

        /// <summary>Lista os funcionarios da locadora que atendem aos filtros, ordenados por nome, paginados. Busca ja normalizada pelo servico.</summary>
        Task<List<Funcionario>> FindAllAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo, int deslocamento, int quantidade);

        /// <summary>Conta os funcionarios da locadora que atendem aos mesmos filtros da listagem.</summary>
        Task<int> CountAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo);

        /// <summary>Conta os ADMINISTRADOR ativos da locadora, ignorando o id informado (base da regra do ultimo administrador).</summary>
        Task<int> CountAdministradoresAtivosAsync(int? idFuncionarioIgnorado);

        /// <summary>Grava o funcionario (id gerado, auditoria com DateTime.Now, DisabledAt nulo) e devolve o registro gravado.</summary>
        Task<Funcionario> InsertAsync(Funcionario funcionario);

        /// <summary>Atualiza nome, matricula, e-mail, data de admissao e cargo. Nao altera CPF, senha, empresa nem situacao. Falso se nao achou.</summary>
        Task<bool> UpdateAsync(Funcionario funcionario);

        /// <summary>Grava a situacao do acesso (Ativo). Nao usa exclusao. Falso se nao achou o funcionario da locadora.</summary>
        Task<bool> SetSituacaoAsync(int idFuncionario, bool ativo);
    }
}
