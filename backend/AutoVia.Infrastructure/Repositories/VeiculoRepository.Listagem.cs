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
        public async Task<List<Veiculo>> FindAllAsync(int? idCategoria, StatusVeiculo? status, string? placa, string? busca, int deslocamento, int quantidade)
        {
            try
            {
                List<Veiculo> veiculos = await ConsultaFrotaPaginada(idCategoria, status, placa, busca, deslocamento, quantidade)
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
        /// Consulta nomeada da pagina da frota: filtros do AplicarFiltros, categoria e locadora, a capa (primeira foto ativa,
        /// menor ordem) por Include da colecao de fotos, ordenacao por placa e id, e a pagina. Sem bypass: o filtro Empresa vale.
        /// Monta so o IQueryable; quem executa e o FindAllAsync.
        /// </summary>
        public IQueryable<Veiculo> ConsultaFrotaPaginada(int? idCategoria, StatusVeiculo? status, string? placa, string? busca, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<Veiculo> consulta = AplicarFiltros(idCategoria, status, placa, busca)
                    .Include(x => x.Categoria)
                    .Include(x => x.Empresa)
                    .Include(x => x.Fotos.Where(foto => foto.DisabledAt == null).OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto).Take(ConstantsValues.QuantidadeFotoCapaPorVeiculo))
                    .OrderBy(x => x.Placa)
                    .ThenBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade);

                return consulta;
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
        public async Task<int> CountAsync(int? idCategoria, StatusVeiculo? status, string? placa, string? busca)
        {
            try
            {
                int total = await AplicarFiltros(idCategoria, status, placa, busca).CountAsync();

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
        /// A busca chega aparada; placa, chassi, modelo e marca casam por contem, e os tres textos sem diferenciar maiusculas.
        /// O grupo da busca (OR) entra por AND com os demais filtros.
        /// </summary>
        private IQueryable<Veiculo> AplicarFiltros(int? idCategoria, StatusVeiculo? status, string? placa, string? busca)
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

                if (!string.IsNullOrEmpty(busca))
                {
                    string termoBusca = busca.ToLowerInvariant();
                    string termoBuscaPlaca = StaticMethods.NormalizePlaca(busca);
                    bool temBuscaPlaca = termoBuscaPlaca.Length > 0;
                    consulta = consulta.Where(x => x.Chassi.ToLower().Contains(termoBusca)
                        || x.Modelo.ToLower().Contains(termoBusca)
                        || x.Marca.ToLower().Contains(termoBusca)
                        || (temBuscaPlaca && x.Placa.Contains(termoBuscaPlaca)));
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
