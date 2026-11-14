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
    /// paginada, consulta por id para funcionario e verificacao do cadastro pela locadora
    /// (T-BE-06). Nao tem regra de negocio: a entrada e validada pelo FluentValidation antes
    /// da action, a logica fica nos servicos e a resposta sai somente em VO. Erros viram
    /// ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/clientes")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IClienteLocadoraService _clienteLocadoraService;
        private readonly ILogService _logService;

        public ClienteController(IClienteService clienteService, IClienteLocadoraService clienteLocadoraService, ILogService logService)
        {
            _clienteService = clienteService;
            _clienteLocadoraService = clienteLocadoraService;
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
        /// GetAll - Lista os clientes ativos, paginados, com filtro opcional por status (PENDENTE, APROVADO, RECUSADO). Exige token de funcionario e so devolve clientes com reserva, locacao, venda ou vinculo de cadastro com a locadora do funcionario (total e pagina no mesmo escopo). A CNH completa sai so para ATENDENTE e ADMINISTRADOR; outros cargos recebem a CNH mascarada.
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
        /// GetById - Consulta um cliente ativo pelo id. Exige token de funcionario. Desativado, inexistente ou sem reserva, locacao, venda ou vinculo de cadastro na locadora do funcionario conta como inexistente (404). A CNH completa sai so para ATENDENTE e ADMINISTRADOR; outros cargos recebem a CNH mascarada.
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

        /// <summary>
        /// Verificacao - Registra a decisao da locadora sobre o cadastro do cliente (APROVADO ou RECUSADO). Exige token de
        /// ATENDENTE ou ADMINISTRADOR da locadora. A justificativa e obrigatoria na recusa e fica interna. Devolve 204 sem
        /// corpo. Cliente sem vinculo com a locadora do contexto devolve 404; cadastro ja aprovado devolve 409.
        /// </summary>
        [HttpPatch("{id:int}/verificacao")]
        [Authorize(Policy = ConstantsValues.PoliticaVerificadorCadastro)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Verificacao(int id, [FromBody] VerificacaoCadastroVO verificacaoVO)
        {
            try
            {
                await _clienteLocadoraService.DecidirAsync(id, verificacaoVO);

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
                _logService.Write(ex, ConstantsMessage.ErroDecidirCadastroCliente, GetType().ToString());
                throw;
            }
        }
    }
}
