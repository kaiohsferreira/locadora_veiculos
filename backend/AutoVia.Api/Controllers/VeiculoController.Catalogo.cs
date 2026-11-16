using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Catalogo de seminovos (P6-BE-01). Arquivo parcial do VeiculoController. Esta acao e
    /// PUBLICA: o [AllowAnonymous] sobrepoe a politica Funcionario aplicada na classe, porque
    /// o catalogo e exibido no site para quem ainda nao tem conta. Sem regra de negocio aqui;
    /// a resposta sai so no VO publico, sem placa, chassi nem renavam.
    /// </summary>
    public partial class VeiculoController
    {
        /// <summary>
        /// GetCatalogo - Lista os veiculos a venda (DISPONIVEL e com valor de venda), do menor preco
        /// para o maior, com filtros opcionais por marca, faixa de preco, ano do modelo e quilometragem maxima.
        /// </summary>
        [HttpGet("a-venda")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginaVO<ReturnVeiculoCatalogoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCatalogo([FromQuery] FiltroCatalogoVO filtro)
        {
            try
            {
                PaginaVO<ReturnVeiculoCatalogoVO> pagina = await _veiculoService.GetCatalogoAsync(filtro);

                return Ok(pagina);
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
