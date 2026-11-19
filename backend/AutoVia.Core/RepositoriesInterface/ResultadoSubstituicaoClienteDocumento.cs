using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Resultado da substituicao do documento: o documento gravado e os documentos desativados (com a chave, so para o servico remover o arquivo).</summary>
    public sealed record ResultadoSubstituicaoClienteDocumento(ClienteDocumento Gravado, IReadOnlyList<DocumentoClienteDesativado> Desativados);
}
