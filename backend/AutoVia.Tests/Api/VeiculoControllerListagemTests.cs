using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do VeiculoController.GetAll chamado direto (sem servidor HTTP): a action devolve 200
    /// com PaginaVO de ReturnVeiculoVO e nunca uma entidade. Usa VeiculoService real, o repositorio
    /// em memoria, o mapper real e o log em memoria.
    /// </summary>
    public class VeiculoControllerListagemTests
    {
        [Fact]
        public async Task GetAll_DevolveOkComPaginaDeVeiculosAtivos()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11"));
            Veiculo desativado = CriarVeiculo(suv, "BBB2B22");
            desativado.Disable();
            repositorio.Adicionar(desativado);
            VeiculoController controller = CriarController(repositorio);

            IActionResult resultado = await controller.GetAll(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 20 });

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            PaginaVO<ReturnVeiculoVO> pagina = Assert.IsType<PaginaVO<ReturnVeiculoVO>>(ok.Value);
            List<string> placas = pagina.Itens.Select(x => x.Placa).ToList();
            Assert.Equal(1, pagina.Total);
            Assert.Equal(new List<string> { "AAA1A11" }, placas);
            Assert.Equal("SUV", pagina.Itens[0].CategoriaNome);
        }

        private static Veiculo CriarVeiculo(Categoria categoria, string placa)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = placa,
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m
            };

            return veiculo;
        }

        private static VeiculoController CriarController(FakeVeiculoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService logService = new FakeLogService();
            VeiculoService servico = new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, logService);
            VeiculoController controller = new VeiculoController(servico, logService);

            return controller;
        }
    }
}
