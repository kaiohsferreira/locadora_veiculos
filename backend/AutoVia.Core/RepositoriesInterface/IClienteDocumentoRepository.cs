using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Persistencia dos documentos do cliente (cliente_documento). A tabela nao tem filtro "Empresa": o repositorio so recebe o id
    /// do cliente que veio da sessao. Leituras filtram DisabledAt nulo, exceto a contagem do teto (que inclui desativados).
    /// Exclusao sempre logica (Disable), nunca Remove().
    /// </summary>
    public interface IClienteDocumentoRepository
    {
        /// <summary>Verdadeiro quando o cliente existe e nao esta desativado.</summary>
        Task<bool> ClienteAtivoAsync(int idCliente, CancellationToken cancellationToken = default);

        /// <summary>Quantos documentos o cliente enviou desde a data, ativos ou desativados (teto de envios em 24 horas).</summary>
        Task<int> CountEnviadosDesdeWithDisabledsAsync(int idCliente, DateTime desde, CancellationToken cancellationToken = default);

        /// <summary>Documentos ativos do cliente, sem rastreamento, por id.</summary>
        Task<List<ClienteDocumento>> FindAtivosDoClienteAsync(int idCliente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Grava o documento novo e desativa os ativos do mesmo cliente e tipo, numa unica gravacao. Devolve o documento gravado e
        /// a chave e o id de cada documento desativado, para o servico remover os arquivos antigos.
        /// </summary>
        Task<ResultadoSubstituicaoClienteDocumento> SubstituirAsync(ClienteDocumento novo, CancellationToken cancellationToken = default);
    }
}
