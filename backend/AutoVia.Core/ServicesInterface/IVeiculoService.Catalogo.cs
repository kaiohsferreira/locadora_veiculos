using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato do catalogo de seminovos (P6-BE-01). Arquivo parcial do IVeiculoService.
    /// </summary>
    public partial interface IVeiculoService
    {
        /// <summary>
        /// Lista os veiculos a venda (ativos, DISPONIVEL e com valor de venda), paginados, do menor
        /// preco para o maior, com filtros opcionais por marca, preco, ano do modelo e quilometragem.
        /// </summary>
        Task<PaginaVO<ReturnVeiculoCatalogoVO>> GetCatalogoAsync(FiltroCatalogoVO filtro);
    }
}
