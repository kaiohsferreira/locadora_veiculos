using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Listagem de veiculos (P1-BE-03). Arquivo parcial do VeiculoService: normaliza a placa
    /// do filtro (vazia ou so espacos = sem filtro), conta e pagina com os mesmos filtros e
    /// devolve PaginaVO.
    /// </summary>
    public partial class VeiculoService
    {
        /// <summary>
        /// Lista os veiculos ativos, paginados e ordenados por placa, com filtros opcionais por categoria, status e placa.
        /// </summary>
        public async Task<PaginaVO<ReturnVeiculoVO>> GetAllAsync(FiltroVeiculoVO filtro)
        {
            try
            {
                string placaNormalizada = StaticMethods.NormalizePlaca(filtro.Placa);
                string? placaBusca = placaNormalizada.Length > 0 ? placaNormalizada : null;

                int total = await _veiculoRepository.CountAsync(filtro.IdCategoria, filtro.Status, placaBusca);
                List<Veiculo> veiculos = await _veiculoRepository.FindAllAsync(filtro.IdCategoria, filtro.Status, placaBusca, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnVeiculoVO> itens = _mapper.Map<List<ReturnVeiculoVO>>(veiculos);
                PaginaVO<ReturnVeiculoVO> pagina = new PaginaVO<ReturnVeiculoVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

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
                _logService.Write(ex, ConstantsMessage.ErroListarVeiculos, GetType().ToString());
                throw;
            }
        }
    }
}
