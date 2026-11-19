using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Painel;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Regras da visao geral (P11-BE-01 e P11-BE-02) com repositorio falso e data controlada: janela e limites de cada periodo,
    /// periodo anterior, variacao com base zero e positiva, corte de valores para os seis cargos (com a conferencia de que as
    /// somas nem sao consultadas sem permissao), ocupacao com denominador zero, primeiro acesso, frota com todos os status,
    /// isolamento por locadora no contexto e falha com log.
    /// </summary>
    public class PainelServiceTests
    {
        private const int EmpresaA = 1;
        private const int EmpresaB = 2;
        private const string DataHojeTexto = "14/11/2026";
        private const string FormatoData = "dd/MM/yyyy";
        private const decimal ReceitaAtual = 1500m;
        private const decimal ReceitaAnterior = 1000m;
        private const decimal ReceberFixo = 800m;
        private const decimal AtrasoFixo = 300m;
        private const int QuantidadeReceber = 4;
        private const int QuantidadeAtraso = 2;

        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 14, 10, 30, 0);
        private static readonly DateOnly FimExclusivoAtual = new DateOnly(2026, 11, 15);

        /// <summary>Dados da locadora A: receita atual de 1500 e anterior de 1000, a receber e em atraso preenchidos, frota com locados.</summary>
        private static DadosPainelFake DadosBasicos()
        {
            return new DadosPainelFake
            {
                ReceitaPorJanela = (inicio, fimExclusivo) => fimExclusivo == FimExclusivoAtual ? ReceitaAtual : ReceitaAnterior,
                AReceber = ReceberFixo,
                QuantidadeAReceber = QuantidadeReceber,
                EmAtraso = AtrasoFixo,
                QuantidadeEmAtraso = QuantidadeAtraso,
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 3 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 2 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 4 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.BAIXADO, Quantidade = 1 }
                },
                LocacoesEmAndamento = 2,
                DevolucoesPrevistas = 1
            };
        }

        private static Dictionary<int, DadosPainelFake> DadosPorEmpresa(DadosPainelFake dados)
        {
            return new Dictionary<int, DadosPainelFake> { [EmpresaA] = dados };
        }

        private static (PainelService Servico, FakePainelRepository Repositorio, FakeLogService Log) Criar(
            IContextoEmpresa contexto,
            Dictionary<int, DadosPainelFake> dados,
            DateTime? agora = null)
        {
            FakePainelRepository repositorio = new FakePainelRepository(contexto, dados);
            FakeLogService log = new FakeLogService();
            PainelService servico = new PainelService(repositorio, contexto, new FakeRelogio(agora ?? AgoraFixo), log);

            return (servico, repositorio, log);
        }

        private static DateOnly Data(string texto)
        {
            return DateOnly.ParseExact(texto, FormatoData, CultureInfo.InvariantCulture);
        }

        [Theory]
        [InlineData(PeriodoVisaoGeral.HOJE, "14/11/2026")]
        [InlineData(PeriodoVisaoGeral.SETE_DIAS, "08/11/2026")]
        [InlineData(PeriodoVisaoGeral.TRINTA_DIAS, "16/10/2026")]
        [InlineData(PeriodoVisaoGeral.MES_ATUAL, "01/11/2026")]
        public async Task Periodo_TemInicioIncluidoEFimExcluidoPelaDataDeHoje(PeriodoVisaoGeral periodo, string inicioEsperado)
        {
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(DadosBasicos()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(periodo);

            Assert.Equal(inicioEsperado, visao.PeriodoInicioStr);
            Assert.Equal(DataHojeTexto, visao.PeriodoFimStr);
            Assert.Equal(periodo, visao.Periodo);
            (DateOnly inicio, DateOnly fimExclusivo) = repositorio.JanelasReceita[0];
            Assert.Equal(Data(inicioEsperado), inicio);
            Assert.Equal(new DateOnly(2026, 11, 15), fimExclusivo);
        }

        [Theory]
        [InlineData(PeriodoVisaoGeral.HOJE)]
        [InlineData(PeriodoVisaoGeral.SETE_DIAS)]
        [InlineData(PeriodoVisaoGeral.TRINTA_DIAS)]
        [InlineData(PeriodoVisaoGeral.MES_ATUAL)]
        public async Task PeriodoAnterior_TemAMesmaDuracaoELogoAntesDoAtual(PeriodoVisaoGeral periodo)
        {
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.FINANCEIRO), DadosPorEmpresa(DadosBasicos()));

            await servico.ObterVisaoGeralAsync(periodo);

            (DateOnly inicioAtual, DateOnly fimAtual) = repositorio.JanelasReceita[0];
            (DateOnly inicioAnterior, DateOnly fimAnterior) = repositorio.JanelasReceita[1];
            Assert.Equal(fimAtual.DayNumber - inicioAtual.DayNumber, fimAnterior.DayNumber - inicioAnterior.DayNumber);
            Assert.Equal(inicioAtual, fimAnterior);
        }

        [Fact]
        public async Task MesAtual_NoPrimeiroDiaDoMes_JanelaTemUmDia()
        {
            DateTime primeiroDia = new DateTime(2026, 11, 1, 9, 0, 0);
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(DadosBasicos()), primeiroDia);

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.MES_ATUAL);

            Assert.Equal("01/11/2026", visao.PeriodoInicioStr);
            Assert.Equal("01/11/2026", visao.PeriodoFimStr);
            Assert.Equal(new DateOnly(2026, 11, 1), repositorio.JanelasReceita[0].Inicio);
            Assert.Equal(new DateOnly(2026, 11, 2), repositorio.JanelasReceita[0].FimExclusivo);
        }

        [Fact]
        public async Task Variacao_BaseZero_SaiNula()
        {
            DadosPainelFake dados = DadosBasicos();
            dados.ReceitaPorJanela = (inicio, fimExclusivo) => fimExclusivo == FimExclusivoAtual ? ReceitaAtual : 0m;
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.HOJE);

            Assert.Null(visao.VariacaoReceitaPercentual);
            Assert.Equal((decimal?)ReceitaAtual, visao.ReceitaPeriodo);
        }

        [Theory]
        [InlineData(1500, 1000, 50.0)]
        [InlineData(500, 1000, -50.0)]
        [InlineData(1000, 3000, -66.7)]
        [InlineData(1000, 1000, 0.0)]
        public async Task Variacao_BasePositiva_CalculaComUmaCasaDecimal(decimal atual, decimal anterior, decimal esperado)
        {
            DadosPainelFake dados = DadosBasicos();
            dados.ReceitaPorJanela = (inicio, fimExclusivo) => fimExclusivo == FimExclusivoAtual ? atual : anterior;
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.HOJE);

            Assert.Equal((decimal?)esperado, visao.VariacaoReceitaPercentual);
        }

        [Theory]
        [InlineData(CargoFuncionario.ADMINISTRADOR, true)]
        [InlineData(CargoFuncionario.FINANCEIRO, true)]
        [InlineData(CargoFuncionario.ATENDENTE, false)]
        [InlineData(CargoFuncionario.VENDEDOR, false)]
        [InlineData(CargoFuncionario.GESTOR_FROTA, false)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, false)]
        public async Task CorteDeValores_PorCargo_SomasSoRodamComPermissao(CargoFuncionario cargo, bool podeVerValores)
        {
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, cargo), DadosPorEmpresa(DadosBasicos()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(podeVerValores, visao.ValoresVisiveis);
            Assert.Equal(QuantidadeReceber, visao.QuantidadeCobrancasEmAberto);
            Assert.Equal(QuantidadeAtraso, visao.QuantidadeCobrancasVencidas);
            Assert.Equal(2, visao.LocacoesAtivas);
            if (podeVerValores)
            {
                Assert.Equal((decimal?)ReceitaAtual, visao.ReceitaPeriodo);
                Assert.Equal(StaticMethods.FormateToMonetary(ReceitaAtual), visao.ReceitaPeriodoStr);
                Assert.Equal((decimal?)ReceberFixo, visao.AReceber);
                Assert.Equal(StaticMethods.FormateToMonetary(ReceberFixo), visao.AReceberStr);
                Assert.Equal((decimal?)AtrasoFixo, visao.EmAtraso);
                Assert.Equal(StaticMethods.FormateToMonetary(AtrasoFixo), visao.EmAtrasoStr);
                Assert.Equal((decimal?)50.0m, visao.VariacaoReceitaPercentual);
                Assert.Equal(2, repositorio.ChamadasSomaReceita);
                Assert.Equal(1, repositorio.ChamadasSomaAReceber);
                Assert.Equal(1, repositorio.ChamadasSomaEmAtraso);
            }
            else
            {
                Assert.Null(visao.ReceitaPeriodo);
                Assert.Equal(string.Empty, visao.ReceitaPeriodoStr);
                Assert.Null(visao.AReceber);
                Assert.Equal(string.Empty, visao.AReceberStr);
                Assert.Null(visao.EmAtraso);
                Assert.Equal(string.Empty, visao.EmAtrasoStr);
                Assert.Null(visao.VariacaoReceitaPercentual);
                Assert.Equal(0, repositorio.ChamadasSomaReceita);
                Assert.Equal(0, repositorio.ChamadasSomaAReceber);
                Assert.Equal(0, repositorio.ChamadasSomaEmAtraso);
            }
        }

        [Fact]
        public async Task Ocupacao_SobreOsVeiculosEmOperacao_ComUmaCasaDecimal()
        {
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(DadosBasicos()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(5, visao.TotalVeiculos);
            Assert.Equal(2, visao.VeiculosLocados);
            Assert.Equal(40.0m, visao.TaxaOcupacaoPercentual);
        }

        [Fact]
        public async Task Ocupacao_ArredondaParaUmaCasa()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 1 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 2 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(66.7m, visao.TaxaOcupacaoPercentual);
        }

        [Fact]
        public async Task Ocupacao_DenominadorZero_DevolveZeroSemErro()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 2 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.BAIXADO, Quantidade = 1 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(0, visao.TotalVeiculos);
            Assert.Equal(0, visao.VeiculosLocados);
            Assert.Equal(0m, visao.TaxaOcupacaoPercentual);
        }

        [Fact]
        public async Task PrimeiroAcesso_SemNenhumVeiculo_EVerdadeiro()
        {
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(new DadosPainelFake()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.True(visao.PrimeiroAcesso);
            Assert.Equal(0, visao.TotalVeiculos);
        }

        [Fact]
        public async Task PrimeiroAcesso_SoComVeiculoVendidoOuBaixado_NaoEVerdadeiro()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 1 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.False(visao.PrimeiroAcesso);
            Assert.Equal(0, visao.TotalVeiculos);
        }

        [Fact]
        public async Task FrotaPorSituacao_TrazTodosOsStatusNaOrdemDoEnum_ComZeroOnde_NaoHaVeiculo()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 2 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            StatusVeiculo[] esperado = Enum.GetValues<StatusVeiculo>();
            Assert.Equal(esperado.Length, visao.FrotaPorSituacao.Count);
            Assert.Equal(esperado, visao.FrotaPorSituacao.Select(x => x.Situacao).ToArray());
            Assert.Equal(2, visao.FrotaPorSituacao.Single(x => x.Situacao == StatusVeiculo.LOCADO).Quantidade);
            Assert.All(visao.FrotaPorSituacao.Where(x => x.Situacao != StatusVeiculo.LOCADO), x => Assert.Equal(0, x.Quantidade));
        }

        [Fact]
        public async Task ContextoVazio_FailClosed_TudoZeradoEPrimeiroAcesso()
        {
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Vazio(), DadosPorEmpresa(DadosBasicos()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.True(visao.PrimeiroAcesso);
            Assert.False(visao.ValoresVisiveis);
            Assert.Equal(0, visao.TotalVeiculos);
            Assert.Equal(0, visao.LocacoesAtivas);
            Assert.Equal(0, visao.QuantidadeCobrancasEmAberto);
            Assert.Equal(0, visao.QuantidadeCobrancasVencidas);
            Assert.Equal(0, visao.VeiculosNovosNoMes);
            Assert.All(visao.FrotaPorSituacao, x => Assert.Equal(0, x.Quantidade));
        }

        [Fact]
        public async Task DuasLocadoras_CadaUmaVeSoOsSeusNumeros()
        {
            DadosPainelFake dadosA = DadosBasicos();
            DadosPainelFake dadosB = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 7 }
                },
                LocacoesEmAndamento = 9
            };
            Dictionary<int, DadosPainelFake> dados = new Dictionary<int, DadosPainelFake> { [EmpresaA] = dadosA, [EmpresaB] = dadosB };

            (PainelService servicoA, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), dados);
            (PainelService servicoB, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaB, CargoFuncionario.ADMINISTRADOR), dados);

            ReturnVisaoGeralVO visaoA = await servicoA.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);
            ReturnVisaoGeralVO visaoB = await servicoB.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(5, visaoA.TotalVeiculos);
            Assert.Equal(2, visaoA.LocacoesAtivas);
            Assert.Equal(7, visaoB.TotalVeiculos);
            Assert.Equal(9, visaoB.LocacoesAtivas);
            Assert.Equal(0, visaoB.VeiculosLocados);
        }

        [Fact]
        public async Task LocacoesEDevolucoes_VemDoRepositorio()
        {
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(DadosBasicos()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(2, visao.LocacoesAtivas);
            Assert.Equal(1, visao.DevolucoesPrevistasHoje);
        }

        /// <summary>Frota com os seis status em quantidades diferentes: DISPONIVEL 3, RESERVADO 1, LOCADO 2, MANUTENCAO 4, VENDIDO 5, BAIXADO 6.</summary>
        private static DadosPainelFake DadosSeisStatus()
        {
            return new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 3 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.RESERVADO, Quantidade = 1 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 2 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.MANUTENCAO, Quantidade = 4 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 5 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.BAIXADO, Quantidade = 6 }
                }
            };
        }

        [Fact]
        public async Task SeisStatus_TotalLocadosTaxaEItensConferidos()
        {
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(DadosSeisStatus()));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(10, visao.TotalVeiculos);
            Assert.Equal(2, visao.VeiculosLocados);
            Assert.Equal(20.0m, visao.TaxaOcupacaoPercentual);
            Assert.Equal(6, visao.FrotaPorSituacao.Count);
            Dictionary<StatusVeiculo, int> esperado = new Dictionary<StatusVeiculo, int>
            {
                [StatusVeiculo.DISPONIVEL] = 3,
                [StatusVeiculo.RESERVADO] = 1,
                [StatusVeiculo.LOCADO] = 2,
                [StatusVeiculo.MANUTENCAO] = 4,
                [StatusVeiculo.VENDIDO] = 5,
                [StatusVeiculo.BAIXADO] = 6
            };
            foreach (KeyValuePair<StatusVeiculo, int> item in esperado)
            {
                Assert.Equal(item.Value, visao.FrotaPorSituacao.Single(x => x.Situacao == item.Key).Quantidade);
            }
        }

        [Fact]
        public async Task Ocupacao_ManutencaoEReservado_EntramNoDenominador()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.RESERVADO, Quantidade = 1 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.MANUTENCAO, Quantidade = 3 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 1 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(5, visao.TotalVeiculos);
            Assert.Equal(1, visao.VeiculosLocados);
            Assert.Equal(20.0m, visao.TaxaOcupacaoPercentual);
        }

        [Fact]
        public async Task Ocupacao_VendidoEBaixado_FicamForaDoDenominador()
        {
            DadosPainelFake dados = new DadosPainelFake
            {
                Frota = new List<FrotaSituacaoContagem>
                {
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.DISPONIVEL, Quantidade = 1 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.LOCADO, Quantidade = 1 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.VENDIDO, Quantidade = 10 },
                    new FrotaSituacaoContagem { Situacao = StatusVeiculo.BAIXADO, Quantidade = 10 }
                }
            };
            (PainelService servico, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(2, visao.TotalVeiculos);
            Assert.Equal(1, visao.VeiculosLocados);
            Assert.Equal(50.0m, visao.TaxaOcupacaoPercentual);
            Assert.Equal(10, visao.FrotaPorSituacao.Single(x => x.Situacao == StatusVeiculo.VENDIDO).Quantidade);
            Assert.Equal(10, visao.FrotaPorSituacao.Single(x => x.Situacao == StatusVeiculo.BAIXADO).Quantidade);
        }

        [Fact]
        public async Task VeiculosNovosNoMes_LimitesDaJanela_NoMesDeNovembro()
        {
            DadosPainelFake dados = DadosBasicos();
            dados.VeiculosCadastrados = new List<CadastroVeiculoFake>
            {
                new CadastroVeiculoFake(new DateTime(2026, 10, 31, 23, 59, 59, 999), true),
                new CadastroVeiculoFake(new DateTime(2026, 11, 1, 0, 0, 0), true),
                new CadastroVeiculoFake(new DateTime(2026, 11, 30, 23, 59, 59, 999), true),
                new CadastroVeiculoFake(new DateTime(2026, 12, 1, 0, 0, 0), true),
                new CadastroVeiculoFake(new DateTime(2026, 11, 5, 10, 0, 0), false)
            };
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados));

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(2, visao.VeiculosNovosNoMes);
            (DateTime inicioJanela, DateTime fimJanela) = repositorio.JanelasVeiculosNovos[0];
            Assert.Equal(new DateTime(2026, 11, 1), inicioJanela);
            Assert.Equal(new DateTime(2026, 12, 1), fimJanela);
        }

        [Fact]
        public async Task VeiculosNovosNoMes_ViradaDeAno_NoMesDeDezembro()
        {
            DadosPainelFake dados = DadosBasicos();
            dados.VeiculosCadastrados = new List<CadastroVeiculoFake>
            {
                new CadastroVeiculoFake(new DateTime(2026, 11, 30, 23, 59, 59, 999), true),
                new CadastroVeiculoFake(new DateTime(2026, 12, 1, 0, 0, 0), true),
                new CadastroVeiculoFake(new DateTime(2026, 12, 31, 23, 59, 59, 999), true),
                new CadastroVeiculoFake(new DateTime(2027, 1, 1, 0, 0, 0), true)
            };
            DateTime ultimoDiaDoAno = new DateTime(2026, 12, 31, 23, 0, 0);
            (PainelService servico, FakePainelRepository repositorio, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ATENDENTE), DadosPorEmpresa(dados), ultimoDiaDoAno);

            ReturnVisaoGeralVO visao = await servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(2, visao.VeiculosNovosNoMes);
            (DateTime inicioJanela, DateTime fimJanela) = repositorio.JanelasVeiculosNovos[0];
            Assert.Equal(new DateTime(2026, 12, 1), inicioJanela);
            Assert.Equal(new DateTime(2027, 1, 1), fimJanela);
        }

        [Fact]
        public async Task VeiculosNovosNoMes_DuasLocadoras_CadaUmaSoOsSeus()
        {
            CadastroVeiculoFake cadastroNoMes = new CadastroVeiculoFake(new DateTime(2026, 11, 2, 9, 0, 0), true);
            DadosPainelFake dadosA = DadosBasicos();
            dadosA.VeiculosCadastrados = Enumerable.Repeat(cadastroNoMes, 2).ToList();
            DadosPainelFake dadosB = DadosBasicos();
            dadosB.VeiculosCadastrados = Enumerable.Repeat(cadastroNoMes, 5).ToList();
            Dictionary<int, DadosPainelFake> dados = new Dictionary<int, DadosPainelFake> { [EmpresaA] = dadosA, [EmpresaB] = dadosB };

            (PainelService servicoA, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), dados);
            (PainelService servicoB, _, _) = Criar(ContextoEmpresaDeTeste.Criar(EmpresaB, CargoFuncionario.ADMINISTRADOR), dados);

            ReturnVisaoGeralVO visaoA = await servicoA.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);
            ReturnVisaoGeralVO visaoB = await servicoB.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS);

            Assert.Equal(2, visaoA.VeiculosNovosNoMes);
            Assert.Equal(5, visaoB.VeiculosNovosNoMes);
        }

        [Fact]
        public async Task FalhaNoRepositorio_RegistraNoLogERelanca()
        {
            FakePainelRepository repositorioComFalha = new FakePainelRepository(ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), DadosPorEmpresa(DadosBasicos()))
            {
                ExcecaoAoConsultar = new InvalidOperationException("falha simulada")
            };
            FakeLogService log = new FakeLogService();
            PainelService servico = new PainelService(repositorioComFalha, ContextoEmpresaDeTeste.Criar(EmpresaA, CargoFuncionario.ADMINISTRADOR), new FakeRelogio(AgoraFixo), log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ObterVisaoGeralAsync(PeriodoVisaoGeral.TRINTA_DIAS));

            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroConsultarVisaoGeral));
        }
    }
}
