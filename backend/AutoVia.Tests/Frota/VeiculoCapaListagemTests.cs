using AutoMapper;
using AutoVia.Communication.Enums;
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
    /// <summary>
    /// Capa do veiculo na lista de frota (GET /api/veiculos, via VeiculoService) e no GET por id, que nao recebe capa.
    /// Prova: a lista responde 200 com os mesmos itens e total mesmo quando o armazenamento falha; so o item com foto
    /// cadastrada recebe erroFotoCapa; uma assinatura por pagina; GET por id sai nulo sem chamar o armazenamento.
    /// </summary>
    public class VeiculoCapaListagemTests
    {
        private const int IdEmpresaDoTeste = 4242;
        private const int IdVeiculoComFoto = 1;
        private const int IdVeiculoSemFoto = 2;

        private static VeiculoFoto CriarFotoAtiva(int idVeiculo)
        {
            VeiculoFoto foto = new VeiculoFoto
            {
                IdVeiculoFoto = 100 + idVeiculo,
                IdVeiculo = idVeiculo,
                Ordem = 1,
                ChaveObjeto = "veiculos/" + idVeiculo + "/fotos/capa.jpg",
                IdFuncionarioEnvio = 1
            };

            return foto;
        }

        private static Veiculo CriarVeiculo(int idVeiculo, Categoria categoria, bool comFoto)
        {
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idVeiculo,
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                IdEmpresa = IdEmpresaDoTeste,
                Placa = "PLC" + idVeiculo,
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m,
                Status = StatusVeiculo.DISPONIVEL
            };

            if (comFoto)
            {
                veiculo.Fotos = new List<VeiculoFoto> { CriarFotoAtiva(idVeiculo) };
            }

            return veiculo;
        }

        private static FakeVeiculoRepository CriarRepositorioComDoisVeiculos()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(IdVeiculoComFoto, suv, true));
            repositorio.Adicionar(CriarVeiculo(IdVeiculoSemFoto, suv, false));

            return repositorio;
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository repositorio, FakeArmazenamentoGateway gateway, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(gateway, log));
        }

        [Fact]
        public async Task Lista_SoOItemComFotoRecebeCapa_ComUmaAssinaturaNaPagina()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 20 });

            ReturnVeiculoVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.NotNull(comFoto.FotoCapa);
            Assert.Null(comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task FalhaDoArmazenamento_Responde200ComOsMesmosItensETotal_ComErroSoNoItemComFoto()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 20 });

            ReturnVeiculoVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.Null(comFoto.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Single(log.Entries);
        }

        [Fact]
        public async Task PaginaSemFotoCadastrada_NaoChamaOArmazenamento()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria suv = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(IdVeiculoSemFoto, suv, false));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            VeiculoService servico = CriarServico(repositorio, gateway, new FakeLogService());

            PaginaVO<ReturnVeiculoVO> pagina = await servico.GetAllAsync(new FiltroVeiculoVO { Pagina = 1, TamanhoPagina = 20 });

            Assert.Single(pagina.Itens);
            Assert.Null(pagina.Itens[0].FotoCapa);
            Assert.Null(pagina.Itens[0].ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }

        [Fact]
        public async Task GetPorId_NaoPreencheACapa_MesmoComFotoCadastrada_E_NaoChamaOArmazenamento()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            VeiculoService servico = CriarServico(CriarRepositorioComDoisVeiculos(), gateway, new FakeLogService());

            ReturnVeiculoVO retorno = await servico.GetByIdAsync(IdVeiculoComFoto);

            Assert.Equal(IdVeiculoComFoto, retorno.IdVeiculo);
            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }
    }
}
