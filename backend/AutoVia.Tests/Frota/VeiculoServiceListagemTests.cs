using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes da listagem de veiculos (VeiculoService.GetAllAsync): so ativos, filtros combinados
    /// com AND, placa normalizada (sem caixa e sem hifen), paginacao com total estavel e ordem
    /// por placa com desempate por id. Usa o repositorio em memoria, o mapper real e o log em memoria.
    /// </summary>
    public class VeiculoServiceListagemTests
    {
        [Fact]
        public async Task SemFiltro_DevolveSoAtivasComTotalPaginaETamanho()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "CCC3C33", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.LOCADO));
            repositorio.Adicionar(CriarVeiculo(suv, "BBB2B22", StatusVeiculo.MANUTENCAO));
            Veiculo desativado = CriarVeiculo(suv, "ZZZ9Z99", StatusVeiculo.DISPONIVEL);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(3, pagina.Total);
            Assert.Equal(1, pagina.Pagina);
            Assert.Equal(20, pagina.TamanhoPagina);
            Assert.Equal(new List<string> { "AAA1A11", "BBB2B22", "CCC3C33" }, PlacasDe(pagina));
        }

        [Fact]
        public async Task FiltroDeStatus_DevolveSoOStatusInformado()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "BBB2B22", StatusVeiculo.LOCADO));
            repositorio.Adicionar(CriarVeiculo(suv, "CCC3C33", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Status = StatusVeiculo.DISPONIVEL });

            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<string> { "AAA1A11", "CCC3C33" }, PlacasDe(pagina));
        }

        [Fact]
        public async Task FiltroDeCategoria_DevolveSoAQueFoiInformada()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Categoria economico = new Categoria { IdCategoria = 2, Nome = "Economico", ValorDiariaBase = 80m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(economico, "BBB2B22", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { IdCategoria = economico.IdCategoria });

            Assert.Equal(1, pagina.Total);
            Assert.Equal(new List<string> { "BBB2B22" }, PlacasDe(pagina));
            Assert.Equal("Economico", pagina.Itens[0].CategoriaNome);
        }

        [Fact]
        public async Task PlacaParcialComHifenESemCaixa_AcheAPlacaCompleta()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "ABC1D23", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "XYZ9A87", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Placa = "abc-1" });

            Assert.Equal(1, pagina.Total);
            Assert.Equal(new List<string> { "ABC1D23" }, PlacasDe(pagina));
        }

        [Fact]
        public async Task FiltrosCombinados_SaoAND()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Categoria economico = new Categoria { IdCategoria = 2, Nome = "Economico", ValorDiariaBase = 80m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA2B22", StatusVeiculo.LOCADO));
            repositorio.Adicionar(CriarVeiculo(economico, "BBB3C33", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(economico, "AAA4D44", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);
            FiltroVeiculoVO filtro = new FiltroVeiculoVO
            {
                IdCategoria = suv.IdCategoria,
                Status = StatusVeiculo.DISPONIVEL,
                Placa = "aaa"
            };

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(filtro);

            Assert.Equal(1, pagina.Total);
            Assert.Equal(new List<string> { "AAA1A11" }, PlacasDe(pagina));
        }

        [Fact]
        public async Task SegundaPagina_MantemOTotalEDevolveOResto()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A01", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A02", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A03", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A04", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A05", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina1 = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 2 });
            PaginaVO<ReturnVeiculoVO> pagina2 = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(5, pagina1.Total);
            Assert.Equal(new List<string> { "AAA0A01", "AAA0A02" }, PlacasDe(pagina1));
            Assert.Equal(5, pagina2.Total);
            Assert.Equal(2, pagina2.Pagina);
            Assert.Equal(new List<string> { "AAA0A03", "AAA0A04" }, PlacasDe(pagina2));
        }

        [Fact]
        public async Task PaginaAlemDoFim_VeiculosVazioComTotalCerto()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A01", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A02", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "AAA0A03", StatusVeiculo.DISPONIVEL));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 5, TamanhoPagina = 2 });

            Assert.Empty(pagina.Itens);
            Assert.Equal(3, pagina.Total);
            Assert.Equal(5, pagina.Pagina);
        }

        [Fact]
        public async Task Desativados_NuncaAparecemNemNoTotalNemNosFiltros()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL));
            Veiculo desativado = CriarVeiculo(suv, "AAA2B22", StatusVeiculo.DISPONIVEL);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO
            {
                Status = StatusVeiculo.DISPONIVEL,
                Placa = "AAA"
            });

            Assert.Equal(1, pagina.Total);
            Assert.Equal(new List<string> { "AAA1A11" }, PlacasDe(pagina));
        }

        [Fact]
        public async Task OrdemPorPlaca_ComDesempatePorId()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo primeiro = CriarVeiculo(suv, "BBB2B22", StatusVeiculo.DISPONIVEL);
            Veiculo segundo = CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL);
            Veiculo terceiro = CriarVeiculo(suv, "AAA1A11", StatusVeiculo.LOCADO);
            repositorio.Adicionar(primeiro);
            repositorio.Adicionar(segundo);
            repositorio.Adicionar(terceiro);
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO());

            List<int> idsEsperados = new List<int> { segundo.IdVeiculo, terceiro.IdVeiculo, primeiro.IdVeiculo };
            List<int> idsObtidos = pagina.Itens.Select(x => x.IdVeiculo).ToList();
            Assert.Equal(idsEsperados, idsObtidos);
        }

        [Fact]
        public async Task PlacaSoComEspacos_EIgnorada()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(suv, "AAA1A11", StatusVeiculo.DISPONIVEL));
            repositorio.Adicionar(CriarVeiculo(suv, "BBB2B22", StatusVeiculo.LOCADO));
            VeiculoService servico = CriarServico(repositorio);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Placa = "   " });

            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<string> { "AAA1A11", "BBB2B22" }, PlacasDe(pagina));
        }

        private static List<string> PlacasDe(PaginaVO<ReturnVeiculoVO> pagina)
        {
            List<string> placas = pagina.Itens.Select(x => x.Placa).ToList();

            return placas;
        }

        private static Veiculo CriarVeiculo(Categoria categoria, string placa, StatusVeiculo status)
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
                ValorDiaria = 150m,
                Status = status
            };

            return veiculo;
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            VeiculoService servico = new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, new FakeLogService());

            return servico;
        }
    }
}
