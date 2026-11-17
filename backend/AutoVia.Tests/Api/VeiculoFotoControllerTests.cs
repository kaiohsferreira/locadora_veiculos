using AutoVia.Api.Controllers;
using AutoVia.Api.Requests.VeiculoFoto;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Controller de fotos do veiculo: politica Funcionario na classe, teto de corpo, limite por IP so no envio, no-store,
    /// 201 com Location para a lista, e o formato de erro do perfil (relanca sem log as excecoes tipadas; registra o resto).
    /// </summary>
    public sealed class VeiculoFotoControllerTests
    {
        private const int IdVeiculo = 7;
        private const int IdFoto = 3;

        /// <summary>Servico falso: devolve a foto fixa ou lanca a excecao configurada.</summary>
        private sealed class ServicoFotoFalso : IVeiculoFotoService
        {
            public Exception? Falha { get; set; }

            /// <summary>Conteudo recebido pelo ultimo EnviarAsync (prova que o controller copia o arquivo para a memoria).</summary>
            public byte[]? UltimoConteudo { get; private set; }

            /// <summary>Content-Type recebido pelo ultimo EnviarAsync.</summary>
            public string? UltimoContentType { get; private set; }

            /// <summary>Token recebido pelo ultimo EnviarAsync (o controller repassa HttpContext.RequestAborted).</summary>
            public CancellationToken UltimoToken { get; private set; }

            public Task<ReturnVeiculoFotoVO> EnviarAsync(int idVeiculo, byte[]? conteudo, string? contentType, CancellationToken cancellationToken = default)
            {
                UltimoConteudo = conteudo;
                UltimoContentType = contentType;
                UltimoToken = cancellationToken;
                if (Falha != null) throw Falha;
                return Task.FromResult(new ReturnVeiculoFotoVO { IdVeiculoFoto = IdFoto, Ordem = 1, Url = "https://storage.example/foto", ExpiraEm = "2026-11-16T13:15:00.0000000+00:00" });
            }

            public Task<List<ReturnVeiculoFotoVO>> GetAllAsync(int idVeiculo, CancellationToken cancellationToken = default)
            {
                if (Falha != null) throw Falha;
                return Task.FromResult(new List<ReturnVeiculoFotoVO>());
            }

            public Task RemoverAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default)
            {
                if (Falha != null) throw Falha;
                return Task.CompletedTask;
            }
        }

        private static VeiculoFotoController CriarController(ServicoFotoFalso servico, FakeLogService log)
        {
            VeiculoFotoController controller = new VeiculoFotoController(servico, log);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        private static MethodInfo Acao(string nome)
        {
            return typeof(VeiculoFotoController).GetMethod(nome, BindingFlags.Public | BindingFlags.Instance)!;
        }

        [Fact]
        public void Classe_TemPoliticaFuncionarioENaoEhAnonima()
        {
            AuthorizeAttribute? autorizacao = typeof(VeiculoFotoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao.Policy);
            Assert.False(Attribute.IsDefined(typeof(VeiculoFotoController), typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute)));
        }

        [Fact]
        public void Enviar_TetoDeCorpoEhOTamanhoDaConstante()
        {
            RequestSizeLimitAttribute? tamanho = Acao(nameof(VeiculoFotoController.Enviar)).GetCustomAttribute<RequestSizeLimitAttribute>();

            Assert.NotNull(tamanho);
            IRequestSizeLimitMetadata metadado = tamanho;
            Assert.Equal<long?>(ConstantsValues.TamanhoMaximoCorpoFotoVeiculo, metadado.MaxRequestBodySize);
        }

        [Fact]
        public void LimiteDeIpSoNaAcaoEnviar_ComAPoliticaDoEnvio()
        {
            EnableRateLimitingAttribute? limiteEnviar = Acao(nameof(VeiculoFotoController.Enviar)).GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limiteEnviar);
            Assert.Equal(ConstantsValues.PoliticaLimiteEnvioFotoVeiculo, limiteEnviar.PolicyName);
            Assert.Null(typeof(VeiculoFotoController).GetCustomAttribute<EnableRateLimitingAttribute>());
            Assert.Null(Acao(nameof(VeiculoFotoController.GetAll)).GetCustomAttribute<EnableRateLimitingAttribute>());
            Assert.Null(Acao(nameof(VeiculoFotoController.Remover)).GetCustomAttribute<EnableRateLimitingAttribute>());
        }

        [Fact]
        public void EnviarEGetAll_TemNoStoreComUrlAssinada()
        {
            ResponseCacheAttribute? envio = Acao(nameof(VeiculoFotoController.Enviar)).GetCustomAttribute<ResponseCacheAttribute>();
            ResponseCacheAttribute? lista = Acao(nameof(VeiculoFotoController.GetAll)).GetCustomAttribute<ResponseCacheAttribute>();

            Assert.NotNull(envio);
            Assert.True(envio.NoStore);
            Assert.NotNull(lista);
            Assert.True(lista.NoStore);
        }

        [Fact]
        public void Enviar_EhPostMultipartERotaDeFotosDoVeiculo()
        {
            RouteAttribute? rota = typeof(VeiculoFotoController).GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(rota);
            Assert.Equal("api/veiculos/{idVeiculo:int}/fotos", rota.Template);
            Assert.NotNull(Acao(nameof(VeiculoFotoController.Enviar)).GetCustomAttribute<HttpPostAttribute>());
            Assert.NotNull(Acao(nameof(VeiculoFotoController.Enviar)).GetCustomAttribute<ConsumesAttribute>());
        }

        [Fact]
        public async Task Enviar_Sucesso_Devolve201ComLocationParaALista()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso();
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            IActionResult resultado = await controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest());

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(nameof(VeiculoFotoController.GetAll), criado.ActionName);
            Assert.True(criado.RouteValues?.TryGetValue("idVeiculo", out object? idNaRota) == true && Equals(idNaRota, IdVeiculo));
            Assert.IsType<ReturnVeiculoFotoVO>(criado.Value);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Enviar_RecusaDeNegocio_RelancaSemRegistrar()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso { Falha = new BusinessException(ConstantsMessage.ErroVeiculoFotoLimiteAtingido) };
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            await Assert.ThrowsAsync<BusinessException>(() => controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest()));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Enviar_RecusaDaImagem_RelancaSemRegistrar()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso { Falha = new FluentValidation.ValidationException(ConstantsMessage.ErroVeiculoFotoFormatoInvalido) };
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest()));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Enviar_FilaDoProcessadorCheia_RelancaSemRegistrar()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso { Falha = new LimiteExcedidoException(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado) };
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest()));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Enviar_CopiaOArquivoParaAMemoriaEPassaOTipoEOTokenDaRequisicao()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso();
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            controller.HttpContext.RequestAborted = cancelamento.Token;
            byte[] bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 };
            using MemoryStream fluxo = new MemoryStream(bytes);
            FormFile arquivo = new FormFile(fluxo, 0, bytes.Length, "foto", "original.png") { Headers = new HeaderDictionary(), ContentType = ConstantsArmazenamento.MimePng };

            IActionResult resultado = await controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest { Foto = arquivo });

            Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(bytes, servico.UltimoConteudo);
            Assert.Equal(ConstantsArmazenamento.MimePng, servico.UltimoContentType);
            Assert.Equal(cancelamento.Token, servico.UltimoToken);
        }

        [Fact]
        public async Task Enviar_FalhaInesperada_RegistraERelanca()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso { Falha = new InvalidOperationException("falha") };
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Enviar(IdVeiculo, new EnviarFotoVeiculoRequest()));

            Assert.Single(log.Entries);
            Assert.Contains(ConstantsMessage.ErroEnviarFotoVeiculo, log.Entries[0]);
        }

        [Fact]
        public async Task Remover_Sucesso_Devolve204()
        {
            VeiculoFotoController controller = CriarController(new ServicoFotoFalso(), new FakeLogService());

            IActionResult resultado = await controller.Remover(IdVeiculo, IdFoto);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task GetAll_Sucesso_Devolve200ComAListaSemRegistrar()
        {
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(new ServicoFotoFalso(), log);

            IActionResult resultado = await controller.GetAll(IdVeiculo);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.IsType<List<ReturnVeiculoFotoVO>>(ok.Value);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Remover_NaoEncontrada_RelancaSemRegistrar()
        {
            ServicoFotoFalso servico = new ServicoFotoFalso { Falha = new NotFoundException(ConstantsMessage.ErroVeiculoFotoNaoEncontrada) };
            FakeLogService log = new FakeLogService();
            VeiculoFotoController controller = CriarController(servico, log);

            await Assert.ThrowsAsync<NotFoundException>(() => controller.Remover(IdVeiculo, IdFoto));

            Assert.Empty(log.Entries);
        }
    }
}
