using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.PainelOperacional;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.PainelOperacional
{
    public class PainelOperacionalServiceTests
    {
        [Fact]
        public async Task GetPainelDiario_AgrupaReservasELocacoesPorRetiradaEDevolucao()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(31, StatusReserva.ATIVA, hoje.AddHours(8), hoje.AddDays(1)));
            repositorio.Reservas.Add(CriarReserva(32, StatusReserva.CONFIRMADA, hoje.AddDays(-2), hoje.AddHours(17)));
            repositorio.Locacoes.Add(CriarLocacao(41, StatusLocacao.EM_ANDAMENTO, hoje.AddHours(9), hoje.AddHours(18)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal(DateOnly.FromDateTime(hoje), retorno.Data);
            Assert.Equal(2, retorno.Retiradas.Count);
            Assert.Equal(2, retorno.Devolucoes.Count);
            Assert.Equal(nameof(Reserva).ToUpperInvariant(), retorno.Retiradas[0].Origem);
            Assert.Equal(31, retorno.Retiradas[0].IdReserva);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(hoje.AddHours(8)), retorno.Retiradas[0].DataHora);
            Assert.Equal(nameof(Locacao).ToUpperInvariant(), retorno.Retiradas[1].Origem);
            Assert.Equal(41, retorno.Retiradas[1].IdLocacao);
            Assert.Equal(nameof(Reserva).ToUpperInvariant(), retorno.Devolucoes[0].Origem);
            Assert.Equal(32, retorno.Devolucoes[0].IdReserva);
            Assert.Equal(nameof(Locacao).ToUpperInvariant(), retorno.Devolucoes[1].Origem);
            Assert.Equal(41, retorno.Devolucoes[1].IdLocacao);
            Assert.Equal("Nome Cliente", retorno.Retiradas[0].NomeCliente);
            Assert.Equal("ABC1234", retorno.Retiradas[0].Placa);
        }

        [Fact]
        public async Task GetPainelDiario_ExcluiStatusCanceladosFinalizadosERegistrosDesativados()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(51, StatusReserva.CANCELADA, hoje.AddHours(8), hoje.AddHours(10)));
            repositorio.Reservas.Add(CriarReserva(52, StatusReserva.CONCLUIDA, hoje.AddHours(8), hoje.AddHours(10)));
            Reserva reservaDesativada = CriarReserva(53, StatusReserva.ATIVA, hoje.AddHours(8), hoje.AddHours(10));
            reservaDesativada.Disable();
            repositorio.Reservas.Add(reservaDesativada);
            repositorio.Locacoes.Add(CriarLocacao(61, StatusLocacao.CANCELADA, hoje.AddHours(8), hoje.AddHours(10)));
            repositorio.Locacoes.Add(CriarLocacao(62, StatusLocacao.FINALIZADA, hoje.AddHours(8), hoje.AddHours(10)));
            Locacao locacaoDesativada = CriarLocacao(63, StatusLocacao.EM_ANDAMENTO, hoje.AddHours(8), hoje.AddHours(10));
            locacaoDesativada.Disable();
            repositorio.Locacoes.Add(locacaoDesativada);
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Empty(retorno.Retiradas);
            Assert.Empty(retorno.Devolucoes);
        }

        [Fact]
        public async Task GetPainelDiario_UsaInicioInclusivoEFimExclusivoDoDiaLocal()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(71, StatusReserva.ATIVA, hoje, hoje.AddHours(17)));
            repositorio.Reservas.Add(CriarReserva(72, StatusReserva.ATIVA, hoje.AddDays(1), hoje.AddDays(2)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal(hoje, repositorio.InicioRecebido);
            Assert.Equal(hoje.AddDays(1), repositorio.ProximoDiaRecebido);
            Assert.Single(retorno.Retiradas);
            Assert.Equal(71, retorno.Retiradas[0].IdReserva);
            Assert.Single(retorno.Devolucoes);
            Assert.Equal(71, retorno.Devolucoes[0].IdReserva);
        }

        [Fact]
        public async Task GetPainelDiario_SemMovimentosRetornaGruposVazios()
        {
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Empty(retorno.Retiradas);
            Assert.Empty(retorno.Devolucoes);
        }

        [Fact]
        public async Task GetPainelDiario_PropagaEFazLogDaFalhaDoRepositorio()
        {
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository
            {
                LancarExcecao = true
            };
            FakeLogService log = new FakeLogService();
            PainelOperacionalService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetPainelDiarioAsync());

            Assert.Contains(log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroConsultarPainelOperacional, StringComparison.Ordinal));
        }

        [Fact]
        public async System.Threading.Tasks.Task RelogioAs2359_ItemDasZeroHoraDoDiaSeguinteFicaFora()
        {
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            DateTime diaSeguinte = new DateTime(2026, 11, 17);
            repositorio.Reservas.Add(CriarReserva(81, StatusReserva.ATIVA, diaSeguinte, diaSeguinte.AddDays(2)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService(), new DateTime(2026, 11, 16, 23, 59, 0));

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Empty(retorno.Retiradas);
            Assert.Equal(new DateOnly(2026, 11, 16), retorno.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task RelogioAs0000_ItemDasZeroHoraDoDiaEntra()
        {
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            DateTime diaSeguinte = new DateTime(2026, 11, 17);
            repositorio.Reservas.Add(CriarReserva(82, StatusReserva.ATIVA, diaSeguinte, diaSeguinte.AddDays(2)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService(), new DateTime(2026, 11, 17, 0, 0, 0));

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Single(retorno.Retiradas);
            Assert.Equal(new DateOnly(2026, 11, 17), retorno.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnPainelDiario_TrazDataEDataStrDoMesmoDia()
        {
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService(), new DateTime(2026, 11, 16, 10, 0, 0));

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal(new DateOnly(2026, 11, 16), retorno.Data);
            Assert.Equal("16/11/2026", retorno.DataStr);
        }

        [Fact]
        public async Task GetPainelDiario_CodigoReservaDoItemDeReservaELocacao()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(31, StatusReserva.ATIVA, hoje.AddHours(8), hoje.AddDays(1)));
            repositorio.Locacoes.Add(CriarLocacaoDeReserva(41, 37, StatusLocacao.EM_ANDAMENTO, hoje.AddHours(9), hoje.AddDays(1)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal("AV-00031", retorno.Retiradas[0].CodigoReserva);
            Assert.Equal("AV-00037", retorno.Retiradas[1].CodigoReserva);
            Assert.Null(retorno.Retiradas[1].IdReserva);
        }

        [Fact]
        public async Task GetPainelDiario_LocacaoSemReservaDeOrigem_CodigoNulo()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(31, StatusReserva.ATIVA, hoje.AddHours(8), hoje.AddDays(1)));
            repositorio.Locacoes.Add(CriarLocacao(41, StatusLocacao.EM_ANDAMENTO, hoje.AddHours(9), hoje.AddDays(1)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal("AV-00031", retorno.Retiradas[0].CodigoReserva);
            Assert.Null(retorno.Retiradas[1].CodigoReserva);
        }

        [Fact]
        public async Task GetPainelDiario_MesmoRegistroNasDuasListas_MesmoCodigo()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Locacoes.Add(CriarLocacaoDeReserva(41, 37, StatusLocacao.EM_ANDAMENTO, hoje.AddHours(9), hoje.AddHours(18)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal("AV-00037", retorno.Retiradas[0].CodigoReserva);
            Assert.Equal("AV-00037", retorno.Devolucoes[0].CodigoReserva);
        }

        [Fact]
        public async Task GetPainelDiario_IdComSeisDigitos_CodigoSemCortar()
        {
            DateTime hoje = AgoraFixa.Date;
            FakePainelOperacionalRepository repositorio = new FakePainelOperacionalRepository();
            repositorio.Reservas.Add(CriarReserva(123456, StatusReserva.ATIVA, hoje.AddHours(8), hoje.AddDays(1)));
            PainelOperacionalService servico = CriarServico(repositorio, new FakeLogService());

            ReturnPainelDiarioVO retorno = await servico.GetPainelDiarioAsync();

            Assert.Equal("AV-123456", retorno.Retiradas[0].CodigoReserva);
        }

        private static Locacao CriarLocacaoDeReserva(int idLocacao, int idReserva, StatusLocacao status, DateTime dataRetirada, DateTime dataDevolucao)
        {
            Locacao locacao = CriarLocacao(idLocacao, status, dataRetirada, dataDevolucao);
            locacao.IdReserva = idReserva;

            return locacao;
        }

        private static Reserva CriarReserva(int idReserva, StatusReserva status, DateTime dataRetirada, DateTime dataDevolucao)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 101,
                Nome = "Nome Cliente"
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 201,
                Placa = "ABC1234",
                Marca = "Marca",
                Modelo = "Modelo"
            };
            return new Reserva
            {
                IdReserva = idReserva,
                IdCliente = cliente.IdCliente,
                Cliente = cliente,
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                DataRetiradaPrevista = dataRetirada,
                DataDevolucaoPrevista = dataDevolucao,
                Status = status
            };
        }

        private static Locacao CriarLocacao(int idLocacao, StatusLocacao status, DateTime dataRetirada, DateTime dataDevolucao)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 102,
                Nome = "Nome Cliente"
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 202,
                Placa = "DEF5678",
                Marca = "Marca",
                Modelo = "Modelo"
            };
            return new Locacao
            {
                IdLocacao = idLocacao,
                IdCliente = cliente.IdCliente,
                Cliente = cliente,
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                DataRetirada = dataRetirada,
                DataDevolucaoPrevista = dataDevolucao,
                Status = status
            };
        }

        /// <summary>Data e hora fixas do relogio dos testes do painel: o dia da consulta nao depende do dia da execucao.</summary>
        private static readonly DateTime AgoraFixa = new DateTime(2026, 11, 16, 10, 0, 0);

        private static PainelOperacionalService CriarServico(FakePainelOperacionalRepository repositorio, FakeLogService log)
        {
            return CriarServico(repositorio, log, AgoraFixa);
        }

        private static PainelOperacionalService CriarServico(FakePainelOperacionalRepository repositorio, FakeLogService log, DateTime agora)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<PainelOperacionalProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            return new PainelOperacionalService(repositorio, new FakeRelogio(agora), mapper, log);
        }
    }
}
