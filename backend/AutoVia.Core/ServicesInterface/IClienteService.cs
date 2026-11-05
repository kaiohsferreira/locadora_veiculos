using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do servico de cliente (RF-07): cadastro publico, listagem paginada e
    /// consulta para funcionario. As regras de CPF e CNH unicos entre ativos, de
    /// normalizacao e de status inicial ficam na implementacao (ClienteService).
    /// </summary>
    public interface IClienteService
    {
        /// <summary>Lista os clientes ativos, paginados, com filtro opcional por status, ordenados por data de cadastro.</summary>
        Task<PaginaVO<ReturnClienteVO>> GetAllAsync(ClienteFiltroVO filtro);

        /// <summary>Consulta um cliente ativo pelo id; lanca NotFoundException se nao existir ou estiver desativado.</summary>
        Task<ReturnClienteVO> GetByIdAsync(int idCliente);

        /// <summary>Cadastra um cliente PENDENTE; lanca BusinessException se o CPF ou a CNH ja existem entre os ativos.</summary>
        Task<ReturnClienteVO> InsertAsync(ClienteVO clienteVO);
    }
}
