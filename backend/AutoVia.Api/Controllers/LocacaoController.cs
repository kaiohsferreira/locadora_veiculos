using AutoVia.Api.Extensions;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints de locacao restritos a funcionarios autenticados. A abertura (POST) tem ainda a politica AberturaLocacao: com a
    /// caucao exigida, so ATENDENTE e ADMINISTRADOR; e, so com a caucao exigida, o teto de tentativas por funcionario.
    /// </summary>
    [ApiController]
    [Route("api/locacoes")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public class LocacaoController : ControllerBase
    {
        private readonly ILocacaoService _locacaoService;
        private readonly ILogService _logService;
        private readonly CaucaoRetiradaOptions _caucaoRetiradaOptions;
        private readonly TetoCaucaoPorFuncionario _tetoCaucao;

        public LocacaoController(ILocacaoService locacaoService, ILogService logService, CaucaoRetiradaOptions caucaoRetiradaOptions, TetoCaucaoPorFuncionario tetoCaucao)
        {
            _locacaoService = locacaoService;
            _logService = logService;
            _caucaoRetiradaOptions = caucaoRetiradaOptions;
            _tetoCaucao = tetoCaucao;
        }

        /// <summary>Consulta o contexto da retirada de uma reserva da locadora autenticada.</summary>
        [HttpGet("retirada/{idReserva:int}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnRetiradaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRetirada(int idReserva)
        {
            try
            {
                ReturnRetiradaVO retirada = await _locacaoService.GetRetiradaAsync(idReserva);
                return Ok(retirada);
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta uma locacao ativa pelo identificador.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnLocacaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnLocacaoVO locacao = await _locacaoService.GetByIdAsync(id);

                return Ok(locacao);
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (UnauthorizedException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroNaoAutenticado, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Inicia uma locacao de uma reserva ativa ou confirmada; com caucao exigida, pre-autoriza o cartao e grava a caucao junto; devolve 201 e Location.</summary>
        [HttpPost]
        [Authorize(Policy = ConstantsValues.PoliticaAberturaLocacao)]
        [ProducesResponseType(typeof(ReturnLocacaoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> StartFromReserva([FromBody] LocacaoVO locacaoVO)
        {
            try
            {
                Claim? claimFuncionario = User.FindFirst(ClaimTypes.NameIdentifier);

                if (claimFuncionario == null ||
                    !int.TryParse(claimFuncionario.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) ||
                    idFuncionario <= 0)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                VerificarTetoCaucao(idFuncionario);

                ReturnLocacaoVO criada = await _locacaoService.StartFromReservaAsync(locacaoVO, idFuncionario);

                return CreatedAtAction(nameof(GetById), new { id = criada.IdLocacao }, criada);
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (UnauthorizedException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroNaoAutenticado, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Conta a abertura no teto do funcionario SO quando a caucao e exigida (com valor 0 nada muda); recusa com 429 quando a
        /// janela esgotou. Roda depois da claim e antes do servico (nenhuma chamada ao provedor quando recusa).
        /// </summary>
        private void VerificarTetoCaucao(int idFuncionario)
        {
            if (!_caucaoRetiradaOptions.CaucaoExigida)
            {
                return;
            }

            string chave = ConstantsValues.PrefixoChaveLimiteCaucaoFuncionario + idFuncionario.ToString(CultureInfo.InvariantCulture);

            if (!_tetoCaucao.TentarEntrar(chave))
            {
                throw new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes);
            }
        }
    }
}
