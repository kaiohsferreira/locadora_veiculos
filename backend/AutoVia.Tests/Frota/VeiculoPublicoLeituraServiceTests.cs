using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Leituras publicas de veiculo entre locadoras (RF-11, RF-26 e RF-72) no VeiculoService, com o repositorio em memoria
    /// e o mapper real: so locadora ativa e nao desativada aparece; cada item traz nome fantasia, cidade e UF da locadora e
    /// kmAtual (no catalogo); sem a navegacao carregada os dados da locadora vem vazios; e falha do repositorio gera log
    /// com a constante do metodo e relanca a mesma excecao (D-9).
    /// </summary>
    public class VeiculoPublicoLeituraServiceTests
    {
        private const string NomeLocadoraAtiva = "Locadora Ativa";
        private const string CidadeLocadoraAtiva = "Sao Paulo";
        private const string UfLocadoraAtiva = "SP";
        private const string NomeLocadoraInativa = "Locadora Inativa";
        private const string NomeLocadoraDesativada = "Locadora Desativada";
        private const string MarcaJeep = "Jeep";
        private const string MensagemFalhaSimulada = "falha simulada";
        private const int KmDoSeminovo = 30000;

        private static Veiculo CriarVeiculo(Categoria categoria, Empresa empresa, StatusVeiculo status, decimal diaria, decimal? valorVenda, string marca)
        {
            return new Veiculo
            {
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                IdEmpresa = empresa.IdEmpresa,
                Empresa = empresa,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = marca,
                Modelo = "Renegade",
                AnoFabricacao = 2022,
                AnoModelo = 2022,
                KmAtual = KmDoSeminovo,
                ValorDiaria = diaria,
                ValorVenda = valorVenda,
                Status = status
            };
        }

        private static Empresa CriarEmpresa(int idEmpresa, string nomeFantasia, bool ativo, bool desativada)
        {
            return new Empresa
            {
                IdEmpresa = idEmpresa,
                NomeFantasia = nomeFantasia,
                Cidade = CidadeLocadoraAtiva,
                Uf = UfLocadoraAtiva,
                Ativo = ativo,
                DisabledAt = desativada ? DateTime.Now : null
            };
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository repositorio, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VeiculoDisponivelProfile>();
                cfg.AddProfile<VeiculoCatalogoProfile>();
            }, NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), log));
        }

        private static FiltroVeiculoDisponibilidadeVO FiltroDisponibilidade()
        {
            return new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 12)
            };
        }

        [Fact]
        public async Task Busca_TraSoVeiculoDeLocadoraAtivaENaoDeInativaNemDesativada()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, null, MarcaJeep));
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(2, NomeLocadoraInativa, false, false), StatusVeiculo.DISPONIVEL, 150m, null, MarcaJeep));
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(3, NomeLocadoraDesativada, true, true), StatusVeiculo.DISPONIVEL, 150m, null, MarcaJeep));
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDisponibilidade());

            Assert.Equal(1, pagina.Total);
            ReturnVeiculoDisponivelVO unico = Assert.Single(pagina.Itens);
            Assert.Equal(NomeLocadoraAtiva, unico.LocadoraNomeFantasia);
        }

        [Fact]
        public async Task Busca_CadaItemTrazNomeCidadeEUfDaLocadoraDona()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, null, MarcaJeep));
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDisponibilidade());

            ReturnVeiculoDisponivelVO item = Assert.Single(pagina.Itens);
            Assert.Equal(NomeLocadoraAtiva, item.LocadoraNomeFantasia);
            Assert.Equal(CidadeLocadoraAtiva, item.LocadoraCidade);
            Assert.Equal(UfLocadoraAtiva, item.LocadoraUf);
        }

        [Fact]
        public async Task Busca_SemANavegacaoDaLocadoraOsTresCamposVemVazios()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo semNavegacao = CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, null, MarcaJeep);
            semNavegacao.Empresa = null!;
            repositorio.Adicionar(semNavegacao);
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDisponibilidade());

            ReturnVeiculoDisponivelVO item = Assert.Single(pagina.Itens);
            Assert.Equal(string.Empty, item.LocadoraNomeFantasia);
            Assert.Equal(string.Empty, item.LocadoraCidade);
            Assert.Equal(string.Empty, item.LocadoraUf);
        }

        [Fact]
        public async Task Catalogo_SoSeminovoDeLocadoraAtivaAparece_ETrazKmEDadosDaLocadora()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, 90000m, MarcaJeep));
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(2, NomeLocadoraInativa, false, false), StatusVeiculo.DISPONIVEL, 150m, 80000m, MarcaJeep));
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(3, NomeLocadoraDesativada, true, true), StatusVeiculo.DISPONIVEL, 150m, 70000m, MarcaJeep));
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(new FiltroCatalogoVO());

            ReturnVeiculoCatalogoVO unico = Assert.Single(pagina.Itens);
            Assert.Equal(1, pagina.Total);
            Assert.Equal(NomeLocadoraAtiva, unico.LocadoraNomeFantasia);
            Assert.Equal(CidadeLocadoraAtiva, unico.LocadoraCidade);
            Assert.Equal(UfLocadoraAtiva, unico.LocadoraUf);
            Assert.Equal(KmDoSeminovo, unico.KmAtual);
        }

        [Fact]
        public async Task Catalogo_MarcaComEspacosEMinusculasAcharAMarcaEmMaiusculas()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, 90000m, MarcaJeep));
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(new FiltroCatalogoVO { Marca = "  jeep  " });

            Assert.Equal(1, pagina.Total);
        }

        [Fact]
        public async Task Catalogo_SemANavegacaoDaLocadoraOsTresCamposVemVazios_EKmMantido()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo semNavegacao = CriarVeiculo(categoria, CriarEmpresa(1, NomeLocadoraAtiva, true, false), StatusVeiculo.DISPONIVEL, 150m, 90000m, MarcaJeep);
            semNavegacao.Empresa = null!;
            repositorio.Adicionar(semNavegacao);
            VeiculoService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(new FiltroCatalogoVO());

            ReturnVeiculoCatalogoVO item = Assert.Single(pagina.Itens);
            Assert.Equal(string.Empty, item.LocadoraNomeFantasia);
            Assert.Equal(string.Empty, item.LocadoraCidade);
            Assert.Equal(string.Empty, item.LocadoraUf);
            Assert.Equal(KmDoSeminovo, item.KmAtual);
        }

        [Fact]
        public async Task FalhaDoRepositorioNaBusca_RegistraLogERelancaAMesmaExcecao()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository
            {
                ExcecaoAoConsultarDisponiveis = new InvalidOperationException(MensagemFalhaSimulada)
            };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(repositorio, log);

            InvalidOperationException relancada = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetDisponiveisAsync(FiltroDisponibilidade()));

            Assert.Same(repositorio.ExcecaoAoConsultarDisponiveis, relancada);
            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroConsultarDisponibilidade, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDoRepositorioNoCatalogo_RegistraLogERelancaAMesmaExcecao()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository
            {
                ExcecaoAoListarCatalogo = new InvalidOperationException(MensagemFalhaSimulada)
            };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(repositorio, log);

            InvalidOperationException relancada = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetCatalogoAsync(new FiltroCatalogoVO()));

            Assert.Same(repositorio.ExcecaoAoListarCatalogo, relancada);
            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroListarCatalogo, StringComparison.Ordinal));
        }
    }
}
