using AutoVia.Communication.ViewObjects.Equipe;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Equipe da locadora (P10-BE-03): listar, consultar, cadastrar, editar e desativar/reativar funcionarios da locadora
    /// do ADMINISTRADOR autenticado. A empresa vem sempre do contexto; id de outra locadora responde como inexistente.
    /// </summary>
    public interface IEquipeService
    {
        /// <summary>Lista paginada da equipe com os totais da locadora inteira.</summary>
        Task<ReturnEquipeVO> GetAllAsync(FiltroEquipeVO filtro);

        /// <summary>Consulta um funcionario da locadora pelo id.</summary>
        Task<ReturnFuncionarioVO> GetByIdAsync(int idFuncionario);

        /// <summary>Cadastra um funcionario na locadora do contexto, com senha inicial gravada so como hash.</summary>
        Task<ReturnFuncionarioVO> InsertAsync(FuncionarioVO funcionarioVO);

        /// <summary>Altera nome, matricula, e-mail, admissao e cargo. CPF nunca muda.</summary>
        Task UpdateAsync(int idFuncionario, FuncionarioEdicaoVO funcionarioVO);

        /// <summary>Desativa (false) ou reativa (true) o acesso do funcionario da locadora.</summary>
        Task AlterarSituacaoAsync(int idFuncionario, SituacaoFuncionarioVO situacaoVO);
    }
}
