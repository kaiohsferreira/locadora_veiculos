using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Abertura com caucao ponta a ponta no servico, com o CaucaoGateway, o FakeCaucaoProvider e o CaucaoFakeStore REAIS (trava
    /// liberada: chave Pagamentos:PermitirProvedorFake, Development e integracao PAGAMENTO em SANDBOX). So o repositorio de locacao
    /// e o log sao de teste. Prova: o id gravado e o do FAKE e permite capturar e cancelar depois (aceite do RF-20, correcao i, um
    /// teste para cada operacao); a corrida com o MESMO token nao cancela a caucao que a outra abertura gravou (correcao a); falha do
    /// banco e EM_ANALISE cancelam o id novo; a repeticao depois disso pede nova tentativa; a trava fechada recusa antes de tocar no
    /// store; e a leitura da retirada (GET) concorda com a abertura sobre a trava do FAKE (correcao f).
    /// </summary>
    public class CaucaoRetiradaFluxoTests
    {
        private const int IdReservaA = 42;
        private const int IdReservaB = 43;
        private const int IdFuncionario = 20;
        private const decimal ValorDaCaucao = 1200.00m;
        private const string Bandeira = "visa";
        private const string EmailCliente = "cliente.fluxo@exemplo.com";
        private const string CpfCliente = "12345678909";
        private const string AmbienteDesenvolvimento = "Development";
        private const string AmbienteProducao = "Production";
        private const string PadraoIdFake = "^FAKE-CAU-[0-9a-f]{12}$";

        private sealed class Fluxo
        {
            public Fluxo(CaucaoFakeStore store, string ambienteDeHospedagem = AmbienteDesenvolvimento, string? valorDaChave = "true", AmbienteIntegracao ambienteDaIntegracao = AmbienteIntegracao.SANDBOX)
            {
                Store = store;
                Gateway = CriarGateway(store, ambienteDeHospedagem, valorDaChave, ambienteDaIntegracao, Log);
                Repositorio.Adicionar(CriarReserva(IdReservaA, 40));
                Repositorio.Adicionar(CriarReserva(IdReservaB, 41));
                Repositorio.Adicionar(new Funcionario { IdFuncionario = IdFuncionario, Ativo = true });
                Caucao = new CaucaoRetiradaService(Gateway, Repositorio, new CaucaoRetiradaOptions(ValorDaCaucao), Log);
                Servico = new LocacaoService(Repositorio, new FakeRelogio(new DateTime(2026, 11, 24, 10, 0, 0)), CriarMapper(), Log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), Log), new FakeClienteDocumentoRepository(), Caucao);
            }

            public CaucaoFakeStore Store { get; }

            public FakeLocacaoRepository Repositorio { get; } = new FakeLocacaoRepository();

            public FakeLogService Log { get; } = new FakeLogService();

            public CaucaoGateway Gateway { get; }

            public CaucaoRetiradaService Caucao { get; }

            public LocacaoService Servico { get; }

            public Task<ReturnLocacaoVO> AbrirAsync(int idReserva, string token, string? dispositivo = null)
            {
                LocacaoVO locacaoVO = new LocacaoVO
                {
                    IdReserva = idReserva,
                    CaucaoTokenCartao = token,
                    CaucaoMetodoPagamentoId = Bandeira,
                    CaucaoIdentificadorDispositivo = dispositivo
                };

                return Servico.StartFromReservaAsync(locacaoVO, IdFuncionario);
            }

            /// <summary>Leitura da retirada (GET), com o mesmo gateway e o mesmo FAKE da abertura.</summary>
            public Task<ReturnRetiradaVO> LerRetiradaAsync(int idReserva)
            {
                return Servico.GetRetiradaAsync(idReserva);
            }
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<LocacaoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        /// <summary>Reserva ATIVA da locadora 1, com o veiculo RESERVADO e as navegacoes que a leitura da retirada mostra (categoria e locadora).</summary>
        private static Reserva CriarReserva(int idReserva, int idVeiculo)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 30, Nome = "Maria da Silva", Email = EmailCliente, Cpf = CpfCliente, CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B, CnhValidade = new DateOnly(2030, 1, 1)
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idVeiculo, IdEmpresa = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", AnoModelo = 2024,
                KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO, IdCategoria = 3,
                Categoria = new Categoria { IdCategoria = 3, Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = 1, Ativo = true, NomeFantasia = "Locadora Teste", Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "Campinas", Uf = "SP" }
            };

            return new Reserva
            {
                IdReserva = idReserva,
                IdCliente = 30,
                Cliente = cliente,
                IdVeiculo = idVeiculo,
                Veiculo = veiculo,
                DataRetiradaPrevista = new DateTime(2026, 11, 24, 9, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 27, 9, 0, 0),
                Status = StatusReserva.ATIVA
            };
        }

        /// <summary>Gateway REAL com a integracao PAGAMENTO do ambiente dado (provedor FAKE) e o FAKE real com a chave e o ambiente de hospedagem dados.</summary>
        private static CaucaoGateway CriarGateway(CaucaoFakeStore store, string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao, FakeLogService log)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = ambienteDaIntegracao,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 30000,
                Ativo = true
            };
            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveProvedor, Valor = ConstantsCaucao.ProvedorFake, Protegido = false });

            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integration);
            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);

            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valorDaChave != null)
            {
                valores.Add(ConstantsPix.ChavePermitirProvedorFake, valorDaChave);
            }

            IConfiguration configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            ICaucaoProvider[] providers = { new FakeCaucaoProvider(store, new GoogleProvaDeTeste.AmbienteFalso(ambienteDeHospedagem), configuracao, log) };

            return new CaucaoGateway(resolver, providers, log);
        }

        /// <summary>
        /// Identificador que o FAKE da a tentativa (reserva + token + valor), descoberto num store separado com a trava liberada:
        /// o id so depende da chave de idempotencia, nunca do store.
        /// </summary>
        private static async Task<string> IdDoFakeAsync(int idReserva, string token)
        {
            CaucaoGateway gateway = CriarGateway(new CaucaoFakeStore(), AmbienteDesenvolvimento, "true", AmbienteIntegracao.SANDBOX, new FakeLogService());
            CaucaoSolicitacao solicitacao = new CaucaoSolicitacao
            {
                ReferenciaExterna = RegrasCaucaoRetirada.ReferenciaDaCaucao(idReserva),
                Valor = ValorDaCaucao,
                Descricao = ConstantsValues.CaucaoDescricaoRetirada,
                TokenCartao = token,
                MetodoPagamentoId = Bandeira,
                Pagador = new CaucaoPagador { Email = EmailCliente }
            };

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(solicitacao);

            return resultado.IdentificadorProvedor;
        }

        private static string IdDaCaucaoGravada(List<Locacao> gravadas, int idReserva)
        {
            Locacao locacao = gravadas.Single((Locacao x) => x.IdReserva == idReserva);
            Pagamento caucao = Assert.Single(locacao.Pagamentos);

            return Assert.IsType<string>(caucao.IdTransacao);
        }

        [Fact]
        public async Task Aprovado_GravaOIdDoFake_EAConsultaDeleDevolveReservada()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());

            ReturnLocacaoVO retorno = await fluxo.AbrirAsync(IdReservaA, "FAKE-tok-aprovado-0001");

            string idGravado = IdDaCaucaoGravada(fluxo.Repositorio.LocacoesGravadas(), IdReservaA);
            Assert.Matches(PadraoIdFake, idGravado);
            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);

            CaucaoResultado consultada = await fluxo.Gateway.ConsultarAsync(idGravado);
            Assert.Equal(CaucaoStatus.RESERVADA, consultada.Status);
            Assert.Equal(ValorDaCaucao, consultada.Valor);
            Assert.Equal(RegrasCaucaoRetirada.ReferenciaDaCaucao(IdReservaA), consultada.ReferenciaExterna);
        }

        /// <summary>
        /// Correcao a: duas aberturas da mesma reserva com o MESMO token (mesma chave, mesmo id no provedor). A segunda passa pela
        /// leitura sem trava (congelada antes da gravacao da primeira) e falha na reconferencia; a releitura acha o MESMO id
        /// gravado e NAO cancela. Resultado: uma locacao, uma CAUCAO, e a reserva no FAKE continua RESERVADA.
        /// </summary>
        [Fact]
        public async Task CorridaComOMesmoToken_UmaLocacaoUmaCaucao_EAReservaDoFakeContinuaReservada()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            fluxo.Repositorio.LeituraSemTravaCongelada = true;
            const string token = "FAKE-tok-corrida-0001";

            ReturnLocacaoVO primeira = await fluxo.AbrirAsync(IdReservaA, token);
            BusinessException segunda = await Assert.ThrowsAsync<BusinessException>(() => fluxo.AbrirAsync(IdReservaA, token));

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, segunda.Message);
            Assert.Equal(2, fluxo.Repositorio.ChamadasStartFromReserva);
            Locacao gravada = Assert.Single(fluxo.Repositorio.LocacoesGravadas());
            Assert.Equal(primeira.IdLocacao, gravada.IdLocacao);
            string idGravado = IdDaCaucaoGravada(fluxo.Repositorio.LocacoesGravadas(), IdReservaA);
            CaucaoResultado noFake = Assert.IsType<CaucaoResultado>(fluxo.Store.Obter(idGravado));
            Assert.Equal(CaucaoStatus.RESERVADA, noFake.Status);
        }

        [Fact]
        public async Task FalhaDoBancoDepoisDeAutorizar_OIdTerminaCanceladoNoFake()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            InvalidOperationException original = new InvalidOperationException();
            fluxo.Repositorio.FalhasNaAbertura.Enqueue(original);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => fluxo.AbrirAsync(IdReservaA, "FAKE-tok-banco-0001"));

            Assert.Same(original, excecao);
            CaucaoAberturaRegistro tentativa = Assert.IsType<CaucaoAberturaRegistro>(fluxo.Repositorio.UltimaCaucao);
            CaucaoResultado noFake = Assert.IsType<CaucaoResultado>(fluxo.Store.Obter(tentativa.IdTransacao));
            Assert.Equal(CaucaoStatus.CANCELADA, noFake.Status);
            Assert.Empty(fluxo.Repositorio.LocacoesGravadas());
        }

        /// <summary>v1.1, CS-1: depois da falha do banco (id cancelado), o front sem resposta repete o MESMO token: nova tentativa, nada gravado.</summary>
        [Fact]
        public async Task FalhaDoBancoERepeticaoComOMesmoToken_SegundaPedeNovaTentativaENadaEGravado()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            fluxo.Repositorio.FalhasNaAbertura.Enqueue(new InvalidOperationException());
            const string token = "FAKE-tok-repeticao-0001";

            await Assert.ThrowsAsync<InvalidOperationException>(() => fluxo.AbrirAsync(IdReservaA, token));
            BusinessException segunda = await Assert.ThrowsAsync<BusinessException>(() => fluxo.AbrirAsync(IdReservaA, token));

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoNovaTentativa, segunda.Message);
            Assert.Equal(1, fluxo.Repositorio.ChamadasStartFromReserva);
            Assert.Empty(fluxo.Repositorio.LocacoesGravadas());
        }

        [Fact]
        public async Task EmAnalise_OIdTerminaCancelado_ERepetirNaMesmaReservaPedeNovaTentativa()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());

            BusinessException primeira = await Assert.ThrowsAsync<BusinessException>(() => fluxo.AbrirAsync(IdReservaA, ConstantsCaucao.TokenFakeAnalise));

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoEmAnalise, primeira.Message);
            string id = await IdDoFakeAsync(IdReservaA, ConstantsCaucao.TokenFakeAnalise);
            CaucaoResultado noFake = Assert.IsType<CaucaoResultado>(fluxo.Store.Obter(id));
            Assert.Equal(CaucaoStatus.CANCELADA, noFake.Status);

            BusinessException segunda = await Assert.ThrowsAsync<BusinessException>(() => fluxo.AbrirAsync(IdReservaA, ConstantsCaucao.TokenFakeAnalise));

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoNovaTentativa, segunda.Message);
            Assert.Equal(0, fluxo.Repositorio.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task Recusa_409DoCartaoRecusado_ENadaNoRepositorio()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => fluxo.AbrirAsync(IdReservaA, ConstantsCaucao.TokenFakeRecusado));

            Assert.Equal(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro), excecao.Message);
            Assert.Equal(0, fluxo.Repositorio.ChamadasStartFromReserva);
            Assert.Empty(fluxo.Repositorio.LocacoesGravadas());
        }

        /// <summary>
        /// Linha CAUCAO gravada com a locacao da reserva (o "banco" e o repositorio de teste): tipo CAUCAO, id_locacao da locacao,
        /// id_venda nulo e o id_transacao do FAKE (FAKE-CAU- + 12 hex). Devolve o id_transacao GRAVADO.
        /// </summary>
        private static string IdTransacaoDaLinhaCaucaoGravada(FakeLocacaoRepository repositorio, int idReserva)
        {
            Locacao locacao = Assert.Single(repositorio.LocacoesGravadas(), (Locacao x) => x.IdReserva == idReserva);
            Pagamento caucao = Assert.Single(locacao.Pagamentos);

            Assert.Equal(TipoPagamento.CAUCAO, caucao.Tipo);
            Assert.Equal(locacao.IdLocacao, caucao.IdLocacao);
            Assert.Null(caucao.IdVenda);
            Assert.Equal(StatusPagamento.PENDENTE, caucao.Status);
            string idTransacao = Assert.IsType<string>(caucao.IdTransacao);
            Assert.Matches(PadraoIdFake, idTransacao);

            return idTransacao;
        }

        /// <summary>
        /// Aceite do RF-20, 'pode ser capturada depois' (correcao i): abre a locacao da reserva A com a caucao pelo gateway REAL e o
        /// FAKE, le o id_transacao da linha CAUCAO GRAVADA e captura com ele pelo ICaucaoGateway, como a P5-BE-04 vai fazer na
        /// devolucao. Sem servico nem rota novos.
        /// </summary>
        [Fact]
        public async Task IdGravado_PermiteCapturarDepois()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            await fluxo.AbrirAsync(IdReservaA, "FAKE-tok-captura-0001");
            string idGravado = IdTransacaoDaLinhaCaucaoGravada(fluxo.Repositorio, IdReservaA);
            ICaucaoGateway gateway = fluxo.Gateway;

            CaucaoResultado capturada = await gateway.CapturarAsync(idGravado);

            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Equal(idGravado, capturada.IdentificadorProvedor);
            Assert.Equal(ValorDaCaucao, capturada.ValorCapturado);
            CaucaoResultado noFake = Assert.IsType<CaucaoResultado>(fluxo.Store.Obter(idGravado));
            Assert.Equal(CaucaoStatus.CAPTURADA, noFake.Status);
        }

        /// <summary>
        /// Aceite do RF-20, 'pode ser cancelada depois' (correcao i): abre a locacao da reserva B com a caucao pelo gateway REAL e o
        /// FAKE, le o id_transacao da linha CAUCAO GRAVADA e cancela (libera) com ele pelo ICaucaoGateway, como a P5-BE-04 vai
        /// fazer na devolucao. Sem servico nem rota novos.
        /// </summary>
        [Fact]
        public async Task IdGravado_PermiteCancelarDepois()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            await fluxo.AbrirAsync(IdReservaB, "FAKE-tok-cancela-0001");
            string idGravado = IdTransacaoDaLinhaCaucaoGravada(fluxo.Repositorio, IdReservaB);
            ICaucaoGateway gateway = fluxo.Gateway;

            CaucaoResultado cancelada = await gateway.CancelarAsync(idGravado);

            Assert.Equal(CaucaoStatus.CANCELADA, cancelada.Status);
            Assert.Equal(idGravado, cancelada.IdentificadorProvedor);
            CaucaoResultado noFake = Assert.IsType<CaucaoResultado>(fluxo.Store.Obter(idGravado));
            Assert.Equal(CaucaoStatus.CANCELADA, noFake.Status);
        }

        /// <summary>
        /// Correcao f: o modo de simulacao da leitura vem da MESMA regra da trava do FAKE que a abertura usa. Trava liberada: a
        /// leitura mostra a caucao exigida, disponivel e em simulacao, e a abertura passa; depois de aberta, a leitura fala so da
        /// configuracao (indicadores falsos), sem consultar o provedor.
        /// </summary>
        [Fact]
        public async Task TravaLiberada_LeituraMostraSimulacaoDisponivel_EAAberturaPassa()
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());

            ReturnRetiradaVO antes = await fluxo.LerRetiradaAsync(IdReservaA);
            ReturnLocacaoVO aberta = await fluxo.AbrirAsync(IdReservaA, "FAKE-tok-leitura-0001");
            ReturnRetiradaVO depois = await fluxo.LerRetiradaAsync(IdReservaA);

            Assert.True(antes.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, antes.ValorCaucao);
            Assert.True(antes.CaucaoModoSimulacao);
            Assert.True(antes.CaucaoDisponivel);
            Assert.Equal(ValorDaCaucao, aberta.ValorCaucao);
            Assert.Equal(aberta.IdLocacao, depois.IdLocacaoExistente);
            Assert.True(depois.CaucaoExigida);
            Assert.False(depois.CaucaoModoSimulacao);
            Assert.False(depois.CaucaoDisponivel);
        }

        /// <summary>
        /// Correcao f: com a trava do FAKE fechada (qualquer uma das tres condicoes), a leitura nao cai nem registra falha: mostra a
        /// caucao exigida e indisponivel. A abertura, pela mesma regra, recusa com 502 antes de tocar no repositorio.
        /// </summary>
        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task TravaFechada_LeituraMostraIndisponivelSemFalha_EAAberturaRecusa(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore(), ambienteDeHospedagem, valorDaChave, ambienteDaIntegracao);

            ReturnRetiradaVO leitura = await fluxo.LerRetiradaAsync(IdReservaA);
            IntegrationException abertura = await Assert.ThrowsAsync<IntegrationException>(() => fluxo.AbrirAsync(IdReservaA, "FAKE-tok-leitura-0002"));

            Assert.True(leitura.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, leitura.ValorCaucao);
            Assert.False(leitura.CaucaoModoSimulacao);
            Assert.False(leitura.CaucaoDisponivel);
            string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaConfiguracaoIndisponivel, IdReservaA);
            Assert.DoesNotContain(fluxo.Log.Entries, (string entrada) => entrada.StartsWith(registro, StringComparison.Ordinal));
            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, abertura.Message);
            Assert.Equal(0, fluxo.Repositorio.ChamadasStartFromReserva);
        }

        public static TheoryData<string, string?, AmbienteIntegracao> TravaFechada => new TheoryData<string, string?, AmbienteIntegracao>
        {
            { AmbienteDesenvolvimento, "false", AmbienteIntegracao.SANDBOX },
            { AmbienteDesenvolvimento, null, AmbienteIntegracao.SANDBOX },
            { AmbienteProducao, "true", AmbienteIntegracao.SANDBOX },
            { AmbienteDesenvolvimento, "true", AmbienteIntegracao.PRODUCAO },
            { AmbienteDesenvolvimento, "true", AmbienteIntegracao.HOMOLOGACAO }
        };

        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task TravaDoFakeFechada_502ForaDoSandbox_NadaNoRepositorioNemNoStore(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            Fluxo fluxo = new Fluxo(store, ambienteDeHospedagem, valorDaChave, ambienteDaIntegracao);
            const string token = "FAKE-tok-trava-0001";

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => fluxo.AbrirAsync(IdReservaA, token));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, fluxo.Repositorio.ChamadasStartFromReserva);
            string id = await IdDoFakeAsync(IdReservaA, token);
            Assert.Null(store.Obter(id));
        }

        public static TheoryData<string> CasosDeLog => new TheoryData<string> { "aprovado", "recusa", "em-analise", "falha-do-banco" };

        /// <summary>Com o gateway e o FAKE reais no mesmo log: nenhuma entrada tem token, e-mail, CPF, device id nem a chave de idempotencia.</summary>
        [Theory]
        [MemberData(nameof(CasosDeLog))]
        public async Task Log_ComOGatewayReal_SemTokenEmailCpfDispositivoNemChaveDeIdempotencia(string caso)
        {
            const string EmailSecreto = "segredo.cliente@exemplo.com";
            const string CpfSecreto = "98765432100";
            const string DispositivoSecreto = "dispositivo-SEGREDO-7391";
            Fluxo fluxo = new Fluxo(new CaucaoFakeStore());
            Reserva reserva = CriarReserva(44, 44);
            reserva.Cliente.Email = EmailSecreto;
            reserva.Cliente.Cpf = CpfSecreto;
            fluxo.Repositorio.Adicionar(reserva);
            string token = caso == "recusa" ? ConstantsCaucao.TokenFakeRecusado
                : caso == "em-analise" ? ConstantsCaucao.TokenFakeAnalise
                : "FAKE-tok-SEGREDO-7391";

            if (caso == "falha-do-banco")
            {
                fluxo.Repositorio.FalhasNaAbertura.Enqueue(new InvalidOperationException());
            }

            Exception? excecao = await Record.ExceptionAsync(() => fluxo.AbrirAsync(44, token, DispositivoSecreto));

            Assert.Equal(caso == "aprovado", excecao == null);
            Assert.NotEmpty(fluxo.Log.Entries);

            foreach (string entrada in fluxo.Log.Entries)
            {
                Assert.DoesNotContain(token, entrada);
                Assert.DoesNotContain(EmailSecreto, entrada);
                Assert.DoesNotContain(CpfSecreto, entrada);
                Assert.DoesNotContain(DispositivoSecreto, entrada);
                Assert.DoesNotMatch("caucao-[0-9a-f]{40}", entrada);
            }
        }
    }
}
