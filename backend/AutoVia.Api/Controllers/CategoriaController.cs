using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints de categorias de veiculos (modulo Frota): listagem paginada, consulta,
    /// cadastro e edicao. Nao tem regra de negocio: a entrada e validada pelo
    /// FluentValidation (antes da action), a logica fica no ICategoriaService e a
    /// resposta sai somente em VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/categorias")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ILogService _logService;

        public CategoriaController(ICategoriaService categoriaService, ILogService logService)
        {
            _categoriaService = categoriaService;
            _logService = logService;
        }

        /// <summary>
        /// GetAll - Lista as categorias ativas, paginadas e ordenadas por nome.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginaVO<ReturnCategoriaVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] FiltroPaginacaoVO filtro)
        {
            try
            {
                PaginaVO<ReturnCategoriaVO> pagina = await _categoriaService.GetAllAsync(filtro);

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
                _logService.Write(ex, ConstantsMessage.ErroListarCategorias, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetById - Consulta uma categoria ativa pelo id. Desativada conta como inexistente (404).
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReturnCategoriaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnCategoriaVO categoria = await _categoriaService.GetByIdAsync(id);

                return Ok(categoria);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarCategoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Insert - Cadastra uma nova categoria. Devolve 201 com o header Location apontando para a consulta.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
        [ProducesResponseType(typeof(ReturnCategoriaVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Insert([FromBody] CategoriaVO categoriaVO)
        {
            try
            {
                ReturnCategoriaVO criada = await _categoriaService.InsertAsync(categoriaVO);

                return CreatedAtAction(nameof(GetById), new { id = criada.IdCategoria }, criada);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Update - Altera os dados de uma categoria ativa. Devolve 204 sem corpo.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaVO categoriaVO)
        {
            try
            {
                await _categoriaService.UpdateAsync(id, categoriaVO);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }
    }
}
