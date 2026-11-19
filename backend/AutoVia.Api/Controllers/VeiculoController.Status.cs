using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Troca manual de status do veiculo (P1-BE-04). Arquivo parcial do VeiculoController. A acao exige a politica
    /// GestorFrota (GESTOR_FROTA ou ADMINISTRADOR) por cima da politica Funcionario da classe. Sem regra de negocio aqui.
    /// </summary>
    public partial class VeiculoController
    {
        /// <summary>
        /// AlterarStatus - Troca manual do status do veiculo: MANUTENCAO, DISPONIVEL (volta da manutencao) ou BAIXADO. Devolve 204 sem corpo.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = ConstantsValues.PoliticaGestorFrota)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AlterarStatus(int id, [FromBody] AlterarStatusVeiculoVO alterarStatusVeiculoVO)
        {
            try
            {
                await _veiculoService.AlterarStatusAsync(id, alterarStatusVeiculoVO);

                return NoContent();
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
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
