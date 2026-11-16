using AutoVia.Api.Controllers;
using AutoVia.Api.Requests.Avaria;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Avarias;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Reflection;

namespace AutoVia.Tests.Avarias
{
    public sealed class AvariaControllerTests
    {
        private const int IdVistoriaDoTeste = 4;
        private const int IdAvariaDoTeste = 1;
        private const string ChaveIdAvaria = "idAvaria";
        private const string ChaveRetryUploadUrl = "retryUploadUrl";

        [Fact]
        public async Task Criar_RetornaCreatedAtAction()
        {
            FakeAvariaControllerService servico = new FakeAvariaControllerService();
            AvariaController controller = CriarController(servico);

            IActionResult resultado = await controller.Criar(IdVistoriaDoTeste, new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFotoValida()
            });

            Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(1, servico.CriarCount);
        }

        [Fact]
        public async Task Criar_UploadFalhaRetorna502ComRetry()
        {
            FakeAvariaControllerService servico = new FakeAvariaControllerService { UploadFalha = true };
            AvariaController controller = CriarController(servico);

            IActionResult resultado = await controller.Criar(IdVistoriaDoTeste, new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFotoValida()
            });

            ObjectResult response = Assert.IsType<ObjectResult>(resultado);
            ProblemDetails detalhe = Assert.IsType<ProblemDetails>(response.Value);
            Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
            Assert.Equal(IdAvariaDoTeste, detalhe.Extensions[ChaveIdAvaria]);
        }

        [Fact]
        public async Task Criar_UploadFalhaCorpoTrazDetailERetryUploadUrlDoContrato()
        {
            FakeAvariaControllerService servico = new FakeAvariaControllerService { UploadFalha = true };
            AvariaController controller = CriarController(servico);

            IActionResult resultado = await controller.Criar(IdVistoriaDoTeste, new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFotoValida()
            });

            ProblemDetails detalhe = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(resultado).Value);
            string caminhoEsperado = string.Format(CultureInfo.InvariantCulture, ConstantsValues.FormatoRotaRetryFotoAvaria, IdVistoriaDoTeste, IdAvariaDoTeste);
            Assert.Equal(ConstantsMessage.ErroUploadAvariaPendente, detalhe.Detail);
            Assert.Equal(StatusCodes.Status502BadGateway, detalhe.Status);
            Assert.Equal(caminhoEsperado, detalhe.Extensions[ChaveRetryUploadUrl]);
            Assert.Equal("/api/vistorias/4/avarias/1/foto", detalhe.Extensions[ChaveRetryUploadUrl]);
        }

        [Fact]
        public async Task RetryUpload_UploadFalhaRetorna502ComIdAvariaERetryUploadUrl()
        {
            FakeAvariaControllerService servico = new FakeAvariaControllerService { UploadFalha = true };
            AvariaController controller = CriarController(servico);

            IActionResult resultado = await controller.RetryUpload(IdVistoriaDoTeste, IdAvariaDoTeste, new RetryAvariaFotoRequest { Foto = CriarFotoValida() });

            ObjectResult response = Assert.IsType<ObjectResult>(resultado);
            ProblemDetails detalhe = Assert.IsType<ProblemDetails>(response.Value);
            Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
            Assert.Equal(ConstantsMessage.ErroUploadAvariaPendente, detalhe.Detail);
            Assert.Equal(IdAvariaDoTeste, detalhe.Extensions[ChaveIdAvaria]);
            Assert.Equal("/api/vistorias/4/avarias/1/foto", detalhe.Extensions[ChaveRetryUploadUrl]);
            Assert.Equal(1, servico.RetryCount);
        }

        [Theory]
        [InlineData(nameof(AvariaController.Criar))]
        [InlineData(nameof(AvariaController.RetryUpload))]
        public void TetoDeCorpo_EhIgualAoContratoNasDuasActionsMultipart(string nomeAction)
        {
            MethodInfo? metodo = typeof(AvariaController).GetMethod(nomeAction, BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(metodo);
            RequestSizeLimitAttribute? tamanho = metodo.GetCustomAttribute<RequestSizeLimitAttribute>();
            Assert.NotNull(tamanho);
            IRequestSizeLimitMetadata metadadoTamanho = tamanho;
            Assert.Equal<long?>(ConstantsValues.TamanhoMaximoCorpoFotoAvaria, metadadoTamanho.MaxRequestBodySize);
        }

        private sealed class FakeAvariaControllerService : IAvariaService
        {
            public bool UploadFalha { get; set; }
            public int CriarCount { get; private set; }
            public int RetryCount { get; private set; }

            public Task<ReturnAvariaVO> CriarAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, byte[]? foto, string? contentType, CancellationToken cancellationToken = default)
            {
                CriarCount++;
                if (UploadFalha) throw new AvariaUploadException(idVistoria, IdAvariaDoTeste, ConstantsMessage.ErroUploadAvariaPendente, new InvalidOperationException());
                return Task.FromResult(new ReturnAvariaVO { IdAvaria = IdAvariaDoTeste, IdVistoria = idVistoria });
            }

            public Task<ReturnAvariaVO> GetByIdAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default) => Task.FromResult(new ReturnAvariaVO());

            public Task<ReturnAvariaVO> RetryUploadAsync(int idVistoria, int idAvaria, byte[] foto, string contentType, CancellationToken cancellationToken = default)
            {
                RetryCount++;
                if (UploadFalha) throw new AvariaUploadException(idVistoria, idAvaria, ConstantsMessage.ErroUploadAvariaPendente, new InvalidOperationException());
                return Task.FromResult(new ReturnAvariaVO { IdAvaria = idAvaria, IdVistoria = idVistoria });
            }

            public Task<ReturnAvariaVO> GetFotoUrlAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default) => Task.FromResult(new ReturnAvariaVO());
            public Task RemoverFotoAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private static IFormFile CriarFotoValida()
        {
            HeaderDictionary headers = new HeaderDictionary();
            FormFile foto = new FormFile(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), 0, 3, "foto", "foto.jpg")
            {
                Headers = headers,
                ContentType = "image/jpeg"
            };
            return foto;
        }

        private static AvariaController CriarController(IAvariaService servico)
        {
            AvariaController controller = new AvariaController(servico, new FakeLogService());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }
    }
}
