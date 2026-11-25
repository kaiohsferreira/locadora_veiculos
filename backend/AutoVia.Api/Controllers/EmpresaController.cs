using AutoVia.Api.Filters;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Cadastro público de locadoras, verificação de CNPJ e e-mail e consulta das cidades com locadoras ativas.
    /// A chave Locadoras:CadastroPublicoHabilitado controla o cadastro e a verificação. Erros seguem ProblemDetails.
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

        /// <summary>Lista as cidades com locadoras ativas, com cidade e UF.</summary>
        [HttpGet("cidades")]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteLeituraPublica)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(List<ReturnCidadeLocadoraVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetCidades()
        {
            try
            {
                List<ReturnCidadeLocadoraVO> cidades = await _empresaService.GetCidadesAsync();
                return Ok(cidades);
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
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
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
