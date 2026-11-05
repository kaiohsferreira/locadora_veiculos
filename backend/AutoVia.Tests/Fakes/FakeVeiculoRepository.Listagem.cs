using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
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
        /// <summary>Lista os veiculos ativos que atendem aos filtros, ordenados por placa e id, na pagina pedida.</summary>
        public Task<List<Veiculo>> FindAllAsync(int? idCategoria, StatusVeiculo? status, string? placa, int deslocamento, int quantidade)
        {
            List<Veiculo> veiculos = FiltrarVeiculos(idCategoria, status, placa)
                .OrderBy(x => x.Placa)
                .ThenBy(x => x.IdVeiculo)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(veiculos);
        }

        /// <summary>Conta os veiculos ativos que atendem aos mesmos filtros de FindAllAsync.</summary>
        public Task<int> CountAsync(int? idCategoria, StatusVeiculo? status, string? placa)
        {
            int total = FiltrarVeiculos(idCategoria, status, placa).Count();

            return Task.FromResult(total);
        }

        /// <summary>Aplica os filtros (AND) sobre os veiculos ativos, sem copiar.</summary>
        private IEnumerable<Veiculo> FiltrarVeiculos(int? idCategoria, StatusVeiculo? status, string? placa)
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

            return consulta;
        }
    }
}
