using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Seminovo a venda pelo id (P6-BE-02 e P6-FE-02). Arquivo parcial do VeiculoRepository: o detalhe publico, a simulacao e
    /// a proposta leem o veiculo por aqui. Reaproveita ConsultaCatalogo (VeiculoRepository.Catalogo.cs) sem filtros: o recorte
    /// publico e o mesmo e este arquivo nao tem bypass proprio do filtro de locadora.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>
        /// Busca um veiculo do catalogo pelo id, sem rastreamento, com a categoria, a locadora e a capa (fotos ativas, primeira por
        /// ordem), como a pagina do catalogo. Nulo quando o veiculo nao esta a venda.
        /// </summary>
        public async Task<Veiculo?> FindAVendaByIdAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await ConsultaCatalogo(null, null, null, null, null, null)
                    .Where(x => x.IdVeiculo == idVeiculo)
                    .Include(x => x.Categoria)
                    .Include(x => x.Empresa)
                    .Include(x => x.Fotos.Where(foto => foto.DisabledAt == null).OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto).Take(ConstantsValues.QuantidadeFotoCapaPorVeiculo))
                    .FirstOrDefaultAsync();

                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPropostaBuscarVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
