using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
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
    /// Endpoints do cadastro do cliente (RF-07): cadastro publico (sem token), listagem
    /// paginada e consulta por id para funcionario. Nao tem regra de negocio: a entrada
    /// e validada pelo FluentValidation antes da action, a logica fica no IClienteService
    /// e a resposta sai somente em VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/clientes")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly ILogService _logService;

        public ClienteController(IClienteService clienteService, ILogService logService)
        {
            _clienteService = clienteService;
            _logService = logService;
        }

        /// <summary>
        /// Insert - Cadastra um cliente com status PENDENTE (rota publica, sem token). Devolve 201 com o header Location apontando para a consulta. Nao devolve token: o login e feito depois em /api/auth/login.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(typeof(ReturnClienteVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Insert([FromBody] ClienteVO clienteVO)
        {
            try
            {
                ReturnClienteVO criado = await _clienteService.InsertAsync(clienteVO);

                return CreatedAtAction(nameof(GetById), new { id = criado.IdCliente }, criado);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetAll - Lista os clientes ativos, paginados, com filtro opcional por status (PENDENTE, APROVADO, RECUSADO). Exige token de funcionario.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
        [ProducesResponseType(typeof(PaginaVO<ReturnClienteVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] ClienteFiltroVO filtro)
        {
            try
            {
                PaginaVO<ReturnClienteVO> pagina = await _clienteService.GetAllAsync(filtro);

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
                _logService.Write(ex, ConstantsMessage.ErroListarClientes, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// VerificarCadastro - Informa se ja existe cliente ativo com o CPF, o e-mail ou a CNH informados (um campo por chamada, valor so no corpo). Rota publica, com limite proprio por IP. Devolve 200 com { existe } (true ou false) e nada mais.
        /// </summary>
        [HttpPost("verificar-cadastro")]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteVerificacaoCadastro)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoVerificacaoCadastro)]
        [ProducesResponseType(typeof(ReturnVerificacaoCadastroVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerificarCadastro([FromBody] VerificarCadastroVO verificarCadastroVO)
        {
            try
            {
                ReturnVerificacaoCadastroVO retorno = await _clienteService.VerificarCadastroAsync(verificarCadastroVO);

                return Ok(retorno);
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
                _logService.Write(ex, ConstantsMessage.ErroVerificarCadastroCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetById - Consulta um cliente ativo pelo id. Exige token de funcionario. Desativado conta como inexistente (404).
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
        [ProducesResponseType(typeof(ReturnClienteVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnClienteVO cliente = await _clienteService.GetByIdAsync(id);

                return Ok(cliente);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }
    }
}
