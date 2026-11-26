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
        /// <summary>Falha simulada da busca publica: quando informada, CountDisponiveisAsync e FindDisponiveisAsync a relancam (teste de log).</summary>
        public Exception? ExcecaoAoConsultarDisponiveis { get; set; }

        public Task<int> CountDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, string? cidade = null, string? uf = null)
        {
            if (ExcecaoAoConsultarDisponiveis != null)
            {
                throw ExcecaoAoConsultarDisponiveis;
            }

            int total = FiltrarDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax, cidade, uf).Count();
            return Task.FromResult(total);
        }

        public Task<List<Veiculo>> FindDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade, string? cidade = null, string? uf = null)
        {
            if (ExcecaoAoConsultarDisponiveis != null)
            {
                throw ExcecaoAoConsultarDisponiveis;
            }

            List<Veiculo> veiculos = FiltrarDisponiveis(retirada, devolucao, idCategoria, valorMin, valorMax, cidade, uf)
                .OrderBy(x => x.IdVeiculo)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(Copiar)
                .ToList();
            return Task.FromResult(veiculos);
        }

        /// <summary>
        /// Regra da busca publica: veiculo ativo DISPONIVEL ou RESERVADO, sem reserva ativa ou confirmada que se sobreponha,
        /// de locadora ativa e nao desativada (RF-72). Veiculo semeado sem a navegacao Empresa conta como de locadora ativa.
        /// </summary>
        private IEnumerable<Veiculo> FiltrarDisponiveis(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, string? cidade, string? uf)
        {
            DateTime inicio = retirada.ToDateTime(TimeOnly.MinValue);
            DateTime fim = devolucao.ToDateTime(TimeOnly.MinValue);
            IEnumerable<Veiculo> consulta = _veiculos.Where(x => x.DisabledAt == null
                && (x.Status == StatusVeiculo.DISPONIVEL || x.Status == StatusVeiculo.RESERVADO)
                && (x.Empresa == null || (x.Empresa.Ativo && x.Empresa.DisabledAt == null))
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
            if (cidade != null && uf != null)
            {
                string cidadeNormalizada = cidade.Trim();
                string ufNormalizada = uf.Trim();
                consulta = consulta.Where(x => x.Empresa != null
                    && string.Equals(x.Empresa.Cidade.Trim(), cidadeNormalizada, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Empresa.Uf, ufNormalizada, StringComparison.OrdinalIgnoreCase));
            }
            return consulta;
        }
    }
}
