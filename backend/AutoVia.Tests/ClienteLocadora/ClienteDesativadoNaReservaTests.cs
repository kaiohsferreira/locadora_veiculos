using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Ramo 404 do teto (item 6): cliente desativado sem vinculo na locadora do veiculo. A reserva para antes de gravar qualquer
    /// pedido. O ReservaService troca o 404 do cadastro pelo de veiculo nao encontrado (mapeamento existente); esta prova fixa esse
    /// comportamento atual, e a escolha da mensagem fica para o Kaio.
    /// </summary>
    public class ClienteDesativadoNaReservaTests
    {
        private const int IdCliente = 31;
        private const int IdEmpresa = 1;
        private const int IdVeiculo = 8;
        private const string NomeLocadora = "Locadora Um";
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 16, 10, 0, 0);
        private static readonly DateOnly RetiradaPadrao = new DateOnly(2026, 12, 10);
        private static readonly DateOnly DevolucaoPadrao = new DateOnly(2026, 12, 14);

        [Fact]
        public async Task ClienteDesativadoSemVinculo_NaReserva_Devolve404SemPedidoNemInsercao()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Ativo = true });
            armazenamento.ClientesDesativados.Add(IdCliente);
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);
            CriarReservaVO entrada = new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = RetiradaPadrao, DataDevolucao = DevolucaoPadrao };

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdCliente, entrada));

            Assert.Equal(ConstantsMessage.ErroReservaVeiculoNaoEncontrado, erro.Message);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(armazenamento.Vinculos);
        }

        private static Veiculo CriarVeiculo()
        {
            return new Veiculo
            {
                IdVeiculo = IdVeiculo,
                IdEmpresa = IdEmpresa,
                Status = StatusVeiculo.DISPONIVEL,
                ValorDiaria = 150.50m,
                Placa = "ABC1D23",
                Marca = "Jeep",
                Modelo = "Renegade",
                AnoModelo = 2024,
                Categoria = new Categoria { Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Uf = "SP", Ativo = true }
            };
        }

        private static ReservaService CriarServico(FakeReservaRepository reservas, FakeLogService log, ArmazenamentoClienteLocadora armazenamento)
        {
            IMapper mapper = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance).CreateMapper();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            contexto.DefinirCliente(IdCliente);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, armazenamento);
            FakeRelogio relogio = new FakeRelogio(AgoraFixo);
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());
            return new ReservaService(reservas, clienteLocadora, relogio, mapper, log, new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), log));
        }
    }
}
