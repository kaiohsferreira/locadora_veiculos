using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do servico da troca manual de status (P1-BE-04) com o repositorio em memoria, que aplica a mesma regra pura do
    /// repositorio real. Confere: a gravacao e o log informativo so quando alterou (sem placa, chassi nem renavam); mesmo
    /// status manual sem gravar, sem log e sem mudar UpdatedAt; 404 e 409 relancados sem log; numero e minusculas nunca viram
    /// status; falha inesperada logada.
    /// </summary>
    public class VeiculoServiceStatusTests
    {
        private const int IdEmpresaDoContexto = 7;
        private const int IdEmpresaAlheia = 9;
        private const int IdFuncionarioDoContexto = 1;
        private const int IdVeiculoInexistente = 999;
        private const string PlacaDoTeste = "ABC1D23";
        private const string ChassiDoTeste = "9BWZZZ377VT004251";
        private const string RenavamDoTeste = "12345678901";

        private static readonly DateTime MomentoDeCriacao = new DateTime(2020, 1, 1, 10, 0, 0);

        private static AlterarStatusVeiculoVO Pedido(string? status)
        {
            return new AlterarStatusVeiculoVO { Status = status };
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository veiculos, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VeiculoProfile>();
                cfg.AddProfile<VeiculoCadastroProfile>();
            }, NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new VeiculoService(veiculos, new FakeCategoriaRepository(), mapper, log, ContextoEmpresaDeTeste.Criar(IdEmpresaDoContexto), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), log));
        }

        private static Veiculo CriarVeiculo(FakeVeiculoRepository veiculos, StatusVeiculo status, int idEmpresa, DateTime? desativadoEm = null)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = 1,
                IdEmpresa = idEmpresa,
                Placa = PlacaDoTeste,
                Chassi = ChassiDoTeste,
                Renavam = RenavamDoTeste,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 0,
                ValorDiaria = 150m,
                Status = status,
                CreatedAt = MomentoDeCriacao,
                UpdatedAt = MomentoDeCriacao,
                DisabledAt = desativadoEm
            };
            veiculos.Adicionar(veiculo);

            return veiculo;
        }

        [Fact]
        public async Task AlterouDeDisponivelParaManutencao_GravaOStatusELogaUmaVezSemDadosDoVeiculo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaDoContexto);
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            await servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("MANUTENCAO"));

            Veiculo gravado = Assert.Single(veiculos.Veiculos);
            Assert.Equal(StatusVeiculo.MANUTENCAO, gravado.Status);
            Assert.NotEqual(MomentoDeCriacao, gravado.UpdatedAt);
            List<string> entradas = log.Snapshot();
            string esperado = string.Format(ConstantsMessage.InfoVeiculoStatusAlterado, veiculo.IdVeiculo, StatusVeiculo.DISPONIVEL, StatusVeiculo.MANUTENCAO, IdFuncionarioDoContexto);
            Assert.Equal(new List<string> { esperado }, entradas);
            Assert.DoesNotContain(PlacaDoTeste, entradas[0]);
            Assert.DoesNotContain(ChassiDoTeste, entradas[0]);
            Assert.DoesNotContain(RenavamDoTeste, entradas[0]);
        }

        [Fact]
        public async Task MesmoStatusManual_NaoGravaNaoLogaENaoMudaUpdatedAt()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.MANUTENCAO, IdEmpresaDoContexto);
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            await servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("MANUTENCAO"));

            Veiculo depois = Assert.Single(veiculos.Veiculos);
            Assert.Equal(StatusVeiculo.MANUTENCAO, depois.Status);
            Assert.Equal(MomentoDeCriacao, depois.UpdatedAt);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task VeiculoInexistente_RelancaNotFoundSemLog()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.AlterarStatusAsync(IdVeiculoInexistente, Pedido("MANUTENCAO")));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task VeiculoDesativado_RelancaNotFoundSemLogENaoMudaOStatus()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaDoContexto, MomentoDeCriacao);
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("MANUTENCAO")));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
            Assert.Empty(log.Snapshot());
            Assert.Equal(StatusVeiculo.DISPONIVEL, Assert.Single(veiculos.Veiculos).Status);
        }

        [Fact]
        public async Task VeiculoDeOutraLocadora_RelancaNotFoundSemLogENaoTocaNoVeiculo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaAlheia);
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("MANUTENCAO")));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
            Assert.Empty(log.Snapshot());
            Assert.Equal(StatusVeiculo.DISPONIVEL, Assert.Single(veiculos.Veiculos).Status);
        }

        [Theory]
        [InlineData(StatusVeiculo.DISPONIVEL, StatusVeiculo.RESERVADO, false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, StatusVeiculo.LOCADO, false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, StatusVeiculo.VENDIDO, false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo)]
        [InlineData(StatusVeiculo.DISPONIVEL, StatusVeiculo.MANUTENCAO, false, true, ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto)]
        [InlineData(StatusVeiculo.DISPONIVEL, StatusVeiculo.MANUTENCAO, true, false, ConstantsMessage.ErroVeiculoStatusComReservaEmAberto)]
        [InlineData(StatusVeiculo.BAIXADO, StatusVeiculo.DISPONIVEL, false, false, ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado)]
        [InlineData(StatusVeiculo.VENDIDO, StatusVeiculo.MANUTENCAO, false, false, ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado)]
        [InlineData(StatusVeiculo.LOCADO, StatusVeiculo.DISPONIVEL, false, false, ConstantsMessage.ErroVeiculoStatusOrigemPorFluxo)]
        [InlineData(StatusVeiculo.RESERVADO, StatusVeiculo.MANUTENCAO, false, false, ConstantsMessage.ErroVeiculoStatusOrigemPorFluxo)]
        public async Task Recusa409_RelancaBusinessExceptionSemLogENaoMudaOStatus(StatusVeiculo origem, StatusVeiculo destino, bool reserva, bool locacao, string mensagem)
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, origem, IdEmpresaDoContexto);

            if (reserva)
            {
                veiculos.ReservasDoVeiculo.Add(new Reserva { IdVeiculo = veiculo.IdVeiculo, Status = StatusReserva.ATIVA });
            }

            if (locacao)
            {
                veiculos.LocacoesDoVeiculo.Add(new Locacao { IdVeiculo = veiculo.IdVeiculo, Status = StatusLocacao.EM_ANDAMENTO });
            }

            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido(destino.ToString())));

            Assert.Equal(mensagem, excecao.Message);
            Assert.Empty(log.Snapshot());
            Assert.Equal(origem, Assert.Single(veiculos.Veiculos).Status);
        }

        [Fact]
        public async Task TextoComEspacosNasPontas_EAceitoEGrava()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaDoContexto);
            VeiculoService servico = CriarServico(veiculos, new FakeLogService());

            await servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("  BAIXADO  "));

            Assert.Equal(StatusVeiculo.BAIXADO, Assert.Single(veiculos.Veiculos).Status);
        }

        [Theory]
        [InlineData("3")]
        [InlineData("manutencao")]
        [InlineData("XYZ")]
        public async Task NumeroOuTextoFora_NuncaViraStatus_LogaErroESobe(string texto)
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaDoContexto);
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido(texto)));

            Assert.Equal(StatusVeiculo.DISPONIVEL, Assert.Single(veiculos.Veiculos).Status);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroAlterarStatusVeiculo, entradas[0]);
        }

        [Fact]
        public async Task FalhaInesperada_LogaOErroEhRelancada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = CriarVeiculo(veiculos, StatusVeiculo.DISPONIVEL, IdEmpresaDoContexto);
            veiculos.ExcecaoAoAlterarStatus = new InvalidOperationException("falha tecnica de teste");
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.AlterarStatusAsync(veiculo.IdVeiculo, Pedido("MANUTENCAO")));

            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroAlterarStatusVeiculo, entradas[0]);
        }
    }
}
