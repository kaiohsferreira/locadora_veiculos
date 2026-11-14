using AutoVia.Api.Filters;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints do cadastro publico de locadora (P10-BE-01), anonimos e sem token: o cadastro da empresa com o
    /// responsavel ADMINISTRADOR e a verificacao de CNPJ e e-mail. Sem regra de negocio: a entrada e validada pelo
    /// FluentValidation antes da action, a logica fica no IEmpresaService e a resposta sai somente em VO. Com a
    /// chave Locadoras:CadastroPublicoHabilitado desligada, as duas rotas respondem 404 antes de validar o corpo.
    /// Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/empresas")]
    public class EmpresaController : ControllerBase
    {
        private readonly IEmpresaService _empresaService;
        private readonly ILogService _logService;

        public EmpresaController(IEmpresaService empresaService, ILogService logService)
        {
            _empresaService = empresaService;
            _logService = logService;
        }

        /// <summary>
        /// Insert - Cadastra a locadora e o seu responsavel (ADMINISTRADOR) numa unica gravacao. Rota publica, sem token,
        /// com limite de 5 cadastros por hora por IP. Devolve 201 com os dados da empresa e do responsavel, sem Location
        /// (nao ha GET de empresa neste escopo) e sem token: o login e feito depois em /api/auth/login.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [CadastroLocadoraHabilitado]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteCadastroEmpresa)]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoCadastroLocadora)]
        [ProducesResponseType(typeof(ReturnEmpresaVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Insert([FromBody] CadastroLocadoraVO cadastroLocadoraVO)
        {
            try
            {
                ReturnEmpresaVO criada = await _empresaService.InsertAsync(cadastroLocadoraVO);

                return StatusCode(StatusCodes.Status201Created, criada);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// VerificarCadastro - Informa se ja existe empresa ativa com o CNPJ ou o e-mail informado (um campo por chamada,
        /// valor so no corpo). Rota publica, com limite proprio por IP. Devolve 200 com { existe } e nada mais; nunca
        /// fica em cache.
        /// </summary>
        [HttpPost("verificar-cadastro")]
        [AllowAnonymous]
        [CadastroLocadoraHabilitado]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteVerificacaoCadastro)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoVerificacaoCadastro)]
        [ProducesResponseType(typeof(ReturnVerificacaoCadastroEmpresaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerificarCadastro([FromBody] VerificarCadastroEmpresaVO verificarCadastroVO)
        {
            try
            {
                ReturnVerificacaoCadastroEmpresaVO retorno = await _empresaService.VerificarCadastroAsync(verificarCadastroVO);

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
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroVerificarCadastroLocadora, GetType().ToString());
                throw;
            }
        }
    }
}
