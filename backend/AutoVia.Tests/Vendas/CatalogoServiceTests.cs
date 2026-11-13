using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes do catalogo de seminovos no VeiculoService (P6-BE-01, RF-26): so veiculos
    /// ativos, DISPONIVEL e com valor de venda; cada filtro; ordem por preco com desempate
    /// por id (paginacao sem repetir nem pular); VO publico sem placa. Usa o repositorio
    /// em memoria e o mapper real.
    /// </summary>
    public class CatalogoServiceTests
    {
        [Fact]
        public async Task SoAparecemDisponiveisComValorDeVendaEAtivos()
        {
            Cenario cenario = new Cenario();
            cenario.Semear("Jeep", "Renegade", 2022, 30000, 90000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 30000, null, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Pulse", 2022, 30000, 80000m, StatusVeiculo.MANUTENCAO);
            cenario.Semear("Fiat", "Argo", 2022, 30000, 70000m, StatusVeiculo.LOCADO);
            cenario.Semear("Fiat", "Mobi", 2022, 30000, 60000m, StatusVeiculo.VENDIDO);
            cenario.Semear("VW", "Gol", 2022, 30000, 50000m, StatusVeiculo.DISPONIVEL, true);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO());

            Assert.Equal(1, pagina.Total);
            ReturnVeiculoCatalogoVO unico = Assert.Single(pagina.Itens);
            Assert.Equal("Renegade", unico.Modelo);
        }

        [Fact]
        public async Task VeiculoQueEntraEmManutencao_SaiDoCatalogo()
        {
            Cenario cenario = new Cenario();
            Veiculo veiculo = cenario.Semear("Jeep", "Compass", 2023, 10000, 150000m, StatusVeiculo.DISPONIVEL);
            PaginaVO<ReturnVeiculoCatalogoVO> antes = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO());

            cenario.Veiculos.AlterarStatus(veiculo.IdVeiculo, StatusVeiculo.MANUTENCAO);
            PaginaVO<ReturnVeiculoCatalogoVO> depois = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO());

            Assert.Equal(1, antes.Total);
            Assert.Equal(veiculo.IdVeiculo, antes.Itens[0].IdVeiculo);
            Assert.Equal(0, depois.Total);
            Assert.Empty(depois.Itens);
        }

        [Fact]
        public async Task OrdenaPorMenorPrecoEDesempataPorId()
        {
            Cenario cenario = new Cenario();
            Veiculo caro = cenario.Semear("Jeep", "Commander", 2024, 1000, 200000m, StatusVeiculo.DISPONIVEL);
            Veiculo empateA = cenario.Semear("Fiat", "Toro", 2022, 20000, 100000m, StatusVeiculo.DISPONIVEL);
            Veiculo barato = cenario.Semear("Fiat", "Mobi", 2020, 50000, 40000m, StatusVeiculo.DISPONIVEL);
            Veiculo empateB = cenario.Semear("VW", "T-Cross", 2022, 20000, 100000m, StatusVeiculo.DISPONIVEL);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO());

            List<int> ids = pagina.Itens.Select(item => item.IdVeiculo).ToList();
            Assert.Equal(new List<int> { barato.IdVeiculo, empateA.IdVeiculo, empateB.IdVeiculo, caro.IdVeiculo }, ids);
        }

        [Fact]
        public async Task PaginasComPrecosIguais_NaoRepetemNemPulamCarros()
        {
            Cenario cenario = new Cenario();
            List<int> esperados = new List<int>();

            for (int indice = 0; indice < 5; indice++)
            {
                Veiculo veiculo = cenario.Semear("Fiat", "Argo", 2022, 20000, 70000m, StatusVeiculo.DISPONIVEL);
                esperados.Add(veiculo.IdVeiculo);
            }

            List<int> vistos = new List<int>();

            for (int numeroPagina = 1; numeroPagina <= 3; numeroPagina++)
            {
                FiltroCatalogoVO filtro = new FiltroCatalogoVO { Pagina = numeroPagina, TamanhoPagina = 2 };
                PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(filtro);
                vistos.AddRange(pagina.Itens.Select(item => item.IdVeiculo));
                Assert.Equal(5, pagina.Total);
            }

            Assert.Equal(esperados, vistos);
        }

        [Fact]
        public async Task FiltroDeMarca_ParcialSemDiferenciarMaiusculasEIgnorandoEspacos()
        {
            Cenario cenario = new Cenario();
            cenario.Semear("Jeep", "Renegade", 2022, 30000, 90000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 30000, 100000m, StatusVeiculo.DISPONIVEL);
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { Marca = "  jE " };

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(filtro);

            ReturnVeiculoCatalogoVO unico = Assert.Single(pagina.Itens);
            Assert.Equal("Jeep", unico.Marca);
        }

        [Fact]
        public async Task MarcaSoComEspacos_EhTratadaComoSemFiltro()
        {
            Cenario cenario = new Cenario();
            cenario.Semear("Jeep", "Renegade", 2022, 30000, 90000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 30000, 100000m, StatusVeiculo.DISPONIVEL);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO { Marca = "   " });

            Assert.Equal(2, pagina.Total);
        }

        [Fact]
        public async Task FiltrosDePrecoAnoDoModeloEKm_CombinamComAndEIncluemOsLimites()
        {
            Cenario cenario = new Cenario();
            Veiculo dentro = cenario.Semear("Fiat", "Toro", 2022, 40000, 100000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 40000, 100000.01m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2019, 40000, 90000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 40001, 90000m, StatusVeiculo.DISPONIVEL);
            cenario.Semear("Fiat", "Toro", 2022, 40000, 59999.99m, StatusVeiculo.DISPONIVEL);
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { PrecoMinimo = 60000m, PrecoMaximo = 100000m, AnoMinimo = 2020, AnoMaximo = 2022, KmMaximo = 40000 };

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(filtro);

            ReturnVeiculoCatalogoVO unico = Assert.Single(pagina.Itens);
            Assert.Equal(dentro.IdVeiculo, unico.IdVeiculo);
            Assert.Equal(1, pagina.Total);
        }

        [Fact]
        public async Task FiltroDeAnoUsaAnoDoModeloENaoODeFabricacao()
        {
            Cenario cenario = new Cenario();
            cenario.Semear("Fiat", "Toro", 2023, 10000, 120000m, StatusVeiculo.DISPONIVEL, anoFabricacao: 2022);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO { AnoMinimo = 2023 });

            ReturnVeiculoCatalogoVO unico = Assert.Single(pagina.Itens);
            Assert.Equal(2023, unico.AnoModelo);
            Assert.Equal(2022, unico.AnoFabricacao);
        }

        [Fact]
        public async Task VOPublico_TrazCategoriaEPrecoFormatado()
        {
            Cenario cenario = new Cenario();
            cenario.Semear("Jeep", "Renegade", 2022, 30000, 89900m, StatusVeiculo.DISPONIVEL);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await cenario.Servico.GetCatalogoAsync(new FiltroCatalogoVO());

            ReturnVeiculoCatalogoVO item = Assert.Single(pagina.Itens);
            Assert.Equal("SUV", item.CategoriaNome);
            Assert.Equal(89900m, item.ValorVenda);
            Assert.Equal(StaticMethods.FormateToMonetary(89900m), item.ValorVendaStr);
            Assert.Null(typeof(ReturnVeiculoCatalogoVO).GetProperty("Placa"));
            Assert.Null(typeof(ReturnVeiculoCatalogoVO).GetProperty("Chassi"));
            Assert.Null(typeof(ReturnVeiculoCatalogoVO).GetProperty("Renavam"));
        }

        /// <summary>Repositorios em memoria, servico real com o profile do catalogo e uma categoria "SUV".</summary>
        private sealed class Cenario
        {
            public Cenario()
            {
                Veiculos = new FakeVeiculoRepository();
                Categorias = new FakeCategoriaRepository();
                Categoria = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
                Categorias.Adicionar(Categoria);

                MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoCatalogoProfile>(), NullLoggerFactory.Instance);
                IMapper mapper = configuracao.CreateMapper();
                Servico = new VeiculoService(Veiculos, Categorias, mapper, new FakeLogService());
            }

            public FakeVeiculoRepository Veiculos { get; }

            public FakeCategoriaRepository Categorias { get; }

            public Categoria Categoria { get; }

            public VeiculoService Servico { get; }

            /// <summary>Semeia um veiculo; placa, chassi e renavam sao fixos porque o catalogo nao os usa. Ano de fabricacao = ano do modelo, salvo se informado.</summary>
            public Veiculo Semear(string marca, string modelo, short anoModelo, int km, decimal? valorVenda, StatusVeiculo status, bool desativado = false, short? anoFabricacao = null)
            {
                Veiculo veiculo = new Veiculo
                {
                    IdCategoria = Categoria.IdCategoria,
                    Categoria = Categoria,
                    Placa = "ABC1D23",
                    Chassi = "9BWZZZ377VT004251",
                    Renavam = "12345678901",
                    Marca = marca,
                    Modelo = modelo,
                    AnoFabricacao = anoFabricacao ?? anoModelo,
                    AnoModelo = anoModelo,
                    KmAtual = km,
                    ValorDiaria = 150m,
                    ValorVenda = valorVenda,
                    Status = status
                };

                if (desativado)
                {
                    veiculo.Disable();
                }

                Veiculos.Adicionar(veiculo);

                return veiculo;
            }
        }
    }
}
