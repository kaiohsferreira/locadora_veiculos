using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints de cadastro e edicao de veiculos (P1-BE-02). Sem regra de negocio:
    /// a entrada e validada pelo FluentValidation antes da action, a logica fica no
    /// IVeiculoService e a resposta sai somente em VO. Erros viram ProblemDetails no
    /// ErrorHandlingMiddleware.
    /// </summary>
    public partial class VeiculoController
    {
        /// <summary>
        /// GetById - Consulta um veiculo ativo pelo id, com o nome da categoria. Desativado conta como inexistente (404).
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnVeiculoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnVeiculoVO veiculo = await _veiculoService.GetByIdAsync(id);

                return Ok(veiculo);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Insert - Cadastra um veiculo com status DISPONIVEL. Devolve 201 com o header Location apontando para a consulta.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnVeiculoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Insert([FromBody] VeiculoVO veiculoVO)
        {
            try
            {
                ReturnVeiculoVO criado = await _veiculoService.InsertAsync(veiculoVO);

                return CreatedAtAction(nameof(GetById), new { id = criado.IdVeiculo }, criado);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Update - Altera os dados cadastrais de um veiculo ativo; o status nao muda aqui. Devolve 204 sem corpo.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] VeiculoVO veiculoVO)
        {
            try
            {
                await _veiculoService.UpdateAsync(id, veiculoVO);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
