using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Email;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Api
{
    /// <summary>POST real, com persistência simulada e fila, gateway e caixa local FAKE da aplicação.</summary>
    public sealed class ReservaEmailSandboxHttpTests
    {
        private const string RotaReserva = "/api/reservas";
        private const string CampoIdReserva = "idReserva";
        private const string CampoCodigo = "codigo";
        private const string CampoValorEstimado = "valorEstimado";
        private const string Destinatario = "titular.t-int-01@example.test";
        private const string Remetente = "autovia@example.test";
        private const string NomeRemetente = "AutoVia de teste";
        private const string UrlInutilizada = "https://email.example.test";
        private const string NomeLocadora = "Locadora de teste";
        private const string PrefixoPasta = "autovia-t-int-01-";
        private const int IdEmpresa = 21;
        private const int IdVeiculo = 39;
        private const int TimeoutIntegracaoMs = 1000;
        private const int EsperaCaixaMs = 20;
        private const int PrazoCaixaSegundos = 5;
        private static readonly DateOnly Retirada = new DateOnly(2026, 12, 10);
        private static readonly DateOnly Devolucao = new DateOnly(2026, 12, 15);
        private static readonly DateTime Agora = new DateTime(2026, 11, 20, 12, 0, 0);
        private const decimal ValorDiaria = 150m;
        private const string MensagemFalhaNotificacaoTeste = "Falha simulada na notificacao de teste, sem dados do titular.";

        [Fact]
        public async Task Post_CaixaSandboxRecebeConfirmacaoComMesmoCodigoQueO201()
        {
            DirectoryInfo raiz = Directory.CreateTempSubdirectory(PrefixoPasta);
            string pastaCaixa = Path.Combine(raiz.FullName, ConstantsEmail.PastaAppData, ConstantsEmail.PastaEmailFake);

            try
            {
                using FabricaEmailSandbox fabrica = new FabricaEmailSandbox(raiz.FullName, clienteAprovado: true);
                using HttpClient cliente = fabrica.CriarClienteAutenticado();
                using HttpResponseMessage resposta = await CriarReservaAsync(cliente);

                Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
                using JsonDocument corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
                string codigo = corpo.RootElement.GetProperty(CampoCodigo).GetString()!;
                decimal valorEstimado = corpo.RootElement.GetProperty(CampoValorEstimado).GetDecimal();
                Assert.False(string.IsNullOrWhiteSpace(codigo));
                Assert.NotNull(fabrica.Reservas.Armazenada);

                EmailFakeStore caixa = fabrica.Services.GetRequiredService<EmailFakeStore>();
                using CancellationTokenSource prazo = new CancellationTokenSource(TimeSpan.FromSeconds(PrazoCaixaSegundos));
                while (caixa.Quantidade == 0)
                {
                    await Task.Delay(EsperaCaixaMs, prazo.Token);
                }

                EmailFakeEnviado enviado = Assert.Single(caixa.Enviados());
                Assert.Equal(Destinatario, enviado.DestinatarioEmail);
                Assert.Contains(codigo, enviado.CorpoTexto, StringComparison.Ordinal);
                Assert.Contains(StaticMethods.FormateDateToPTBR(Retirada), enviado.CorpoTexto, StringComparison.Ordinal);
                Assert.Contains(StaticMethods.FormateDateToPTBR(Devolucao), enviado.CorpoTexto, StringComparison.Ordinal);
                Assert.Contains(StaticMethods.FormateToMonetary(valorEstimado), enviado.CorpoTexto, StringComparison.Ordinal);

                string arquivo = Assert.Single(Directory.GetFiles(pastaCaixa));
                string conteudo = await File.ReadAllTextAsync(arquivo);
                string enderecoNoArquivo = ConstantsEmail.AberturaEndereco + Destinatario + ConstantsEmail.FechamentoEndereco;
                Assert.Contains(ConstantsEmail.RotuloPara + enderecoNoArquivo, conteudo, StringComparison.Ordinal);
                Assert.Contains(codigo, conteudo, StringComparison.Ordinal);
            }
            finally
            {
                if (Directory.Exists(pastaCaixa))
                {
                    foreach (string arquivo in Directory.EnumerateFiles(pastaCaixa))
                    {
                        File.Delete(arquivo);
                    }

                    Directory.Delete(pastaCaixa);
                    Directory.Delete(Path.Combine(raiz.FullName, ConstantsEmail.PastaAppData));
                }

                raiz.Delete();
            }
        }

        [Fact]
        public async Task Post_ComFalhaNaNotificacao_PreservaReservaERegistraErroSemEmailDoTitular()
        {
            using FabricaEmailSandbox fabrica = new FabricaEmailSandbox(
                Path.GetTempPath(),
                clienteAprovado: true,
                notificacaoDeTeste: new NotificacaoComFalha());
            using HttpClient cliente = fabrica.CriarClienteAutenticado();
            using HttpResponseMessage resposta = await CriarReservaAsync(cliente);

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Equal(1, fabrica.Reservas.ChamadasDeInsercao);
            Reserva? armazenada = fabrica.Reservas.Armazenada;
            Assert.NotNull(armazenada);
            using JsonDocument corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(armazenada.IdReserva, corpo.RootElement.GetProperty(CampoIdReserva).GetInt32());

            List<string> entradas = fabrica.Log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.Contains(ConstantsMessage.ErroEmailNotificarReservaCriada, StringComparison.Ordinal)
                && entrada.Contains(MensagemFalhaNotificacaoTeste, StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains(Destinatario, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Post_ComFilaCheiaOuCadastroRecusado_PreservaResultadoDaReserva(bool clienteAprovado)
        {
            FakeEmailFila fila = new FakeEmailFila { Aceitar = false };
            using FabricaEmailSandbox fabrica = new FabricaEmailSandbox(Path.GetTempPath(), clienteAprovado, fila);
            using HttpClient cliente = fabrica.CriarClienteAutenticado();
            using HttpResponseMessage resposta = await CriarReservaAsync(cliente);

            Assert.Equal(clienteAprovado ? HttpStatusCode.Created : HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(clienteAprovado ? 1 : 0, fabrica.Reservas.ChamadasDeInsercao);
            Assert.Equal(clienteAprovado ? 1 : 0, fila.TotalChamadas);
            Assert.Empty(fila.Snapshot());
        }

        private static async Task<HttpResponseMessage> CriarReservaAsync(HttpClient cliente)
        {
            using JsonContent entrada = JsonContent.Create(new { idVeiculo = IdVeiculo, dataRetirada = Retirada, dataDevolucao = Devolucao });
            return await cliente.PostAsync(RotaReserva, entrada);
        }

        private sealed class FabricaEmailSandbox : AutenticacaoWebFactory
        {
            private readonly string _raizDaCaixa;
            private readonly FakeEmailFila? _filaDeTeste;
            private readonly INotificacaoTransacionalService? _notificacaoDeTeste;
            private readonly ArmazenamentoClienteLocadora _locadoras = new ArmazenamentoClienteLocadora();
            private readonly FakeIntegrationRepository _integracoes = new FakeIntegrationRepository();

            public FabricaEmailSandbox(
                string raizDaCaixa,
                bool clienteAprovado,
                FakeEmailFila? filaDeTeste = null,
                INotificacaoTransacionalService? notificacaoDeTeste = null)
            {
                _raizDaCaixa = raizDaCaixa;
                _filaDeTeste = filaDeTeste;
                _notificacaoDeTeste = notificacaoDeTeste;

                Cliente titular = Repositorio.Clientes[0];
                titular.Email = Destinatario;
                Empresa locadora = new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Ativo = true };
                _locadoras.AdicionarEmpresa(locadora);
                _locadoras.Semear(titular.IdCliente, IdEmpresa, clienteAprovado ? StatusCadastroCliente.APROVADO : StatusCadastroCliente.RECUSADO);
                Reservas.VincularVeiculo(new Veiculo
                {
                    IdVeiculo = IdVeiculo,
                    IdEmpresa = IdEmpresa,
                    Empresa = locadora,
                    Status = StatusVeiculo.DISPONIVEL,
                    ValorDiaria = ValorDiaria
                });
                Reservas.VincularEmailTitular(titular.IdCliente, Destinatario);
                _integracoes.Add(CriarIntegracaoSandbox());
            }

            public FakeReservaRepository Reservas { get; } = new FakeReservaRepository();

            public FakeLogService Log { get; } = new FakeLogService();

            public HttpClient CriarClienteAutenticado()
            {
                HttpClient cliente = CreateClient();
                using IServiceScope escopo = Services.CreateScope();
                ITokenService tokens = escopo.ServiceProvider.GetRequiredService<ITokenService>();
                Cliente titular = Repositorio.Clientes[0];
                string versao = tokens.CalcularVersaoSessao(HashSenhaCliente);
                string token = tokens.GerarToken(PerfilUsuario.CLIENTE, titular.IdCliente, titular.Nome, null, versao, false, null).Token;
                cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
                return cliente;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.UseEnvironment(Environments.Development);
                builder.ConfigureLogging(logs => logs.ClearProviders());
                builder.ConfigureTestServices(servicos =>
                {
                    servicos.Replace(ServiceDescriptor.Singleton<ILogService>(Log));
                    servicos.Replace(ServiceDescriptor.Singleton<IReservaRepository>(Reservas));
                    servicos.Replace(ServiceDescriptor.Scoped<IClienteLocadoraRepository>(provedor =>
                        new FakeClienteLocadoraRepository(provedor.GetRequiredService<IContextoEmpresa>(), _locadoras)));
                    servicos.Replace(ServiceDescriptor.Singleton<IIntegrationRepository>(_integracoes));
                    servicos.Replace(ServiceDescriptor.Singleton<IRelogio>(new FakeRelogio(Agora)));
                    servicos.Replace(ServiceDescriptor.Scoped<IEmailProvider>(provedor => new FakeEmailProvider(
                        provedor.GetRequiredService<EmailFakeStore>(),
                        new AmbienteCaixa(provedor.GetRequiredService<IHostEnvironment>(), _raizDaCaixa),
                        provedor.GetRequiredService<ILogService>())));

                    if (_filaDeTeste != null)
                    {
                        servicos.Replace(ServiceDescriptor.Singleton<IEmailFila>(_filaDeTeste));
                    }

                    if (_notificacaoDeTeste != null)
                    {
                        servicos.Replace(ServiceDescriptor.Singleton<INotificacaoTransacionalService>(_notificacaoDeTeste));
                    }
                });
            }

            private static Integration CriarIntegracaoSandbox()
            {
                return new Integration
                {
                    Codigo = ConstantsIntegration.Email,
                    Nome = NomeRemetente,
                    BaseUrl = UrlInutilizada,
                    Ambiente = AmbienteIntegracao.SANDBOX,
                    Ativo = true,
                    TimeoutMs = TimeoutIntegracaoMs,
                    Settings = new List<IntegrationSetting>
                    {
                        new IntegrationSetting { Chave = ConstantsEmail.ChaveProvedor, Valor = ConstantsEmail.ProvedorFake },
                        new IntegrationSetting { Chave = ConstantsEmail.ChaveRemetenteEmail, Valor = Remetente },
                        new IntegrationSetting { Chave = ConstantsEmail.ChaveRemetenteNome, Valor = NomeRemetente }
                    }
                };
            }
        }

        private sealed class NotificacaoComFalha : INotificacaoTransacionalService
        {
            public Task ConfirmarReservaAsync(ReservaCriadaEmail evento)
            {
                return Task.FromException(new InvalidOperationException(MensagemFalhaNotificacaoTeste));
            }

            public Task EmitirReciboDevolucaoAsync(LocacaoFinalizadaEmail evento) => Task.CompletedTask;

            public Task AvisarCobrancaAsync(CobrancaLancadaEmail evento) => Task.CompletedTask;
        }

        private sealed class AmbienteCaixa : IHostEnvironment
        {
            public AmbienteCaixa(IHostEnvironment original, string raiz)
            {
                ApplicationName = original.ApplicationName;
                EnvironmentName = original.EnvironmentName;
                ContentRootPath = raiz;
            }

            public string ApplicationName { get; set; }
            public string EnvironmentName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
