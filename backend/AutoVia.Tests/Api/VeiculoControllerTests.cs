using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
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
    /// Testes do VeiculoController chamado direto (sem servidor HTTP): GET {id} devolve 200,
    /// POST devolve 201 com Location para o GET, PUT devolve 204, e as excecoes de negocio
    /// (404 e 409) sao propagadas para o ErrorHandlingMiddleware. Usa o VeiculoService real,
    /// os repositorios em memoria, o mapper real e o log em memoria.
    /// </summary>
    public class VeiculoControllerTests
    {
        [Fact]
        public async Task GetById_VeiculoExistente_DevolveOkComOVo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoController controller = CriarController(veiculos, categorias);

            IActionResult resultado = await controller.GetById(veiculo.IdVeiculo);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnVeiculoVO valor = Assert.IsType<ReturnVeiculoVO>(ok.Value);
            Assert.Equal(veiculo.IdVeiculo, valor.IdVeiculo);
            Assert.Equal("SUV", valor.CategoriaNome);
        }

        [Fact]
        public async Task GetById_VeiculoInexistente_PropagaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            VeiculoController controller = CriarController(veiculos, categorias);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(99));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task Insert_Valido_DevolveCreatedAtApontandoParaGetById()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            IActionResult resultado = await controller.Insert(vo);

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(VeiculoController.GetById), criado.ActionName);
            ReturnVeiculoVO valor = Assert.IsType<ReturnVeiculoVO>(criado.Value);
            Assert.True(valor.IdVeiculo > 0);
            Assert.Equal(StatusVeiculo.DISPONIVEL, valor.Status);
            object? idRota = criado.RouteValues?["id"];
            Assert.Equal(valor.IdVeiculo, Convert.ToInt32(idRota));
        }

        [Fact]
        public async Task Insert_CategoriaInexistente_PropagaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(99);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.Insert(vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Insert_PlacaDeVeiculoAtivo_PropagaPlacaDuplicada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "1HGCM82633A004352", "98765432100");
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Insert(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task Update_Valido_DevolveNoContent()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Marca = "Ford";

            IActionResult resultado = await controller.Update(veiculo.IdVeiculo, vo);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task Update_VeiculoInexistente_PropagaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.Update(99, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task Update_ChassiDeOutroVeiculoAtivo_PropagaChassiDuplicado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            Veiculo outro = CriarVeiculo(veiculos, suv, "XYZ9A87", "1HGCM82633A004352", "98765432100");
            VeiculoController controller = CriarController(veiculos, categorias);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "KLM2B34";
            vo.Chassi = "9BWZZZ377VT004251";
            vo.Renavam = "11111111111";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Update(outro.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoChassiDuplicado, excecao.Message);
        }

        private static VeiculoController CriarController(FakeVeiculoRepository veiculos, FakeCategoriaRepository categorias)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VeiculoProfile>();
                cfg.AddProfile<VeiculoCadastroProfile>();
            }, NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService log = new FakeLogService();
            VeiculoService servico = new VeiculoService(veiculos, categorias, mapper, log);
            VeiculoController controller = new VeiculoController(servico, log);

            return controller;
        }

        private static Categoria CriarCategoria(FakeCategoriaRepository categorias, string nome)
        {
            Categoria categoria = new Categoria { Nome = nome, ValorDiariaBase = 200m };
            categorias.Adicionar(categoria);

            return categoria;
        }

        private static Veiculo CriarVeiculo(FakeVeiculoRepository veiculos, Categoria categoria, string placa, string chassi, string renavam)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = placa,
                Chassi = chassi,
                Renavam = renavam,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                ValorDiaria = 150m
            };
            veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static VeiculoVO CriarVOValido(int idCategoria)
        {
            VeiculoVO vo = new VeiculoVO
            {
                IdCategoria = idCategoria,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                Cor = "Branco",
                KmAtual = 0,
                ValorDiaria = 150m
            };

            return vo;
        }
    }
}
