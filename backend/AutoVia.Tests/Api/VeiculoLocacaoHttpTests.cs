using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    public class VeiculoLocacaoHttpTests : IDisposable
    {
        private const int IdVeiculo = 420;
        private const string Rota = "/api/veiculos/locacao/420";
        private const string ConsultaPeriodo = "?dataRetirada=2026-12-10&dataDevolucao=2026-12-12";
        private const string MediaProblema = "application/problem+json";
        private const string UrlFotos = "https://storage.example/foto/";
        private readonly FabricaLocacao _fabrica = new FabricaLocacao();
        private readonly HttpClient _cliente;

        public VeiculoLocacaoHttpTests()
        {
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        [Fact]
        public async Task AnonimoSemDatas_RecebeSomenteCamposPublicosESemCache()
        {
            Veiculo veiculo = CriarVeiculo();
            _fabrica.Veiculos.Adicionar(veiculo);

            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota);
            JsonElement ficha = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.True(resposta.Headers.CacheControl?.NoStore);
            Assert.Equal(IdVeiculo, ficha.GetProperty("idVeiculo").GetInt32());
            Assert.Equal(2025, ficha.GetProperty("anoFabricacao").GetInt32());
            Assert.Equal(2026, ficha.GetProperty("anoModelo").GetInt32());
            Assert.Equal(veiculo.ValorDiaria, ficha.GetProperty("valorDiaria").GetDecimal());
            Assert.Equal(StaticMethods.FormateToMonetary(veiculo.ValorDiaria), ficha.GetProperty("valorDiariaStr").GetString());
            Assert.Equal(veiculo.Empresa.NomeFantasia, ficha.GetProperty("locadoraNomeFantasia").GetString());
            Assert.Equal(veiculo.Empresa.Cidade, ficha.GetProperty("locadoraCidade").GetString());
            Assert.Equal(veiculo.Empresa.Uf, ficha.GetProperty("locadoraUf").GetString());
            Assert.Equal(JsonValueKind.Null, ficha.GetProperty("disponivelNoPeriodo").ValueKind);
            Assert.Equal(JsonValueKind.Null, ficha.GetProperty("erroFotos").ValueKind);
            Assert.Empty(ficha.GetProperty("fotos").EnumerateArray());
            Assert.Equal(0, _fabrica.Gateway.AssinaturaLoteCount);
            string[] camposEsperados =
            [
                "anoFabricacao", "anoModelo", "categoriaNome", "cor", "disponivelNoPeriodo", "erroFotos", "fotos",
                "idCategoria", "idVeiculo", "locadoraCidade", "locadoraNomeFantasia", "locadoraUf", "marca", "modelo",
                "valorDiaria", "valorDiariaStr"
            ];
            Assert.Equal(camposEsperados, ficha.EnumerateObject().Select(campo => campo.Name).Order(StringComparer.Ordinal));
        }

        [Theory]
        [InlineData("?dataRetirada=2026-12-10")]
        [InlineData("?dataDevolucao=2026-12-12")]
        [InlineData("?dataRetirada=2026-12-12&dataDevolucao=2026-12-12")]
        [InlineData("?dataRetirada=2026-12-13&dataDevolucao=2026-12-12")]
        [InlineData("?dataRetirada=2026-02-30&dataDevolucao=2026-12-12")]
        [InlineData("?dataRetirada=12/10/2026&dataDevolucao=2026-12-12")]
        [InlineData("?dataRetirada=&dataDevolucao=")]
        public async Task DatasInvalidas_Devolvem400ProblemDetails(string consulta)
        {
            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota + consulta);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData(StatusReserva.CONFIRMADA, 9, 11, false, false)]
        [InlineData(StatusReserva.ATIVA, 11, 13, false, false)]
        [InlineData(StatusReserva.ATIVA, 9, 10, false, true)]
        [InlineData(StatusReserva.CONFIRMADA, 12, 13, false, true)]
        [InlineData(StatusReserva.CANCELADA, 9, 13, false, true)]
        [InlineData(StatusReserva.CONCLUIDA, 9, 13, false, true)]
        [InlineData(StatusReserva.CONFIRMADA, 9, 13, true, true)]
        public async Task DatasValidas_PreservamFichaEInformamDisponibilidade(
            StatusReserva status, int inicio, int fim, bool desativada, bool disponivel)
        {
            Veiculo veiculo = CriarVeiculo();
            veiculo.Status = StatusVeiculo.RESERVADO;
            veiculo.Reservas.Add(new Reserva
            {
                Status = status,
                DataRetiradaPrevista = new DateTime(2026, 12, inicio),
                DataDevolucaoPrevista = new DateTime(2026, 12, fim),
                DisabledAt = desativada ? new DateTime(2026, 12, 1) : null
            });
            _fabrica.Veiculos.Adicionar(veiculo);

            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota + ConsultaPeriodo);
            JsonElement ficha = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(disponivel, ficha.GetProperty("disponivelNoPeriodo").GetBoolean());
        }

        [Theory]
        [InlineData(false, false, false, StatusVeiculo.DISPONIVEL)]
        [InlineData(true, true, false, StatusVeiculo.DISPONIVEL)]
        [InlineData(true, false, true, StatusVeiculo.DISPONIVEL)]
        [InlineData(true, false, false, StatusVeiculo.VENDIDO)]
        [InlineData(true, false, false, StatusVeiculo.LOCADO)]
        [InlineData(true, false, false, StatusVeiculo.MANUTENCAO)]
        [InlineData(true, false, false, StatusVeiculo.BAIXADO)]
        public async Task VeiculoForaDaFrotaPublica_Devolve404SemAssinarFotos(
            bool semear, bool veiculoDesativado, bool locadoraInativa, StatusVeiculo status)
        {
            if (semear)
            {
                Veiculo veiculo = CriarVeiculo();
                veiculo.Status = status;
                veiculo.Empresa.Ativo = !locadoraInativa;
                veiculo.Fotos.Add(CriarFoto(1, 1, "privada"));
                if (veiculoDesativado)
                {
                    veiculo.Disable();
                }

                _fabrica.Veiculos.Adicionar(veiculo);
            }

            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota);

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(0, _fabrica.Gateway.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Galeria_OrdenaDesempataELimitaAssinaturaAoVeiculo()
        {
            Veiculo veiculo = CriarVeiculo();
            for (int id = 16; id >= 1; id--)
            {
                veiculo.Fotos.Add(CriarFoto(id, id < 3 ? 1 : id, $"imagem-{id}"));
            }

            VeiculoFoto desativada = CriarFoto(17, 0, "desativada");
            desativada.Disable();
            veiculo.Fotos.Add(desativada);
            VeiculoFoto alheia = CriarFoto(18, 0, "alheia");
            alheia.IdVeiculo = IdVeiculo + 1;
            veiculo.Fotos.Add(alheia);
            _fabrica.Veiculos.Adicionar(veiculo);

            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota);
            JsonElement ficha = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement[] fotos = ficha.GetProperty("fotos").EnumerateArray().ToArray();
            Assert.Equal(ConstantsValues.MaximoFotosPorVeiculo, fotos.Length);
            for (int indice = 0; indice < fotos.Length; indice++)
            {
                JsonElement foto = fotos[indice];
                Assert.Equal($"{UrlFotos}imagem-{indice + 1}", foto.GetProperty("url").GetString());
                Assert.Equal(JsonValueKind.Null, foto.GetProperty("urlMiniatura").ValueKind);
                Assert.True(foto.GetProperty("expiraEm").TryGetDateTimeOffset(out DateTimeOffset _));
                Assert.Equal(new string[] { "expiraEm", "url", "urlMiniatura" },
                    foto.EnumerateObject().Select(campo => campo.Name).Order(StringComparer.Ordinal));
            }

            Assert.Equal(1, _fabrica.Gateway.AssinaturaLoteCount);
            Assert.Equal(ConstantsValues.MaximoFotosPorVeiculo, _fabrica.Gateway.ChavesAssinadas.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FalhaNoArmazenamento_PreservaFichaEFotosDisponiveis(bool falhaTotal)
        {
            Veiculo veiculo = CriarVeiculo();
            VeiculoFoto disponivel = CriarFoto(2, 2, "disponivel");
            disponivel.ChaveMiniatura = "miniatura";
            veiculo.Fotos.Add(disponivel);
            veiculo.Fotos.Add(CriarFoto(1, 1, "ausente"));
            _fabrica.Veiculos.Adicionar(veiculo);
            _fabrica.Gateway.ChavesOmitidasNaAssinatura.Add("ausente");
            _fabrica.Gateway.FalharAssinatura = falhaTotal;

            using HttpResponseMessage resposta = await _cliente.GetAsync(Rota + ConsultaPeriodo);
            JsonElement ficha = await LerJsonAsync(resposta);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(IdVeiculo, ficha.GetProperty("idVeiculo").GetInt32());
            Assert.True(ficha.GetProperty("disponivelNoPeriodo").GetBoolean());
            Assert.Equal(ConstantsMessage.ErroCarregarGaleriaVeiculo, ficha.GetProperty("erroFotos").GetString());
            JsonElement.ArrayEnumerator fotos = ficha.GetProperty("fotos").EnumerateArray();
            if (falhaTotal)
            {
                Assert.Empty(fotos);
            }
            else
            {
                JsonElement foto = Assert.Single(fotos);
                Assert.Equal(UrlFotos + "disponivel", foto.GetProperty("url").GetString());
                Assert.Equal(UrlFotos + "miniatura", foto.GetProperty("urlMiniatura").GetString());
            }

            Assert.Equal(1, _fabrica.Gateway.AssinaturaLoteCount);
        }

        [Fact]
        public async Task LimitePublico_UsaAMesmaPoliticaQueBusca()
        {
            _fabrica.Veiculos.Adicionar(CriarVeiculo());
            using HttpResponseMessage ficha = await _cliente.GetAsync(Rota);
            using HttpResponseMessage busca = await _cliente.GetAsync("/api/veiculos/disponiveis" + ConsultaPeriodo);

            Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, busca.StatusCode);
            Assert.Equal(MediaProblema, busca.Content.Headers.ContentType?.MediaType);
        }

        private Veiculo CriarVeiculo() => new Veiculo
        {
            IdVeiculo = IdVeiculo,
            IdCategoria = _fabrica.CategoriaSuv.IdCategoria,
            Categoria = _fabrica.CategoriaSuv,
            IdEmpresa = 93,
            Empresa = new Empresa { NomeFantasia = "Locadora Horizonte", Cidade = "Curitiba", Uf = "PR", Ativo = true },
            Placa = "XYZ0000",
            Chassi = "CHASSI-INTERNO",
            Renavam = "RENAVAM-INTERNO",
            Marca = "Fiat",
            Modelo = "Fastback",
            AnoFabricacao = 2025,
            AnoModelo = 2026,
            ValorDiaria = 189.90m,
            ValorVenda = 99000m,
            KmAtual = 14000
        };

        private static VeiculoFoto CriarFoto(int id, int ordem, string chave) => new VeiculoFoto
        {
            IdVeiculo = IdVeiculo,
            IdVeiculoFoto = id,
            Ordem = ordem,
            ChaveObjeto = chave
        };

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string conteudo = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(conteudo);
            return documento.RootElement.Clone();
        }

        private sealed class FabricaLocacao : VeiculoWebFactory
        {
            public FakeArmazenamentoGateway Gateway { get; } = new FakeArmazenamentoGateway();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureAppConfiguration((_, configuracao) => configuracao.AddInMemoryCollection(
                    new Dictionary<string, string?> { [ConstantsValues.ChaveLimiteLeituraPublica] = "1" }));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IArmazenamentoGateway>();
                    services.AddSingleton<IArmazenamentoGateway>(Gateway);
                });
            }
        }
    }
}
