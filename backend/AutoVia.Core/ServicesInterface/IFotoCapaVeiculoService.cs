using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Foto de capa nas listas (frota, reservas do cliente, busca e catalogo). Uma assinatura em lote por pagina; nenhum veiculo
    /// com foto ativa, nenhuma chamada. Falha do armazenamento vira erroFotoCapa no item (200); cancelamento e excecao nao
    /// prevista sobem.
    /// </summary>
    public interface IFotoCapaVeiculoService
    {
        /// <summary>
        /// Preenche fotoCapa e erroFotoCapa de cada item pelo IdVeiculo, usando a capa (primeira foto ativa) de cada veiculo
        /// carregado com as fotos. Veiculo nulo ou sem capa ativa deixa o item como esta (nulo).
        /// </summary>
        Task PreencherAsync<T>(IReadOnlyCollection<Veiculo?> veiculos, IReadOnlyCollection<T> itens, CancellationToken cancellationToken = default) where T : IComFotoCapa;
    }
}
