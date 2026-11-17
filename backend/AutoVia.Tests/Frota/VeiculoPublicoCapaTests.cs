using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Capa do veiculo nas leituras publicas: busca de disponiveis (VeiculoService.GetDisponiveisAsync) e catalogo de seminovos
    /// (VeiculoService.GetCatalogoAsync), com o repositorio em memoria e o mapper real. Prova: com foto, a capa vem com uma
    /// assinatura por pagina; sem foto, nulo nos dois campos e zero chamadas ao armazenamento; falha do armazenamento responde
    /// 200 com os mesmos itens e total, com erroFotoCapa so no item que tem foto; os VOs publicos ganham so os dois campos da
    /// capa; os profiles nao preenchem a capa; e as duas acoes saem com no-store.
    /// </summary>
    public class VeiculoPublicoCapaTests
    {
        private const int IdVeiculoComFoto = 1;
        private const int IdVeiculoSemFoto = 2;
        private const string PrefixoDaChave = "veiculos/";
        private const string SufixoDaChave = "/fotos/capa.jpg";
        private const string MarcaDoTeste = "Fiat";
        private const string ModeloDoTeste = "Toro";
        private const string CategoriaDoTeste = "SUV";
        private const string NomeDaCampoErroFotoCapa = "ErroFotoCapa";
        private const string NomeDoCampoFotoCapa = "FotoCapa";
        private const string JsonErroFotoCapa = "\"erroFotoCapa\":";
        private const string TextoDeArmazenamento = "armazen";
        private const string TextoDeEndereco = "http";
        private const string TextoDeEnderecoSeguro = "https://";
        private const int PaginaDoTeste = 1;
        private const int TamanhoPaginaDoTeste = 20;
        private const decimal ValorVendaDoTeste = 90000m;
        private const decimal ValorDiariaDoTeste = 150m;

        private static readonly DateOnly RetiradaDoTeste = new DateOnly(2026, 12, 10);
        private static readonly DateOnly DevolucaoDoTeste = new DateOnly(2026, 12, 12);

        private static readonly string[] PropriedadesDaBuscaPublica = new string[]
        {
            "IdVeiculo", "IdCategoria", "CategoriaNome", "Marca", "Modelo", "AnoModelo", "Cor", "ValorDiaria", "ValorDiariaStr",
            "LocadoraNomeFantasia", "LocadoraCidade", "LocadoraUf", NomeDoCampoFotoCapa, NomeDaCampoErroFotoCapa
        };

        private static readonly string[] PropriedadesDoCatalogoPublico = new string[]
        {
            "IdVeiculo", "Marca", "Modelo", "AnoFabricacao", "AnoModelo", "Cor", "KmAtual", "CategoriaNome", "ValorVenda", "ValorVendaStr",
            "LocadoraNomeFantasia", "LocadoraCidade", "LocadoraUf", NomeDoCampoFotoCapa, NomeDaCampoErroFotoCapa
        };

        private static Veiculo CriarVeiculo(int idVeiculo, Categoria categoria, bool comFoto)
        {
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idVeiculo,
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = "PLC" + idVeiculo,
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = MarcaDoTeste,
                Modelo = ModeloDoTeste,
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = ValorDiariaDoTeste,
                ValorVenda = ValorVendaDoTeste,
                Status = StatusVeiculo.DISPONIVEL
            };

            if (comFoto)
            {
                veiculo.Fotos = new List<VeiculoFoto>
                {
                    new VeiculoFoto
                    {
                        IdVeiculoFoto = 100 + idVeiculo,
                        IdVeiculo = idVeiculo,
                        Ordem = 1,
                        ChaveObjeto = PrefixoDaChave + idVeiculo + SufixoDaChave,
                        IdFuncionarioEnvio = 1
                    }
                };
            }

            return veiculo;
        }

        private static FakeVeiculoRepository CriarRepositorioComDoisVeiculos()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = CategoriaDoTeste, ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(IdVeiculoComFoto, categoria, true));
            repositorio.Adicionar(CriarVeiculo(IdVeiculoSemFoto, categoria, false));

            return repositorio;
        }

        private static VeiculoService CriarServicoDaBusca(FakeVeiculoRepository repositorio, FakeArmazenamentoGateway gateway, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoDisponivelProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(gateway, log));
        }

        private static VeiculoService CriarServicoDoCatalogo(FakeVeiculoRepository repositorio, FakeArmazenamentoGateway gateway, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoCatalogoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new VeiculoService(repositorio, new FakeCategoriaRepository(), mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(gateway, log));
        }

        private static FiltroVeiculoDisponibilidadeVO FiltroDaBusca()
        {
            return new FiltroVeiculoDisponibilidadeVO
            {
                Pagina = PaginaDoTeste,
                TamanhoPagina = TamanhoPaginaDoTeste,
                DataRetirada = RetiradaDoTeste,
                DataDevolucao = DevolucaoDoTeste
            };
        }

        private static FiltroCatalogoVO FiltroDoCatalogo()
        {
            return new FiltroCatalogoVO
            {
                Pagina = PaginaDoTeste,
                TamanhoPagina = TamanhoPaginaDoTeste
            };
        }

        private static List<string> NomesPublicos(Type tipo)
        {
            return tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .OrderBy(nome => nome, StringComparer.Ordinal)
                .ToList();
        }

        [Fact]
        public async Task Busca_ComFoto_RecebeACapaComUmaAssinaturaNaPagina()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServicoDaBusca(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDaBusca());

            ReturnVeiculoDisponivelVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoDisponivelVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.NotNull(comFoto.FotoCapa);
            Assert.Null(comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Busca_FalhaDoArmazenamento_Responde200ComOsMesmosItensETotal_ErroSoNoItemComFoto()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServicoDaBusca(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDaBusca());

            ReturnVeiculoDisponivelVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoDisponivelVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.Null(comFoto.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Single(log.Entries);
            Assert.All(log.Entries, entrada => Assert.DoesNotContain(TextoDeEnderecoSeguro, entrada));
            Assert.All(log.Entries, entrada => Assert.DoesNotContain(PrefixoDaChave, entrada));
        }

        [Fact]
        public async Task Busca_PaginaSemFotoCadastrada_NaoChamaOArmazenamento()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = CategoriaDoTeste, ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(IdVeiculoSemFoto, categoria, false));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            VeiculoService servico = CriarServicoDaBusca(repositorio, gateway, new FakeLogService());

            PaginaVO<ReturnVeiculoDisponivelVO> pagina = await servico.GetDisponiveisAsync(FiltroDaBusca());

            ReturnVeiculoDisponivelVO item = Assert.Single(pagina.Itens);
            Assert.Null(item.FotoCapa);
            Assert.Null(item.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Catalogo_ComFoto_RecebeACapaComUmaAssinaturaNaPagina()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServicoDoCatalogo(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(FiltroDoCatalogo());

            ReturnVeiculoCatalogoVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoCatalogoVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.NotNull(comFoto.FotoCapa);
            Assert.Null(comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Catalogo_FalhaDoArmazenamento_Responde200ComOsMesmosItensETotal_ErroSoNoItemComFoto()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServicoDoCatalogo(CriarRepositorioComDoisVeiculos(), gateway, log);

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(FiltroDoCatalogo());

            ReturnVeiculoCatalogoVO comFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoComFoto);
            ReturnVeiculoCatalogoVO semFoto = pagina.Itens.Single(item => item.IdVeiculo == IdVeiculoSemFoto);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.Null(comFoto.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, comFoto.ErroFotoCapa);
            Assert.Null(semFoto.FotoCapa);
            Assert.Null(semFoto.ErroFotoCapa);
            Assert.Single(log.Entries);
            Assert.All(log.Entries, entrada => Assert.DoesNotContain(TextoDeEnderecoSeguro, entrada));
            Assert.All(log.Entries, entrada => Assert.DoesNotContain(PrefixoDaChave, entrada));
        }

        [Fact]
        public async Task Catalogo_PaginaSemFotoCadastrada_NaoChamaOArmazenamento()
        {
            FakeVeiculoRepository repositorio = new FakeVeiculoRepository();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = CategoriaDoTeste, ValorDiariaBase = 200m };
            repositorio.Adicionar(CriarVeiculo(IdVeiculoSemFoto, categoria, false));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            VeiculoService servico = CriarServicoDoCatalogo(repositorio, gateway, new FakeLogService());

            PaginaVO<ReturnVeiculoCatalogoVO> pagina = await servico.GetCatalogoAsync(FiltroDoCatalogo());

            ReturnVeiculoCatalogoVO item = Assert.Single(pagina.Itens);
            Assert.Null(item.FotoCapa);
            Assert.Null(item.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
        }

        [Fact]
        public void ReturnVeiculoDisponivelVO_TemSoOsCamposPublicosMaisOsDoisDaCapa()
        {
            List<string> esperados = PropriedadesDaBuscaPublica.OrderBy(nome => nome, StringComparer.Ordinal).ToList();

            Assert.Equal(esperados, NomesPublicos(typeof(ReturnVeiculoDisponivelVO)));
        }

        [Fact]
        public void ReturnVeiculoCatalogoVO_TemSoOsCamposPublicosMaisOsDoisDaCapa()
        {
            List<string> esperados = PropriedadesDoCatalogoPublico.OrderBy(nome => nome, StringComparer.Ordinal).ToList();

            Assert.Equal(esperados, NomesPublicos(typeof(ReturnVeiculoCatalogoVO)));
        }

        [Fact]
        public void OsDoisVOsPublicosImplementamIComFotoCapa_ComOsTiposDosCampos()
        {
            Assert.True(typeof(IComFotoCapa).IsAssignableFrom(typeof(ReturnVeiculoDisponivelVO)));
            Assert.True(typeof(IComFotoCapa).IsAssignableFrom(typeof(ReturnVeiculoCatalogoVO)));
            Assert.Equal(typeof(FotoCapaVeiculoVO), typeof(ReturnVeiculoDisponivelVO).GetProperty(NomeDoCampoFotoCapa)!.PropertyType);
            Assert.Equal(typeof(FotoCapaVeiculoVO), typeof(ReturnVeiculoCatalogoVO).GetProperty(NomeDoCampoFotoCapa)!.PropertyType);
            Assert.Equal(typeof(string), typeof(ReturnVeiculoDisponivelVO).GetProperty(NomeDaCampoErroFotoCapa)!.PropertyType);
            Assert.Equal(typeof(string), typeof(ReturnVeiculoCatalogoVO).GetProperty(NomeDaCampoErroFotoCapa)!.PropertyType);
        }

        [Fact]
        public void TextoDeErroDaCapaNaoExpoeArmazenamentoNemEndereco()
        {
            JsonSerializerOptions opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            ReturnVeiculoDisponivelVO comErro = new ReturnVeiculoDisponivelVO { IdVeiculo = IdVeiculoComFoto, ErroFotoCapa = ConstantsMessage.ErroFotoCapaIndisponivel };

            string json = JsonSerializer.Serialize(comErro, opcoes);

            Assert.Contains(JsonErroFotoCapa, json);
            Assert.DoesNotContain(TextoDeEndereco, json);
            Assert.DoesNotContain(TextoDeArmazenamento, ConstantsMessage.ErroFotoCapaIndisponivel, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void VeiculoDisponivelProfile_NaoPreencheACapa_MesmoComFotoCadastrada()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoDisponivelProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = CategoriaDoTeste, ValorDiariaBase = 200m };

            ReturnVeiculoDisponivelVO retorno = mapper.Map<ReturnVeiculoDisponivelVO>(CriarVeiculo(IdVeiculoComFoto, categoria, true));

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
        }

        [Fact]
        public void VeiculoCatalogoProfile_NaoPreencheACapa_MesmoComFotoCadastrada()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoCatalogoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = CategoriaDoTeste, ValorDiariaBase = 200m };

            ReturnVeiculoCatalogoVO retorno = mapper.Map<ReturnVeiculoCatalogoVO>(CriarVeiculo(IdVeiculoComFoto, categoria, true));

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
        }

        [Fact]
        public void BuscaECatalogo_TemNoStoreNaAcao()
        {
            AssertNoStore(typeof(VeiculoDisponibilidadeController).GetMethod("GetDisponiveis"));
            AssertNoStore(typeof(VeiculoController).GetMethod("GetCatalogo"));
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
