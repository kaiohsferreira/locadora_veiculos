using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Detalhe publico do seminovo a venda (P6-FE-02, tela de proposta de compra). Arquivo parcial do VeiculoController.
    /// Acao PUBLICA, como o catalogo: o [AllowAnonymous] sobrepoe a politica Funcionario da classe. A resposta sai no VO
    /// publico do catalogo, sem placa, chassi nem renavam. Sem regra de negocio aqui.
    /// </summary>
    public partial class VeiculoController
    {
        /// <summary>GetAVendaById - Retorna um veiculo do catalogo de seminovos (DISPONIVEL, com valor de venda, locadora ativa).</summary>
        [HttpGet("a-venda/{idVeiculo:int}")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteLeituraPublica)]
        [ProducesResponseType(typeof(ReturnVeiculoCatalogoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetAVendaById(int idVeiculo)
        {
            try
            {
                ReturnVeiculoCatalogoVO retorno = await _veiculoService.GetAVendaByIdAsync(idVeiculo);

                return Ok(retorno);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPropostaBuscarVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
