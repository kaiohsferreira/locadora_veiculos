using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do servico de apolices (P9-BE-01): listagem paginada com destaque de
    /// vencimento, consulta, cadastro e edicao. As regras (veiculo existente, numero unico
    /// entre as ativas, calculo do destaque) ficam na implementacao (SeguroService).
    /// </summary>
    public interface ISeguroService
    {
        /// <summary>Lista as apolices ativas, paginadas e ordenadas pelo fim da vigencia, com filtros opcionais.</summary>
        Task<PaginaVO<ReturnSeguroVO>> GetAllAsync(FiltroSeguroVO filtro);

        /// <summary>Consulta uma apolice ativa pelo id; lanca NotFoundException se nao existir.</summary>
        Task<ReturnSeguroVO> GetByIdAsync(int idSeguro);

        /// <summary>Cadastra uma apolice; NotFoundException se o veiculo nao existir, BusinessException se o numero ja existir.</summary>
        Task<ReturnSeguroVO> InsertAsync(SeguroVO seguroVO);

        /// <summary>Altera uma apolice ativa; lanca NotFoundException ou BusinessException conforme a regra.</summary>
        Task UpdateAsync(int idSeguro, SeguroVO seguroVO);
    }
}
