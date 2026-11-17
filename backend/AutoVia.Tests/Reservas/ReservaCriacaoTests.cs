using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.Validators.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using System;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using AutoVia.Communication.CustomExceptions;

namespace AutoVia.Tests.Reservas
{
    public class ReservaCriacaoTests
    {
        private const int IdEmpresaDoVeiculo = 1;
        private const int IdVeiculoReservado = 8;
        private const string NomeLocadora = "Locadora Um";
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 16, 10, 0, 0);

        [Fact]
        public async System.Threading.Tasks.Task ClienteAprovadoCriaReservaDoTitularComStatusAtiva()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(new Veiculo { IdVeiculo = IdVeiculoReservado, IdEmpresa = IdEmpresaDoVeiculo, Status = StatusVeiculo.DISPONIVEL });
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamento();
            armazenamento.Semear(31, IdEmpresaDoVeiculo, StatusCadastroCliente.APROVADO);
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, armazenamento, log, 31);
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = IdVeiculoReservado,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(31, entrada);

            Assert.Equal(31, reservas.Armazenada?.IdCliente);
            Assert.Equal(IdVeiculoReservado, retorno.IdVeiculo);
            Assert.Equal(1, retorno.IdReserva);
            Assert.Equal(StatusReserva.ATIVA, retorno.Status);
            Assert.Empty(log.Snapshot());
        }

        [Theory]
        [InlineData(StatusCadastroCliente.PENDENTE)]
        [InlineData(StatusCadastroCliente.RECUSADO)]
        public async System.Threading.Tasks.Task ClienteNaoAprovadoNaoInsereReserva(StatusCadastroCliente status)
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(new Veiculo { IdVeiculo = IdVeiculoReservado, IdEmpresa = IdEmpresaDoVeiculo, Status = StatusVeiculo.DISPONIVEL });
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamento();
            armazenamento.Semear(32, IdEmpresaDoVeiculo, status);
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, armazenamento, log, 32);
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = IdVeiculoReservado,
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

        /// <summary>Armazenamento com a locadora do veiculo ativa; os vinculos do cliente sao semeados por cenario.</summary>
        private static ArmazenamentoClienteLocadora CriarArmazenamento()
        {
            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresaDoVeiculo, NomeFantasia = NomeLocadora, Ativo = true });
            return armazenamento;
        }

        /// <summary>Servico real: a reserva usa o ClienteLocadoraService real sobre o repositorio fake do vinculo, com o cliente do contexto definido (como a sessao faz).</summary>
        private static ReservaService CriarServico(FakeReservaRepository reservas, ArmazenamentoClienteLocadora armazenamento, FakeLogService log, int idCliente)
        {
            IMapper mapper = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance).CreateMapper();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            contexto.DefinirCliente(idCliente);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, armazenamento);
            FakeRelogio relogio = new FakeRelogio(AgoraFixo);
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());
            return new ReservaService(reservas, clienteLocadora, relogio, mapper, log, new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), log));
        }
    }
}
