using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints da equipe da locadora (P10-BE-03): listar, consultar, cadastrar, editar e desativar/reativar funcionarios.
    /// Todas as rotas exigem a politica Administrador (ADMINISTRADOR autenticado com a locadora do banco). Sem regra de
    /// negocio: a entrada e validada pelo FluentValidation antes da action, a logica fica no IEquipeService e a resposta
    /// sai somente em VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/equipe")]
    [Authorize(Policy = ConstantsValues.PoliticaAdministrador)]
    public class EquipeController : ControllerBase
    {
        private readonly IEquipeService _equipeService;
        private readonly ILogService _logService;

        public EquipeController(IEquipeService equipeService, ILogService logService)
        {
            _equipeService = equipeService;
            _logService = logService;
        }

        /// <summary>
        /// GetAll - Lista paginada da equipe da locadora, com busca (nome, CPF ou matricula), cargo e situacao (ativo true,
        /// false ou ausente = todos). Traz os totais da locadora inteira (ativos, inativos, administradores e cadastrados).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ReturnEquipeVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] FiltroEquipeVO filtro)
        {
            try
            {
                ReturnEquipeVO equipe = await _equipeService.GetAllAsync(filtro);

                return Ok(equipe);
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
                _logService.Write(ex, ConstantsMessage.ErroListarEquipe, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetById - Consulta um funcionario da locadora pelo id (usado na edicao e no Location do cadastro). Inexistente,
        /// desativado ou de outra locadora devolve 404 com a mesma mensagem.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnFuncionarioVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnFuncionarioVO funcionario = await _equipeService.GetByIdAsync(id);

                return Ok(funcionario);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Insert - Cadastra um funcionario na locadora do ADMINISTRADOR com a senha inicial informada. Devolve 201 com o
        /// header Location apontando para a consulta. A senha nunca volta na resposta.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnFuncionarioVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Insert([FromBody] FuncionarioVO funcionarioVO)
        {
            try
            {
                ReturnFuncionarioVO criado = await _equipeService.InsertAsync(funcionarioVO);

                return CreatedAtAction(nameof(GetById), new { id = criado.IdFuncionario }, criado);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Update - Altera nome, matricula, e-mail, data de admissao e cargo do funcionario da locadora. O CPF nao e
        /// editavel. Devolve 204. Se o cargo mudar, a sessao do funcionario e descartada: vale na requisicao seguinte.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] FuncionarioEdicaoVO funcionarioVO)
        {
            try
            {
                await _equipeService.UpdateAsync(id, funcionarioVO);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// AlterarSituacao - Desativa (ativo = false) ou reativa (ativo = true) o acesso do funcionario. Nao e exclusao:
        /// o registro continua na equipe. Devolve 204.
        /// </summary>
        [HttpPatch("{id:int}/situacao")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AlterarSituacao(int id, [FromBody] SituacaoFuncionarioVO situacaoVO)
        {
            try
            {
                await _equipeService.AlterarSituacaoAsync(id, situacaoVO);

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
                _logService.Write(ex, ConstantsMessage.ErroAlterarSituacaoFuncionario, GetType().ToString());
                throw;
            }
        }
    }
}
