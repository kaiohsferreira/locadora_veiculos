using AutoVia.Api.Requests.Avaria;
using AutoVia.Api.Validators;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Api.Filters;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Tests.Avarias
{
    public sealed class CriarAvariaRequestValidatorTests
    {
        /// <summary>Caractere NUL na descricao: o PostgreSQL recusaria o texto (500). A recusa sai com 400, no campo Descricao, com ErroCampoInvalido.</summary>
        [Fact]
        public async Task DescricaoComCaractereNulo_EInvalidaNoCampoDescricao()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(new CriarAvariaRequest
            {
                Descricao = "Arra\0nhao na porta",
                Gravidade = "LEVE",
                Foto = CriarFoto("image/jpeg", new byte[] { 1 })
            });

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.ErrorMessage == string.Format(ConstantsMessage.ErroCampoInvalido, "Descricao"));
        }

        [Fact]
        public async Task GravidadeForaDaLista_EInvalida()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "CRITICA",
                Foto = CriarFoto("image/jpeg", new byte[] { 1 })
            });

            Assert.Contains(resultado.Errors, x => x.ErrorMessage == ConstantsMessage.ErroAvariaGravidadeInvalida);
        }

        [Fact]
        public async Task UploadComMimeInvalido_EInterceptadoComo400()
        {
            CriarAvariaRequest request = CriarRequest("text/plain", new byte[] { 1, 2, 3 });
            BadRequestObjectResult resultado = await ExecutarFiltroAsync(request, new CriarAvariaRequestValidator());

            Assert.IsType<ValidationProblemDetails>(resultado.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, resultado.StatusCode);
        }

        [Fact]
        public async Task RetryComAssinaturaInvalida_EInterceptadoComo400()
        {
            RetryAvariaFotoRequest request = new RetryAvariaFotoRequest { Foto = CriarFoto("image/jpeg", new byte[] { 1, 2, 3 }) };
            BadRequestObjectResult resultado = await ExecutarFiltroAsync(request, new RetryAvariaFotoRequestValidator());

            Assert.IsType<ValidationProblemDetails>(resultado.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, resultado.StatusCode);
        }

        [Fact]
        public async Task PostSemArquivo_EInterceptadoComo400()
        {
            CriarAvariaRequest request = new CriarAvariaRequest { Descricao = "Dano", Gravidade = "LEVE" };
            BadRequestObjectResult resultado = await ExecutarFiltroAsync(request, new CriarAvariaRequestValidator());

            Assert.Equal(StatusCodes.Status400BadRequest, resultado.StatusCode);
        }

        [Fact]
        public async Task RetryAcimaDoLimite_EInterceptadoComo400()
        {
            byte[] bytes = new byte[ConstantsArmazenamento.TamanhoMaximoFotoBytes + 1];
            RetryAvariaFotoRequest request = new RetryAvariaFotoRequest { Foto = CriarFoto("image/jpeg", bytes) };
            BadRequestObjectResult resultado = await ExecutarFiltroAsync(request, new RetryAvariaFotoRequestValidator());

            Assert.Equal(StatusCodes.Status400BadRequest, resultado.StatusCode);
        }

        [Fact]
        public async Task PostAcimaDoLimite_EInterceptadoComo400()
        {
            byte[] bytes = new byte[ConstantsArmazenamento.TamanhoMaximoFotoBytes + 1];
            bytes[0] = 0xFF;
            bytes[1] = 0xD8;
            bytes[2] = 0xFF;
            CriarAvariaRequest request = CriarRequest("image/jpeg", bytes);
            BadRequestObjectResult resultado = await ExecutarFiltroAsync(request, new CriarAvariaRequestValidator());

            Assert.Equal(StatusCodes.Status400BadRequest, resultado.StatusCode);
        }

        [Fact]
        public async Task JpegComBytesDePng_EReprovadoComAMensagemDeFormato()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            byte[] assinaturaPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFoto("image/jpeg", assinaturaPng)
            });

            Assert.Contains(resultado.Errors, x => x.ErrorMessage == ConstantsMessage.ErroAvariaFotoFormatoInvalido);
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("image/jpeg")]
        public async Task PdfNaFotoDaAvaria_EReprovadoComAMensagemDeFormato(string contentType)
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFoto(contentType, ConstantsValues.AssinaturaPdf)
            });

            Assert.Contains(resultado.Errors, x => x.ErrorMessage == ConstantsMessage.ErroAvariaFotoFormatoInvalido);
        }

        [Theory]
        [InlineData("frente")]
        [InlineData("DIANTEIRA")]
        [InlineData("0")]
        public async Task AnguloForaDaListaOuMinusculo_EInvalidoEmAngulo(string angulo)
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao(angulo, "10", "10"));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.PropertyName == "Angulo" && x.ErrorMessage == ConstantsMessage.ErroAvariaAnguloInvalido);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("100.01")]
        [InlineData("50,5")]
        [InlineData("1e2")]
        public async Task PosXForaDaFaixaOuDoFormato_EInvalidaEmPosX(string posX)
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao("FRENTE", posX, "10"));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.PropertyName == "PosX" && x.ErrorMessage == ConstantsMessage.ErroAvariaPosicaoInvalida);
        }

        [Fact]
        public async Task PosYForaDaFaixa_EInvalidaEmPosY()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao("FRENTE", "10", "100.01"));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.PropertyName == "PosY" && x.ErrorMessage == ConstantsMessage.ErroAvariaPosicaoInvalida);
        }

        [Fact]
        public async Task AnguloForaDaListaSemPosicao_TemExatamenteUmaMensagemEmAngulo()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao("DIANTEIRA", null, null));

            List<FluentValidation.Results.ValidationFailure> doAngulo = resultado.Errors.Where(x => x.PropertyName == "Angulo").ToList();
            Assert.Single(doAngulo);
            Assert.Equal(ConstantsMessage.ErroAvariaAnguloInvalido, doAngulo[0].ErrorMessage);
        }

        [Fact]
        public async Task SoPosXPreenchida_EParcialEAponta_EmAngulo()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao(null, "10", null));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.PropertyName == "Angulo" && x.ErrorMessage == ConstantsMessage.ErroAvariaPosicaoIncompleta);
        }

        [Fact]
        public async Task AnguloEPosYSemPosX_EParcial()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao("TRASEIRA", null, "40"));

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.ErrorMessage == ConstantsMessage.ErroAvariaPosicaoIncompleta);
        }

        [Fact]
        public async Task TresCamposVazios_SaoAceitos()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();
            FluentValidation.Results.ValidationResult resultado = await validator.ValidateAsync(CriarRequestComPosicao(null, null, null));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task AnguloEPosicoesNosLimites_SaoAceitos()
        {
            CriarAvariaRequestValidator validator = new CriarAvariaRequestValidator();

            FluentValidation.Results.ValidationResult zero = await validator.ValidateAsync(CriarRequestComPosicao("LATERAL_DIREITA", "0", "0"));
            FluentValidation.Results.ValidationResult cem = await validator.ValidateAsync(CriarRequestComPosicao("LATERAL_ESQUERDA", "100", "100.00"));
            FluentValidation.Results.ValidationResult meio = await validator.ValidateAsync(CriarRequestComPosicao("FRENTE", "42.50", "17"));

            Assert.True(zero.IsValid);
            Assert.True(cem.IsValid);
            Assert.True(meio.IsValid);
        }

        private static CriarAvariaRequest CriarRequestComPosicao(string? angulo, string? posX, string? posY)
        {
            return new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Angulo = angulo,
                PosX = posX,
                PosY = posY,
                Foto = CriarFoto("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })
            };
        }

        private static CriarAvariaRequest CriarRequest(string contentType, byte[] bytes)
        {
            return new CriarAvariaRequest
            {
                Descricao = "Dano",
                Gravidade = "LEVE",
                Foto = CriarFoto(contentType, bytes)
            };
        }

        private static FormFile CriarFoto(string contentType, byte[] bytes)
        {
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "foto", "foto.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static async Task<BadRequestObjectResult> ExecutarFiltroAsync<TRequest>(TRequest request, IValidator<TRequest> validator) where TRequest : class
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(typeof(IValidator<TRequest>), validator);
            ServiceProvider provider = services.BuildServiceProvider();
            DefaultHttpContext httpContext = new DefaultHttpContext { RequestServices = provider };
            ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            Dictionary<string, object?> arguments = new Dictionary<string, object?> { ["request"] = request };
            ActionExecutingContext context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, new object());
            FluentValidationActionFilter filter = new FluentValidationActionFilter(provider);

            await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException());

            return Assert.IsType<BadRequestObjectResult>(context.Result);
        }
    }
}
