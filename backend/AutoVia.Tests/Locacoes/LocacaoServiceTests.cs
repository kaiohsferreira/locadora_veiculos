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
        [Fact]
        public async Task IniciarReservaValida_CriaLocacaoEAtualizaEstados()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            LocacaoService servico = CriarServico(repositorio);
            DateTime antes = DateTime.Now.AddSeconds(-2);

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
            Assert.True(retorno.DataRetirada >= antes);
            Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), retorno.DataDevolucaoPrevista);
            Assert.Equal(StatusReserva.CONCLUIDA, reserva.Status);
            Assert.Equal(StatusVeiculo.LOCADO, veiculo.Status);
        }

        [Fact]
        public async Task ReservaInexistente_LancaNotFound()
        {
            LocacaoService servico = CriarServico(new FakeLocacaoRepository());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 99 }, 20));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
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

        private static LocacaoService CriarServico(FakeLocacaoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService logService = new FakeLogService();
            LocacaoService servico = new LocacaoService(repositorio, mapper, logService);

            return servico;
        }
    }
}
