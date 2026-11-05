using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
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
    /// Testes do CategoriaController chamado direto (sem servidor HTTP): cada action
    /// devolve o resultado certo (200, 201 com Location, 204) ou propaga a excecao de
    /// negocio para o ErrorHandlingMiddleware. Usa CategoriaService real, o repositorio
    /// em memoria (FakeCategoriaRepository), o mapper real e o log em memoria.
    /// </summary>
    public class CategoriaControllerTests
    {
        [Fact]
        public async Task GetAll_DevolveOkComPaginaSoDasAtivas()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "Sedan", ValorDiariaBase = 110m });
            repositorio.Adicionar(new Categoria { Nome = "Economico", ValorDiariaBase = 80m });
            Categoria desativada = new Categoria { Nome = "Antiga", ValorDiariaBase = 50m };
            desativada.Disable();
            repositorio.Adicionar(desativada);
            CategoriaController controller = CriarController(repositorio);

            IActionResult resultado = await controller.GetAll(new FiltroPaginacaoVO { Pagina = 1, TamanhoPagina = 20 });

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            PaginaVO<ReturnCategoriaVO> pagina = Assert.IsType<PaginaVO<ReturnCategoriaVO>>(ok.Value);
            List<string> nomes = pagina.Itens.Select(x => x.Nome).ToList();
            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<string> { "Economico", "Sedan" }, nomes);
        }

        [Fact]
        public async Task GetById_CategoriaExistente_DevolveOkComACategoria()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria suv = new Categoria { Nome = "SUV", Descricao = "Utilitarios", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaController controller = CriarController(repositorio);

            IActionResult resultado = await controller.GetById(suv.IdCategoria);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnCategoriaVO categoria = Assert.IsType<ReturnCategoriaVO>(ok.Value);
            Assert.Equal(suv.IdCategoria, categoria.IdCategoria);
            Assert.Equal("SUV", categoria.Nome);
            Assert.Equal(StaticMethods.FormateToMonetary(200m), categoria.ValorDiariaBaseStr);
        }

        [Fact]
        public async Task GetById_IdInexistente_PropagaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaController controller = CriarController(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(99));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Insert_Valido_DevolveCreatedAtApontandoParaGetById()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaController controller = CriarController(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "Economico", Descricao = "Carros compactos", ValorDiariaBase = 150m };

            IActionResult resultado = await controller.Insert(vo);

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(CategoriaController.GetById), criado.ActionName);
            ReturnCategoriaVO valor = Assert.IsType<ReturnCategoriaVO>(criado.Value);
            Assert.True(valor.IdCategoria > 0);
            object? idRota = criado.RouteValues?["id"];
            Assert.Equal(valor.IdCategoria, Convert.ToInt32(idRota));
        }

        [Fact]
        public async Task Insert_NomeDeAtivaDiferenteSoEmMaiusculasEEspacos_PropagaNomeDuplicado()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "SUV", ValorDiariaBase = 200m });
            CategoriaController controller = CriarController(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "  suv ", ValorDiariaBase = 180m };

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Insert(vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNomeDuplicado, excecao.Message);
        }

        [Fact]
        public async Task Update_Valido_DevolveNoContentEAlteraACategoria()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria suv = new Categoria { Nome = "SUV", Descricao = "Antiga", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaController controller = CriarController(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV Premium", Descricao = "Nova descricao", ValorDiariaBase = 260.5m };

            IActionResult resultado = await controller.Update(suv.IdCategoria, vo);

            Assert.IsType<NoContentResult>(resultado);
            IActionResult consulta = await controller.GetById(suv.IdCategoria);
            OkObjectResult ok = Assert.IsType<OkObjectResult>(consulta);
            ReturnCategoriaVO atualizada = Assert.IsType<ReturnCategoriaVO>(ok.Value);
            Assert.Equal("SUV Premium", atualizada.Nome);
            Assert.Equal("Nova descricao", atualizada.Descricao);
            Assert.Equal(260.5m, atualizada.ValorDiariaBase);
        }

        [Fact]
        public async Task Update_IdInexistente_PropagaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaController controller = CriarController(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 100m };

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.Update(99, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        private static CategoriaController CriarController(FakeCategoriaRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<CategoriaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService logService = new FakeLogService();
            CategoriaService servico = new CategoriaService(repositorio, mapper, logService);
            CategoriaController controller = new CategoriaController(servico, logService);

            return controller;
        }
    }
}
