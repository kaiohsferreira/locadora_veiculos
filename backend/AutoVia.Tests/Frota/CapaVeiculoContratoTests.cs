using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Reflection;
using System.Text.Json;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Contrato dos campos da capa nos VOs de lista (fotoCapa e erroFotoCapa): interface nos dois VOs, profiles que nao
    /// preenchem, serializacao em camelCase e cabecalho no-store nas acoes que devolvem capa. Sem banco.
    /// </summary>
    public class CapaVeiculoContratoTests
    {
        private const string NomeFotoCapa = "FotoCapa";
        private const string NomeErroFotoCapa = "ErroFotoCapa";
        private const string JsonErroFotoCapa = "\"erroFotoCapa\":";
        private const string JsonFotoCapaNula = "\"fotoCapa\":null";

        private static Veiculo CriarVeiculoComFoto()
        {
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 3,
                IdEmpresa = 4242,
                Categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 200m },
                Empresa = new Empresa { NomeFantasia = "Locadora Teste", Cidade = "Sao Paulo", Uf = "SP", Ativo = true },
                Placa = "PLC0003",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m,
                Status = StatusVeiculo.DISPONIVEL,
                Fotos = new List<VeiculoFoto>
                {
                    new VeiculoFoto { IdVeiculoFoto = 30, IdVeiculo = 3, Ordem = 1, ChaveObjeto = "veiculos/3/fotos/30.jpg", IdFuncionarioEnvio = 1 }
                }
            };

            return veiculo;
        }

        [Fact]
        public void ReturnVeiculoVO_ImplementaIComFotoCapa_ComOsDoisCampos()
        {
            Assert.True(typeof(IComFotoCapa).IsAssignableFrom(typeof(ReturnVeiculoVO)));
            Assert.Equal(typeof(FotoCapaVeiculoVO), typeof(ReturnVeiculoVO).GetProperty(NomeFotoCapa)!.PropertyType);
            Assert.Equal(typeof(string), typeof(ReturnVeiculoVO).GetProperty(NomeErroFotoCapa)!.PropertyType);
        }

        [Fact]
        public void ReturnReservaVO_ImplementaIComFotoCapa_ComOsDoisCampos()
        {
            Assert.True(typeof(IComFotoCapa).IsAssignableFrom(typeof(ReturnReservaVO)));
            Assert.Equal(typeof(FotoCapaVeiculoVO), typeof(ReturnReservaVO).GetProperty(NomeFotoCapa)!.PropertyType);
            Assert.Equal(typeof(string), typeof(ReturnReservaVO).GetProperty(NomeErroFotoCapa)!.PropertyType);
        }

        [Fact]
        public void ReturnReservaCriadaVO_NaoGanhaOsCamposDaCapa()
        {
            Assert.Null(typeof(ReturnReservaCriadaVO).GetProperty(NomeFotoCapa));
            Assert.Null(typeof(ReturnReservaCriadaVO).GetProperty(NomeErroFotoCapa));
        }

        [Fact]
        public void VeiculoProfile_NaoPreencheACapa_MesmoComFotoCadastrada()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            ReturnVeiculoVO retorno = mapper.Map<ReturnVeiculoVO>(CriarVeiculoComFoto());

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
        }

        [Fact]
        public void ReservaProfile_NaoPreencheACapa_MesmoComFotoCadastrada()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            Reserva reserva = new Reserva
            {
                IdReserva = 40,
                IdCliente = 7,
                IdVeiculo = 3,
                Veiculo = CriarVeiculoComFoto(),
                DataReserva = new DateTime(2026, 11, 16, 9, 0, 0),
                DataRetiradaPrevista = new DateTime(2026, 12, 10),
                DataDevolucaoPrevista = new DateTime(2026, 12, 15),
                ValorEstimado = 300m,
                Status = StatusReserva.ATIVA
            };

            ReturnReservaVO retorno = mapper.Map<ReturnReservaVO>(reserva);

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
        }

        [Fact]
        public void Serializacao_TrazErroFotoCapaEmCamelCaseELeNulosSemOmitir()
        {
            JsonSerializerOptions opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            ReturnVeiculoVO comErro = new ReturnVeiculoVO { IdVeiculo = 3, ErroFotoCapa = ConstantsMessage.ErroFotoCapaIndisponivel };

            string json = JsonSerializer.Serialize(comErro, opcoes);

            Assert.Contains(JsonErroFotoCapa, json);
            Assert.Contains(JsonFotoCapaNula, json);
            Assert.DoesNotContain("ErroFotoCapa", json);
        }

        [Fact]
        public void Acoes_QueDevolvemCapa_TemCabecalhoNoStore()
        {
            AssertNoStore(typeof(VeiculoController).GetMethod("GetAll"));
            AssertNoStore(typeof(ReservaController).GetMethod("GetByIdAsync"));
            AssertNoStore(typeof(ReservaController).GetMethod("GetAllAsync"));
        }

        private static void AssertNoStore(MethodInfo? metodo)
        {
            Assert.NotNull(metodo);
            ResponseCacheAttribute? atributo = metodo!.GetCustomAttribute<ResponseCacheAttribute>();
            Assert.NotNull(atributo);
            Assert.True(atributo!.NoStore);
            Assert.Equal(ResponseCacheLocation.None, atributo.Location);
        }
    }
}
