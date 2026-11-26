using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Servico da lista "Minhas locacoes" com o repositorio em memoria, o relogio fixo em 26/11/2026 18:00 e o servico de capa
    /// real sobre o gateway de teste. Cobre posse, situacao, locadora desativada, paginacao, atraso, falha da capa (a lista
    /// segue com 200 no controller) e o log da falha da leitura.
    /// </summary>
    public class MinhasLocacoesServiceTests
    {
        private const int IdClienteDoTeste = 1;
        private const int IdClienteAlheio = 2;
        private const string PlacaDeTeste = "ABC1D23";
        private static readonly DateTime Agora = new DateTime(2026, 11, 26, 18, 0, 0);

        private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MinhasLocacoesProfile>(), NullLoggerFactory.Instance).CreateMapper();

        private static Empresa CriarEmpresa(int idEmpresa, bool ativa)
        {
            return new Empresa
            {
                IdEmpresa = idEmpresa,
                NomeFantasia = "Locadora " + idEmpresa,
                Logradouro = "Rua das Flores",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Campinas",
                Uf = "SP",
                Ativo = ativa
            };
        }

        private static Locacao CriarLocacao(int idLocacao, int idCliente, StatusLocacao status, Empresa empresa, DateTime retirada, DateTime devolucaoPrevista, bool comFoto = false)
        {
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "Economico" };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idLocacao,
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = PlacaDeTeste,
                Marca = "Fiat",
                Modelo = "Mobi",
                AnoModelo = 2024,
                IdEmpresa = empresa.IdEmpresa,
                Empresa = empresa
            };

            if (comFoto)
            {
                veiculo.Fotos.Add(new VeiculoFoto
                {
                    IdVeiculoFoto = idLocacao,
                    IdVeiculo = veiculo.IdVeiculo,
                    Ordem = 1,
                    ChaveObjeto = "fotos/" + idLocacao
                });
            }

            return new Locacao
            {
                IdLocacao = idLocacao,
                IdCliente = idCliente,
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                DataRetirada = retirada,
                DataDevolucaoPrevista = devolucaoPrevista,
                KmSaida = 1000,
                ValorDiaria = 150m,
                Status = status
            };
        }

        private static MinhasLocacoesService CriarServico(FakeLocacaoRepository repositorio, FakeLogService log, FakeArmazenamentoGateway gateway)
        {
            FotoCapaVeiculoService capa = new FotoCapaVeiculoService(gateway, log);

            return new MinhasLocacoesService(repositorio, new FakeRelogio(Agora), Mapper, log, capa);
        }

        private static FiltroMinhaLocacaoVO Filtro(string? situacao = null, int pagina = 1, int tamanhoPagina = 20)
        {
            return new FiltroMinhaLocacaoVO { Situacao = situacao, Pagina = pagina, TamanhoPagina = tamanhoPagina };
        }

        [Fact]
        public async Task DoisClientes_CadaUmVeSoAsProprias()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2)));
            repositorio.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-2), Agora.AddDays(2)));
            repositorio.Adicionar(CriarLocacao(3, IdClienteAlheio, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2)));
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro());

            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
            Assert.All(pagina.Itens, item => Assert.NotEqual(3, item.IdLocacao));
        }

        [Fact]
        public async Task Historico_DevolveSoFinalizadaECancelada()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2)));
            repositorio.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.FINALIZADA, empresa, Agora.AddDays(-9), Agora.AddDays(-7)));
            repositorio.Adicionar(CriarLocacao(3, IdClienteDoTeste, StatusLocacao.CANCELADA, empresa, Agora.AddDays(-20), Agora.AddDays(-18)));
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro(ConstantsValues.SituacaoMinhasLocacoesHistorico));

            Assert.Equal(2, pagina.Total);
            Assert.Contains(pagina.Itens, item => item.Status == nameof(StatusLocacao.FINALIZADA));
            Assert.Contains(pagina.Itens, item => item.Status == nameof(StatusLocacao.CANCELADA));
            Assert.DoesNotContain(pagina.Itens, item => item.Status == nameof(StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public async Task Paginacao_DevolveATerceiraLocacaoNaSegundaPaginaComOTotalCerto()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            for (int indice = 1; indice <= 3; indice++)
            {
                repositorio.Adicionar(CriarLocacao(indice, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-indice), Agora.AddDays(2)));
            }
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro(pagina: 2, tamanhoPagina: 2));

            Assert.Equal(3, pagina.Total);
            Assert.Equal(2, pagina.Pagina);
            Assert.Single(pagina.Itens);
            Assert.Equal(3, pagina.Itens[0].IdLocacao);
        }

        [Fact]
        public async Task LocadoraDesativada_NaoApareceNaLista()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, CriarEmpresa(1, false), Agora.AddDays(-1), Agora.AddDays(2)));
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro());

            Assert.Equal(0, pagina.Total);
            Assert.Empty(pagina.Itens);
        }

        [Fact]
        public async Task DuasLocadoras_AsDuasAparecem()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, CriarEmpresa(1, true), Agora.AddDays(-1), Agora.AddDays(2)));
            repositorio.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, CriarEmpresa(2, true), Agora.AddDays(-2), Agora.AddDays(2)));
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro());

            Assert.Equal(2, pagina.Total);
            Assert.Contains(pagina.Itens, item => item.LocadoraNomeFantasia == "Locadora 1");
            Assert.Contains(pagina.Itens, item => item.LocadoraNomeFantasia == "Locadora 2");
        }

        [Fact]
        public async Task Atraso_DevolucaoCombinadaJaPassada_MarcaAtrasada()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(-1)));
            repositorio.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2)));
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), new FakeArmazenamentoGateway());

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro());

            Assert.True(pagina.Itens.Single(item => item.IdLocacao == 1).Atrasada);
            Assert.False(pagina.Itens.Single(item => item.IdLocacao == 2).Atrasada);
        }

        [Fact]
        public async Task FalhaDaCapa_NaoDerrubaALista_E_MarcaSoOItemComFoto()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2), comFoto: true));
            repositorio.Adicionar(CriarLocacao(2, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-2), Agora.AddDays(2)));
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            MinhasLocacoesService servico = CriarServico(repositorio, new FakeLogService(), gateway);

            PaginaVO<ReturnMinhaLocacaoVO> pagina = await servico.ListarAsync(IdClienteDoTeste, Filtro());

            Assert.Equal(2, pagina.Total);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, pagina.Itens.Single(item => item.IdLocacao == 1).ErroFotoCapa);
            Assert.Null(pagina.Itens.Single(item => item.IdLocacao == 1).FotoCapa);
            Assert.Null(pagina.Itens.Single(item => item.IdLocacao == 2).ErroFotoCapa);
        }

        [Fact]
        public async Task FalhaDaLeitura_RegistraNoLogComOTextoDaConstanteERelanca()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository { FalhaNaListagemMinhas = new InvalidOperationException("falha de teste") };
            FakeLogService log = new FakeLogService();
            MinhasLocacoesService servico = CriarServico(repositorio, log, new FakeArmazenamentoGateway());

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ListarAsync(IdClienteDoTeste, Filtro()));

            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroListarMinhasLocacoes));
        }

        [Fact]
        public async Task FalhaDepoisDoMapeamento_LogNaoTrazAPlaca()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Empresa empresa = CriarEmpresa(1, true);
            repositorio.Adicionar(CriarLocacao(1, IdClienteDoTeste, StatusLocacao.EM_ANDAMENTO, empresa, Agora.AddDays(-1), Agora.AddDays(2), comFoto: true));
            FakeLogService log = new FakeLogService();
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { LancarExcecaoGenericaAssinatura = true };
            MinhasLocacoesService servico = CriarServico(repositorio, log, gateway);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ListarAsync(IdClienteDoTeste, Filtro()));

            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroListarMinhasLocacoes));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(PlacaDeTeste));
        }

        [Fact]
        public async Task FalhaDeNegocio_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada));
        }

        [Fact]
        public async Task FalhaNaoEncontrada_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada));
        }

        [Fact]
        public async Task LimiteExcedido_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes));
        }

        [Fact]
        public async Task ValidacaoFluent_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new FluentValidation.ValidationException("falha de validacao de teste"));
        }

        /// <summary>Confere que a excecao de negocio sobe com o mesmo tipo e que o servico nao a registra no log.</summary>
        private static async Task VerificarRelancaSemLogAsync(Exception excecao)
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository { FalhaNaListagemMinhas = excecao };
            FakeLogService log = new FakeLogService();
            MinhasLocacoesService servico = CriarServico(repositorio, log, new FakeArmazenamentoGateway());

            Exception capturada = await Assert.ThrowsAnyAsync<Exception>(() => servico.ListarAsync(IdClienteDoTeste, Filtro()));

            Assert.Equal(excecao.GetType(), capturada.GetType());
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroListarMinhasLocacoes));
        }

        [Fact]
        public void VO_NaoExpoeCamposProibidosDaLocacao()
        {
            string[] proibidos = new string[]
            {
                "IdCliente", "IdFuncionario", "Chassi", "Renavam", "IdTransacao", "ValorCaucao", "IdVeiculo",
                "IdEmpresa", "Cpf", "Email", "Telefone", "CnhNumero", "KmChegada"
            };

            string[] publicos = typeof(ReturnMinhaLocacaoVO)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .ToArray();

            Assert.Empty(publicos.Intersect(proibidos));
        }
    }
}
