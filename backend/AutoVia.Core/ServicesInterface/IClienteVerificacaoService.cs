using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Leituras da equipe que verifica o cadastro do cliente na locadora do contexto (P2-BE-05, RF-10): fila, detalhe e
    /// documentos. Tudo passa pelo vinculo ativo com a locadora do contexto; cliente sem vinculo responde NotFoundException,
    /// igual ao cliente inexistente. A decisao (PATCH) continua no IClienteLocadoraService.
    /// </summary>
    public interface IClienteVerificacaoService
    {
        /// <summary>Lista paginada da fila: clientes com vinculo ativo com a locadora do contexto, com o status do vinculo.</summary>
        Task<PaginaVO<ReturnClienteFilaVO>> ListarFilaAsync(FiltroClienteVerificacaoVO filtro);

        /// <summary>Detalhe do cadastro do cliente para verificacao. NotFoundException sem vinculo ativo com a locadora.</summary>
        Task<ReturnClienteVerificacaoVO> ObterVerificacaoAsync(int idCliente);

        /// <summary>Documentos ativos do cliente para a equipe (URL temporaria nova). NotFoundException sem vinculo; registra o acesso sem dado pessoal.</summary>
        Task<List<ReturnClienteDocumentoVO>> ListarDocumentosAsync(int idCliente, CancellationToken cancellationToken = default);
    }
}
