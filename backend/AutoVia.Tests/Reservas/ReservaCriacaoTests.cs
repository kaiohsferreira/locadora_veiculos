using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.Validators.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using AutoVia.Communication.CustomExceptions;

namespace AutoVia.Tests.Reservas
{
    public class ReservaCriacaoTests
    {
        [Fact]
        public async System.Threading.Tasks.Task ClienteAprovadoCriaReservaDoTitularComStatusAtiva()
        {
            FakeClienteRepository clientes = new FakeClienteRepository();
            clientes.Adicionar(new Cliente { IdCliente = 31, StatusCadastro = StatusCadastroCliente.APROVADO });
            FakeReservaRepository reservas = new FakeReservaRepository();
            FakeLogService log = new FakeLogService();
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            ReservaService servico = new ReservaService(clientes, reservas, mapper, log);
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = 8,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(31, entrada);

            Assert.Equal(31, reservas.Armazenada?.IdCliente);
            Assert.Equal(8, retorno.IdVeiculo);
            Assert.Equal(1, retorno.IdReserva);
            Assert.Equal(StatusReserva.ATIVA, retorno.Status);
            Assert.Empty(log.Snapshot());
        }

        [Theory]
        [InlineData(StatusCadastroCliente.PENDENTE)]
        [InlineData(StatusCadastroCliente.RECUSADO)]
        public async System.Threading.Tasks.Task ClienteNaoAprovadoNaoInsereReserva(StatusCadastroCliente status)
        {
            FakeClienteRepository clientes = new FakeClienteRepository();
            clientes.Adicionar(new Cliente { IdCliente = 32, StatusCadastro = status });
            FakeReservaRepository reservas = new FakeReservaRepository();
            FakeLogService log = new FakeLogService();
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            ReservaService servico = new ReservaService(clientes, reservas, configuracao.CreateMapper(), log);
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = 8,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(32, entrada));

            Assert.Equal(0, reservas.ChamadasDeInsercao);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidadorRejeitaVeiculoInvalido(int idVeiculo)
        {
            CriarReservaVOValidator validador = new CriarReservaVOValidator();
            FluentValidation.Results.ValidationResult resultado = validador.Validate(new CriarReservaVO
            {
                IdVeiculo = idVeiculo,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            });

            Assert.False(resultado.IsValid);
        }

        [Fact]
        public void ValidadorRejeitaPeriodoInvertidoOuIgual()
        {
            CriarReservaVOValidator validador = new CriarReservaVOValidator();
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = 8,
                DataRetirada = new DateOnly(2026, 12, 15),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(entrada);

            Assert.False(resultado.IsValid);
        }
    }
}
