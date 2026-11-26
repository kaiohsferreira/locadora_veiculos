using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Reservas
{
    /// <summary>
    /// Capa do veiculo nas reservas do cliente (lista e GET por id, via ReservaService). Prova: a lista responde 200 com os
    /// mesmos itens e total quando o armazenamento falha; so o item com foto recebe erroFotoCapa; uma assinatura por pagina;
    /// veiculo desativado nao recebe capa. Sem banco.
    /// </summary>
    public class ReservaCapaTests
    {
        private const int IdClienteDoTeste = 7;
        private const int IdVeiculoComFoto = 5;
        private const int IdVeiculoSemFoto = 6;
        private const int IdReservaComFoto = 51;
        private const int IdReservaSemFoto = 52;
        private const int IdEmpresaDaLocadora = 4242;
        private static readonly DateTime DataDeDesativacaoDoTeste = new DateTime(2026, 11, 16, 9, 0, 0);

        private static Reserva CriarReserva(int idReserva, Veiculo veiculo)
        {
            Reserva reserva = new Reserva
            {
                IdReserva = idReserva,
                IdCliente = IdClienteDoTeste,
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                DataReserva = new DateTime(2026, 11, 16, 9, 0, 0),
                DataRetiradaPrevista = new DateTime(2026, 12, 10),
                DataDevolucaoPrevista = new DateTime(2026, 12, 15),
                ValorEstimado = 825.50m,
                Status = StatusReserva.CONFIRMADA
            };

            return reserva;
        }

        private static Veiculo CriarVeiculo(int idVeiculo, bool comFoto)
        {
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idVeiculo,
                IdEmpresa = IdEmpresaDaLocadora,
                Categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m },
                Empresa = new Empresa { NomeFantasia = "Locadora Teste", Cidade = "Sao Paulo", Uf = "SP", Ativo = true },
                Placa = "PLC" + idVeiculo,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoModelo = 2023,
                ValorDiaria = 150m,
                Status = StatusVeiculo.RESERVADO
            };

            if (comFoto)
            {
                VeiculoFoto foto = new VeiculoFoto
                {
                    IdVeiculoFoto = 900 + idVeiculo,
                    IdVeiculo = idVeiculo,
                    Ordem = 1,
                    ChaveObjeto = "veiculos/" + idVeiculo + "/fotos/capa.jpg",
                    IdFuncionarioEnvio = 1
                };
                veiculo.Fotos = new List<VeiculoFoto> { foto };
            }

            return veiculo;
        }

        /// <summary>Servico com o cliente do contexto e o vinculo aprovado na locadora do veiculo (o POST exige cadastro aprovado).</summary>
        private static ReservaService CriarServicoComCadastroAprovado(FakeReservaRepository repositorio, FakeArmazenamentoGateway gateway, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            contexto.DefinirCliente(IdClienteDoTeste);
            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresaDaLocadora, NomeFantasia = "Locadora Teste", Cidade = "Sao Paulo", Uf = "SP", Ativo = true });
            armazenamento.Semear(IdClienteDoTeste, IdEmpresaDaLocadora, StatusCadastroCliente.APROVADO);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, armazenamento);
            FakeRelogio relogio = new FakeRelogio(new DateTime(2026, 11, 16, 10, 0, 0));
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());

            return new ReservaService(repositorio, clienteLocadora, relogio, mapper, log, new FotoCapaVeiculoService(gateway, log), new FakeNotificacaoTransacionalService());
        }

        private static FakeReservaRepository CriarRepositorioComDuasReservas()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            repositorio.Adicionar(CriarReserva(IdReservaComFoto, CriarVeiculo(IdVeiculoComFoto, true)));
            repositorio.Adicionar(CriarReserva(IdReservaSemFoto, CriarVeiculo(IdVeiculoSemFoto, false)));

            return repositorio;
        }

        private static ReservaService CriarServico(FakeReservaRepository repositorio, FakeArmazenamentoGateway gateway, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, new ArmazenamentoClienteLocadora());
            FakeRelogio relogio = new FakeRelogio(new DateTime(2026, 11, 16, 10, 0, 0));
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());

            return new ReservaService(repositorio, clienteLocadora, relogio, mapper, log, new FotoCapaVeiculoService(gateway, log), new FakeNotificacaoTransacionalService());
        }

        [Fact]
        public async Task Lista_SoAReservaComFotoRecebeCapa_ComUmaAssinatura()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(CriarRepositorioComDuasReservas(), gateway, log);

            PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(IdClienteDoTeste, new FiltroReservaVO { Pagina = 1, TamanhoPagina = 20 });

            ReturnReservaVO comFoto = pagina.Itens.Single(item => item.IdReserva == IdReservaComFoto);
            ReturnReservaVO semFoto = pagina.Itens.Single(item => item.IdReserva == IdReservaSemFoto);
            Assert.NotNull(comFoto.FotoCapa);
            Assert.Null(comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Equal(0, gateway.HeadCount);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Lista_FalhaDoArmazenamento_Responde200ComMesmosItens_ErroSoNoItemComFoto()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(CriarRepositorioComDuasReservas(), gateway, log);

            PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(IdClienteDoTeste, new FiltroReservaVO { Pagina = 1, TamanhoPagina = 20 });

            ReturnReservaVO comFoto = pagina.Itens.Single(item => item.IdReserva == IdReservaComFoto);
            ReturnReservaVO semFoto = pagina.Itens.Single(item => item.IdReserva == IdReservaSemFoto);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.Null(comFoto.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Single(log.Entries);
        }

        [Fact]
        public async Task GetPorId_ComFoto_RecebeCapaSemErro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            ReservaService servico = CriarServico(CriarRepositorioComDuasReservas(), gateway, new FakeLogService());

            ReturnReservaVO retorno = await servico.GetByIdAsync(IdReservaComFoto, IdClienteDoTeste);

            Assert.NotNull(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Equal(0, gateway.HeadCount);
        }

        [Fact]
        public async Task GetPorId_FalhaDoArmazenamento_Responde200ComErroNoCampo()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            ReservaService servico = CriarServico(CriarRepositorioComDuasReservas(), gateway, new FakeLogService());

            ReturnReservaVO retorno = await servico.GetByIdAsync(IdReservaComFoto, IdClienteDoTeste);

            Assert.Equal(IdReservaComFoto, retorno.IdReserva);
            Assert.Null(retorno.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, retorno.ErroFotoCapa);
        }

        [Fact]
        public async Task GetPorId_ReservaDeVeiculoVendido_TrazACapaSemErro()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Veiculo vendido = CriarVeiculo(IdVeiculoComFoto, true);
            vendido.Status = StatusVeiculo.VENDIDO;
            repositorio.Adicionar(CriarReserva(IdReservaComFoto, vendido));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            ReservaService servico = CriarServico(repositorio, gateway, new FakeLogService());

            ReturnReservaVO retorno = await servico.GetByIdAsync(IdReservaComFoto, IdClienteDoTeste);

            Assert.NotNull(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
        }

        /// <summary>Criar e cancelar a reserva nao leem a capa: o armazenamento fica sem nenhuma chamada (assinatura e existencia).</summary>
        [Fact]
        public async Task CriarECancelarReserva_NaoChamamOArmazenamento()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Veiculo veiculo = CriarVeiculo(IdVeiculoComFoto, true);
            veiculo.Status = StatusVeiculo.DISPONIVEL;
            repositorio.VincularVeiculo(veiculo);
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            ReservaService servico = CriarServicoComCadastroAprovado(repositorio, gateway, new FakeLogService());
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = IdVeiculoComFoto,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            ReturnReservaCriadaVO criada = await servico.CriarAsync(IdClienteDoTeste, entrada);
            await servico.CancelarAsync(criada.IdReserva, IdClienteDoTeste);

            Assert.Equal(0, gateway.AssinaturaLoteCount);
            Assert.Equal(0, gateway.HeadCount);
        }

        [Fact]
        public async Task GetPorId_SemFotoCadastrada_NaoChamaOArmazenamento()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            ReservaService servico = CriarServico(CriarRepositorioComDuasReservas(), gateway, new FakeLogService());

            ReturnReservaVO retorno = await servico.GetByIdAsync(IdReservaSemFoto, IdClienteDoTeste);

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }

        [Fact]
        public async Task ReservaDeVeiculoDesativado_NaoRecebeCapa_NemChamaOArmazenamento()
        {
            FakeReservaRepository repositorio = new FakeReservaRepository();
            Veiculo desativado = CriarVeiculo(IdVeiculoComFoto, true);
            desativado.DisabledAt = DataDeDesativacaoDoTeste;
            repositorio.Adicionar(CriarReserva(IdReservaComFoto, desativado));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            ReservaService servico = CriarServico(repositorio, gateway, new FakeLogService());

            ReturnReservaVO retorno = await servico.GetByIdAsync(IdReservaComFoto, IdClienteDoTeste);

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }
    }
}
