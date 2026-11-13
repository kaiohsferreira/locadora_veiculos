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
    /// Catalogo de seminovos (P6-BE-01). Arquivo parcial do VeiculoRepository: leitura sem
    /// rastreamento e a regra do RF-26 sempre aplicada (ativo, DISPONIVEL e valor de venda
    /// preenchido). Contagem e pagina usam o mesmo AplicarFiltrosCatalogo.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>
        /// Lista os veiculos a venda que atendem aos filtros, ordenados por valor de venda e id
        /// (paginacao estavel no empate de preco), na pagina pedida, com a categoria.
        /// </summary>
        public async Task<List<Veiculo>> FindAllAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade)
        {
            try
            {
                List<Veiculo> veiculos = await AplicarFiltrosCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo)
                    .Include(x => x.Categoria)
                    .OrderBy(x => x.ValorVenda)
                    .ThenBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return veiculos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta os veiculos a venda que atendem aos mesmos filtros de FindAllAVendaAsync.</summary>
        public async Task<int> CountAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            try
            {
                int total = await AplicarFiltrosCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta do catalogo: ativos, DISPONIVEL e com valor de venda (RF-26), sem
        /// rastreamento, mais os filtros informados combinados por AND. O ano filtrado e o do modelo.
        /// </summary>
        private IQueryable<Veiculo> AplicarFiltrosCatalogo(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Status == StatusVeiculo.DISPONIVEL
                        && x.ValorVenda != null);

                if (!string.IsNullOrEmpty(marca))
                {
                    string termoMarca = marca;
                    consulta = consulta.Where(x => x.Marca.ToUpper().Contains(termoMarca));
                }

                if (precoMinimo.HasValue)
                {
                    decimal precoMinimoFiltro = precoMinimo.Value;
                    consulta = consulta.Where(x => x.ValorVenda >= precoMinimoFiltro);
                }

                if (precoMaximo.HasValue)
                {
                    decimal precoMaximoFiltro = precoMaximo.Value;
                    consulta = consulta.Where(x => x.ValorVenda <= precoMaximoFiltro);
                }

                if (anoMinimo.HasValue)
                {
                    int anoMinimoFiltro = anoMinimo.Value;
                    consulta = consulta.Where(x => x.AnoModelo >= anoMinimoFiltro);
                }

                if (anoMaximo.HasValue)
                {
                    int anoMaximoFiltro = anoMaximo.Value;
                    consulta = consulta.Where(x => x.AnoModelo <= anoMaximoFiltro);
                }

                if (kmMaximo.HasValue)
                {
                    int kmMaximoFiltro = kmMaximo.Value;
                    consulta = consulta.Where(x => x.KmAtual <= kmMaximoFiltro);
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }
    }
}
