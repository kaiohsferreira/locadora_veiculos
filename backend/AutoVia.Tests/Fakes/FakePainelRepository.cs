using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IPainelRepository em memoria, com o escopo do repositorio real: so devolve os numeros da locadora do contexto
    /// informado. Sem contexto (id 0) ou locadora sem dados, tudo volta zerado (fail-closed). Conta as somas em reais
    /// para os testes provarem que elas nao rodam quando o cargo nao ve valores. Pode simular falha.
    /// </summary>
    public class FakePainelRepository : IPainelRepository
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly Dictionary<int, DadosPainelFake> _dadosPorEmpresa;

        public FakePainelRepository(IContextoEmpresa contextoEmpresa, Dictionary<int, DadosPainelFake> dadosPorEmpresa)
        {
            _contextoEmpresa = contextoEmpresa;
            _dadosPorEmpresa = dadosPorEmpresa;
        }

        /// <summary>Quantas vezes a soma da receita foi consultada.</summary>
        public int ChamadasSomaReceita { get; private set; }

        /// <summary>Quantas vezes a soma a receber foi consultada.</summary>
        public int ChamadasSomaAReceber { get; private set; }

        /// <summary>Quantas vezes a soma em atraso foi consultada.</summary>
        public int ChamadasSomaEmAtraso { get; private set; }

        /// <summary>Janelas [inicio, fim) pedidas para a receita, na ordem das chamadas.</summary>
        public List<(DateOnly Inicio, DateOnly FimExclusivo)> JanelasReceita { get; } = new List<(DateOnly Inicio, DateOnly FimExclusivo)>();

        /// <summary>Quando nao for null, toda consulta lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoConsultar { get; set; }

        public Task<decimal> SomarReceitaPeriodoAsync(DateOnly inicio, DateOnly fimExclusivo)
        {
            LancarSeConfigurado();
            ChamadasSomaReceita++;
            JanelasReceita.Add((inicio, fimExclusivo));

            DadosPainelFake dados = DadosDaEmpresa();

            return Task.FromResult(dados.ReceitaPorJanela(inicio, fimExclusivo));
        }

        public Task<decimal> SomarAReceberAsync(DateOnly hoje)
        {
            LancarSeConfigurado();
            ChamadasSomaAReceber++;

            return Task.FromResult(DadosDaEmpresa().AReceber);
        }

        public Task<int> ContarAReceberAsync(DateOnly hoje)
        {
            LancarSeConfigurado();

            return Task.FromResult(DadosDaEmpresa().QuantidadeAReceber);
        }

        public Task<decimal> SomarEmAtrasoAsync(DateOnly hoje)
        {
            LancarSeConfigurado();
            ChamadasSomaEmAtraso++;

            return Task.FromResult(DadosDaEmpresa().EmAtraso);
        }

        public Task<int> ContarEmAtrasoAsync(DateOnly hoje)
        {
            LancarSeConfigurado();

            return Task.FromResult(DadosDaEmpresa().QuantidadeEmAtraso);
        }

        public Task<List<FrotaSituacaoContagem>> ContarVeiculosPorSituacaoAsync()
        {
            LancarSeConfigurado();

            return Task.FromResult(new List<FrotaSituacaoContagem>(DadosDaEmpresa().Frota));
        }

        public Task<int> ContarLocacoesEmAndamentoAsync()
        {
            LancarSeConfigurado();

            return Task.FromResult(DadosDaEmpresa().LocacoesEmAndamento);
        }

        public Task<int> ContarDevolucoesPrevistasAsync(DateTime inicioDia, DateTime fimDia)
        {
            LancarSeConfigurado();

            return Task.FromResult(DadosDaEmpresa().DevolucoesPrevistas);
        }

        /// <summary>Dados da locadora do contexto; zerados quando nao ha contexto ou a locadora nao tem dados (fail-closed).</summary>
        private DadosPainelFake DadosDaEmpresa()
        {
            if (_contextoEmpresa.IdEmpresa <= 0)
            {
                return new DadosPainelFake();
            }

            if (_dadosPorEmpresa.TryGetValue(_contextoEmpresa.IdEmpresa, out DadosPainelFake? dados))
            {
                return dados;
            }

            return new DadosPainelFake();
        }

        private void LancarSeConfigurado()
        {
            if (ExcecaoAoConsultar != null)
            {
                throw ExcecaoAoConsultar;
            }
        }
    }

    /// <summary>Numeros de uma locadora no fake do painel. Os padroes sao todos zero.</summary>
    public class DadosPainelFake
    {
        /// <summary>Receita por janela [inicio, fim): permite dar valores diferentes ao periodo atual e ao anterior.</summary>
        public Func<DateOnly, DateOnly, decimal> ReceitaPorJanela { get; set; } = (inicio, fimExclusivo) => 0m;

        public decimal AReceber { get; set; }

        public int QuantidadeAReceber { get; set; }

        public decimal EmAtraso { get; set; }

        public int QuantidadeEmAtraso { get; set; }

        public List<FrotaSituacaoContagem> Frota { get; set; } = new List<FrotaSituacaoContagem>();

        public int LocacoesEmAndamento { get; set; }

        public int DevolucoesPrevistas { get; set; }
    }
}
