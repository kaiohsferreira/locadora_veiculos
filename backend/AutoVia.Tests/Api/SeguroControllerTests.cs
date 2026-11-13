using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do SeguroController chamado direto (sem servidor HTTP): GET lista e GET {id}
    /// devolvem 200, POST devolve 201 com Location para o GET, PUT devolve 204, e as excecoes
    /// de negocio (404 e 409) sao propagadas para o ErrorHandlingMiddleware.
    /// </summary>
    public class SeguroControllerTests
    {
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

        [Fact]
        public async Task GetAll_DevolveOkComAPagina()
        {
            FakeSeguroRepository seguros = new FakeSeguroRepository();
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            Veiculo veiculo = SemearVeiculo(veiculos);
            seguros.Adicionar(new Seguro { IdVeiculo = veiculo.IdVeiculo, Veiculo = veiculo, Seguradora = "Porto", NumeroApolice = "AP-1", DataInicio = Hoje, DataFim = Hoje.AddDays(30), ValorPremio = 500m });
            SeguroController controller = CriarController(seguros, veiculos);

            IActionResult resultado = await controller.GetAll(new FiltroSeguroVO());

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            PaginaVO<ReturnSeguroVO> pagina = Assert.IsType<PaginaVO<ReturnSeguroVO>>(ok.Value);
            ReturnSeguroVO item = Assert.Single(pagina.Itens);
            Assert.True(item.VenceEm60Dias);
        }

        [Fact]
        public async Task Insert_Valido_DevolveCreatedAtApontandoParaGetById()
        {
            FakeSeguroRepository seguros = new FakeSeguroRepository();
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            Veiculo veiculo = SemearVeiculo(veiculos);
            SeguroController controller = CriarController(seguros, veiculos);

            IActionResult resultado = await controller.Insert(CriarVO(veiculo.IdVeiculo, "AP-2"));

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(SeguroController.GetById), criado.ActionName);
            ReturnSeguroVO valor = Assert.IsType<ReturnSeguroVO>(criado.Value);
            Assert.Equal(valor.IdSeguro, Convert.ToInt32(criado.RouteValues?["id"]));
        }

        [Fact]
        public async Task Insert_NumeroDuplicado_PropagaBusinessException()
        {
            FakeSeguroRepository seguros = new FakeSeguroRepository();
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            Veiculo veiculo = SemearVeiculo(veiculos);
            SeguroController controller = CriarController(seguros, veiculos);
            await controller.Insert(CriarVO(veiculo.IdVeiculo, "AP-3"));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Insert(CriarVO(veiculo.IdVeiculo, "AP-3")));

            Assert.Equal(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado, excecao.Message);
        }

        [Fact]
        public async Task GetById_Inexistente_PropagaNaoEncontrada()
        {
            SeguroController controller = CriarController(new FakeSeguroRepository(), new FakeVeiculoRepository());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(99));

            Assert.Equal(ConstantsMessage.ErroSeguroNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task Update_Valido_DevolveNoContent()
        {
            FakeSeguroRepository seguros = new FakeSeguroRepository();
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            Veiculo veiculo = SemearVeiculo(veiculos);
            SeguroController controller = CriarController(seguros, veiculos);
            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(await controller.Insert(CriarVO(veiculo.IdVeiculo, "AP-4")));
            ReturnSeguroVO seguro = Assert.IsType<ReturnSeguroVO>(criado.Value);

            IActionResult resultado = await controller.Update(seguro.IdSeguro, CriarVO(veiculo.IdVeiculo, "AP-4"));

            Assert.IsType<NoContentResult>(resultado);
        }

        private static SeguroVO CriarVO(int idVeiculo, string numeroApolice)
        {
            SeguroVO vo = new SeguroVO
            {
                IdVeiculo = idVeiculo,
                Seguradora = "Porto Seguro",
                NumeroApolice = numeroApolice,
                DataInicio = Hoje,
                DataFim = Hoje.AddYears(1),
                ValorPremio = 1200m
            };

            return vo;
        }

        private static Veiculo SemearVeiculo(FakeVeiculoRepository veiculos)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = 1,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                ValorDiaria = 150m
            };

            veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static SeguroController CriarController(FakeSeguroRepository seguros, FakeVeiculoRepository veiculos)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<SeguroProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService log = new FakeLogService();
            SeguroService servico = new SeguroService(seguros, veiculos, mapper, log);
            SeguroController controller = new SeguroController(servico, log);

            return controller;
        }
    }
}
