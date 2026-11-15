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
            DateTime hoje = DateTime.Now.Date;
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
            DateTime hoje = DateTime.Now.Date;
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
            DateTime hoje = DateTime.Now.Date;
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

        private static PainelOperacionalService CriarServico(FakePainelOperacionalRepository repositorio, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<PainelOperacionalProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            return new PainelOperacionalService(repositorio, mapper, log);
        }
    }
}
