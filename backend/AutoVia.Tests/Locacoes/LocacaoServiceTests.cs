using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    public class LocacaoServiceTests
    {
        private static readonly DateTime AgoraDoRelogio = new DateTime(2026, 11, 19, 9, 30, 0);

        [Fact]
        public async Task IniciarReservaValida_CriaLocacaoEAtualizaEstados()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            LocacaoService servico = CriarServico(repositorio);

            ReturnLocacaoVO retorno = await servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20);

            Reserva reserva = repositorio.ObterReserva(10)!;
            Veiculo veiculo = repositorio.ObterVeiculo(10)!;
            Assert.True(retorno.IdLocacao > 0);
            Assert.Equal(10, retorno.IdReserva);
            Assert.Equal(30, retorno.IdCliente);
            Assert.Equal(40, retorno.IdVeiculo);
            Assert.Equal(20, retorno.IdFuncionario);
            Assert.Equal(1200, retorno.KmSaida);
            Assert.Equal(175m, retorno.ValorDiaria);
            Assert.Equal(0m, retorno.ValorCaucao);
            Assert.Equal(StatusLocacao.EM_ANDAMENTO, retorno.Status);
            Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), retorno.DataDevolucaoPrevista);
            Assert.Equal(StatusReserva.CONCLUIDA, reserva.Status);
            Assert.Equal(StatusVeiculo.LOCADO, veiculo.Status);
        }

        [Fact]
        public async Task IniciarReserva_DataDeRetiradaVemDoRelogioEIrParaORepositorio()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            LocacaoService servico = CriarServico(repositorio);

            ReturnLocacaoVO retorno = await servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20);

            Assert.Equal(AgoraDoRelogio, retorno.DataRetirada);
            Assert.Equal(AgoraDoRelogio, repositorio.UltimaDataRetirada);
            Assert.Equal("19/11/2026 09:30", retorno.DataRetiradaStr);
            Assert.Equal("20/11/2026", retorno.DataDevolucaoPrevistaStr);
            Assert.Equal(StaticMethods.FormateToMonetary(175m), retorno.ValorDiariaStr);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), retorno.ValorCaucaoStr);
        }

        [Fact]
        public async Task ReservaInexistente_LancaNotFound()
        {
            LocacaoService servico = CriarServico(new FakeLocacaoRepository());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 99 }, 20));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task ReservaDeOutraLocadora_DevolveNuloNoRepositorio_LancaNotFoundSemAbrirLocacao()
        {
            // O filtro "Empresa" faz a reserva de outra locadora sumir da leitura: o repositorio devolve nulo.
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            LocacaoService servico = CriarServico(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, repositorio.ChamadasStartFromReserva);
        }

        [Theory]
        [InlineData(StatusReserva.CANCELADA)]
        [InlineData(StatusReserva.CONCLUIDA)]
        public async Task ReservaEmEstadoNaoElegivel_LancaConflito(StatusReserva status)
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusReserva: status);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaEstadoInvalido, excecao.Message);
        }

        [Fact]
        public async Task VeiculoQueNaoEstaReservado_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusVeiculo: StatusVeiculo.DISPONIVEL);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoVeiculoIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task ReservaComLocacaoAtiva_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            repositorio.Adicionar(new Locacao { IdReserva = 10, DisabledAt = null });
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task FuncionarioInativo_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(funcionarioAtivo: false);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task ReservaInexistente_RegistraAoMenosUmLogDeErro()
        {
            // Log do Kaio (16/11): o 404 do servico deixa registro. Sem exigir log vazio em 4xx.
            FakeLogService log = new FakeLogService();
            LocacaoService servico = CriarServico(new FakeLocacaoRepository(), log);

            await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 99 }, 20));

            Assert.NotEmpty(log.Entries);
        }

        [Fact]
        public async Task ReservaConcluida_RegistraAoMenosUmLogDeErro()
        {
            // Log do Kaio (16/11): o 409 do servico deixa registro. Sem exigir log vazio em 4xx.
            FakeLogService log = new FakeLogService();
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusReserva: StatusReserva.CONCLUIDA);
            LocacaoService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.NotEmpty(log.Entries);
        }

        private static FakeLocacaoRepository CriarRepositorioValido(
            StatusReserva statusReserva = StatusReserva.ATIVA,
            StatusVeiculo statusVeiculo = StatusVeiculo.RESERVADO,
            bool funcionarioAtivo = true)
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40,
                KmAtual = 1200,
                ValorDiaria = 175m,
                Status = statusVeiculo
            };
            Reserva reserva = new Reserva
            {
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                DataRetiradaPrevista = new DateTime(2026, 11, 19, 10, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 20, 10, 0, 0),
                Status = statusReserva,
                Veiculo = veiculo
            };
            Funcionario funcionario = new Funcionario
            {
                IdFuncionario = 20,
                Ativo = funcionarioAtivo
            };

            repositorio.Adicionar(reserva);
            repositorio.Adicionar(funcionario);

            return repositorio;
        }

        private static LocacaoService CriarServico(FakeLocacaoRepository repositorio, FakeLogService? logService = null)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService log = logService ?? new FakeLogService();
            FakeRelogio relogio = new FakeRelogio(AgoraDoRelogio);
            LocacaoService servico = new LocacaoService(repositorio, relogio, mapper, log, ContextoEmpresaDeTeste.Criar());

            return servico;
        }
    }
}
