using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes do PropostaCompraService (P6-BE-02, RF-27) com os repositorios em memoria, o mapper real e o
    /// ClienteLocadoraService real (cadastro aprovado como na reserva). Conferem que o preco vem do banco, que a simulacao
    /// nao grava nada, as recusas 400/404/409 e que a proposta de outro cliente responde 404.
    /// </summary>
    public class PropostaCompraServiceTests
    {
        private const int IdEmpresa = 1;
        private const int IdCliente = 7;
        private const int OutroCliente = 9;
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 21, 10, 0, 0);

        [Fact]
        public async Task Simular_CalculaComOValorDeVendaDoBancoESemGravar()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(10000m, StatusVeiculo.DISPONIVEL);

            ReturnSimulacaoParcelamentoVO simulacao = await cenario.Servico.SimularAsync(veiculo.IdVeiculo, new SimulacaoParcelamentoVO { ValorEntrada = 0m, NumeroParcelas = 3 });

            Assert.Equal(veiculo.IdVeiculo, simulacao.IdVeiculo);
            Assert.Equal(10000m, simulacao.ValorVenda);
            Assert.Equal(10000m, simulacao.ValorFinanciado);
            Assert.Equal(3333.33m, simulacao.ValorParcela);
            Assert.Equal(3333.34m, simulacao.ValorUltimaParcela);
            Assert.Equal(StaticMethods.FormateToMonetary(3333.33m), simulacao.ValorParcelaStr);
            Assert.Empty(cenario.Propostas.Propostas);
        }

        [Theory]
        [InlineData(StatusVeiculo.MANUTENCAO)]
        [InlineData(StatusVeiculo.LOCADO)]
        [InlineData(StatusVeiculo.VENDIDO)]
        public async Task Simular_VeiculoForaDoCatalogo_Devolve404(StatusVeiculo status)
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(50000m, status);

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.SimularAsync(veiculo.IdVeiculo, new SimulacaoParcelamentoVO { ValorEntrada = 0m, NumeroParcelas = 10 }));

            Assert.Equal(ConstantsMessage.ErroPropostaVeiculoNaoDisponivel, erro.Message);
        }

        [Fact]
        public async Task Simular_VeiculoSemValorDeVenda_Devolve404()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(null, StatusVeiculo.DISPONIVEL);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.SimularAsync(veiculo.IdVeiculo, new SimulacaoParcelamentoVO { ValorEntrada = 0m, NumeroParcelas = 10 }));
        }

        [Fact]
        public async Task Simular_EntradaIgualAoValor_Devolve400NoCampoValorEntrada()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(50000m, StatusVeiculo.DISPONIVEL);

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() => cenario.Servico.SimularAsync(veiculo.IdVeiculo, new SimulacaoParcelamentoVO { ValorEntrada = 50000m, NumeroParcelas = 1 }));

            ValidationFailure falha = Assert.Single(erro.Errors);
            Assert.Equal(nameof(PropostaCompraVO.ValorEntrada), falha.PropertyName);
            Assert.Equal(ConstantsMessage.ErroPropostaEntradaMaiorQueValor, falha.ErrorMessage);
        }

        [Fact]
        public async Task Simular_ParcelaAbaixoDeUmCentavo_Devolve400NoCampoNumeroParcelas()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(1000m, StatusVeiculo.DISPONIVEL);

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() => cenario.Servico.SimularAsync(veiculo.IdVeiculo, new SimulacaoParcelamentoVO { ValorEntrada = 999.50m, NumeroParcelas = 60 }));

            ValidationFailure falha = Assert.Single(erro.Errors);
            Assert.Equal(nameof(PropostaCompraVO.NumeroParcelas), falha.PropertyName);
            Assert.Equal(ConstantsMessage.ErroPropostaParcelaMinima, falha.ErrorMessage);
        }

        [Fact]
        public async Task Criar_ComCadastroAprovado_GravaOsValoresCalculadosNoServidor()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.DISPONIVEL);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.APROVADO);

            ReturnPropostaCompraVO retorno = await cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 20000m, NumeroParcelas = 12 });

            PropostaCompra gravada = Assert.Single(cenario.Propostas.Propostas);
            Assert.Equal(IdCliente, gravada.IdCliente);
            Assert.Equal(veiculo.IdVeiculo, gravada.IdVeiculo);
            Assert.Equal(80000m, gravada.ValorVenda);
            Assert.Equal(20000m, gravada.ValorEntrada);
            Assert.Equal(12, gravada.NumeroParcelas);
            Assert.Equal(5000m, gravada.ValorParcela);
            Assert.Equal(5000m, gravada.ValorUltimaParcela);
            Assert.Equal(StatusPropostaCompra.ENVIADA, gravada.Status);

            Assert.Equal(gravada.IdPropostaCompra, retorno.IdPropostaCompra);
            Assert.Equal("Jeep", retorno.Marca);
            Assert.Equal("Renegade", retorno.Modelo);
            Assert.Equal(60000m, retorno.ValorFinanciado);
            Assert.Equal(nameof(StatusPropostaCompra.ENVIADA), retorno.Status);
            Assert.Equal(gravada.CreatedAt, retorno.DataProposta);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(gravada.CreatedAt), retorno.DataPropostaStr);
        }

        [Fact]
        public async Task Criar_ComCadastroEmAnalise_Devolve409ComAMensagemDaPropostaENaoGrava()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.DISPONIVEL);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.PENDENTE);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 20000m, NumeroParcelas = 12 }));

            Assert.Equal(string.Format(ConstantsMessage.ErroPropostaCadastroEmAnalise, "Locadora Um"), erro.Message);
            Assert.DoesNotContain("reserva", erro.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cenario.Propostas.Propostas);
        }

        [Fact]
        public async Task Criar_SemVinculoNaLocadora_CriaPendenteEDevolveAMensagemDaProposta()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.DISPONIVEL);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 20000m, NumeroParcelas = 12 }));

            Assert.Equal(string.Format(ConstantsMessage.ErroPropostaCadastroEmAnalise, "Locadora Um"), erro.Message);
        }

        [Fact]
        public async Task Criar_SemVinculoNaLocadora_Devolve409ENaoGrava()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.DISPONIVEL);

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 20000m, NumeroParcelas = 12 }));

            Assert.Empty(cenario.Propostas.Propostas);
        }

        [Fact]
        public async Task Criar_ComCadastroRecusado_Devolve409ENaoGrava()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.DISPONIVEL);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.RECUSADO);

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 20000m, NumeroParcelas = 12 }));

            Assert.Empty(cenario.Propostas.Propostas);
        }

        [Fact]
        public async Task Criar_VeiculoForaDoCatalogo_Devolve404ENaoGrava()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(80000m, StatusVeiculo.MANUTENCAO);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.APROVADO);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 0m, NumeroParcelas = 12 }));

            Assert.Empty(cenario.Propostas.Propostas);
        }

        [Fact]
        public async Task GetById_DoProprioCliente_DevolveAProposta()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(10000m, StatusVeiculo.DISPONIVEL);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.APROVADO);
            ReturnPropostaCompraVO criada = await cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 0m, NumeroParcelas = 3 });

            ReturnPropostaCompraVO lida = await cenario.Servico.GetByIdAsync(criada.IdPropostaCompra, IdCliente);

            Assert.Equal(3333.33m, lida.ValorParcela);
            Assert.Equal(3333.34m, lida.ValorUltimaParcela);
        }

        [Fact]
        public async Task GetById_DeOutroCliente_Devolve404()
        {
            Cenario cenario = new Cenario(IdCliente);
            Veiculo veiculo = cenario.SemearVeiculo(10000m, StatusVeiculo.DISPONIVEL);
            cenario.Armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.APROVADO);
            ReturnPropostaCompraVO criada = await cenario.Servico.CriarAsync(IdCliente, new PropostaCompraVO { IdVeiculo = veiculo.IdVeiculo, ValorEntrada = 0m, NumeroParcelas = 3 });

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetByIdAsync(criada.IdPropostaCompra, OutroCliente));

            Assert.Equal(ConstantsMessage.ErroPropostaNaoEncontrada, erro.Message);
        }

        private sealed class Cenario
        {
            public Cenario(int idClienteDoContexto)
            {
                FakeLogService log = new FakeLogService();
                IMapper mapper = new MapperConfiguration(opcoes => opcoes.AddProfile<PropostaCompraProfile>(), NullLoggerFactory.Instance).CreateMapper();

                Veiculos = new FakeVeiculoRepository();
                Propostas = new FakePropostaCompraRepository();
                Armazenamento = new ArmazenamentoClienteLocadora();
                Armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = "Locadora Um", Ativo = true });

                ContextoEmpresa contexto = new ContextoEmpresa(log);
                contexto.DefinirCliente(idClienteDoContexto);
                FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, Armazenamento);
                ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, new FakeRelogio(AgoraFixo), mapper, log, new ClientesOptions());

                Servico = new PropostaCompraService(Veiculos, Propostas, clienteLocadora, mapper, log);
            }

            public FakeVeiculoRepository Veiculos { get; }

            public FakePropostaCompraRepository Propostas { get; }

            public ArmazenamentoClienteLocadora Armazenamento { get; }

            public PropostaCompraService Servico { get; }

            public Veiculo SemearVeiculo(decimal? valorVenda, StatusVeiculo status)
            {
                Veiculo veiculo = new Veiculo
                {
                    IdEmpresa = IdEmpresa,
                    IdCategoria = 1,
                    Placa = "ABC1D23",
                    Chassi = "9BWZZZ377VT004251",
                    Renavam = "12345678901",
                    Marca = "Jeep",
                    Modelo = "Renegade",
                    AnoFabricacao = 2022,
                    AnoModelo = 2022,
                    KmAtual = 30000,
                    ValorDiaria = 150m,
                    ValorVenda = valorVenda,
                    Status = status
                };

                Veiculos.Adicionar(veiculo);

                return veiculo;
            }
        }
    }
}
