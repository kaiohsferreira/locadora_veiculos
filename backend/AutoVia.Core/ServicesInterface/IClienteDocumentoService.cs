using AutoVia.Communication.ViewObjects.Clientes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Documentos do cliente (CNH e comprovante de residencia, RF-08): envio e listagem. O dono e o id da sessao; id da rota
    /// diferente do da sessao responde NotFoundException, como cliente inexistente. O arquivo chega como bytes e o tipo do
    /// conteudo e decidido pelo servico.
    /// </summary>
    public interface IClienteDocumentoService
    {
        /// <summary>Envia o documento (ja lido do formulario): guarda o arquivo como veio, troca o ativo do mesmo tipo e devolve o documento com URL temporaria.</summary>
        Task<ReturnClienteDocumentoVO> EnviarAsync(int idClienteRota, int idClienteSessao, string? tipo, byte[]? conteudo, string? nomeOriginal, CancellationToken cancellationToken = default);

        /// <summary>Lista os documentos ativos do cliente, no maximo um por tipo (CNH antes do comprovante), com URL temporaria nova.</summary>
        Task<List<ReturnClienteDocumentoVO>> ListarAsync(int idClienteRota, int idClienteSessao, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista os documentos ativos de um cliente para a equipe da locadora (o acesso ja foi conferido por quem chama, pelo
        /// vinculo ativo). Mesma regra da listagem do dono: no maximo um por tipo, CNH antes do comprovante, URL nova.
        /// </summary>
        Task<List<ReturnClienteDocumentoVO>> ListarDaEquipeAsync(int idCliente, CancellationToken cancellationToken = default);
    }
}
