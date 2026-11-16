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
    /// real: so ativos, DISPONIVEL e com valor de venda; marca (contem, ja em maiusculas), faixa de
    /// preco, ano do modelo e km maximo com AND; ordena por valor de venda e id e pagina.
    /// </summary>
    public partial class FakeVeiculoRepository
    {
        /// <summary>Lista os veiculos a venda que atendem aos filtros, ordenados por valor de venda e id, na pagina pedida.</summary>
        public Task<List<Veiculo>> FindAllAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade)
        {
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
            int total = FiltrarCatalogo(marca, precoMinimo, precoMaximo, anoMinimo, anoMaximo, kmMaximo).Count();

            return Task.FromResult(total);
        }

        /// <summary>Auxiliar de teste: muda o status de um veiculo semeado (simula, por exemplo, a abertura de uma manutencao).</summary>
        public void AlterarStatus(int idVeiculo, StatusVeiculo novoStatus)
        {
            Veiculo veiculo = _veiculos.First(x => x.IdVeiculo == idVeiculo);
            veiculo.Status = novoStatus;
        }

        /// <summary>Regra do RF-26 mais os filtros (AND), sem copiar.</summary>
        private IEnumerable<Veiculo> FiltrarCatalogo(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo)
        {
            IEnumerable<Veiculo> consulta = _veiculos.Where(x =>
                x.DisabledAt == null &&
                x.Status == StatusVeiculo.DISPONIVEL &&
                x.ValorVenda != null);

            if (!string.IsNullOrEmpty(marca))
            {
                consulta = consulta.Where(x => x.Marca.ToUpper().Contains(marca));
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
