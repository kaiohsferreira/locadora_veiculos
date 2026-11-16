using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Catalogo de seminovos do FakeVeiculoRepository, com as mesmas regras do VeiculoRepository
    /// real: so ativos, DISPONIVEL, com valor de venda e de locadora ativa e nao desativada (RF-72);
    /// marca (contem, sem diferenciar maiusculas; normalizada aqui como no repositorio real), faixa de
    /// preco, ano do modelo e km maximo com AND; ordena por valor de venda e id e pagina.
    /// </summary>
    public partial class FakeVeiculoRepository
    {
        /// <summary>Falha simulada do catalogo: quando informada, FindAllAVendaAsync e CountAVendaAsync a relancam (teste de log).</summary>
        public Exception? ExcecaoAoListarCatalogo { get; set; }

        /// <summary>Lista os veiculos a venda que atendem aos filtros, ordenados por valor de venda e id, na pagina pedida.</summary>
        public Task<List<Veiculo>> FindAllAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade)
        {
            if (ExcecaoAoListarCatalogo != null)
            {
                throw ExcecaoAoListarCatalogo;
            }

            List<Veiculo> veiculos = FiltrarCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo)
                .OrderBy(x => x.ValorVenda)
                .ThenBy(x => x.IdVeiculo)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(veiculos);
        }

        /// <summary>Conta os veiculos a venda com os mesmos filtros de FindAllAVendaAsync.</summary>
        public Task<int> CountAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            if (ExcecaoAoListarCatalogo != null)
            {
                throw ExcecaoAoListarCatalogo;
            }

            int total = FiltrarCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo).Count();

            return Task.FromResult(total);
        }

        /// <summary>Auxiliar de teste: muda o status de um veiculo semeado (simula, por exemplo, a abertura de uma manutencao).</summary>
        public void AlterarStatus(int idVeiculo, StatusVeiculo novoStatus)
        {
            Veiculo veiculo = _veiculos.First(x => x.IdVeiculo == idVeiculo);
            veiculo.Status = novoStatus;
        }

        /// <summary>Regra do RF-26, de locadora ativa (RF-72) e os filtros (AND), sem copiar.</summary>
        private IEnumerable<Veiculo> FiltrarCatalogo(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            IEnumerable<Veiculo> consulta = _veiculos.Where(x =>
                x.DisabledAt == null &&
                x.Status == StatusVeiculo.DISPONIVEL &&
                x.ValorVenda != null &&
                (x.Empresa == null || (x.Empresa.Ativo && x.Empresa.DisabledAt == null)));

            string termoMarca = string.IsNullOrWhiteSpace(marca) ? string.Empty : marca.Trim().ToUpper();

            if (termoMarca.Length > 0)
            {
                consulta = consulta.Where(x => x.Marca.ToUpper().Contains(termoMarca));
            }

            if (precoMinimo.HasValue)
            {
                consulta = consulta.Where(x => x.ValorVenda >= precoMinimo.Value);
            }

            if (precoMaximo.HasValue)
            {
                consulta = consulta.Where(x => x.ValorVenda <= precoMaximo.Value);
            }

            if (anoMinimo.HasValue)
            {
                consulta = consulta.Where(x => x.AnoModelo >= anoMinimo.Value);
            }

            if (anoMaximo.HasValue)
            {
                consulta = consulta.Where(x => x.AnoModelo <= anoMaximo.Value);
            }

            if (kmMaximo.HasValue)
            {
                consulta = consulta.Where(x => x.KmAtual <= kmMaximo.Value);
            }

            return consulta;
        }
    }
}
