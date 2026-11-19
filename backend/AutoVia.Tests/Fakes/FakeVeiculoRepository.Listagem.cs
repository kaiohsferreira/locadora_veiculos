using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Listagem do FakeVeiculoRepository, com as mesmas regras do VeiculoRepository real:
    /// ignora desativados, filtra por categoria, status e placa (contem) combinados com AND,
    /// ordena por placa e id, e pagina por deslocamento e quantidade.
    /// </summary>
    public partial class FakeVeiculoRepository
    {
        /// <summary>Quando nao for null, FindAllAsync e CountAsync lancam esta excecao (falha inesperada na listagem).</summary>
        public Exception? ExcecaoAoListar { get; set; }

        /// <summary>Lista os veiculos ativos que atendem aos filtros, ordenados por placa e id, na pagina pedida.</summary>
        public Task<List<Veiculo>> FindAllAsync(int? idCategoria, StatusVeiculo? status, string? placa, string? busca, int deslocamento, int quantidade)
        {
            if (ExcecaoAoListar != null)
            {
                throw ExcecaoAoListar;
            }

            List<Veiculo> veiculos = FiltrarVeiculos(idCategoria, status, placa, busca)
                .OrderBy(x => x.Placa)
                .ThenBy(x => x.IdVeiculo)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(veiculos);
        }

        /// <summary>Conta os veiculos ativos que atendem aos mesmos filtros de FindAllAsync.</summary>
        public Task<int> CountAsync(int? idCategoria, StatusVeiculo? status, string? placa, string? busca)
        {
            if (ExcecaoAoListar != null)
            {
                throw ExcecaoAoListar;
            }

            int total = FiltrarVeiculos(idCategoria, status, placa, busca).Count();

            return Task.FromResult(total);
        }

        /// <summary>Aplica os filtros (AND) sobre os veiculos ativos, sem copiar. A busca casa por contem em placa, chassi, modelo ou marca.</summary>
        private IEnumerable<Veiculo> FiltrarVeiculos(int? idCategoria, StatusVeiculo? status, string? placa, string? busca)
        {
            IEnumerable<Veiculo> consulta = _veiculos.Where(x => x.DisabledAt == null);

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
                consulta = consulta.Where(x => x.Chassi.ToLowerInvariant().Contains(termoBusca)
                    || x.Modelo.ToLowerInvariant().Contains(termoBusca)
                    || x.Marca.ToLowerInvariant().Contains(termoBusca)
                    || (temBuscaPlaca && x.Placa.Contains(termoBuscaPlaca)));
            }

            return consulta;
        }
    }
}
