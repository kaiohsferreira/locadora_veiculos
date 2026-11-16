using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    public class VeiculoServiceDisponibilidadeTests
    {
        [Fact]
        public async Task ReservaSobrepostaExcluiVeiculo_EPeriodoPosteriorInclui()
        {
            DateOnly retiradaReserva = new DateOnly(2026, 12, 10);
            DateOnly devolucaoReserva = new DateOnly(2026, 12, 15);
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo veiculo = CriarVeiculo(categoria, StatusVeiculo.RESERVADO, 150m);
            veiculo.Reservas.Add(new Reserva
            {
                IdVeiculo = veiculo.IdVeiculo,
                DataRetiradaPrevista = retiradaReserva.ToDateTime(TimeOnly.MinValue),
                DataDevolucaoPrevista = devolucaoReserva.ToDateTime(TimeOnly.MinValue),
                Status = StatusReserva.ATIVA
            });
            repositorio.Adicionar(veiculo);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoDisponivelVO> sobreposta = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 12),
                DataDevolucao = new DateOnly(2026, 12, 14)
            });
            PaginaVO<ReturnVeiculoDisponivelVO> posterior = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 16),
                DataDevolucao = new DateOnly(2026, 12, 18)
            });

            Assert.Empty(sobreposta.Itens);
            Assert.Equal(1, posterior.Total);
            Assert.Single(posterior.Itens);
        }

        [Fact]
        public async Task IntervalosAdjacentesNaoSobrepoemEStatusDeReservaInativoNaoBloqueia()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo veiculo = CriarVeiculo(categoria, StatusVeiculo.RESERVADO, 150m);
            veiculo.Reservas.Add(new Reserva
            {
                DataRetiradaPrevista = new DateTime(2026, 12, 10),
                DataDevolucaoPrevista = new DateTime(2026, 12, 15),
                Status = StatusReserva.CONFIRMADA
            });
            veiculo.Reservas.Add(new Reserva
            {
                DataRetiradaPrevista = new DateTime(2026, 12, 15),
                DataDevolucaoPrevista = new DateTime(2026, 12, 18),
                Status = StatusReserva.CANCELADA
            });
            repositorio.Adicionar(veiculo);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 15),
                DataDevolucao = new DateOnly(2026, 12, 18)
            });

            Assert.Equal(1, pagina.Total);
        }

        [Fact]
        public async Task ReservaConcluidaNaoBloqueiaDisponibilidade()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo veiculo = CriarVeiculo(categoria, StatusVeiculo.RESERVADO, 150m);
            veiculo.Reservas.Add(new Reserva
            {
                DataRetiradaPrevista = new DateTime(2026, 12, 10),
                DataDevolucaoPrevista = new DateTime(2026, 12, 15),
                Status = StatusReserva.CONCLUIDA
            });
            repositorio.Adicionar(veiculo);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 12),
                DataDevolucao = new DateOnly(2026, 12, 14)
            });

            Assert.Equal(1, pagina.Total);
        }

        [Fact]
        public async Task FiltrosDeCategoriaEFaixaDeDiariaSaoInclusivosEPaginados()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Categoria economico = new Categoria { IdCategoria = 2, Nome = "Economico", ValorDiariaBase = 80m };
            repositorio.Adicionar(CriarVeiculo(suv, StatusVeiculo.DISPONIVEL, 100m));
            repositorio.Adicionar(CriarVeiculo(suv, StatusVeiculo.DISPONIVEL, 150m));
            repositorio.Adicionar(CriarVeiculo(suv, StatusVeiculo.MANUTENCAO, 125m));
            repositorio.Adicionar(CriarVeiculo(economico, StatusVeiculo.DISPONIVEL, 125m));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 12),
                IdCategoria = suv.IdCategoria,
                ValorDiariaMin = 100m,
                ValorDiariaMax = 150m,
                Pagina = 2,
                TamanhoPagina = 1
            });

            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Pagina);
            Assert.Equal(1, pagina.TamanhoPagina);
            Assert.Single(pagina.Itens);
            Assert.Equal(150m, pagina.Itens[0].ValorDiaria);
        }

        [Fact]
        public async Task ResultadoPublicoNaoContemCamposSensiveis()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(categoria, StatusVeiculo.DISPONIVEL, 150m));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 12)
            });

            ReturnVeiculoDisponivelVO item = Assert.Single(pagina.Itens);
            Assert.Equal("SUV", item.CategoriaNome);
            Assert.Equal("Fiat", item.Marca);
            Assert.Equal(150m, item.ValorDiaria);
            Assert.DoesNotContain(item.GetType().GetProperties(), x => x.Name.Contains("Chassi") || x.Name.Contains("Renavam") || x.Name.Contains("Placa") || x.Name.Contains("KmAtual") || x.Name.Contains("ValorVenda"));
        }

        [Fact]
        public void ValidadorRejeitaPeriodoEPrecoInvertidos()
        {
            FiltroVeiculoDisponibilidadeVOValidator validador = new FiltroVeiculoDisponibilidadeVOValidator();
            FiltroVeiculoDisponibilidadeVO filtro = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 15),
                DataDevolucao = new DateOnly(2026, 12, 10),
                ValorDiariaMin = 200m,
                ValorDiariaMax = 100m
            };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Equal(2, resultado.Errors.Count);
        }

        [Fact]
        public void ValidadorRejeitaPaginaQueEstourariaODeslocamento()
        {
            FiltroVeiculoDisponibilidadeVOValidator validador = new FiltroVeiculoDisponibilidadeVOValidator();
            FiltroVeiculoDisponibilidadeVO filtro = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 12),
                Pagina = int.MaxValue,
                TamanhoPagina = 100
            };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }

        [Fact]
        public void PaginacaoUsaPadroes20EMaximo100()
        {
            FiltroVeiculoDisponibilidadeVO filtroPadrao = new FiltroVeiculoDisponibilidadeVO();
            FiltroVeiculoDisponibilidadeVOValidator validador = new FiltroVeiculoDisponibilidadeVOValidator();
            filtroPadrao.DataRetirada = new DateOnly(2026, 12, 10);
            filtroPadrao.DataDevolucao = new DateOnly(2026, 12, 12);
            FiltroVeiculoDisponibilidadeVO filtroMaximo = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = filtroPadrao.DataRetirada,
                DataDevolucao = filtroPadrao.DataDevolucao,
                TamanhoPagina = 100
            };
            FiltroVeiculoDisponibilidadeVO filtroExcedido = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = filtroPadrao.DataRetirada,
                DataDevolucao = filtroPadrao.DataDevolucao,
                TamanhoPagina = 101
            };

            FluentValidation.Results.ValidationResult maximo = validador.Validate(filtroMaximo);
            FluentValidation.Results.ValidationResult excedido = validador.Validate(filtroExcedido);

            Assert.Equal(1, filtroPadrao.Pagina);
            Assert.Equal(20, filtroPadrao.TamanhoPagina);
            Assert.True(maximo.IsValid);
            Assert.False(excedido.IsValid);
        }

        private static Veiculo CriarVeiculo(Categoria categoria, StatusVeiculo status, decimal diaria)
        {
            return new Veiculo
            {
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
                ValorDiaria = diaria,
                Status = status
            };
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoDisponivelProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, new FakeLogService(), ContextoEmpresaDeTeste.Criar());
        }
    }
}
