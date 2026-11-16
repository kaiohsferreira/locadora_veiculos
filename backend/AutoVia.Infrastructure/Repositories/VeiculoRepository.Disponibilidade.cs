using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    public partial class VeiculoRepository
    {
        public async Task<int> CountDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax)
        {
            try
            {
                int total = await AplicarFiltroDisponibilidade(retirada, devolucao, idCategoria, valorMin, valorMax).CountAsync();
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        public async Task<List<Veiculo>> FindDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade)
        {
            try
            {
                List<Veiculo> veiculos = await AplicarFiltroDisponibilidade(retirada, devolucao, idCategoria, valorMin, valorMax)
                    .Include(x => x.Categoria)
                    .OrderBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();
                return veiculos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }

        private IQueryable<Veiculo> AplicarFiltroDisponibilidade(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax)
        {
            try
            {
                DateTime inicio = retirada.ToDateTime(TimeOnly.MinValue);
                DateTime fim = devolucao.ToDateTime(TimeOnly.MinValue);
                IQueryable<Veiculo> consulta = _db.Veiculos.AsNoTracking()
                    .Where(x => x.DisabledAt == null && (x.Status == StatusVeiculo.DISPONIVEL || x.Status == StatusVeiculo.RESERVADO))
                    .Where(x => !x.Reservas.Any(r => r.DisabledAt == null
                        && (r.Status == StatusReserva.ATIVA || r.Status == StatusReserva.CONFIRMADA)
                        && r.DataRetiradaPrevista < fim && r.DataDevolucaoPrevista > inicio));
                if (idCategoria.HasValue)
                {
                    int categoria = idCategoria.Value;
                    consulta = consulta.Where(x => x.IdCategoria == categoria);
                }
                if (valorMin.HasValue)
                {
                    decimal minimo = valorMin.Value;
                    consulta = consulta.Where(x => x.ValorDiaria >= minimo);
                }
                if (valorMax.HasValue)
                {
                    decimal maximo = valorMax.Value;
                    consulta = consulta.Where(x => x.ValorDiaria <= maximo);
                }
                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }
    }
}
