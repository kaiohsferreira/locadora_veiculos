using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Caucao
{
    public class CaucaoGatewayTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string TokenCartao = "tok_cartao_9876543210";
        private const string EmailPagador = "maria@exemplo.com";
        private const string CpfPagador = "12345678901";
        private const string Referencia = "CAUCAO-LOC-123";
        private const string IdentificadorProvedor = "123456789";
        private static readonly DateTime Agora = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Local);

        // ------------------------------------------------------------------
        // Escopo de teste: provedor roteirizado (sem rede). Grava o que recebeu e devolve o roteiro.
        // ------------------------------------------------------------------

        private sealed class ProvedorRoteirizado : ICaucaoProvider
        {
            public ProvedorRoteirizado(string nome)
            {
                Nome = nome;
            }

            public string Nome { get; }

            /// <summary>Excecao lancada pela consulta a partir da (ConsultasSemExcecao + 1)-esima chamada.</summary>
            public Exception? ExcecaoConsulta { get; set; }

            public int ConsultasSemExcecao { get; set; }

            public bool IdentificadorValido(string identificadorProvedor)
            {
                string padrao = string.Equals(Nome, ConstantsCaucao.ProvedorFake, StringComparison.OrdinalIgnoreCase)
                    ? ConstantsCaucao.PadraoIdentificadorFake
                    : ConstantsCaucao.PadraoIdentificadorMercadoPago;

                return System.Text.RegularExpressions.Regex.IsMatch(identificadorProvedor, padrao);
            }

            public List<string> ChavesRecebidas { get; } = new List<string>();

            public int ChamadasPreAutorizar { get; private set; }

            public int ChamadasConsultar { get; private set; }

            public int ChamadasCapturar { get; private set; }

            public int ChamadasCancelar { get; private set; }

            public decimal? ValorRecebidoCaptura { get; private set; }

            /// <summary>Estados devolvidos pela consulta, em ordem. Quando resta um, ele se repete.</summary>
            public Queue<CaucaoResultado> Consultas { get; } = new Queue<CaucaoResultado>();

            public CaucaoResultado? RespostaPreAutorizar { get; set; }

            public Exception? ExcecaoPreAutorizar { get; set; }

            public CaucaoResultado? RespostaCaptura { get; set; }

            public Exception? ExcecaoCaptura { get; set; }

            public CaucaoResultado? RespostaCancelamento { get; set; }

            public Exception? ExcecaoCancelamento { get; set; }

            public Task<CaucaoResultado> PreAutorizarAsync(ConfiguracaoIntegracao configuracao, CaucaoSolicitacao solicitacao, string chaveIdempotencia, int validadeReservaDias, CancellationToken cancellationToken = default)
            {
                ChamadasPreAutorizar++;
                ChavesRecebidas.Add(chaveIdempotencia);

                if (ExcecaoPreAutorizar != null)
                {
                    throw ExcecaoPreAutorizar;
                }

                CaucaoResultado resultado = RespostaPreAutorizar ?? Caucao(CaucaoStatus.RESERVADA);
                return Task.FromResult(resultado);
            }

            public Task<CaucaoResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
            {
                ChamadasConsultar++;

                if (ExcecaoConsulta != null && ChamadasConsultar > ConsultasSemExcecao)
                {
                    throw ExcecaoConsulta;
                }

                CaucaoResultado resultado = Consultas.Count > 1 ? Consultas.Dequeue() : Consultas.Peek();
                return Task.FromResult(resultado);
            }

            public Task<CaucaoResultado> CapturarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, decimal? valor, CancellationToken cancellationToken = default)
            {
                ChamadasCapturar++;
                ValorRecebidoCaptura = valor;

                if (ExcecaoCaptura != null)
                {
                    throw ExcecaoCaptura;
                }

                CaucaoResultado resultado = RespostaCaptura ?? Caucao(CaucaoStatus.CAPTURADA, 500m, 500m);
                return Task.FromResult(resultado);
            }

            public Task<CaucaoResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
            {
                ChamadasCancelar++;

                if (ExcecaoCancelamento != null)
                {
                    throw ExcecaoCancelamento;
                }

                CaucaoResultado resultado = RespostaCancelamento ?? Caucao(CaucaoStatus.CANCELADA);
                return Task.FromResult(resultado);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static CaucaoResultado Caucao(CaucaoStatus status, decimal valor = 500m, decimal? valorCapturado = null)
        {
            return new CaucaoResultado
            {
                IdentificadorProvedor = IdentificadorProvedor,
                ReferenciaExterna = Referencia,
                Status = status,
                Valor = valor,
                ValorCapturado = valorCapturado,
                CriadoEm = Agora,
                Provedor = ConstantsCaucao.ProvedorMercadoPago
            };
        }

        private static Integration CriarIntegracao(string? provedor = ConstantsCaucao.ProvedorMercadoPago, AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX, string? validade = null, string? publicKey = null, bool comToken = true)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = ambiente,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 30000,
                Ativo = true
            };

            if (provedor != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveProvedor, Valor = provedor, Protegido = false });
            }

            if (comToken)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveAccessToken, Valor = TokenSecreto, Protegido = true });
            }

            if (validade != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveValidadeDias, Valor = validade, Protegido = false });
            }

            if (publicKey != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChavePublica, Valor = publicKey, Protegido = false });
            }

            return integration;
        }

        private static CaucaoGateway CriarGateway(Integration? integration, FakeLogService log, params ICaucaoProvider[] providers)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();

            if (integration != null)
            {
                repositorio.Add(integration);
            }

            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);

            return new CaucaoGateway(resolver, providers, log);
        }

        private static CaucaoSolicitacao CriarSolicitacao(decimal valor = 500m)
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = valor,
                Descricao = "Caucao da locacao",
                TokenCartao = TokenCartao,
                MetodoPagamentoId = "visa",
                EmissorId = 1234L,
                Pagador = new CaucaoPagador { Email = EmailPagador, Documento = CpfPagador },
                IdentificadorDispositivo = "device-abc-987654"
            };
        }

        private static CaucaoSolicitacao CriarSolicitacaoInvalida(string caso)
        {
            CaucaoSolicitacao solicitacao = CriarSolicitacao();

            switch (caso)
            {
                case "valor-zero":
                    solicitacao.Valor = 0m;
                    break;
                case "valor-negativo":
                    solicitacao.Valor = -5m;
                    break;
                case "valor-tres-casas":
                    solicitacao.Valor = 10.505m;
                    break;
                case "referencia-vazia":
                    solicitacao.ReferenciaExterna = string.Empty;
                    break;
                case "referencia-com-espaco":
                    solicitacao.ReferenciaExterna = "CAUCAO LOC";
                    break;
                case "referencia-longa":
                    solicitacao.ReferenciaExterna = new string('A', 65);
                    break;
                case "descricao-vazia":
                    solicitacao.Descricao = "   ";
                    break;
                case "descricao-longa":
                    solicitacao.Descricao = new string('d', 141);
                    break;
                case "token-curto":
                    solicitacao.TokenCartao = "abc";
                    break;
                case "token-invalido":
                    solicitacao.TokenCartao = "tok cartao!!";
                    break;
                case "metodo-curto":
                    solicitacao.MetodoPagamentoId = "v";
                    break;
                case "metodo-maiuscula":
                    solicitacao.MetodoPagamentoId = "VISA";
                    break;
                case "emissor-zero":
                    solicitacao.EmissorId = 0L;
                    break;
                case "dispositivo-longo":
                    solicitacao.IdentificadorDispositivo = new string('x', 129);
                    break;
                case "email-invalido":
                    solicitacao.Pagador.Email = "sem-arroba";
                    break;
                case "cpf-curto":
                    solicitacao.Pagador.Documento = "123";
                    break;
                case "pagador-nulo":
                    solicitacao.Pagador = null!;
                    break;
            }

            return solicitacao;
        }

        private static string MensagemEsperada(string caso)
        {
            if (caso.StartsWith("valor-", StringComparison.Ordinal))
            {
                return ConstantsMessage.ErroCaucaoValorInvalido;
            }

            if (caso.StartsWith("referencia-", StringComparison.Ordinal))
            {
                return ConstantsMessage.ErroCaucaoReferenciaInvalida;
            }

            if (caso.StartsWith("descricao-", StringComparison.Ordinal))
            {
                return ConstantsMessage.ErroCaucaoDescricaoInvalida;
            }

            if (caso.StartsWith("email-", StringComparison.Ordinal) || caso.StartsWith("cpf-", StringComparison.Ordinal) || caso.StartsWith("pagador-", StringComparison.Ordinal))
            {
                return ConstantsMessage.ErroCaucaoPagadorInvalido;
            }

            return ConstantsMessage.ErroCaucaoCartaoInvalido;
        }

        // ------------------------------------------------------------------
        // Pre-autorizacao: validade, validacao, chave de idempotencia e anomalias.
        // ------------------------------------------------------------------

        [Fact]
        public async Task PreAutorizar_Reservada_PreencheReservaValidaAtePadraoDe5Dias()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(Agora.AddDays(5), resultado.ReservaValidaAte);
        }

        [Fact]
        public async Task PreAutorizar_CaucaoValidadeDiasConfigurado_UsaOValorInformado()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(validade: "7"), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal(Agora.AddDays(7), resultado.ReservaValidaAte);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("31")]
        [InlineData("-1")]
        public async Task PreAutorizar_ValidadeInvalida_LancaIntegrationExceptionSemChamarOProvedor(string validade)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(validade: validade), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoValidadeConfiguracaoInvalida, excecao.Message);
            Assert.Equal(ConstantsCaucao.ChaveValidadeDias, excecao.Chave);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        [Theory]
        [InlineData("valor-zero")]
        [InlineData("valor-negativo")]
        [InlineData("valor-tres-casas")]
        [InlineData("referencia-vazia")]
        [InlineData("referencia-com-espaco")]
        [InlineData("referencia-longa")]
        [InlineData("descricao-vazia")]
        [InlineData("descricao-longa")]
        [InlineData("token-curto")]
        [InlineData("token-invalido")]
        [InlineData("metodo-curto")]
        [InlineData("metodo-maiuscula")]
        [InlineData("emissor-zero")]
        [InlineData("dispositivo-longo")]
        [InlineData("email-invalido")]
        [InlineData("cpf-curto")]
        [InlineData("pagador-nulo")]
        public async Task PreAutorizar_EntradaInvalida_LancaBusinessExceptionAntesDeQualquerChamadaExterna(string caso)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);
            CaucaoSolicitacao solicitacao = CriarSolicitacaoInvalida(caso);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.PreAutorizarAsync(solicitacao));

            Assert.Equal(MensagemEsperada(caso), excecao.Message);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        [Fact]
        public async Task PreAutorizar_MesmaTentativa_MesmaChaveDeIdempotencia()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            await gateway.PreAutorizarAsync(CriarSolicitacao());
            await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal(2, provedor.ChavesRecebidas.Count);
            Assert.Equal(provedor.ChavesRecebidas[0], provedor.ChavesRecebidas[1]);
            Assert.StartsWith(ConstantsCaucao.PrefixoChaveIdempotencia, provedor.ChavesRecebidas[0]);
            Assert.Equal(ConstantsCaucao.PrefixoChaveIdempotencia.Length + ConstantsCaucao.TamanhoHashChaveIdempotencia, provedor.ChavesRecebidas[0].Length);
        }

        [Fact]
        public async Task PreAutorizar_TokenOuValorDiferente_GeraOutraChaveDeIdempotencia()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            await gateway.PreAutorizarAsync(CriarSolicitacao());

            CaucaoSolicitacao outroToken = CriarSolicitacao();
            outroToken.TokenCartao = "tok_cartao_outro_0001";
            await gateway.PreAutorizarAsync(outroToken);

            await gateway.PreAutorizarAsync(CriarSolicitacao(valor: 600m));

            Assert.Equal(3, provedor.ChavesRecebidas.Count);
            Assert.NotEqual(provedor.ChavesRecebidas[0], provedor.ChavesRecebidas[1]);
            Assert.NotEqual(provedor.ChavesRecebidas[0], provedor.ChavesRecebidas[2]);
        }

        [Fact]
        public async Task PreAutorizar_ReservaCapturadaNaCriacao_LancaReservaNaoSuportadaELogaReferenciaEIdentificador()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaPreAutorizar = Caucao(CaucaoStatus.CAPTURADA, 500m, 500m)
            };
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoReservaNaoSuportada, excecao.Message);
            Assert.Equal(1, provedor.ChamadasPreAutorizar);
            Assert.Contains(log.Entries, entrada => entrada.Contains(Referencia) && entrada.Contains(IdentificadorProvedor));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(TokenCartao));
        }

        [Fact]
        public async Task PreAutorizar_RecusaDoProvedor_PropagaABusinessExceptionSemAlterarAMensagem()
        {
            string mensagem = string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoSaldoInsuficiente);
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoPreAutorizar = new BusinessException(mensagem)
            };
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(mensagem, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_EmAnalise_NaoLancaErroEDevolveOStatus()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaPreAutorizar = Caucao(CaucaoStatus.EM_ANALISE)
            };
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(CriarSolicitacao());

            Assert.Equal(CaucaoStatus.EM_ANALISE, resultado.Status);
            Assert.Null(resultado.ReservaValidaAte);
        }

        [Fact]
        public async Task PreAutorizar_ExcecaoInesperadaDoProvedor_LogaEEnvolveEmIntegrationException()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoPreAutorizar = new InvalidOperationException("falha interna simulada")
            };
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoPreAutorizar, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCaucaoPreAutorizar));
        }

        // ------------------------------------------------------------------
        // Consulta
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConsultarAsync_Reservada_PreencheReservaValidaAte()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.ConsultarAsync(IdentificadorProvedor);

            Assert.Equal(Agora.AddDays(5), resultado.ReservaValidaAte);
            Assert.Equal(0, provedor.ChamadasCapturar + provedor.ChamadasCancelar);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ConsultarAsync_IdentificadorVazio_LancaBusinessExceptionSemChamarOProvedor(string identificador)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.ConsultarAsync(identificador));

            Assert.Equal(ConstantsMessage.ErroCaucaoIdentificadorObrigatorio, excecao.Message);
            Assert.Equal(0, provedor.ChamadasConsultar);
        }

        // ------------------------------------------------------------------
        // Captura
        // ------------------------------------------------------------------

        [Fact]
        public async Task CapturarAsync_Integral_ChamaCapturaSemValor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.RespostaCaptura = Caucao(CaucaoStatus.CAPTURADA, 500m, 500m);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(1, provedor.ChamadasCapturar);
            Assert.Null(provedor.ValorRecebidoCaptura);
        }

        [Fact]
        public async Task CapturarAsync_Parcial_EnviaOValorPedido()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.RespostaCaptura = Caucao(CaucaoStatus.CAPTURADA, 500m, 300m);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor, 300m);

            Assert.Equal(300m, provedor.ValorRecebidoCaptura);
            Assert.Equal(300m, resultado.ValorCapturado);
        }

        [Fact]
        public async Task CapturarAsync_AcimaDoReservado_LancaSemChamarCaptura()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync(IdentificadorProvedor, 600m));

            Assert.Equal(ConstantsMessage.ErroCaucaoCapturaAcimaReservado, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        [Theory]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EM_ANALISE)]
        [InlineData(CaucaoStatus.RECUSADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        public async Task CapturarAsync_NaoReservada_LancaNaoReservadaSemChamarCaptura(CaucaoStatus status)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(status, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoNaoReservada, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        [Fact]
        public async Task CapturarAsync_JaCapturadaMesmoValorIntegral_DevolveEstadoSemPut()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CAPTURADA, 500m, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        [Fact]
        public async Task CapturarAsync_JaCapturadaMesmoValorInformado_DevolveEstadoSemPut()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CAPTURADA, 500m, 300m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor, 300m);

            Assert.Equal(300m, resultado.ValorCapturado);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        [Fact]
        public async Task CapturarAsync_JaCapturadaOutroValor_LancaSemPut()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CAPTURADA, 500m, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m));

            Assert.Equal(ConstantsMessage.ErroCaucaoJaCapturadaOutroValor, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        [Fact]
        public async Task CapturarAsync_PutComErroMasCapturadaDepois_ReconciliaESucesso()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new BusinessException(ConstantsMessage.ErroCaucaoSolicitacaoRecusada)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CAPTURADA, 500m, 300m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor, 300m);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(300m, resultado.ValorCapturado);
            Assert.Equal(1, provedor.ChamadasCapturar);
            Assert.Equal(2, provedor.ChamadasConsultar);
        }

        [Fact]
        public async Task CapturarAsync_PutComErroENaoCapturada_RelancaOErroOriginal()
        {
            string mensagem = ConstantsMessage.ErroCaucaoSolicitacaoRecusada;
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new BusinessException(mensagem)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m));

            Assert.Equal(mensagem, excecao.Message);
            Assert.Equal(2, provedor.ChamadasConsultar);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(10.505)]
        public async Task CapturarAsync_ValorInvalido_LancaSemConsultarOProvedor(double valorDouble)
        {
            decimal valor = (decimal)valorDouble;
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync(IdentificadorProvedor, valor));

            Assert.Equal(ConstantsMessage.ErroCaucaoValorInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasConsultar);
            Assert.Equal(0, provedor.ChamadasCapturar);
        }

        // ------------------------------------------------------------------
        // Cancelamento
        // ------------------------------------------------------------------

        [Fact]
        public async Task CancelarAsync_Reservada_EnviaPutEDevolveCancelada()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(1, provedor.ChamadasCancelar);
        }

        [Theory]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.RECUSADA)]
        public async Task CancelarAsync_JaEncerrada_DevolveOEstadoAtualSemPut(CaucaoStatus status)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(status, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(status, resultado.Status);
            Assert.Equal(0, provedor.ChamadasCancelar);
        }

        [Theory]
        [InlineData(CaucaoStatus.CAPTURADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        public async Task CancelarAsync_Capturada_LancaCancelarCapturadaSemPut(CaucaoStatus status)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(status, 500m, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoCancelarCapturada, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task CancelarAsync_PutComErroMasCancelada_ReconciliaESucesso()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new BusinessException(ConstantsMessage.ErroCaucaoSolicitacaoRecusada)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CANCELADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(2, provedor.ChamadasConsultar);
        }

        [Fact]
        public async Task CancelarAsync_PutComErroENaoCancelada_RelancaOErroOriginal()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new BusinessException(ConstantsMessage.ErroCaucaoSolicitacaoRecusada)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
        }

        // ------------------------------------------------------------------
        // Configuracao do cartao (chave publica)
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObterConfiguracaoCartao_MercadoPago_DevolveAPublicKeyDaIntegracao()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(publicKey: "APP_USR-publica-123"), new FakeLogService(), provedor);

            CaucaoConfiguracaoCartao configuracao = await gateway.ObterConfiguracaoCartaoAsync();

            Assert.Equal(ConstantsCaucao.ProvedorMercadoPago, configuracao.Provedor);
            Assert.Equal("APP_USR-publica-123", configuracao.ChavePublica);
        }

        [Fact]
        public async Task ObterConfiguracaoCartao_SemPublicKey_LancaIntegrationExceptionComAChave()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterConfiguracaoCartaoAsync());

            Assert.Equal(ConstantsCaucao.ChavePublica, excecao.Chave);
        }

        [Fact]
        public async Task ObterConfiguracaoCartao_PublicKeyDefinirNoBanco_LancaIntegrationException()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(publicKey: ConstantsValues.MarcadorValorNaoDefinido), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterConfiguracaoCartaoAsync());

            Assert.Equal(ConstantsCaucao.ChavePublica, excecao.Chave);
        }

        [Fact]
        public async Task ObterConfiguracaoCartao_Fake_DevolveAChaveFixaSemPublicKey()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorFake);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(provedor: ConstantsCaucao.ProvedorFake, comToken: false), new FakeLogService(), provedor);

            CaucaoConfiguracaoCartao configuracao = await gateway.ObterConfiguracaoCartaoAsync();

            Assert.Equal(ConstantsCaucao.ProvedorFake, configuracao.Provedor);
            Assert.Equal(ConstantsCaucao.ChavePublicaFake, configuracao.ChavePublica);
        }

        // ------------------------------------------------------------------
        // Selecao do provedor e integracao
        // ------------------------------------------------------------------

        [Fact]
        public async Task ProvedorDesconhecido_LancaIntegrationExceptionComAChaveProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(provedor: "OUTRO"), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoProvedorNaoSuportado, excecao.Message);
            Assert.Equal(ConstantsCaucao.ChaveProvedor, excecao.Chave);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        [Fact]
        public async Task FakeForaDoSandbox_LancaIntegrationExceptionSemChamarOProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorFake);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(provedor: ConstantsCaucao.ProvedorFake, ambiente: AmbienteIntegracao.PRODUCAO), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        [Fact]
        public async Task ProvedorAusente_LancaIntegrationExceptionComAChaveProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(provedor: null), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsCaucao.ChaveProvedor, excecao.Chave);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        // ------------------------------------------------------------------
        // Rodada de correcao: cancelamento em EM_ANALISE, reconciliacao ampliada, estado devolvido,
        // validade so em RESERVADA, formato do identificador, dispositivo, falha com referencia, chave publica.
        // ------------------------------------------------------------------

        [Fact]
        public async Task CancelarAsync_EmAnalise_VaiAoProvedorECancela()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.EM_ANALISE, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(1, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task CancelarAsync_PutComErroEReconsultaEmAnalise_RelancaOErroOriginal()
        {
            string mensagem = ConstantsMessage.ErroCaucaoSolicitacaoRecusada;
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new BusinessException(mensagem)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.EM_ANALISE, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(mensagem, excecao.Message);
            Assert.Equal(1, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task CancelarAsync_IntegrationExceptionMasCancelada_ReconciliaESucesso()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CANCELADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(2, provedor.ChamadasConsultar);
        }

        [Fact]
        public async Task CancelarAsync_EstadoDevolvidoCapturada_LancaRespostaInesperada()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaCancelamento = Caucao(CaucaoStatus.CAPTURADA, 500m, 500m)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCaucaoRespostaInesperada) && entrada.Contains("CAPTURADA"));
        }

        [Fact]
        public async Task CancelarAsync_EstadoDevolvidoExpirada_EhAceito()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaCancelamento = Caucao(CaucaoStatus.EXPIRADA, 500m)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CancelarAsync(IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.EXPIRADA, resultado.Status);
        }

        [Fact]
        public async Task CapturarAsync_EstadoDevolvidoReservada_LancaRespostaInesperada()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaCaptura = Caucao(CaucaoStatus.RESERVADA, 500m)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task CapturarAsync_IntegrationExceptionMasCapturada_ReconciliaESucesso()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.CAPTURADA, 500m, 300m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor, 300m);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(300m, resultado.ValorCapturado);
        }

        [Fact]
        public async Task CapturarAsync_ReconsultaFalha_RelancaAExcecaoOriginalELogaAFalhaDaReconsulta()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoCredenciaisRecusadas),
                ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel),
                ConsultasSemExcecao = 1
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m));

            Assert.Equal(ConstantsMessage.ErroCaucaoCredenciaisRecusadas, excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCaucaoConsultar));
        }

        [Fact]
        public async Task CapturarAsync_NotFound_NaoReconcilia()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            await Assert.ThrowsAsync<NotFoundException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m));

            Assert.Equal(1, provedor.ChamadasConsultar);
        }

        [Theory]
        [InlineData("preautorizar")]
        [InlineData("consultar")]
        [InlineData("capturar")]
        [InlineData("cancelar")]
        public async Task OperacaoCancelada_PropagaOperationCanceled(string operacao)
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            OperationCanceledException cancelada = new OperationCanceledException(cancelamento.Token);
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);

            if (operacao == "preautorizar")
            {
                provedor.ExcecaoPreAutorizar = cancelada;
            }
            else
            {
                provedor.ExcecaoConsulta = cancelada;
            }

            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            Func<Task> chamada;

            if (operacao == "preautorizar")
            {
                chamada = () => gateway.PreAutorizarAsync(CriarSolicitacao(), cancelamento.Token);
            }
            else if (operacao == "consultar")
            {
                chamada = () => gateway.ConsultarAsync(IdentificadorProvedor, cancelamento.Token);
            }
            else if (operacao == "capturar")
            {
                chamada = () => gateway.CapturarAsync(IdentificadorProvedor, null, cancelamento.Token);
            }
            else
            {
                chamada = () => gateway.CancelarAsync(IdentificadorProvedor, cancelamento.Token);
            }

            await Assert.ThrowsAsync<OperationCanceledException>(chamada);
        }

        [Theory]
        [InlineData(CaucaoStatus.EM_ANALISE)]
        [InlineData(CaucaoStatus.CAPTURADA)]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.RECUSADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        public async Task ConsultarAsync_SemEstadoReservada_ReservaValidaAteNula(CaucaoStatus status)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            decimal? valorCapturado = status == CaucaoStatus.CAPTURADA ? (decimal?)500m : null;
            provedor.Consultas.Enqueue(Caucao(status, 500m, valorCapturado));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.ConsultarAsync(IdentificadorProvedor);

            Assert.Null(resultado.ReservaValidaAte);
        }

        [Fact]
        public async Task CapturarAsync_CapturadaNoPut_ReservaValidaAteNula()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            provedor.RespostaCaptura = Caucao(CaucaoStatus.CAPTURADA, 500m, 500m);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            CaucaoResultado resultado = await gateway.CapturarAsync(IdentificadorProvedor);

            Assert.Null(resultado.ReservaValidaAte);
        }

        [Theory]
        [InlineData("..")]
        [InlineData("123/../x")]
        [InlineData("FAKE-CAU-000000000000")]
        public async Task ConsultarAsync_IdentificadorForaDoFormatoDoMercadoPago_LancaSemChamarOProvedor(string identificador)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.ConsultarAsync(identificador));

            Assert.Equal(ConstantsMessage.ErroCaucaoIdentificadorInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasConsultar);
        }

        [Fact]
        public async Task CancelarAsync_IdentificadorNumericoComProvedorFake_LancaSemChamarOProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorFake);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(provedor: ConstantsCaucao.ProvedorFake), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoIdentificadorInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasConsultar);
            Assert.Equal(0, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task CapturarAsync_IdentificadorFakeComProvedorMercadoPago_LancaSemChamarOProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CapturarAsync("FAKE-CAU-0123456789ab"));

            Assert.Equal(ConstantsMessage.ErroCaucaoIdentificadorInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasConsultar);
        }

        [Fact]
        public async Task PreAutorizar_DispositivoComQuebraDeLinha_RecusadoSemChamarOProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);
            CaucaoSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.IdentificadorDispositivo = "abc\r\nX-Evil: 1";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.PreAutorizarAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroCaucaoCartaoInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }

        [Fact]
        public async Task PreAutorizar_DispositivoEmBranco_EhAusenteENaoEErro()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);
            CaucaoSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.IdentificadorDispositivo = "   ";

            CaucaoResultado resultado = await gateway.PreAutorizarAsync(solicitacao);

            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(1, provedor.ChamadasPreAutorizar);
        }

        [Fact]
        public async Task PreAutorizar_ReservaCapturada_MensagemAoClienteNeutraELogaValorEReferenciaEnviada()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                RespostaPreAutorizar = Caucao(CaucaoStatus.CAPTURADA, 500m, 500m)
            };
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.DoesNotContain("estorn", excecao.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(log.Entries, entrada => entrada.Contains("500.00") && entrada.Contains(Referencia) && entrada.Contains("Estorno manual"));
        }

        [Fact]
        public async Task PreAutorizar_FalhaDeRede_LogaAReferenciaSemSegredos()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoPreAutorizar = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessageIntegration.ErroComunicacaoFalhaRede)
            };
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Contains(log.Entries, entrada => entrada.Contains(Referencia));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(TokenCartao));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(EmailPagador));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(CpfPagador));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(provedor.ChavesRecebidas[0]));
        }

        [Fact]
        public async Task ObterConfiguracaoCartao_PublicKeyIgualAoAccessToken_LancaSemDevolverAChave()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(publicKey: TokenSecreto), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterConfiguracaoCartaoAsync());

            Assert.Equal(ConstantsMessage.ErroCaucaoChavePublicaInvalida, excecao.Message);
            Assert.DoesNotContain(TokenSecreto, excecao.Message);
        }

        // ------------------------------------------------------------------
        // Rodada 2: chave publica contra qualquer valor protegido; reconsulta que falha no cancelamento; OCE no PUT.
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObterConfiguracaoCartao_PublicKeyIgualAoWebhookSecret_LancaSemDevolverAChave()
        {
            const string segredo = "whsec_segredo_123";
            Integration integration = CriarIntegracao(publicKey: segredo);
            integration.Settings.Add(new IntegrationSetting { Chave = "webhook_secret", Valor = segredo, Protegido = true });
            CaucaoGateway gateway = CriarGateway(integration, new FakeLogService(), new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterConfiguracaoCartaoAsync());

            Assert.Equal(ConstantsMessage.ErroCaucaoChavePublicaInvalida, excecao.Message);
            Assert.DoesNotContain(segredo, excecao.Message);
        }

        [Fact]
        public async Task CancelarAsync_ReconsultaFalha_RelancaAExcecaoOriginalELogaAFalhaDaReconsulta()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoCredenciaisRecusadas),
                ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorIndisponivel),
                ConsultasSemExcecao = 1
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            FakeLogService log = new FakeLogService();
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CancelarAsync(IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoCredenciaisRecusadas, excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCaucaoConsultar));
        }

        [Fact]
        public async Task CapturarAsync_OperationCanceledNoPut_PropagaOperationCanceled()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCaptura = new OperationCanceledException(cancelamento.Token)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);
            cancelamento.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.CapturarAsync(IdentificadorProvedor, 300m, cancelamento.Token));

            Assert.Equal(1, provedor.ChamadasCapturar);
        }

        [Fact]
        public async Task CancelarAsync_OperationCanceledNoPut_PropagaOperationCanceled()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago)
            {
                ExcecaoCancelamento = new OperationCanceledException(cancelamento.Token)
            };
            provedor.Consultas.Enqueue(Caucao(CaucaoStatus.RESERVADA, 500m));
            CaucaoGateway gateway = CriarGateway(CriarIntegracao(), new FakeLogService(), provedor);
            cancelamento.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.CancelarAsync(IdentificadorProvedor, cancelamento.Token));

            Assert.Equal(1, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task IntegracaoInexistente_LancaIntegrationException()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsCaucao.ProvedorMercadoPago);
            CaucaoGateway gateway = CriarGateway(null, new FakeLogService(), provedor);

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.PreAutorizarAsync(CriarSolicitacao()));

            Assert.Equal(0, provedor.ChamadasPreAutorizar);
        }
    }
}
