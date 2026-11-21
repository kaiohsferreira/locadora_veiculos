using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Proposta de compra (P6-BE-02). Arquivo parcial do VeiculoRepository: o veiculo a venda pelo id, para a simulacao e a
    /// proposta. Reaproveita ConsultaCatalogo (VeiculoRepository.Catalogo.cs) sem filtros: o recorte publico e o mesmo e este
    /// arquivo nao tem bypass proprio do filtro de locadora.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>Busca um veiculo do catalogo pelo id, sem rastreamento. Nulo quando o veiculo nao esta a venda.</summary>
        public async Task<Veiculo?> FindAVendaByIdAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await ConsultaCatalogo(null, null, null, null, null, null)
                    .Where(x => x.IdVeiculo == idVeiculo)
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
