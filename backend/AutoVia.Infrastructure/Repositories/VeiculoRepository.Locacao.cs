using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    public partial class VeiculoRepository
    {
        public async Task<VeiculoLocacaoConsulta?> FindParaLocacaoAsync(int idVeiculo, DateOnly? retirada, DateOnly? devolucao, CancellationToken cancellationToken = default)
        {
            try
            {
                VeiculoLocacaoConsulta? detalhe = await ConsultaVeiculoLocacao(idVeiculo, retirada, devolucao)
                    .FirstOrDefaultAsync(cancellationToken);
                return detalhe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarVeiculoLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta por ID com galeria limitada e, quando solicitado, disponibilidade para o período.</summary>
        public IQueryable<VeiculoLocacaoConsulta> ConsultaVeiculoLocacao(int idVeiculo, DateOnly? retirada, DateOnly? devolucao)
        {
            try
            {
                IQueryable<Veiculo> veiculos = ConsultaFrotaPublicaLocacao()
                    .Where(x => x.IdVeiculo == idVeiculo)
                    .Include(x => x.Categoria)
                    .Include(x => x.Empresa)
                    .Include(x => x.Fotos.Where(foto => foto.DisabledAt == null)
                        .OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto)
                        .Take(ConstantsValues.MaximoFotosPorVeiculo));

                if (!retirada.HasValue || !devolucao.HasValue)
                {
                    return veiculos.Select(x => new VeiculoLocacaoConsulta { Veiculo = x });
                }

                IQueryable<Reserva> reservas = ConsultaReservasSobrepostas(retirada.Value, devolucao.Value);
                return veiculos.Select(x => new VeiculoLocacaoConsulta
                {
                    Veiculo = x,
                    DisponivelNoPeriodo = !reservas.Any(r => r.IdVeiculo == x.IdVeiculo)
                });
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarVeiculoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
