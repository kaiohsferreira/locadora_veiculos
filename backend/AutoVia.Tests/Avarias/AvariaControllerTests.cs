using AutoVia.Api.Controllers;
using AutoVia.Api.Requests.Avaria;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Avarias;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Tests.Avarias
{
    public sealed class AvariaControllerTests
    {
        [Fact]
        public async Task Criar_RetornaCreatedAtAction()
        {
            FakeAvariaControllerService servico = new FakeAvariaControllerService();
            AvariaController controller = CriarController(servico);

            IActionResult resultado = await controller.Criar(4, new CriarAvariaRequest
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

            IActionResult resultado = await controller.Criar(4, new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFotoValida()
            });

            ObjectResult response = Assert.IsType<ObjectResult>(resultado);
            ProblemDetails detalhe = Assert.IsType<ProblemDetails>(response.Value);
            Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
            Assert.Equal(1, detalhe.Extensions["idAvaria"]);
        }

        private sealed class FakeAvariaControllerService : IAvariaService
        {
            public bool UploadFalha { get; set; }
            public int CriarCount { get; private set; }

            public Task<ReturnAvariaVO> CriarAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, byte[]? foto, string? contentType, CancellationToken cancellationToken = default)
            {
                CriarCount++;
                if (UploadFalha) throw new AvariaUploadException(idVistoria, 1, ConstantsMessage.ErroUploadAvariaPendente, new InvalidOperationException());
                return Task.FromResult(new ReturnAvariaVO { IdAvaria = 1, IdVistoria = idVistoria });
            }

            public Task<ReturnAvariaVO> GetByIdAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default) => Task.FromResult(new ReturnAvariaVO());
            public Task<ReturnAvariaVO> RetryUploadAsync(int idVistoria, int idAvaria, byte[] foto, string contentType, CancellationToken cancellationToken = default) => Task.FromResult(new ReturnAvariaVO());
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
