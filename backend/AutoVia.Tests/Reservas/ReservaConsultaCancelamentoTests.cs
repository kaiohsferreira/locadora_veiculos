using AutoMapper;
using AutoVia.Api.Middleware;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Reservas;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Reservas
{
    public class ReservaConsultaCancelamentoTests
    {
        [Fact]
        public async Task GetByIdMapeiaCamposPermitidosDaReservaDoTitular()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Reserva reserva = CriarReserva(51, 7, StatusReserva.CONFIRMADA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));
            reserva.ValorEstimado = 825.50m;
            repositorio.Adicionar(reserva);
            ReservaService servico = CriarServico(repositorio);

            ReturnReservaVO retorno = await servico.GetByIdAsync(51, 7);

            Assert.Equal(51, retorno.IdReserva);
            Assert.Equal(7, retorno.IdVeiculo);
            Assert.Equal(new DateOnly(2026, 12, 10), retorno.DataRetirada);
            Assert.Equal(new DateOnly(2026, 12, 15), retorno.DataDevolucao);
            Assert.Equal(StatusReserva.CONFIRMADA.ToString(), retorno.Status);
            Assert.Equal(825.50m, retorno.ValorEstimado);
            Assert.DoesNotContain(retorno.GetType().GetProperties(), propriedade => propriedade.Name == "IdCliente");
        }

        [Fact]
        public async Task GetAllFiltraStatusETitularEPaginaPorDataEReservaDecrescentes()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            repositorio.Adicionar(CriarReserva(1, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 12)));
            repositorio.Adicionar(CriarReserva(2, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 11), new DateTime(2026, 12, 13)));
            repositorio.Adicionar(CriarReserva(3, 8, StatusReserva.ATIVA, new DateTime(2026, 12, 12), new DateTime(2026, 12, 14)));
            repositorio.Adicionar(CriarReserva(4, 7, StatusReserva.CANCELADA, new DateTime(2026, 12, 13), new DateTime(2026, 12, 15)));
            ReservaService servico = CriarServico(repositorio);
            FiltroReservaVO filtro = new FiltroReservaVO
            {
                Status = StatusReserva.ATIVA.ToString(),
                Pagina = 2,
                TamanhoPagina = 1
            };

            PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(7, filtro);

            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Pagina);
            Assert.Equal(1, pagina.TamanhoPagina);
            Assert.Equal(1, Assert.Single(pagina.Itens).IdReserva);
        }

        [Fact]
        public async Task CancelamentoDeAtivaEIdempotente()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Reserva reserva = CriarReserva(51, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));
            repositorio.Adicionar(reserva);
            ReservaService servico = CriarServico(repositorio);

            await servico.CancelarAsync(51, 7);
            await servico.CancelarAsync(51, 7);
            ReturnReservaVO retorno = await servico.GetByIdAsync(51, 7);

            Assert.Equal(StatusReserva.CANCELADA.ToString(), retorno.Status);
        }

        [Fact]
        public async Task ReservaConcluidaNaoPodeSerCanceladaEMiddlewareConvertePara409()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            repositorio.Adicionar(CriarReserva(51, 7, StatusReserva.CONCLUIDA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15)));
            ReservaService servico = CriarServico(repositorio);
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Response.Body = new System.IO.MemoryStream();
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => servico.CancelarAsync(51, 7), new FakeLogService());

            await middleware.InvokeAsync(contexto);

            contexto.Response.Body.Position = 0;
            JsonDocument documento = await JsonDocument.ParseAsync(contexto.Response.Body);
            Assert.Equal(StatusCodes.Status409Conflict, contexto.Response.StatusCode);
            Assert.Equal(ConstantsMessage.ErroReservaConcluidaNaoCancelavel, documento.RootElement.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task ReservaAusenteDesativadaOuDeOutroTitularRetornaNotFound()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            repositorio.Adicionar(CriarReserva(51, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15)));
            Reserva desativada = CriarReserva(52, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));
            desativada.DisabledAt = new DateTime(2026, 12, 1);
            repositorio.Adicionar(desativada);
            ReservaService servico = CriarServico(repositorio);

            await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(51, 8));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(52, 7));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(99, 7));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.CancelarAsync(51, 8));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.CancelarAsync(52, 7));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.CancelarAsync(99, 7));
        }

        [Theory]
        [InlineData("ATIVA", true)]
        [InlineData("CONFIRMADA", true)]
        [InlineData("CANCELADA", true)]
        [InlineData("CONCLUIDA", true)]
        [InlineData("0", false)]
        [InlineData("ativa", false)]
        [InlineData("99", false)]
        public void ValidadorAceitaSomenteNomesTextuaisDosStatus(string status, bool esperado)
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroReservaVO { Status = status });

            Assert.Equal(esperado, resultado.IsValid);
            if (!esperado)
            {
                string mensagemEsperada = string.Format(ConstantsMessage.ErroReservaStatusInvalido, string.Join(", ", Enum.GetNames<StatusReserva>()));
                Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == mensagemEsperada);
            }
        }

        [Fact]
        public void ValidadorRejeitaPaginaQueEstourariaODeslocamentoDaReserva()
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();
            FiltroReservaVO filtro = new FiltroReservaVO
            {
                Pagina = int.MaxValue,
                TamanhoPagina = 100
            };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            // Uma unica mensagem: a regra de estouro vem so do FiltroPaginacaoVOValidator (sem copia no validador da reserva).
            FluentValidation.Results.ValidationFailure erro = Assert.Single(resultado.Errors);
            Assert.Equal(ConstantsMessage.ErroPaginaDeslocamentoInvalido, erro.ErrorMessage);
        }

        [Fact]
        public async Task CancelarReservaCompartilhadaLiberaConsultaDeDisponibilidadeNoMesmoPeriodo()
        {
            DateOnly retirada = new DateOnly(2026, 12, 10);
            DateOnly devolucao = new DateOnly(2026, 12, 15);
            Reserva reserva = CriarReserva(51, 7, StatusReserva.ATIVA, retirada.ToDateTime(TimeOnly.MinValue), devolucao.ToDateTime(TimeOnly.MinValue));
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.Adicionar(reserva);
            Categoria categoria = new Categoria { IdCategoria = 3, Nome = "SUV", ValorDiariaBase = 200m };
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 7,
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m,
                Status = StatusVeiculo.RESERVADO,
                Reservas = new List<Reserva> { reserva }
            };
            veiculos.Adicionar(veiculo);
            reservas.VincularVeiculo(veiculo);
            VeiculoService disponibilidade = CriarServicoDisponibilidade(veiculos);
            ReservaService cancelamento = CriarServico(reservas);
            FiltroVeiculoDisponibilidadeVO filtro = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = retirada,
                DataDevolucao = devolucao
            };

            PaginaVO<ReturnVeiculoDisponivelVO> antes = await disponibilidade.GetDisponiveisAsync(filtro);
            await cancelamento.CancelarAsync(51, 7);
            PaginaVO<ReturnVeiculoDisponivelVO> depois = await disponibilidade.GetDisponiveisAsync(filtro);

            Assert.Empty(antes.Itens);
            Assert.Equal(1, depois.Total);
            Assert.Equal(7, Assert.Single(depois.Itens).IdVeiculo);
            Veiculo? veiculoPersistido = reservas.VeiculoPersistido;
            Assert.NotNull(veiculoPersistido);
            Assert.Equal(StatusVeiculo.DISPONIVEL, veiculoPersistido.Status);
        }

        [Fact]
        public async Task CancelarReservaMantemVeiculoReservadoQuandoExisteOutraReservaAtiva()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Reserva reservaCancelada = CriarReserva(51, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));
            Reserva reservaMantida = CriarReserva(52, 7, StatusReserva.CONFIRMADA, new DateTime(2026, 12, 16), new DateTime(2026, 12, 20));
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 7,
                Status = StatusVeiculo.RESERVADO
            };
            repositorio.Adicionar(reservaCancelada);
            repositorio.Adicionar(reservaMantida);
            repositorio.VincularVeiculo(veiculo);
            ReservaService servico = CriarServico(repositorio);

            await servico.CancelarAsync(51, 7);

            Assert.Equal(StatusReserva.CANCELADA, reservaCancelada.Status);
            Veiculo? veiculoPersistido = repositorio.VeiculoPersistido;
            Assert.NotNull(veiculoPersistido);
            Assert.Equal(StatusVeiculo.RESERVADO, veiculoPersistido.Status);
        }

        [Theory]
        [InlineData(StatusVeiculo.LOCADO)]
        [InlineData(StatusVeiculo.MANUTENCAO)]
        public async Task CancelarReservaPreservaStatusDeVeiculoForaDosEstadosRecalculaveis(StatusVeiculo statusOriginal)
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Reserva reserva = CriarReserva(51, 7, StatusReserva.ATIVA, new DateTime(2026, 12, 10), new DateTime(2026, 12, 15));
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 7,
                Status = statusOriginal
            };
            repositorio.Adicionar(reserva);
            repositorio.VincularVeiculo(veiculo);
            ReservaService servico = CriarServico(repositorio);

            await servico.CancelarAsync(51, 7);

            Veiculo? veiculoPersistido = repositorio.VeiculoPersistido;
            Assert.NotNull(veiculoPersistido);
            Assert.Equal(statusOriginal, veiculoPersistido.Status);
        }

        private static Reserva CriarReserva(int idReserva, int idCliente, StatusReserva status, DateTime retirada, DateTime devolucao)
        {
            return new Reserva
            {
                IdReserva = idReserva,
                IdCliente = idCliente,
                IdVeiculo = 7,
                DataRetiradaPrevista = retirada,
                DataDevolucaoPrevista = devolucao,
                Status = status
            };
        }

        private static ReservaService CriarServico(FakeReservaRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService log = new FakeLogService();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, new ArmazenamentoClienteLocadora());
            FakeRelogio relogio = new FakeRelogio(new DateTime(2026, 11, 16, 10, 0, 0));
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());
            return new ReservaService(repositorio, clienteLocadora, relogio, mapper, new FakeLogService(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), new FakeLogService()));
        }

        private static VeiculoService CriarServicoDisponibilidade(FakeVeiculoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<VeiculoDisponivelProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, new FakeLogService(), ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), new FakeLogService()));
        }
    }
}
