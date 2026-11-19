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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Fila e detalhe da verificacao do cadastro do cliente pela equipe da locadora (P2-BE-05, RF-10): fila paginada, detalhe
    /// e documentos. Politica VerificadorCadastro (ATENDENTE ou ADMINISTRADOR). A decisao (PATCH) fica no ClienteController.
    /// Sem regra de negocio aqui: a entrada e validada antes da action, a logica fica no servico e a resposta sai somente em VO.
    /// </summary>
    [ApiController]
    [Route("api/clientes")]
    public class ClienteVerificacaoController : ControllerBase
    {
        private readonly IClienteVerificacaoService _clienteVerificacaoService;
        private readonly ILogService _logService;

        public ClienteVerificacaoController(IClienteVerificacaoService clienteVerificacaoService, ILogService logService)
        {
            _clienteVerificacaoService = clienteVerificacaoService;
            _logService = logService;
        }

        /// <summary>
        /// Fila - Lista os cadastros da locadora do funcionario: clientes com vinculo ativo com ela, com o status desse vinculo
        /// (PENDENTE, APROVADO ou RECUSADO; ausente = todos). Busca por nome, e-mail ou CPF (ate 60 caracteres). Ordem: data do
        /// pedido, do mais antigo para o mais novo. Exige token de ATENDENTE ou ADMINISTRADOR.
        /// </summary>
        [HttpGet("verificacoes")]
        [Authorize(Policy = ConstantsValues.PoliticaVerificadorCadastro)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(PaginaVO<ReturnClienteFilaVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ListarFila([FromQuery] FiltroClienteVerificacaoVO filtro)
        {
            try
            {
                PaginaVO<ReturnClienteFilaVO> pagina = await _clienteVerificacaoService.ListarFilaAsync(filtro);

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
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Detalhe - Dados do cadastro do cliente para verificacao: os campos da fila e os dados de cadastro, CNH e endereco.
        /// Sem senha. Cliente sem vinculo ativo com a locadora do funcionario devolve 404, igual ao cliente inexistente.
        /// </summary>
        [HttpGet("{id:int}/verificacao")]
        [Authorize(Policy = ConstantsValues.PoliticaVerificadorCadastro)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnClienteVerificacaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterVerificacao(int id)
        {
            try
            {
                ReturnClienteVerificacaoVO detalhe = await _clienteVerificacaoService.ObterVerificacaoAsync(id);

                return Ok(detalhe);
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
                _logService.Write(ex, ConstantsMessage.ErroObterVerificacaoCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Documentos da verificacao - Lista os documentos ativos do cliente (no maximo um por tipo, CNH antes do comprovante),
        /// cada um com URL temporaria nova. Cliente sem vinculo ativo com a locadora do funcionario devolve 404. Cada chamada
        /// bem-sucedida fica registrada (funcionario, cliente e quantidade). Resposta sem cache e com limite por IP.
        /// </summary>
        [HttpGet("{id:int}/verificacao/documentos")]
        [Authorize(Policy = ConstantsValues.PoliticaVerificadorCadastro)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteListaDocumentoEquipe)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(List<ReturnClienteDocumentoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> ListarDocumentos(int id)
        {
            try
            {
                List<ReturnClienteDocumentoVO> documentos = await _clienteVerificacaoService.ListarDocumentosAsync(id, HttpContext.RequestAborted);

                return Ok(documentos);
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
                _logService.Write(ex, ConstantsMessage.ErroListarDocumentosVerificacao, GetType().ToString());
                throw;
            }
        }
    }
}
