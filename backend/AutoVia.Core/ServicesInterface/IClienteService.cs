using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do servico de cliente (RF-07): cadastro publico, verificacao publica de
    /// existencia (CPF, e-mail ou CNH), listagem paginada e consulta para funcionario. As
    /// regras de CPF, e-mail e CNH unicos entre ativos, de normalizacao e de status inicial
    /// ficam na implementacao (ClienteService).
    /// </summary>
    public interface IClienteService
    {
        /// <summary>Lista os clientes ativos, paginados, com filtro opcional por status, ordenados por data de cadastro.</summary>
        Task<PaginaVO<ReturnClienteVO>> GetAllAsync(ClienteFiltroVO filtro);

        /// <summary>Consulta um cliente ativo pelo id; lanca NotFoundException se nao existir ou estiver desativado.</summary>
        Task<ReturnClienteVO> GetByIdAsync(int idCliente);

        /// <summary>Cadastra um cliente PENDENTE; lanca BusinessException se o CPF, o e-mail ou a CNH ja existem entre os ativos (nessa ordem).</summary>
        Task<ReturnClienteVO> InsertAsync(ClienteVO clienteVO);

        /// <summary>Informa se existe cliente ativo com o valor do campo (CPF, EMAIL ou CNH), normalizado como no cadastro. Devolve so a existencia.</summary>
        Task<ReturnVerificacaoCadastroVO> VerificarCadastroAsync(VerificarCadastroVO verificarCadastroVO);
    }
}
