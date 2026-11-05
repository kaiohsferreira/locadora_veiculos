using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do servico de categorias de veiculos: listagem paginada, consulta,
    /// cadastro e edicao. As regras de nome unico entre as ativas e de existencia
    /// ficam na implementacao (CategoriaService).
    /// </summary>
    public interface ICategoriaService
    {
        /// <summary>Lista as categorias ativas, paginadas e ordenadas por nome.</summary>
        Task<PaginaVO<ReturnCategoriaVO>> GetAllAsync(FiltroPaginacaoVO filtro);

        /// <summary>Consulta uma categoria ativa pelo id; lanca NotFoundException se nao existir.</summary>
        Task<ReturnCategoriaVO> GetByIdAsync(int idCategoria);

        /// <summary>Cadastra uma nova categoria; lanca BusinessException se o nome ja existe entre as ativas.</summary>
        Task<ReturnCategoriaVO> InsertAsync(CategoriaVO categoriaVO);

        /// <summary>Altera uma categoria ativa; lanca NotFoundException ou BusinessException conforme a regra.</summary>
        Task UpdateAsync(int idCategoria, CategoriaVO categoriaVO);
    }
}
