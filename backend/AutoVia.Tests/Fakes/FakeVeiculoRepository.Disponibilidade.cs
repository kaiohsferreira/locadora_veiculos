using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public partial class FakeVeiculoRepository
    {
        public Task<int> CountDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax)
        {
            int total = FiltrarDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax).Count();
            return Task.FromResult(total);
        }

        public Task<List<Veiculo>> FindDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade)
        {
            List<Veiculo> veiculos = FiltrarDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax)
                .OrderBy(x => x.IdVeiculo)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(Copiar)
                .ToList();
            return Task.FromResult(veiculos);
        }

        private IEnumerable<Veiculo> FiltrarDisponiveis(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax)
        {
            DateTime inicio = retirada.ToDateTime(TimeOnly.MinValue);
            DateTime fim = devolucao.ToDateTime(TimeOnly.MinValue);
            IEnumerable<Veiculo> consulta = _veiculos.Where(x => x.DisabledAt == null
                && (x.Status == StatusVeiculo.DISPONIVEL || x.Status == StatusVeiculo.RESERVADO)
                && !x.Reservas.Any(r => r.DisabledAt == null
                    && (r.Status == StatusReserva.ATIVA || r.Status == StatusReserva.CONFIRMADA)
                    && r.DataRetiradaPrevista < fim
                    && r.DataDevolucaoPrevista > inicio));
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
    }
}
