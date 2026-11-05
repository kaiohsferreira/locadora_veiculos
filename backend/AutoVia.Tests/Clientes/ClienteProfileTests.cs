using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Reflection;
using System.Text.Json;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Testes do ClienteProfile: formatacao de CPF, datas e data de cadastro no retorno,
    /// data de cadastro sem fuso no JSON, entrada sem campos do servidor (id, status, hash,
    /// auditoria e navegacoes) e a garantia por reflexao de que o retorno nao expoe nenhuma senha.
    /// </summary>
    public class ClienteProfileTests
    {
        private const string PadraoIsoSemFuso = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$";

        [Fact]
        public void DataDeCadastroSaiSemFusoNoJson()
        {
            IMapper mapper = CriarMapper();
            Cliente cliente = CriarCliente();
            cliente.DataCadastro = DateTime.Now;

            ReturnClienteVO retorno = mapper.Map<ReturnClienteVO>(cliente);

            Assert.Equal(DateTimeKind.Unspecified, retorno.DataCadastro.Kind);
            Assert.Equal(cliente.DataCadastro.Ticks, retorno.DataCadastro.Ticks);
            JsonSerializerOptions opcoes = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            using JsonDocument documento = JsonDocument.Parse(JsonSerializer.Serialize(retorno, opcoes));
            string dataJson = documento.RootElement.GetProperty("dataCadastro").GetString()!;
            Assert.Matches(PadraoIsoSemFuso, dataJson);
        }

        [Fact]
        public void MapeiaClienteParaRetornoComCpfEDatasFormatados()
        {
            IMapper mapper = CriarMapper();
            Cliente cliente = CriarCliente();

            ReturnClienteVO retorno = mapper.Map<ReturnClienteVO>(cliente);

            Assert.Equal(cliente.IdCliente, retorno.IdCliente);
            Assert.Equal("12345678909", retorno.Cpf);
            Assert.Equal("123.456.789-09", retorno.CpfStr);
            Assert.Equal("20/05/1990", retorno.DataNascimentoStr);
            Assert.Equal("01/01/2030", retorno.CnhValidadeStr);
            Assert.Equal("05/11/2026 14:30", retorno.DataCadastroStr);
            Assert.Equal(StatusCadastroCliente.PENDENTE, retorno.StatusCadastro);
            Assert.Equal(CategoriaCnh.B, retorno.CnhCategoria);
        }

        [Fact]
        public void RetornoNaoTemNenhumaPropriedadeDeSenha()
        {
            List<string> propriedadesDeSenha = typeof(ReturnClienteVO)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .Where(nome => nome.StartsWith("Senha", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Empty(propriedadesDeSenha);
        }

        [Fact]
        public void VODeEntradaViraClienteSemCamposDoServidor()
        {
            IMapper mapper = CriarMapper();
            ClienteVO vo = new ClienteVO
            {
                Nome = "Joao Pereira",
                Cpf = "98765432100",
                DataNascimento = new DateOnly(1985, 3, 10),
                Email = "joao@exemplo.com",
                Telefone = "11987654321",
                CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.AB,
                CnhValidade = new DateOnly(2032, 6, 30),
                Uf = "RJ",
                Cep = "20000000",
                Senha = "senha1234"
            };

            Cliente cliente = mapper.Map<Cliente>(vo);

            Assert.Equal("Joao Pereira", cliente.Nome);
            Assert.Equal("98765432100", cliente.Cpf);
            Assert.Equal(CategoriaCnh.AB, cliente.CnhCategoria);
            Assert.Equal(0, cliente.IdCliente);
            Assert.Equal(default(DateTime), cliente.DataCadastro);
            Assert.Null(cliente.SenhaHash);
            Assert.Null(cliente.DisabledAt);
            Assert.Equal(default(DateTime), cliente.CreatedAt);
            Assert.Equal(default(DateTime), cliente.UpdatedAt);
            Assert.Empty(cliente.Reservas);
            Assert.Empty(cliente.Locacoes);
            Assert.Empty(cliente.Vendas);
        }

        [Fact]
        public void ConfiguracaoDosDoisMapeamentosEhValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteProfile>(), NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }

        private static Cliente CriarCliente()
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 7,
                Nome = "Maria da Silva",
                Cpf = "12345678909",
                DataNascimento = new DateOnly(1990, 5, 20),
                Email = "maria@exemplo.com",
                Telefone = "11987654321",
                CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = new DateOnly(2030, 1, 1),
                DataCadastro = new DateTime(2026, 11, 5, 14, 30, 0),
                StatusCadastro = StatusCadastroCliente.PENDENTE,
                SenhaHash = "HASH:interno-que-nao-deve-sair"
            };

            return cliente;
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }
    }
}
