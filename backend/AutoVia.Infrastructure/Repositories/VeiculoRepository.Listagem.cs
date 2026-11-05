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
    /// <summary>
    /// Listagem de veiculos (P1-BE-03). Arquivo parcial do VeiculoRepository: leitura sem
    /// rastreamento, so ativos, filtros combinados com AND. A contagem e a pagina usam o
    /// mesmo AplicarFiltros, para que o total bata com o que for paginado.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>
        /// Lista os veiculos ativos que atendem aos filtros, ordenados por placa e id, na pagina pedida, com a categoria.
        /// </summary>
        public async Task<List<Veiculo>> FindAllAsync(int? idCategoria, StatusVeiculo? status, string? placa, int deslocamento, int quantidade)
        {
            try
            {
                List<Veiculo> veiculos = await AplicarFiltros(idCategoria, status, placa)
                    .Include(x => x.Categoria)
                    .OrderBy(x => x.Placa)
                    .ThenBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return veiculos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarVeiculos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Conta os veiculos ativos que atendem aos mesmos filtros de FindAllAsync.
        /// </summary>
        public async Task<int> CountAsync(int? idCategoria, StatusVeiculo? status, string? placa)
        {
            try
            {
                int total = await AplicarFiltros(idCategoria, status, placa).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarVeiculos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta dos veiculos ativos, sem rastreamento, com os filtros informados combinados por AND.
        /// A placa chega ja normalizada pelo servico; aqui ela e comparada por parte do texto (contem).
        /// </summary>
        private IQueryable<Veiculo> AplicarFiltros(int? idCategoria, StatusVeiculo? status, string? placa)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idCategoria.HasValue)
                {
                    int idCategoriaFiltro = idCategoria.Value;
                    consulta = consulta.Where(x => x.IdCategoria == idCategoriaFiltro);
                }

                if (status.HasValue)
                {
                    StatusVeiculo statusFiltro = status.Value;
                    consulta = consulta.Where(x => x.Status == statusFiltro);
                }

                if (!string.IsNullOrEmpty(placa))
                {
                    string termoPlaca = placa;
                    consulta = consulta.Where(x => x.Placa.Contains(termoPlaca));
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarVeiculos, GetType().ToString());
                throw;
            }
        }
    }
}
