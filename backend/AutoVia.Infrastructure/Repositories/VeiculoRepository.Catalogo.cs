using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Catalogo de seminovos (P6-BE-01). Arquivo parcial do VeiculoRepository: leitura sem rastreamento e a regra
    /// do RF-26 sempre aplicada (ativo, DISPONIVEL e valor de venda preenchido). O catalogo le as locadoras ativas:
    /// o unico ponto que desliga o filtro de locadora e ConsultaCatalogo. Os quatro metodos do arquivo registram log
    /// e relancam a excecao.
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>
        /// Lista os veiculos a venda que atendem aos filtros, ordenados por valor de venda e id
        /// (paginacao estavel no empate de preco), na pagina pedida, com a categoria e a locadora.
        /// </summary>
        public async Task<List<Veiculo>> FindAllAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade)
        {
            try
            {
                List<Veiculo> veiculos = await ConsultaCatalogoPaginada(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo, deslocamento, quantidade)
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
                int total = await ConsultaCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta do catalogo: seminovos de todas as locadoras ativas (empresa ativa e nao desativada),
        /// ativos, DISPONIVEL e com valor de venda (RF-26), sem rastreamento, mais os filtros informados combinados
        /// por AND. A marca e normalizada aqui (vazia ou so espacos = sem filtro; senao Trim e maiusculas). O ano
        /// filtrado e o do modelo.
        /// </summary>
        public IQueryable<Veiculo> ConsultaCatalogo(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .SemEscopoEmpresa()
                    .ApenasEmpresasAtivas()
                    .Where(x => x.DisabledAt == null
                        && x.Status == StatusVeiculo.DISPONIVEL
                        && x.ValorVenda != null);

                string termoMarca = string.IsNullOrWhiteSpace(marca) ? string.Empty : marca.Trim().ToUpper();

                if (termoMarca.Length > 0)
                {
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

        /// <summary>
        /// Pagina do catalogo: a consulta de ConsultaCatalogo com Categoria e Empresa carregadas, ordenada por valor
        /// de venda e id (menor preco primeiro), com deslocamento e quantidade.
        /// </summary>
        public IQueryable<Veiculo> ConsultaCatalogoPaginada(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<Veiculo> pagina = ConsultaCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo)
                    .Include(x => x.Categoria)
                    .Include(x => x.Empresa)
                    .OrderBy(x => x.ValorVenda)
                    .ThenBy(x => x.IdVeiculo)
                    .Skip(deslocamento)
                    .Take(quantidade);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCatalogo, GetType().ToString());
                throw;
            }
        }
    }
}
