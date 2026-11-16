using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Catalogo de seminovos (P6-BE-01). Arquivo parcial do VeiculoService: normaliza a marca
    /// (vazia ou so espacos = sem filtro), conta e pagina com os mesmos filtros e devolve
    /// PaginaVO com o VO publico do catalogo.
    /// </summary>
    public partial class VeiculoService
    {
        /// <summary>
        /// Lista os veiculos a venda (ativos, DISPONIVEL e com valor de venda), paginados, do menor
        /// preco para o maior, com filtros opcionais por marca, preco, ano do modelo e quilometragem.
        /// </summary>
        public async Task<PaginaVO<ReturnVeiculoCatalogoVO>> GetCatalogoAsync(FiltroCatalogoVO filtro)
        {
            try
            {
                string marcaNormalizada = (filtro.Marca ?? string.Empty).Trim().ToUpper();
                string? marcaBusca = marcaNormalizada.Length > 0 ? marcaNormalizada : null;

                int total = await _veiculoRepository.CountAVendaAsync(marcaBusca, filtro.PrecoMinimo, filtro.PrecoMaximo, filtro.AnoMinimo, filtro.AnoMaximo, filtro.KmMaximo);
                List<Veiculo> veiculos = await _veiculoRepository.FindAllAVendaAsync(marcaBusca, filtro.PrecoMinimo, filtro.PrecoMaximo, filtro.AnoMinimo, filtro.AnoMaximo, filtro.KmMaximo, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnVeiculoCatalogoVO> itens = _mapper.Map<List<ReturnVeiculoCatalogoVO>>(veiculos);
                PaginaVO<ReturnVeiculoCatalogoVO> pagina = new PaginaVO<ReturnVeiculoCatalogoVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

                return pagina;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }
    }
}
