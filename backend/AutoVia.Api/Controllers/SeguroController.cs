using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints de apolices de seguro (P9-BE-01): listagem paginada com destaque de vencimento,
    /// consulta, cadastro e edicao. Restrito a funcionarios pela politica Funcionario, aplicada
    /// na classe. Sem regra de negocio: a entrada e validada pelo FluentValidation (antes da
    /// action), a logica fica no ISeguroService e a resposta sai so em VO. Erros viram
    /// ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/seguros")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public class SeguroController : ControllerBase
    {
        private readonly ISeguroService _seguroService;
        private readonly ILogService _logService;

        public SeguroController(ISeguroService seguroService, ILogService logService)
        {
            _seguroService = seguroService;
            _logService = logService;
        }

        /// <summary>
        /// GetAll - Lista as apolices ativas, paginadas e ordenadas pelo fim da vigencia, com filtro
        /// opcional por veiculo e por apolices que vencem em ate 60 dias (apenasAVencer).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginaVO<ReturnSeguroVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] FiltroSeguroVO filtro)
        {
            try
            {
                PaginaVO<ReturnSeguroVO> pagina = await _seguroService.GetAllAsync(filtro);

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
                _logService.Write(ex, ConstantsMessage.ErroListarSeguros, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetById - Consulta uma apolice ativa pelo id. Desativada conta como inexistente (404).
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnSeguroVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnSeguroVO seguro = await _seguroService.GetByIdAsync(id);

                return Ok(seguro);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Insert - Cadastra uma apolice para um veiculo. Devolve 201 com o header Location apontando para a consulta.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnSeguroVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Insert([FromBody] SeguroVO seguroVO)
        {
            try
            {
                ReturnSeguroVO criado = await _seguroService.InsertAsync(seguroVO);

                return CreatedAtAction(nameof(GetById), new { id = criado.IdSeguro }, criado);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Update - Altera os dados de uma apolice ativa. Devolve 204 sem corpo.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] SeguroVO seguroVO)
        {
            try
            {
                await _seguroService.UpdateAsync(id, seguroVO);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }
    }
}
