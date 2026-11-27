using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes do detalhe publico do seminovo (GET /api/veiculos/a-venda/{id}, P6-FE-02) no VeiculoService: mesmo recorte do
    /// catalogo (so DISPONIVEL, com valor de venda e ativo), VO publico e capa: sem foto fica nula; com falha do armazenamento
    /// vem fotoCapa nula e erroFotoCapa preenchido.
    /// </summary>
    public class VeiculoAVendaServiceTests
    {
        [Fact]
        public async Task VeiculoAVenda_DevolveOVOPublicoComOsDadosDoCartao()
        {
            Cenario cenario = new Cenario();
            Veiculo veiculo = cenario.Semear(89900m, StatusVeiculo.DISPONIVEL, false);

            ReturnVeiculoCatalogoVO retorno = await cenario.Servico.GetAVendaByIdAsync(veiculo.IdVeiculo);

            Assert.Equal(veiculo.IdVeiculo, retorno.IdVeiculo);
            Assert.Equal("Jeep", retorno.Marca);
            Assert.Equal("Renegade", retorno.Modelo);
            Assert.Equal(2023, retorno.AnoModelo);
            Assert.Equal(30000, retorno.KmAtual);
            Assert.Equal(89900m, retorno.ValorVenda);
            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
        }

        [Theory]
        [InlineData(StatusVeiculo.MANUTENCAO)]
        [InlineData(StatusVeiculo.LOCADO)]
        [InlineData(StatusVeiculo.VENDIDO)]
        public async Task ForaDoCatalogo_Devolve404(StatusVeiculo status)
        {
            Cenario cenario = new Cenario();
            Veiculo veiculo = cenario.Semear(89900m, status, false);

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetAVendaByIdAsync(veiculo.IdVeiculo));

            Assert.Equal(ConstantsMessage.ErroPropostaVeiculoNaoDisponivel, erro.Message);
        }

        [Fact]
        public async Task SemValorDeVenda_Devolve404()
        {
            Cenario cenario = new Cenario();
            Veiculo veiculo = cenario.Semear(null, StatusVeiculo.DISPONIVEL, false);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetAVendaByIdAsync(veiculo.IdVeiculo));
        }

        [Fact]
        public async Task ArmazenamentoFalha_CapaNulaComErroFotoCapa()
        {
            Cenario cenario = new Cenario();
            cenario.Gateway.FalharAssinatura = true;
            Veiculo veiculo = cenario.Semear(89900m, StatusVeiculo.DISPONIVEL, true);

            ReturnVeiculoCatalogoVO retorno = await cenario.Servico.GetAVendaByIdAsync(veiculo.IdVeiculo);

            Assert.Null(retorno.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, retorno.ErroFotoCapa);
        }

        private sealed class Cenario
        {
            public Cenario()
            {
                Veiculos = new FakeVeiculoRepository();
                Categorias = new FakeCategoriaRepository();
                Categoria = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
                Categorias.Adicionar(Categoria);
                Gateway = new FakeArmazenamentoGateway();

                IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoCatalogoProfile>(), NullLoggerFactory.Instance).CreateMapper();
                Servico = new VeiculoService(Veiculos, Categorias, mapper, new FakeLogService(), ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(Gateway, new FakeLogService()));
            }

            public FakeVeiculoRepository Veiculos { get; }

            public FakeCategoriaRepository Categorias { get; }

            public Categoria Categoria { get; }

            public FakeArmazenamentoGateway Gateway { get; }

            public VeiculoService Servico { get; }

            public Veiculo Semear(decimal? valorVenda, StatusVeiculo status, bool comFoto)
            {
                Veiculo veiculo = new Veiculo
                {
                    IdCategoria = Categoria.IdCategoria,
                    Categoria = Categoria,
                    Placa = "ABC1D23",
                    Chassi = "9BWZZZ377VT004251",
                    Renavam = "12345678901",
                    Marca = "Jeep",
                    Modelo = "Renegade",
                    AnoFabricacao = 2022,
                    AnoModelo = 2023,
                    KmAtual = 30000,
                    ValorDiaria = 150m,
                    ValorVenda = valorVenda,
                    Status = status
                };

                if (comFoto)
                {
                    veiculo.Fotos = new List<VeiculoFoto>
                    {
                        new VeiculoFoto { IdVeiculoFoto = 50, Ordem = 1, ChaveObjeto = "veiculos/1/fotos/50.jpg", IdFuncionarioEnvio = 1 }
                    };
                }

                Veiculos.Adicionar(veiculo);

                return veiculo;
            }
        }
    }
}
