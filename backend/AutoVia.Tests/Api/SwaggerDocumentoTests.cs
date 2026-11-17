using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Prova que o documento OpenAPI v1 continua sendo gerado com as rotas de avaria, inclusive as multipart (POST e PUT da foto).
    /// Resolve o provedor do Swagger no servidor de teste, sem subir o Swagger da Development.
    /// </summary>
    public sealed class SwaggerDocumentoTests
    {
        private const string NomeDocumento = "v1";
        private const string RotaAvarias = "/api/vistorias/{idVistoria}/avarias";
        private const string RotaFotoAvaria = "/api/vistorias/{idVistoria}/avarias/{idAvaria}/foto";
        private const string TipoMultipart = "multipart/form-data";
        private const string RotaFotosVeiculo = "/api/veiculos/{idVeiculo}/fotos";
        private const string RotaFotoVeiculo = "/api/veiculos/{idVeiculo}/fotos/{idFoto}";

        [Fact]
        public void Swagger_GeraAsRotasDeFotosDoVeiculo()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            ISwaggerProvider provedor = fabrica.Services.GetRequiredService<ISwaggerProvider>();

            OpenApiDocument documento = provedor.GetSwagger(NomeDocumento);

            Assert.NotNull(documento.Paths);
            Assert.True(documento.Paths.ContainsKey(RotaFotosVeiculo));
            Assert.True(documento.Paths.ContainsKey(RotaFotoVeiculo));
        }

        [Fact]
        public void Swagger_RotaDeEnvioDeFotoDeclaraMultipartFormData()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            ISwaggerProvider provedor = fabrica.Services.GetRequiredService<ISwaggerProvider>();

            OpenApiDocument documento = provedor.GetSwagger(NomeDocumento);

            Assert.True(DeclaraMultipart(documento, RotaFotosVeiculo, HttpMethod.Post));
        }

        [Fact]
        public void Swagger_GeraDocumentoV1ComAsRotasDeAvaria()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            ISwaggerProvider provedor = fabrica.Services.GetRequiredService<ISwaggerProvider>();

            OpenApiDocument documento = provedor.GetSwagger(NomeDocumento);

            Assert.NotNull(documento.Paths);
            Assert.True(documento.Paths.ContainsKey(RotaAvarias));
            Assert.True(documento.Paths.ContainsKey(RotaFotoAvaria));
        }

        [Fact]
        public void Swagger_RotasMultipartDeAvariaDeclaramMultipartFormData()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            ISwaggerProvider provedor = fabrica.Services.GetRequiredService<ISwaggerProvider>();

            OpenApiDocument documento = provedor.GetSwagger(NomeDocumento);

            Assert.True(DeclaraMultipart(documento, RotaAvarias, HttpMethod.Post));
            Assert.True(DeclaraMultipart(documento, RotaFotoAvaria, HttpMethod.Put));
        }

        /// <summary>Confere se a operacao da rota e do metodo tem corpo com o tipo multipart/form-data.</summary>
        private static bool DeclaraMultipart(OpenApiDocument documento, string rota, HttpMethod metodo)
        {
            if (documento.Paths == null) return false;
            if (!documento.Paths.TryGetValue(rota, out IOpenApiPathItem? itemDaRota) || itemDaRota == null) return false;
            if (itemDaRota.Operations == null || !itemDaRota.Operations.TryGetValue(metodo, out OpenApiOperation? operacao)) return false;
            if (operacao == null || operacao.RequestBody == null || operacao.RequestBody.Content == null) return false;

            return operacao.RequestBody.Content.ContainsKey(TipoMultipart);
        }
    }
}
