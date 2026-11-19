// Testes de integracao ponta a ponta do armazenamento em pasta (provedor DISCO_LOCAL): matriz de aceite MA-01 a MA-20 do
// contrato armazenamento-disco v1 (tarefa D5). Servidor de teste com a API real (controllers, gateway, provedor de disco,
// servico de leitura, limitador e ErrorHandlingMiddleware), pasta temporaria propria apagada no fim, sem banco, sem rede e sem
// variavel de ambiente do processo (a configuracao vai por AddInMemoryCollection; o caminho da maquina vai por construtor).
//
// FICAM DE FORA, dependentes de decisao do Kaio (nao fechar aqui): (2) a MA-19, foto de avaria com EXIF e GPS servida por link (DP-04).
// A parte da lista de fotos do veiculo da MA-07 foi decidida em 18/11/2026 (mostrar com erro): os casos L1 no fim deste arquivo
// provam a lista 200 com erroFoto por item e a remocao sem pasta (contrato veiculo-foto-arquivo-ausente).
// A MA-21 (cliente que encerra a conexao no meio do envio) e provada no teste do resultado (D3), por isso nao esta aqui.

using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Matriz de aceite do armazenamento em pasta, de ponta a ponta: envio pelo gateway e pela rota de foto, leitura pela rota
    /// publica com os mesmos bytes, links recusados (400 e 404 com o mesmo corpo), cache (ETag e 304), prazo em janela, limites
    /// (429 com registro agregado), segredo criado so no envio, lote e capa com arquivo sumido, troca de provedor e logs.
    /// </summary>
    public sealed class ArmazenamentoDiscoHttpTests
    {
        private const string ChaveFoto = "veiculos/1/fotos/capa-d5.jpg";
        private const string ChaveDocumentoCliente = "clientes/1/documentos/cnh-d5.pdf";
        private const string CabecalhoReferrerPolicy = "Referrer-Policy";
        private const string ChaveOutraFoto = "veiculos/1/fotos/outra-d5.jpg";
        private const string ChaveAvariaJpeg = "vistorias/3/avarias/5/foto";
        private const string ChaveAvariaPng = "vistorias/3/avarias/6/foto";
        private const string ChaveGif = "veiculos/1/fotos/gif-d5.jpg";
        private const string ChaveAusente = "veiculos/1/fotos/ausente-d5.jpg";
        private const string ChaveComPontoPonto = "veiculos/../segredo/assinatura.key";
        private const string ChaveDoSegredo = "segredo/assinatura.key";
        private const string MarcadorChave = "capa-d5";
        private const string DiretorioRelativo = "dados-relativos-d5";
        private const string RotaFotos = "/api/veiculos/1/fotos";
        private const string RotaCatalogo = "/api/veiculos/a-venda";
        private const string RotaDisponiveis = "/api/veiculos/disponiveis?dataRetirada=2026-12-10&dataDevolucao=2026-12-12";
        private const string CampoItens = "itens";
        private const string CampoFotoCapa = "fotoCapa";
        private const string CampoErroFotoCapa = "erroFotoCapa";
        private const string NomeCategoriaDeTeste = "SUV";
        private const string PlacaDeTeste = "ABC1D23";
        private const string ChassiDeTeste = "9BWZZZ377VT004251";
        private const string RenavamDeTeste = "12345678901";
        private const string MarcaDeTeste = "Jeep";
        private const string ModeloDeTeste = "Modelo";
        private const int IdCategoriaDeTeste = 1;
        private const int AnoDeTeste = 2022;
        private const int AnoModeloDeTeste = 2023;
        private const int KmDeTeste = 20000;
        private const int OrdemDaCapa = 1;
        private const int IdFotoDeTeste = 1;
        private const string ChaveSemArquivoL1 = "veiculos/1/fotos/sem-arquivo-l1.jpg";
        private const string ChaveDeOutroVeiculoL1 = "veiculos/2/fotos/outra-l1.jpg";
        private const string FormatoChaveDoTetoL1 = "veiculos/1/fotos/teto-l1-{0}.jpg";
        private const string CampoExpiraEm = "expiraEm";
        private const string CampoErroFoto = "erroFoto";
        private const int IdVeiculoDoTesteL1 = 1;
        private const int IdFuncionarioDoTesteL1 = 1;
        private const int IdDaFotoSemArquivoL1 = 1;
        private const int IdDaFotoComArquivoL1 = 2;
        private const int OrdemDaFotoSemArquivoL1 = 1;
        private const int OrdemDaFotoComArquivoL1 = 2;
        private const int PosicaoPrimeiraFotoL1 = 0;
        private const int PosicaoSegundaFotoL1 = 1;
        private const int QuantidadeDeDuasFotosL1 = 2;
        private const int QuantidadeSemItensL1 = 0;
        private const decimal ValorDiariaDeTeste = 200m;
        private const decimal ValorVendaDeTeste = 89900m;
        private const string RotaFotoRemover = "/api/veiculos/1/fotos/{0}";
        private const string RotaLogin = "/api/auth/login";
        private const string RotaSaude = "/api/health";
        private const string CampoFoto = "Foto";
        private const string NomeArquivoFoto = "foto.jpg";
        private const string PerfilFuncionario = "FUNCIONARIO";
        private const string CpfFuncionario = "11144477735";
        private const string SenhaFuncionario = "SenhaFunc#2026";
        private const string CampoToken = "token";
        private const string CampoUrl = "url";
        private const string CampoIdFoto = "idVeiculoFoto";
        private const string CampoTitle = "title";
        private const string CampoDetail = "detail";
        private const string CabecalhoIp = "X-Forwarded-For";
        private const string CabecalhoIfNoneMatch = "If-None-Match";
        private const string CabecalhoXContentTypeOptions = "X-Content-Type-Options";
        private const string CabecalhoContentSecurityPolicy = "Content-Security-Policy";
        private const string CabecalhoContentDisposition = "Content-Disposition";
        private const string IpA = "198.51.100.10";
        private const string IpB = "198.51.100.20";
        private const string IpC = "198.51.100.30";
        private const string MarcadorEtag = "{ETAG}";
        private const string ValorOutroEtag = "\"outro\"";
        private const string PrefixoFraco = "W/";
        private const string ValorCuringa = "*";
        private const string OrigemLocal = "http://localhost";
        private const string PrefixoLinkLeitura = OrigemLocal + "/" + ConstantsValues.RotaLeituraArquivoArmazenado + "?";
        private const string TipoProblema = "application/problem+json";
        private const string CabecalhoGif = "GIF89a";
        private const string SigSubstituto = "A";
        private const string SigSubstitutoAlternativo = "B";
        private const string SinalMais = "+";
        private const string Espaco = " ";
        private const string QuebraDeLinha = "\n";
        private const string SufixoLF = "\n";
        private const string SufixoCRLF = "\r\n";
        private const string SufixoTabulacao = "\t";
        private const string ParametroSinal = "sig";
        private const int TamanhoFoto = 64;
        private const int TamanhoFotoNova = 100;
        private const int SegundosDeFolga = 100;
        private const int SegundosDeUmSegundo = 1;
        private const int SegundosDeTrintaSegundos = 30;
        private const int PrazoConfigurado = 60;
        private const int LimiteIpDois = 2;
        private const int TetoDois = 2;
        private const int TetoCinco = 5;
        private const int ChamadasDoIpRecusado = 10;
        private const int ChamadasComSucessoDoIpRecusado = 2;
        private const int ChamadasDoIpB = 2;
        private const int UmRegistro = 1;
        private const int DuasRecusas = 2;
        private const int ZeroRegistros = 0;
        private const int TamanhoSegredo = ConstantsValues.TamanhoSegredoAssinaturaBytes;
        private const int JanelaPadraoSegundos = ConstantsArmazenamento.ExpiracaoMaximaSegundos / ConstantsValues.DivisorJanelaLinkArmazenamento;
        private const int CaracteresDaAssinatura = ConstantsValues.TamanhoAssinaturaLinkCaracteres;
        private const int TamanhoChaveAcimaDoLimite = ConstantsValues.TamanhoMaximoChaveArmazenamento + 1;
        private const int TamanhoSigCurta = CaracteresDaAssinatura - 1;
        private const int PosicaoPrimeiroCaractereDaSig = 0;
        private const int UmCaractere = 1;
        private const int CasoExpComZeroAEsquerda = 0;
        private const int CasoExpComLetra = 1;
        private const int CasoExpComOnzeDigitos = 2;
        private const int CasoExpComEspacoNoFim = 3;
        private const int CasoExpComLF = 4;
        private const int CasoSigComQuarentaEDois = 5;
        private const int CasoSigComQuarentaEQuatro = 6;
        private const int CasoSigComCaractereForaDoAlfabeto = 7;
        private const int CasoSigComLF = 8;
        private const int CasoSigComQuarentaEDoisELF = 9;
        private const int CasoChaveComDuzentosEUm = 10;
        private const int CasoExpRepetido = 11;
        private const int CasoChaveAusente = 12;
        private const int CasoSigAusente = 13;
        private const int ValorDeExpVencido = 1;
        private const int ValorAcimaDoTetoAdicional = 1;
        private const int ValorDeDiferencaDoExp = 1;

        /// <summary>Corpo de problema lido do ProblemDetails: so title e detail, que sao o que o contrato compara.</summary>
        private sealed record ProblemaRecebido(string Title, string Detail);

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-01 e MA-05: envio e remocao pelas rotas de foto (funcionario autenticado), leitura pela url devolvida
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA01_EnvioPelaRotaDeFotoEGetDaUrlDevolveOsMesmosBytes()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { UrlPublicaApi = string.Empty });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);

            using HttpResponseMessage envio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));

            Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
            string corpo = await envio.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(corpo);
            string url = documento.RootElement.GetProperty(CampoUrl).GetString() ?? string.Empty;
            Assert.StartsWith(PrefixoLinkLeitura, url);

            string arquivo = Assert.Single(Directory.GetFiles(CaminhoDoArquivo(fabrica.Raiz, "veiculos/1/fotos")));
            byte[] gravado = await File.ReadAllBytesAsync(arquivo);
            Assert.Equal(FakeProcessadorFotoService.ConteudoRecodificado, gravado);

            using HttpResponseMessage leitura = await cliente.GetAsync(CaminhoDe(url));

            Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, leitura.Content.Headers.ContentType?.MediaType);
            Assert.Equal(gravado, await leitura.Content.ReadAsByteArrayAsync());
        }

        [Fact]
        public async Task MA05_RemoverApagaOArquivoELeituraSeguinteDa404()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { UrlPublicaApi = string.Empty });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            using HttpResponseMessage envio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));
            Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await envio.Content.ReadAsStringAsync());
            string url = documento.RootElement.GetProperty(CampoUrl).GetString() ?? string.Empty;
            int idFoto = documento.RootElement.GetProperty(CampoIdFoto).GetInt32();
            string arquivo = Assert.Single(Directory.GetFiles(CaminhoDoArquivo(fabrica.Raiz, "veiculos/1/fotos")));

            using HttpRequestMessage remocao = new HttpRequestMessage(HttpMethod.Delete, string.Format(CultureInfo.InvariantCulture, RotaFotoRemover, idFoto));
            remocao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            using HttpResponseMessage respostaRemocao = await cliente.SendAsync(remocao);

            Assert.Equal(HttpStatusCode.NoContent, respostaRemocao.StatusCode);
            Assert.False(File.Exists(arquivo));

            using HttpResponseMessage leitura = await cliente.GetAsync(CaminhoDe(url));
            Assert.Equal(HttpStatusCode.NotFound, leitura.StatusCode);

            using HttpRequestMessage repetida = new HttpRequestMessage(HttpMethod.Delete, string.Format(CultureInfo.InvariantCulture, RotaFotoRemover, idFoto));
            repetida.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            using HttpResponseMessage respostaRepetida = await cliente.SendAsync(repetida);
            Assert.Equal(HttpStatusCode.NotFound, respostaRepetida.StatusCode);
            Assert.Equal(ZeroRegistros, fabrica.Log.Contar(ConstantsMessage.ErroArmazenamentoRemover));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-06: troca de provedor. Com DISCO_LOCAL, capa e avaria abrem e removem; com S3_COMPATIVEL o gateway chama o S3.
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA06_DiscoLocalNaoQuebraCapaNemAvarias()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);

            IReadOnlyDictionary<string, ArquivoArmazenado> capas = await AssinarAsync(fabrica, ChaveFoto);
            Assert.True(capas.ContainsKey(ChaveFoto));
            using HttpResponseMessage respostaCapa = await cliente.GetAsync(CaminhoDe(capas[ChaveFoto].Url));
            Assert.Equal(HttpStatusCode.OK, respostaCapa.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, respostaCapa.Content.Headers.ContentType?.MediaType);

            await EnviarAsync(fabrica, ChaveAvariaJpeg, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            await EnviarAsync(fabrica, ChaveAvariaPng, ConteudoPng(TamanhoFoto), ConstantsArmazenamento.MimePng);

            ArquivoArmazenado? urlJpeg = await ObterUrlAsync(fabrica, ChaveAvariaJpeg);
            ArquivoArmazenado? urlPng = await ObterUrlAsync(fabrica, ChaveAvariaPng);
            Assert.NotNull(urlJpeg);
            Assert.NotNull(urlPng);

            using HttpResponseMessage respostaJpeg = await cliente.GetAsync(CaminhoDe(urlJpeg!.Url));
            Assert.Equal(HttpStatusCode.OK, respostaJpeg.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, respostaJpeg.Content.Headers.ContentType?.MediaType);

            using HttpResponseMessage respostaPng = await cliente.GetAsync(CaminhoDe(urlPng!.Url));
            Assert.Equal(HttpStatusCode.OK, respostaPng.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimePng, respostaPng.Content.Headers.ContentType?.MediaType);

            await RemoverAsync(fabrica, ChaveAvariaPng);
            using HttpResponseMessage depoisDaRemocao = await cliente.GetAsync(CaminhoDe(urlPng.Url));
            Assert.Equal(HttpStatusCode.NotFound, depoisDaRemocao.StatusCode);
        }

        [Fact]
        public async Task MA06_ComS3CompativelOGatewayContinuaChamandoOProvedorS3()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco
            {
                Provedor = ConstantsArmazenamento.ProvedorS3Compativel,
                ProvedorS3Falso = true
            });
            fabrica.CreateClient();

            ArquivoArmazenado? url = await ObterUrlAsync(fabrica, ChaveFoto);

            Assert.NotNull(url);
            Assert.Equal(UmRegistro, fabrica.S3.Chamadas);
            Assert.False(Directory.Exists(fabrica.Raiz));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-02, MA-03, MA-04: prazo vencido, assinatura e prazo adulterados, chave com ".."
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA02_PrazoVencidoDa404()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            ArquivoArmazenado? url = await ObterUrlAsync(fabrica, ChaveFoto);
            Assert.NotNull(url);
            long exp = url!.ExpiraEm.ToUnixTimeSeconds();
            long agora = fabrica.Relogio.GetUtcNow().ToUnixTimeSeconds();

            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(exp - agora - SegundosDeUmSegundo));
            using HttpResponseMessage antesDoVencimento = await cliente.GetAsync(CaminhoDe(url.Url));
            Assert.Equal(HttpStatusCode.OK, antesDoVencimento.StatusCode);

            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(SegundosDeUmSegundo));
            using HttpResponseMessage vencido = await cliente.GetAsync(CaminhoDe(url.Url));
            Assert.Equal(HttpStatusCode.NotFound, vencido.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(vencido);
            Assert.Equal(ConstantsMessage.TituloRecursoNaoEncontrado, problema.Title);
            Assert.Equal(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, problema.Detail);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoLeituraPrazoVencido));
        }

        [Fact]
        public async Task MA03_AssinaturaAdulteradaEExpAlteradoDa404()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;
            string sig = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, exp);

            using HttpResponseMessage controle = await cliente.GetAsync(LinkDe(ChaveFoto, exp, sig));
            Assert.Equal(HttpStatusCode.OK, controle.StatusCode);

            string sigAdulterada = (sig[PosicaoPrimeiroCaractereDaSig] == SigSubstituto[0] ? SigSubstitutoAlternativo : SigSubstituto) + sig.Substring(PosicaoPrimeiroCaractereDaSig + UmCaractere);
            using HttpResponseMessage sigTrocada = await cliente.GetAsync(LinkDe(ChaveFoto, exp, sigAdulterada));
            Assert.Equal(HttpStatusCode.NotFound, sigTrocada.StatusCode);

            using HttpResponseMessage expTrocado = await cliente.GetAsync(LinkDe(ChaveFoto, exp - ValorDeDiferencaDoExp, sig));
            Assert.Equal(HttpStatusCode.NotFound, expTrocado.StatusCode);
            Assert.Equal(DuasRecusas, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoLeituraAssinaturaInvalida));
        }

        [Fact]
        public async Task MA04_ChaveComPontoPontoDa404MesmoComAssinaturaValida()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;

            string assinaturaDaChave = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveComPontoPonto, exp);
            using HttpResponseMessage comAssinaturaValida = await cliente.GetAsync(LinkDe(ChaveComPontoPonto, exp, assinaturaDaChave));
            Assert.Equal(HttpStatusCode.NotFound, comAssinaturaValida.StatusCode);

            using HttpResponseMessage comAssinaturaQualquer = await cliente.GetAsync(LinkDe(ChaveComPontoPonto, exp, new string(SigSubstituto[0], CaracteresDaAssinatura)));
            Assert.Equal(HttpStatusCode.NotFound, comAssinaturaQualquer.StatusCode);

            string assinaturaDoSegredo = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveDoSegredo, exp);
            using HttpResponseMessage segredoServido = await cliente.GetAsync(LinkDe(ChaveDoSegredo, exp, assinaturaDoSegredo));
            Assert.Equal(HttpStatusCode.NotFound, segredoServido.StatusCode);

            Assert.True(File.Exists(CaminhoDoSegredo(fabrica.Raiz)));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-08, MA-10 e a variacao do ETag: 304 sem corpo, 200 com bytes novos depois de gravar de novo, cabecalhos da resposta
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA08_EtagDa304ESegundaGravacaoDa200ComBytesNovos()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;

            using HttpResponseMessage primeira = await cliente.GetAsync(CaminhoDe(url));
            Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
            System.Net.Http.Headers.EntityTagHeaderValue? etag = primeira.Headers.ETag;
            Assert.NotNull(etag);

            using HttpRequestMessage condicional = new HttpRequestMessage(HttpMethod.Get, CaminhoDe(url));
            condicional.Headers.IfNoneMatch.Add(etag!);
            using HttpResponseMessage naoModificada = await cliente.SendAsync(condicional);
            Assert.Equal(HttpStatusCode.NotModified, naoModificada.StatusCode);
            Assert.Empty(await naoModificada.Content.ReadAsByteArrayAsync());

            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFotoNova), ConstantsArmazenamento.MimeJpeg);
            using HttpRequestMessage depoisDeGravarDeNovo = new HttpRequestMessage(HttpMethod.Get, CaminhoDe(url));
            depoisDeGravarDeNovo.Headers.IfNoneMatch.Add(etag!);
            using HttpResponseMessage novo = await cliente.SendAsync(depoisDeGravarDeNovo);

            Assert.Equal(HttpStatusCode.OK, novo.StatusCode);
            Assert.Equal(ConteudoJpeg(TamanhoFotoNova), await novo.Content.ReadAsByteArrayAsync());
        }

        [Theory]
        [InlineData(MarcadorEtag, HttpStatusCode.NotModified)]
        [InlineData(PrefixoFraco + MarcadorEtag, HttpStatusCode.NotModified)]
        [InlineData(ValorOutroEtag + ", " + MarcadorEtag, HttpStatusCode.NotModified)]
        [InlineData(ValorOutroEtag, HttpStatusCode.OK)]
        [InlineData(ValorCuringa, HttpStatusCode.OK)]
        public async Task MA08_IfNoneMatchSoDa304QuandoUmValorConfere(string modelo, HttpStatusCode esperado)
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            using HttpResponseMessage primeira = await cliente.GetAsync(CaminhoDe(url));
            string etagTexto = primeira.Headers.ETag!.Tag;

            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, CaminhoDe(url));
            requisicao.Headers.TryAddWithoutValidation(CabecalhoIfNoneMatch, modelo.Replace(MarcadorEtag, etagTexto));
            using HttpResponseMessage resposta = await cliente.SendAsync(requisicao);

            Assert.Equal(esperado, resposta.StatusCode);
        }

        [Fact]
        public async Task MA10_LeituraSemAuthorizationTemOsCabecalhosDeSeguranca()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;

            using HttpResponseMessage resposta = await cliente.GetAsync(CaminhoDe(url));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsValues.ValorNaoFarejarTipo, ValorDoCabecalho(resposta, CabecalhoXContentTypeOptions));
            Assert.Equal(ConstantsValues.PoliticaConteudoLeituraArquivo, ValorDoCabecalho(resposta, CabecalhoContentSecurityPolicy));
            Assert.Equal(ConstantsArmazenamento.ContentDispositionInline, ValorDoCabecalho(resposta, CabecalhoContentDisposition));
            Assert.True(resposta.Headers.CacheControl?.Private);
            Assert.True(resposta.Headers.CacheControl?.NoCache);
            Assert.Equal(ConstantsValues.ValorReferrerPolicySemReferencia, ValorDoCabecalho(resposta, CabecalhoReferrerPolicy));
        }

        [Fact]
        public async Task PDF01_DocumentoDoClienteBaixaComNomeFixoENoStore()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveDocumentoCliente, ConteudoComCabecalho(ConstantsValues.AssinaturaPdf, TamanhoFoto), ConstantsValues.TipoConteudoPdf);
            string url = (await ObterUrlAsync(fabrica, ChaveDocumentoCliente))!.Url;

            using HttpResponseMessage resposta = await cliente.GetAsync(CaminhoDe(url));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(ConstantsValues.TipoConteudoPdf, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConstantsValues.ContentDispositionAnexoPdf, ValorDoCabecalho(resposta, CabecalhoContentDisposition));
            Assert.Equal(ConstantsValues.ValorNaoFarejarTipo, ValorDoCabecalho(resposta, CabecalhoXContentTypeOptions));
            Assert.Equal(ConstantsValues.ValorReferrerPolicySemReferencia, ValorDoCabecalho(resposta, CabecalhoReferrerPolicy));
            Assert.True(resposta.Headers.CacheControl?.NoStore);
            Assert.Equal(ConteudoComCabecalho(ConstantsValues.AssinaturaPdf, TamanhoFoto), await resposta.Content.ReadAsByteArrayAsync());
        }

        [Fact]
        public async Task PDF02_DocumentoDoClienteComEtagDa304ComNoStoreEReferrer()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveDocumentoCliente, ConteudoComCabecalho(ConstantsValues.AssinaturaPdf, TamanhoFoto), ConstantsValues.TipoConteudoPdf);
            string url = (await ObterUrlAsync(fabrica, ChaveDocumentoCliente))!.Url;
            using HttpResponseMessage primeira = await cliente.GetAsync(CaminhoDe(url));
            System.Net.Http.Headers.EntityTagHeaderValue etag = primeira.Headers.ETag!;

            using HttpRequestMessage condicional = new HttpRequestMessage(HttpMethod.Get, CaminhoDe(url));
            condicional.Headers.IfNoneMatch.Add(etag);
            using HttpResponseMessage naoModificado = await cliente.SendAsync(condicional);

            Assert.Equal(HttpStatusCode.NotModified, naoModificado.StatusCode);
            Assert.True(naoModificado.Headers.CacheControl?.NoStore);
            Assert.Equal(ConstantsValues.ValorReferrerPolicySemReferencia, ValorDoCabecalho(naoModificado, CabecalhoReferrerPolicy));
        }

        [Fact]
        public async Task PDF03_FotoDeVeiculoNaoGanhaNoStoreEA304ComPrivateNoCache()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            using HttpResponseMessage primeira = await cliente.GetAsync(CaminhoDe(url));

            Assert.False(primeira.Headers.CacheControl?.NoStore ?? false);
            Assert.True(primeira.Headers.CacheControl?.Private);
            Assert.True(primeira.Headers.CacheControl?.NoCache);

            using HttpRequestMessage condicional = new HttpRequestMessage(HttpMethod.Get, CaminhoDe(url));
            condicional.Headers.IfNoneMatch.Add(primeira.Headers.ETag!);
            using HttpResponseMessage naoModificado = await cliente.SendAsync(condicional);

            Assert.Equal(HttpStatusCode.NotModified, naoModificado.StatusCode);
            Assert.True(naoModificado.Headers.CacheControl?.Private);
            Assert.True(naoModificado.Headers.CacheControl?.NoCache);
            Assert.False(naoModificado.Headers.CacheControl?.NoStore ?? false);
        }

        [Fact]
        public async Task PDF04_RespostasDeErroDaRotaTambemTemReferrerPolicy()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;
            string sigInvalida = new string(SigSubstituto[0], CaracteresDaAssinatura);

            using HttpResponseMessage malformado = await cliente.GetAsync(PedidoMalformado(CasoExpComZeroAEsquerda, segredo, exp));
            using HttpResponseMessage naoEncontrado = await cliente.GetAsync(LinkDe(ChaveFoto, exp, sigInvalida));

            Assert.Equal(HttpStatusCode.BadRequest, malformado.StatusCode);
            Assert.Equal(ConstantsValues.ValorReferrerPolicySemReferencia, ValorDoCabecalho(malformado, CabecalhoReferrerPolicy));
            Assert.Equal(HttpStatusCode.NotFound, naoEncontrado.StatusCode);
            Assert.Equal(ConstantsValues.ValorReferrerPolicySemReferencia, ValorDoCabecalho(naoEncontrado, CabecalhoReferrerPolicy));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-09 e MA-14: prazo em janela (mesma URL dentro da janela, outra na seguinte) e prazo configurado de 60 segundos
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA09_DuasAssinaturasNaMesmaJanelaDaMesmaUrlEOutraNaSeguinte()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            long restoDaJanela = UnixDe(fabrica.Relogio.GetUtcNow()) % JanelaPadraoSegundos;
            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(JanelaPadraoSegundos - restoDaJanela));

            string primeira = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(SegundosDeFolga));
            string segunda = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(JanelaPadraoSegundos));
            string janelaSeguinte = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;

            Assert.Equal(primeira, segunda);
            Assert.NotEqual(primeira, janelaSeguinte);
        }

        [Fact]
        public async Task MA14_PrazoConfiguradoDe60SegundosDaLinkDe31A60()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { Expiracao = PrazoConfigurado.ToString(CultureInfo.InvariantCulture) });
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            ArquivoArmazenado? url = await ObterUrlAsync(fabrica, ChaveFoto);
            Assert.NotNull(url);
            long emissao = UnixDe(fabrica.Relogio.GetUtcNow());
            long vidaDoLink = url!.ExpiraEm.ToUnixTimeSeconds() - emissao;
            Assert.InRange(vidaDoLink, PrazoConfigurado / ConstantsValues.DivisorJanelaLinkArmazenamento + SegundosDeUmSegundo, PrazoConfigurado);

            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(SegundosDeTrintaSegundos));
            using HttpResponseMessage aos30 = await cliente.GetAsync(CaminhoDe(url.Url));
            Assert.Equal(HttpStatusCode.OK, aos30.StatusCode);

            fabrica.Relogio.Avancar(TimeSpan.FromSeconds(SegundosDeTrintaSegundos));
            using HttpResponseMessage aos60 = await cliente.GetAsync(CaminhoDe(url.Url));
            Assert.Equal(HttpStatusCode.NotFound, aos60.StatusCode);
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-11 e MA-15: 429 pelo limite por IP e pelo teto da rota (mesmo corpo, registro agregado); IP recusado nao gasta teto
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA11_LimiteDoIpEDoTetoDao429ComMesmoCorpoERegistro()
        {
            using FabricaArmazenamentoDisco porIp = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { LimiteIp = LimiteIpDois });
            using HttpClient clientePorIp = porIp.CreateClient();
            await EnviarAsync(porIp, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string urlPorIp = (await ObterUrlAsync(porIp, ChaveFoto))!.Url;
            using HttpResponseMessage primeiraIp = await PedirComIpAsync(clientePorIp, IpA, CaminhoDe(urlPorIp));
            using HttpResponseMessage segundaIp = await PedirComIpAsync(clientePorIp, IpA, CaminhoDe(urlPorIp));
            using HttpResponseMessage terceiraIp = await PedirComIpAsync(clientePorIp, IpA, CaminhoDe(urlPorIp));
            Assert.Equal(HttpStatusCode.OK, primeiraIp.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segundaIp.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceiraIp.StatusCode);
            ProblemaRecebido problemaIp = await LerProblemaAsync(terceiraIp);
            Assert.Equal(TipoProblema, terceiraIp.Content.Headers.ContentType?.MediaType);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraLimitePorIp, UmRegistro), porIp.Log.Mensagens());

            using FabricaArmazenamentoDisco porTeto = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { Teto = TetoDois });
            using HttpClient clienteTeto = porTeto.CreateClient();
            await EnviarAsync(porTeto, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string urlTeto = (await ObterUrlAsync(porTeto, ChaveFoto))!.Url;
            using HttpResponseMessage primeiraTeto = await PedirComIpAsync(clienteTeto, IpA, CaminhoDe(urlTeto));
            using HttpResponseMessage segundaTeto = await PedirComIpAsync(clienteTeto, IpB, CaminhoDe(urlTeto));
            using HttpResponseMessage terceiraTeto = await PedirComIpAsync(clienteTeto, IpC, CaminhoDe(urlTeto));
            Assert.Equal(HttpStatusCode.OK, primeiraTeto.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segundaTeto.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceiraTeto.StatusCode);
            ProblemaRecebido problemaTeto = await LerProblemaAsync(terceiraTeto);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota, UmRegistro), porTeto.Log.Mensagens());

            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, problemaIp.Title);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, problemaIp.Detail);
            Assert.Equal(problemaIp, problemaTeto);
        }

        [Fact]
        public async Task MA15_IpRecusadoNaoGastaOTeto()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { LimiteIp = LimiteIpDois, Teto = TetoCinco });
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            string caminho = CaminhoDe(url);

            int comSucessoDoIpA = 0;
            int recusadosDoIpA = 0;
            for (int chamada = 0; chamada < ChamadasDoIpRecusado; chamada++)
            {
                using HttpResponseMessage resposta = await PedirComIpAsync(cliente, IpA, caminho);
                if (resposta.StatusCode == HttpStatusCode.OK)
                {
                    comSucessoDoIpA++;
                }
                else if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    recusadosDoIpA++;
                }
            }

            Assert.Equal(ChamadasComSucessoDoIpRecusado, comSucessoDoIpA);
            Assert.Equal(ChamadasDoIpRecusado - ChamadasComSucessoDoIpRecusado, recusadosDoIpA);

            for (int chamada = 0; chamada < ChamadasDoIpB; chamada++)
            {
                using HttpResponseMessage respostaB = await PedirComIpAsync(cliente, IpB, caminho);
                Assert.Equal(HttpStatusCode.OK, respostaB.StatusCode);
            }

            using HttpResponseMessage primeiraDoIpC = await PedirComIpAsync(cliente, IpC, caminho);
            Assert.Equal(HttpStatusCode.OK, primeiraDoIpC.StatusCode);

            using HttpResponseMessage segundaDoIpC = await PedirComIpAsync(cliente, IpC, caminho);
            Assert.Equal(HttpStatusCode.TooManyRequests, segundaDoIpC.StatusCode);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota, UmRegistro), fabrica.Log.Mensagens());
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-12: sem chave de armazenamento no Windows fora de container grava na pasta de dados; em container sem pasta, 502
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA12_SemChaveDeArmazenamentoNoWindowsGravaNaPastaDeDadosDoUsuario()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco
            {
                PastaDadosUsuarioNoTeste = true,
                UrlPublicaApi = string.Empty
            });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);

            using HttpResponseMessage envio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));

            Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
            Assert.Single(Directory.GetFiles(CaminhoDoArquivo(fabrica.Raiz, "veiculos/1/fotos")));
        }

        [Fact]
        public async Task MA12_EmContainerSemPastaDa502ComMotivoNoLogENadaGravado()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco
            {
                EmContainer = true,
                Diretorio = string.Empty,
                UrlPublicaApi = string.Empty
            });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);

            using HttpResponseMessage envio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));

            Assert.Equal(HttpStatusCode.BadGateway, envio.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(envio);
            Assert.Equal(ConstantsMessage.TituloErroIntegracao, problema.Title);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, problema.Detail);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoDiscoDiretorioObrigatorio));
            Assert.False(Directory.Exists(fabrica.Raiz));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-13: segredo criado so no primeiro envio; link emitido antes de recriar os servicos continua abrindo
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA13_SegredoNasceNoPrimeiroEnvioENaoNaLeituraNemNoLote()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            string caminhoSegredo = CaminhoDoSegredo(fabrica.Raiz);
            string pastaSegredo = Path.GetDirectoryName(caminhoSegredo)!;

            using HttpResponseMessage semLink = await cliente.GetAsync(LinkAleatorio(ChaveFoto, UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga));
            Assert.Equal(HttpStatusCode.NotFound, semLink.StatusCode);
            Assert.False(Directory.Exists(fabrica.Raiz));

            Assert.Null(await ObterUrlAsync(fabrica, ChaveFoto));
            Assert.Empty(await AssinarAsync(fabrica, ChaveFoto));
            Assert.False(Directory.Exists(fabrica.Raiz));

            GravarArquivoDireto(fabrica.Raiz, ChaveFoto, ConteudoJpeg(TamanhoFoto));
            Assert.Null(await ObterUrlAsync(fabrica, ChaveFoto));
            Assert.Empty(await AssinarAsync(fabrica, ChaveFoto));
            Assert.False(File.Exists(caminhoSegredo));
            Assert.False(Directory.Exists(pastaSegredo));

            await EnviarAsync(fabrica, ChaveOutraFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            Assert.True(File.Exists(caminhoSegredo));
            Assert.Equal(TamanhoSegredo, File.ReadAllBytes(caminhoSegredo).Length);
            Assert.NotNull(await ObterUrlAsync(fabrica, ChaveFoto));
        }

        [Fact]
        public async Task MA13_LinkEmitidoAntesDeRecriarOsServicosContinuaAbrindo()
        {
            using FabricaArmazenamentoDisco primeira = new FabricaArmazenamentoDisco();
            primeira.CreateClient();
            await EnviarAsync(primeira, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(primeira, ChaveFoto))!.Url;

            using FabricaArmazenamentoDisco reiniciada = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { PastaTemporaria = primeira.PastaTemporaria });
            using HttpClient clienteReiniciado = reiniciada.CreateClient();
            using HttpResponseMessage resposta = await clienteReiniciado.GetAsync(CaminhoDe(url));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(ConteudoJpeg(TamanhoFoto), await resposta.Content.ReadAsByteArrayAsync());
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-16 e MA-17: rota anonima numa maquina sem pasta nao cria nada; pasta invalida nao derruba a API
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA16_GetAnonimoComLinkBemFormadoNaoCriaPastaNemArquivo()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await cliente.GetAsync(LinkAleatorio(ChaveFoto, UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, problema.Detail);
            Assert.False(Directory.Exists(fabrica.Raiz));
        }

        [Fact]
        public async Task MA17_DiretorioInvalidoNaoImpedeASubidaESoFalhaOArmazenamento()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { Diretorio = DiretorioRelativo });
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage saude = await cliente.GetAsync(RotaSaude);
            Assert.Equal(HttpStatusCode.OK, saude.StatusCode);

            string token = await LogarFuncionarioAsync(cliente);
            using HttpResponseMessage envio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));

            Assert.Equal(HttpStatusCode.BadGateway, envio.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(envio);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, problema.Detail);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoDiscoDiretorioInvalido));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-18: nenhum registro leva sig, query, chave ou caminho; falha nao prevista da rota gera um registro so
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA18_NenhumRegistroTemSigQueryChaveOuCaminhoNosCasosDeRotina()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;
            string sig = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, exp);

            using HttpResponseMessage ok = await cliente.GetAsync(LinkDe(ChaveFoto, exp, sig));
            using HttpResponseMessage malformado = await cliente.GetAsync(Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, "0" + Texto(exp)), Par(ParametroSinal, sig)));
            using HttpResponseMessage insegura = await cliente.GetAsync(LinkDe(ChaveComPontoPonto, exp, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveComPontoPonto, exp)));
            using HttpResponseMessage assinaturaErrada = await cliente.GetAsync(LinkDe(ChaveFoto, exp, new string(SigSubstituto[0], CaracteresDaAssinatura)));
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, malformado.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, insegura.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, assinaturaErrada.StatusCode);

            List<string> textos = new List<string>();
            textos.AddRange(fabrica.Log.TodosOsTextos());
            textos.AddRange(fabrica.LogsDoHost.Textos());
            Assert.Empty(fabrica.Log.Excecoes());
            foreach (string texto in textos)
            {
                Assert.DoesNotContain(sig, texto);
                Assert.DoesNotContain(ParametroSinal + "=", texto);
                Assert.DoesNotContain(ConstantsValues.ParametroChaveLink + "=", texto);
                Assert.DoesNotContain(MarcadorChave, texto);
                Assert.DoesNotContain(fabrica.Raiz, texto);
            }
        }

        [Fact]
        public async Task MA18_FalhaNaoPrevistaDaRotaGeraUmRegistroSoEQuinhentos()
        {
            string mensagemComCaminho = "falha em " + Path.Combine("pasta-d5", ChaveFoto);
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco
            {
                SegredoSubstituto = new SegredoQueFalhaArmazenamentoDisco(mensagemComCaminho)
            });
            using HttpClient cliente = fabrica.CreateClient();
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;

            using HttpResponseMessage resposta = await cliente.GetAsync(LinkDe(ChaveFoto, exp, new string(SigSubstituto[0], CaracteresDaAssinatura)));

            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloErroInterno, problema.Title);
            Assert.Equal(ConstantsMessage.ErroInterno, problema.Detail);

            List<string> registrosDeErro = fabrica.Log.Mensagens().Where(mensagem => mensagem == typeof(InvalidOperationException).Name).ToList();
            Assert.Single(registrosDeErro);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(typeof(InvalidOperationException).Name));
            Assert.Empty(fabrica.Log.Excecoes());
            Assert.Equal(ZeroRegistros, fabrica.LogsDoHost.Erros());

            foreach (string texto in fabrica.Log.TodosOsTextos())
            {
                Assert.DoesNotContain(mensagemComCaminho, texto);
                Assert.DoesNotContain(MarcadorChave, texto);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-20 e lote com arquivo sumido: rota anonima que chega ao lote nao cria segredo; arquivo sumido e omitido
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA20_LoteSemSegredoNaoCriaSegredoEVoltaAComUrlDepoisDeUmEnvio()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            GravarArquivoDireto(fabrica.Raiz, ChaveFoto, ConteudoJpeg(TamanhoFoto));

            Assert.Empty(await AssinarAsync(fabrica, ChaveFoto));
            Assert.False(Directory.Exists(Path.GetDirectoryName(CaminhoDoSegredo(fabrica.Raiz))!));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoEmissaoSemSegredo, UmRegistro), fabrica.Log.Mensagens());

            await EnviarAsync(fabrica, ChaveOutraFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await AssinarAsync(fabrica, ChaveFoto);
            Assert.True(lote.ContainsKey(ChaveFoto));

            using HttpResponseMessage resposta = await cliente.GetAsync(CaminhoDe(lote[ChaveFoto].Url));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task ArquivoSumidoNoLoteENaRotaNaoQuebraOsDemais()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            await EnviarAsync(fabrica, ChaveOutraFoto, ConteudoJpeg(TamanhoFotoNova), ConstantsArmazenamento.MimeJpeg);
            string urlSumida = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            string urlDeOutra = (await ObterUrlAsync(fabrica, ChaveOutraFoto))!.Url;

            File.Delete(CaminhoDoArquivo(fabrica.Raiz, ChaveFoto));

            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await AssinarAsync(fabrica, ChaveFoto, ChaveOutraFoto);
            Assert.False(lote.ContainsKey(ChaveFoto));
            Assert.True(lote.ContainsKey(ChaveOutraFoto));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLoteChavesOmitidas, UmRegistro, ZeroRegistros), fabrica.Log.Mensagens());
            Assert.Null(await ObterUrlAsync(fabrica, ChaveFoto));

            using HttpResponseMessage sumida = await cliente.GetAsync(CaminhoDe(urlSumida));
            Assert.Equal(HttpStatusCode.NotFound, sumida.StatusCode);

            using HttpResponseMessage outra = await cliente.GetAsync(CaminhoDe(urlDeOutra));
            Assert.Equal(HttpStatusCode.OK, outra.StatusCode);
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // L1 (veiculo-foto-arquivo-ausente): lista com foto sem arquivo responde 200 com erroFoto por item; a remocao da foto sem
        // arquivo da 204 (com ou sem pasta); a foto sem arquivo conta no teto; a avaria com pasta ausente da 204 (regra 12).
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task L1_ListaComArquivoApagadoDaPastaResponde200ComErroNaMesmaPosicaoERegistraUmaVez()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            using HttpResponseMessage primeiro = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));
            using HttpResponseMessage segundo = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFotoNova));
            Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);
            Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);
            string urlDaPrimeira = await UrlDoCorpoAsync(primeiro);
            string urlDaSegunda = await UrlDoCorpoAsync(segundo);
            string chaveDaPrimeira = QueryDe(urlDaPrimeira, ConstantsValues.ParametroChaveLink);
            string chaveDaSegunda = QueryDe(urlDaSegunda, ConstantsValues.ParametroChaveLink);
            File.Delete(CaminhoDoArquivo(fabrica.Raiz, chaveDaPrimeira));

            using HttpResponseMessage lista = await ListarFotosAsync(cliente, token);

            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            JsonElement itens = documento.RootElement;
            Assert.Equal(QuantidadeDeDuasFotosL1, itens.GetArrayLength());
            JsonElement apagada = itens[PosicaoPrimeiraFotoL1];
            Assert.Equal(JsonValueKind.Null, apagada.GetProperty(CampoUrl).ValueKind);
            Assert.Equal(JsonValueKind.Null, apagada.GetProperty(CampoExpiraEm).ValueKind);
            Assert.Equal(ConstantsMessage.ErroVeiculoFotoSemArquivo, apagada.GetProperty(CampoErroFoto).GetString());
            JsonElement outra = itens[PosicaoSegundaFotoL1];
            Assert.Equal(JsonValueKind.Null, outra.GetProperty(CampoErroFoto).ValueKind);
            string urlDaOutra = outra.GetProperty(CampoUrl).GetString() ?? string.Empty;
            using HttpResponseMessage leitura = await cliente.GetAsync(CaminhoDe(urlDaOutra));
            Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
            Assert.Equal(FakeProcessadorFotoService.ConteudoRecodificado, await leitura.Content.ReadAsByteArrayAsync());

            string mensagemDaContagem = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoListaSemUrlAssinada, UmRegistro);
            Assert.Single(fabrica.Log.Mensagens(), mensagem => mensagem == mensagemDaContagem);
            string mensagemDoProvedor = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLoteChavesOmitidas, UmRegistro, ZeroRegistros);
            Assert.Contains(mensagemDoProvedor, fabrica.Log.Mensagens());

            // Caso (f) do contrato: nenhum registro (mensagem, amigavel, origem, excecao ou informativo, do servico ou do host) leva a chave,
            // a assinatura (sig) de qualquer URL devolvida nem o caminho da pasta de armazenamento. Antes, cada valor precisa existir de verdade
            // (nao vazio), senao a conferencia passaria sem provar nada.
            List<string> valoresQueNaoPodemVazar = new List<string>
            {
                chaveDaPrimeira,
                chaveDaSegunda,
                QueryDe(urlDaPrimeira, ParametroSinal),
                QueryDe(urlDaSegunda, ParametroSinal),
                QueryDe(urlDaOutra, ParametroSinal),
                fabrica.Raiz,
                fabrica.PastaTemporaria
            };
            Assert.All(valoresQueNaoPodemVazar, valor => Assert.False(string.IsNullOrEmpty(valor)));

            List<string> registros = new List<string>();
            registros.AddRange(fabrica.Log.TodosOsTextos());
            registros.AddRange(fabrica.Log.Origens());
            registros.AddRange(fabrica.LogsDoHost.Textos());
            Assert.NotEmpty(registros);
            foreach (string registro in registros)
            {
                foreach (string valor in valoresQueNaoPodemVazar)
                {
                    Assert.DoesNotContain(valor, registro);
                }
            }
        }

        [Fact]
        public async Task L1_LinhaDeOutroAmbienteComPastaDoVeiculoAusente_Da200ComErroEDa204NaRemocao()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            GravarArquivoDireto(fabrica.Raiz, ChaveDeOutroVeiculoL1, ConteudoJpeg(TamanhoFoto));
            VeiculoFoto semArquivo = SemearFoto(fabrica, IdDaFotoSemArquivoL1, OrdemDaFotoSemArquivoL1, ChaveSemArquivoL1);

            using HttpResponseMessage antes = await ListarFotosAsync(cliente, token);
            Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
            using JsonDocument documentoAntes = JsonDocument.Parse(await antes.Content.ReadAsStringAsync());
            Assert.Equal(ConstantsMessage.ErroVeiculoFotoSemArquivo, documentoAntes.RootElement[PosicaoPrimeiraFotoL1].GetProperty(CampoErroFoto).GetString());

            using HttpResponseMessage remocao = await RemoverFotoAsync(cliente, token, IdDaFotoSemArquivoL1);

            Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
            Assert.NotNull(semArquivo.DisabledAt);
            Assert.NotNull(semArquivo.IdFuncionarioRemocao);
            Assert.False(Directory.Exists(Path.GetDirectoryName(CaminhoDoArquivo(fabrica.Raiz, ChaveSemArquivoL1))));
            using HttpResponseMessage depois = await ListarFotosAsync(cliente, token);
            using JsonDocument documentoDepois = JsonDocument.Parse(await depois.Content.ReadAsStringAsync());
            Assert.Equal(QuantidadeSemItensL1, documentoDepois.RootElement.GetArrayLength());
            Assert.Equal(ZeroRegistros, fabrica.Log.Contar(ConstantsMessage.ErroArmazenamentoRemover));
        }

        [Fact]
        public async Task L1_LinhaSemNenhumaPastaDaRaiz_Da200ComErroEDa204SemCriarNada()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            SemearFoto(fabrica, IdDaFotoSemArquivoL1, OrdemDaFotoSemArquivoL1, ChaveSemArquivoL1);
            Assert.False(Directory.Exists(fabrica.Raiz));

            using HttpResponseMessage lista = await ListarFotosAsync(cliente, token);

            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            Assert.Equal(ConstantsMessage.ErroVeiculoFotoSemArquivo, documento.RootElement[PosicaoPrimeiraFotoL1].GetProperty(CampoErroFoto).GetString());
            using HttpResponseMessage remocao = await RemoverFotoAsync(cliente, token, IdDaFotoSemArquivoL1);
            Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
            Assert.False(Directory.Exists(fabrica.Raiz));
        }

        [Fact]
        public async Task L1_FotoSemArquivoContaNoTetoEBaixaComARemocao()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            for (int indice = IdDaFotoSemArquivoL1; indice <= ConstantsValues.MaximoFotosPorVeiculo; indice++)
            {
                string chave = string.Format(CultureInfo.InvariantCulture, FormatoChaveDoTetoL1, indice);
                SemearFoto(fabrica, indice, indice, chave);
                if (indice != IdDaFotoSemArquivoL1)
                {
                    GravarArquivoDireto(fabrica.Raiz, chave, ConteudoJpeg(TamanhoFoto));
                }
            }

            using HttpResponseMessage cheio = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));

            Assert.Equal(HttpStatusCode.Conflict, cheio.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(cheio);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoLimiteAtingido, ConstantsValues.MaximoFotosPorVeiculo), problema.Detail);
            using HttpResponseMessage remocao = await RemoverFotoAsync(cliente, token, IdDaFotoSemArquivoL1);
            Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
            using HttpResponseMessage depois = await EnviarFotoAsync(cliente, token, ConteudoJpeg(TamanhoFoto));
            Assert.Equal(HttpStatusCode.Created, depois.StatusCode);
        }

        [Fact]
        public async Task L1_ComProvedorS3ListaSemArquivoTodasComUrlESemErro()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco
            {
                Provedor = ConstantsArmazenamento.ProvedorS3Compativel,
                ProvedorS3Falso = true
            });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            SemearFoto(fabrica, IdDaFotoSemArquivoL1, OrdemDaFotoSemArquivoL1, ChaveFoto);
            SemearFoto(fabrica, IdDaFotoComArquivoL1, OrdemDaFotoComArquivoL1, ChaveOutraFoto);

            using HttpResponseMessage lista = await ListarFotosAsync(cliente, token);

            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            Assert.Equal(QuantidadeDeDuasFotosL1, documento.RootElement.GetArrayLength());
            foreach (JsonElement item in documento.RootElement.EnumerateArray())
            {
                Assert.NotEqual(JsonValueKind.Null, item.GetProperty(CampoUrl).ValueKind);
                Assert.Equal(JsonValueKind.Null, item.GetProperty(CampoErroFoto).ValueKind);
            }
        }

        [Fact]
        public async Task L1_DiretorioInvalidoNaListaContinua502ComOErroDoArmazenamento()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco(new OpcoesFabricaArmazenamentoDisco { Diretorio = DiretorioRelativo });
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarFuncionarioAsync(cliente);
            SemearFoto(fabrica, IdDaFotoSemArquivoL1, OrdemDaFotoSemArquivoL1, ChaveFoto);

            using HttpResponseMessage lista = await ListarFotosAsync(cliente, token);

            Assert.Equal(HttpStatusCode.BadGateway, lista.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(lista);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoObterUrl, problema.Detail);
        }

        [Fact]
        public async Task L1_RemocaoDaFotoDeAvariaComPastaDaAvariaAusenteDaSucessoSemRegistro()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            fabrica.CreateClient();
            GravarArquivoDireto(fabrica.Raiz, ChaveFoto, ConteudoJpeg(TamanhoFoto));

            await RemoverAsync(fabrica, ChaveAvariaJpeg);

            Assert.True(File.Exists(CaminhoDoArquivo(fabrica.Raiz, ChaveFoto)));
            Assert.False(Directory.Exists(Path.GetDirectoryName(CaminhoDoArquivo(fabrica.Raiz, ChaveAvariaJpeg))));
            Assert.Equal(ZeroRegistros, fabrica.Log.Contar(ConstantsMessage.ErroArmazenamentoRemover));
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Link malformado (400), chave com caractere de controle no fim (404) e as seis causas de 404 com o mesmo corpo
        // ---------------------------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(CasoExpComZeroAEsquerda)]
        [InlineData(CasoExpComLetra)]
        [InlineData(CasoExpComOnzeDigitos)]
        [InlineData(CasoExpComEspacoNoFim)]
        [InlineData(CasoExpComLF)]
        [InlineData(CasoSigComQuarentaEDois)]
        [InlineData(CasoSigComQuarentaEQuatro)]
        [InlineData(CasoSigComCaractereForaDoAlfabeto)]
        [InlineData(CasoSigComLF)]
        [InlineData(CasoSigComQuarentaEDoisELF)]
        [InlineData(CasoChaveComDuzentosEUm)]
        [InlineData(CasoExpRepetido)]
        [InlineData(CasoChaveAusente)]
        [InlineData(CasoSigAusente)]
        public async Task LinkMalformadoDa400ComRegistro(int caso)
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;

            using HttpResponseMessage resposta = await cliente.GetAsync(PedidoMalformado(caso, segredo, exp));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            ProblemaRecebido problema = await LerProblemaAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloErroValidacao, problema.Title);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoLinkInvalido, problema.Detail);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoLeituraLinkMalformado));
        }

        [Theory]
        [InlineData(QuebraDeLinha)]
        [InlineData(SufixoCRLF)]
        [InlineData(SufixoTabulacao)]
        [InlineData(Espaco)]
        public async Task ChaveComCaractereDeControleNoFimDa404(string sufixo)
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;
            string chaveComSufixo = ChaveFoto + sufixo;

            using HttpResponseMessage resposta = await cliente.GetAsync(LinkDe(chaveComSufixo, exp, AssinaturaLinkArmazenamento.Calcular(segredo, chaveComSufixo, exp)));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(UmRegistro, fabrica.Log.Contar(ConstantsMessage.LogArmazenamentoLeituraChaveInsegura));
        }

        [Fact]
        public async Task SeisCausasDe404TemOMesmoStatusTituloEDetalheEUmRegistroCada()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            GravarArquivoDireto(fabrica.Raiz, ChaveGif, ConteudoComCabecalho(Encoding.ASCII.GetBytes(CabecalhoGif), TamanhoFoto));
            byte[] segredo = LerSegredo(fabrica.Raiz);
            long agora = UnixDe(fabrica.Relogio.GetUtcNow());
            long expValido = agora + SegundosDeFolga;
            string sigDeOutroExp = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, expValido + ValorDeDiferencaDoExp);

            List<KeyValuePair<string, string>> casos = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(LinkDe(ChaveComPontoPonto, expValido, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveComPontoPonto, expValido)), ConstantsMessage.LogArmazenamentoLeituraChaveInsegura),
                new KeyValuePair<string, string>(LinkDe(ChaveFoto, expValido, sigDeOutroExp), ConstantsMessage.LogArmazenamentoLeituraAssinaturaInvalida),
                new KeyValuePair<string, string>(LinkDe(ChaveFoto, agora - ValorDeExpVencido, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, agora - ValorDeExpVencido)), ConstantsMessage.LogArmazenamentoLeituraPrazoVencido),
                new KeyValuePair<string, string>(LinkDe(ChaveFoto, agora + JanelaPadraoSegundos * 2 + ValorAcimaDoTetoAdicional, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, agora + JanelaPadraoSegundos * 2 + ValorAcimaDoTetoAdicional)), ConstantsMessage.LogArmazenamentoLeituraPrazoAcimaDoTeto),
                new KeyValuePair<string, string>(LinkDe(ChaveAusente, expValido, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveAusente, expValido)), ConstantsMessage.LogArmazenamentoLeituraArquivoAusente),
                new KeyValuePair<string, string>(LinkDe(ChaveGif, expValido, AssinaturaLinkArmazenamento.Calcular(segredo, ChaveGif, expValido)), ConstantsMessage.LogArmazenamentoLeituraTipoNaoSuportado),
            };

            List<ProblemaRecebido> problemas = new List<ProblemaRecebido>();
            foreach (KeyValuePair<string, string> caso in casos)
            {
                using HttpResponseMessage resposta = await cliente.GetAsync(caso.Key);
                Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
                problemas.Add(await LerProblemaAsync(resposta));
                Assert.Equal(UmRegistro, fabrica.Log.Contar(caso.Value));
            }

            foreach (ProblemaRecebido problema in problemas)
            {
                Assert.Equal(ConstantsMessage.TituloRecursoNaoEncontrado, problema.Title);
                Assert.Equal(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, problema.Detail);
            }
        }

        [Theory]
        [InlineData(ConstantsValues.ParametroChaveLink)]
        public async Task ParametroRepetidoDa400(string nome)
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            long exp = UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga;

            using HttpResponseMessage resposta = await cliente.GetAsync(Rota(Par(nome, ChaveFoto), Par(nome, ChaveOutraFoto), Par(ConstantsValues.ParametroExpiracaoLink, Texto(exp)), Par(ParametroSinal, new string(SigSubstituto[0], CaracteresDaAssinatura))));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Registro: nenhum objeto de excecao no log da rota e so o nome do tipo na falha nao prevista (ja coberto acima)
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task LogDoHostNaoRegistraSigComOConfiguracaoPadrao()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            string url = (await ObterUrlAsync(fabrica, ChaveFoto))!.Url;
            string sig = QueryDe(url, ParametroSinal);

            using HttpResponseMessage ok = await cliente.GetAsync(CaminhoDe(url));
            using HttpResponseMessage naoEncontrado = await cliente.GetAsync(LinkAleatorio(ChaveAusente, UnixDe(fabrica.Relogio.GetUtcNow()) + SegundosDeFolga));

            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, naoEncontrado.StatusCode);
            foreach (string texto in fabrica.LogsDoHost.Textos())
            {
                Assert.DoesNotContain(sig, texto);
                Assert.DoesNotContain(ParametroSinal + "=", texto);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // MA-06 (capa das listas abre), MA-07 (capa com arquivo sumido) e MA-20 (rota anonima do catalogo): capa pelo lote
        // ---------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task MA06_CapaDoCatalogoAbreAUrlComOsMesmosBytes()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            fabrica.Veiculos.Adicionar(VeiculoComCapa(ChaveFoto));

            using JsonDocument documento = await CatalogoAsync(cliente);
            JsonElement item = documento.RootElement.GetProperty(CampoItens)[0];
            string url = item.GetProperty(CampoFotoCapa).GetProperty(CampoUrl).GetString() ?? string.Empty;

            using HttpResponseMessage capa = await cliente.GetAsync(CaminhoDe(url));
            Assert.Equal(HttpStatusCode.OK, capa.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, capa.Content.Headers.ContentType?.MediaType);
            Assert.Equal(ConteudoJpeg(TamanhoFoto), await capa.Content.ReadAsByteArrayAsync());
        }

        [Fact]
        public async Task MA07_CapaComArquivoSumidoVaiParaErroFotoCapaSemDerrubarALista()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            await EnviarAsync(fabrica, ChaveFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);
            fabrica.Veiculos.Adicionar(VeiculoComCapa(ChaveFoto));
            File.Delete(CaminhoDoArquivo(fabrica.Raiz, ChaveFoto));

            using JsonDocument documento = await CatalogoAsync(cliente);
            JsonElement item = documento.RootElement.GetProperty(CampoItens)[0];
            bool temCapa = item.TryGetProperty(CampoFotoCapa, out JsonElement capa) && capa.ValueKind != JsonValueKind.Null;

            Assert.False(temCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, item.GetProperty(CampoErroFotoCapa).GetString());
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLoteChavesOmitidas, UmRegistro, ZeroRegistros), fabrica.Log.Mensagens());
        }

        [Fact]
        public async Task MA20_RotaAnonimaDoCatalogoNaoCriaSegredoEVoltaAComUrlDepoisDeUmEnvio()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            GravarArquivoDireto(fabrica.Raiz, ChaveFoto, ConteudoJpeg(TamanhoFoto));
            fabrica.Veiculos.Adicionar(VeiculoComCapa(ChaveFoto));

            using JsonDocument semSegredo = await CatalogoAsync(cliente);
            JsonElement itemSemSegredo = semSegredo.RootElement.GetProperty(CampoItens)[0];
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, itemSemSegredo.GetProperty(CampoErroFotoCapa).GetString());
            Assert.False(File.Exists(CaminhoDoSegredo(fabrica.Raiz)));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoEmissaoSemSegredo, UmRegistro), fabrica.Log.Mensagens());

            await EnviarAsync(fabrica, ChaveOutraFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);

            using JsonDocument comSegredo = await CatalogoAsync(cliente);
            JsonElement itemComSegredo = comSegredo.RootElement.GetProperty(CampoItens)[0];
            string url = itemComSegredo.GetProperty(CampoFotoCapa).GetProperty(CampoUrl).GetString() ?? string.Empty;
            using HttpResponseMessage resposta = await cliente.GetAsync(CaminhoDe(url));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task MA20_RotaAnonimaDeDisponiveisNaoCriaSegredoEVoltaAComUrlDepoisDeUmEnvio()
        {
            using FabricaArmazenamentoDisco fabrica = new FabricaArmazenamentoDisco();
            using HttpClient cliente = fabrica.CreateClient();
            GravarArquivoDireto(fabrica.Raiz, ChaveFoto, ConteudoJpeg(TamanhoFoto));
            fabrica.Veiculos.Adicionar(VeiculoComCapa(ChaveFoto));

            using JsonDocument semSegredo = await DisponiveisAsync(cliente);
            JsonElement itemSemSegredo = semSegredo.RootElement.GetProperty(CampoItens)[0];
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, itemSemSegredo.GetProperty(CampoErroFotoCapa).GetString());
            Assert.False(File.Exists(CaminhoDoSegredo(fabrica.Raiz)));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoEmissaoSemSegredo, UmRegistro), fabrica.Log.Mensagens());

            await EnviarAsync(fabrica, ChaveOutraFoto, ConteudoJpeg(TamanhoFoto), ConstantsArmazenamento.MimeJpeg);

            using JsonDocument comSegredo = await DisponiveisAsync(cliente);
            JsonElement itemComSegredo = comSegredo.RootElement.GetProperty(CampoItens)[0];
            string url = itemComSegredo.GetProperty(CampoFotoCapa).GetProperty(CampoUrl).GetString() ?? string.Empty;
            using HttpResponseMessage resposta = await cliente.GetAsync(CaminhoDe(url));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Helpers de pedido, de link e de leitura de arquivos
        // ---------------------------------------------------------------------------------------------------------------------

        private static async Task<JsonDocument> CatalogoAsync(HttpClient cliente)
        {
            using HttpResponseMessage resposta = await cliente.GetAsync(RotaCatalogo);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        }

        private static async Task<JsonDocument> DisponiveisAsync(HttpClient cliente)
        {
            using HttpResponseMessage resposta = await cliente.GetAsync(RotaDisponiveis);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        }

        private static Veiculo VeiculoComCapa(string chave)
        {
            return new Veiculo
            {
                IdCategoria = IdCategoriaDeTeste,
                Categoria = new Categoria { IdCategoria = IdCategoriaDeTeste, Nome = NomeCategoriaDeTeste, ValorDiariaBase = ValorDiariaDeTeste },
                Placa = PlacaDeTeste,
                Chassi = ChassiDeTeste,
                Renavam = RenavamDeTeste,
                Marca = MarcaDeTeste,
                Modelo = ModeloDeTeste,
                AnoFabricacao = AnoDeTeste,
                AnoModelo = AnoModeloDeTeste,
                KmAtual = KmDeTeste,
                ValorDiaria = ValorDiariaDeTeste,
                ValorVenda = ValorVendaDeTeste,
                Status = StatusVeiculo.DISPONIVEL,
                Fotos = new List<VeiculoFoto>
                {
                    new VeiculoFoto { IdVeiculoFoto = IdFotoDeTeste, Ordem = OrdemDaCapa, ChaveObjeto = chave }
                }
            };
        }

        private static async Task<string> LogarFuncionarioAsync(HttpClient cliente)
        {
            string corpo = JsonSerializer.Serialize(new { perfil = PerfilFuncionario, cpf = CpfFuncionario, senha = SenhaFuncionario });
            using StringContent conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");
            using HttpResponseMessage login = await cliente.PostAsync(RotaLogin, conteudo);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            return documento.RootElement.GetProperty(CampoToken).GetString() ?? string.Empty;
        }

        private static async Task<HttpResponseMessage> EnviarFotoAsync(HttpClient cliente, string token, byte[] conteudo)
        {
            using MultipartFormDataContent formulario = new MultipartFormDataContent();
            ByteArrayContent arquivo = new ByteArrayContent(conteudo);
            arquivo.Headers.ContentType = new MediaTypeHeaderValue(ConstantsArmazenamento.MimeJpeg);
            formulario.Add(arquivo, CampoFoto, NomeArquivoFoto);
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaFotos) { Content = formulario };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<HttpResponseMessage> PedirComIpAsync(HttpClient cliente, string ip, string caminho)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
            requisicao.Headers.Add(CabecalhoIp, ip);
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<ProblemaRecebido> LerProblemaAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);
            string title = documento.RootElement.GetProperty(CampoTitle).GetString() ?? string.Empty;
            string detail = documento.RootElement.GetProperty(CampoDetail).GetString() ?? string.Empty;
            return new ProblemaRecebido(title, detail);
        }

        private static async Task<ArquivoArmazenado> EnviarAsync(FabricaArmazenamentoDisco fabrica, string chave, byte[] conteudo, string tipo)
        {
            using IServiceScope escopo = fabrica.Services.CreateScope();
            IArmazenamentoGateway gateway = escopo.ServiceProvider.GetRequiredService<IArmazenamentoGateway>();
            return await gateway.EnviarAsync(chave, conteudo, tipo);
        }

        private static async Task<ArquivoArmazenado?> ObterUrlAsync(FabricaArmazenamentoDisco fabrica, string chave)
        {
            using IServiceScope escopo = fabrica.Services.CreateScope();
            IArmazenamentoGateway gateway = escopo.ServiceProvider.GetRequiredService<IArmazenamentoGateway>();
            return await gateway.ObterUrlAsync(chave);
        }

        private static async Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarAsync(FabricaArmazenamentoDisco fabrica, params string[] chaves)
        {
            using IServiceScope escopo = fabrica.Services.CreateScope();
            IArmazenamentoGateway gateway = escopo.ServiceProvider.GetRequiredService<IArmazenamentoGateway>();
            return await gateway.AssinarUrlsAsync(chaves);
        }

        private static async Task RemoverAsync(FabricaArmazenamentoDisco fabrica, string chave)
        {
            using IServiceScope escopo = fabrica.Services.CreateScope();
            IArmazenamentoGateway gateway = escopo.ServiceProvider.GetRequiredService<IArmazenamentoGateway>();
            await gateway.RemoverAsync(chave);
        }

        private static async Task<HttpResponseMessage> ListarFotosAsync(HttpClient cliente, string token)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, RotaFotos);
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<HttpResponseMessage> RemoverFotoAsync(HttpClient cliente, string token, int idFoto)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Delete, string.Format(CultureInfo.InvariantCulture, RotaFotoRemover, idFoto));
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<string> UrlDoCorpoAsync(HttpResponseMessage envio)
        {
            using JsonDocument documento = JsonDocument.Parse(await envio.Content.ReadAsStringAsync());
            return documento.RootElement.GetProperty(CampoUrl).GetString() ?? string.Empty;
        }

        /// <summary>Grava uma linha ativa da foto do veiculo de teste, sem tocar no disco: o arquivo fica por conta de cada caso.</summary>
        private static VeiculoFoto SemearFoto(FabricaArmazenamentoDisco fabrica, int idFoto, int ordem, string chave)
        {
            VeiculoFoto foto = new VeiculoFoto
            {
                IdVeiculoFoto = idFoto,
                IdVeiculo = IdVeiculoDoTesteL1,
                Ordem = ordem,
                ChaveObjeto = chave,
                ChaveMiniatura = null,
                IdFuncionarioEnvio = IdFuncionarioDoTesteL1,
                IdFuncionarioRemocao = null
            };
            fabrica.Fotos.Fotos.Add(foto);
            return foto;
        }

        private static byte[] ConteudoJpeg(int tamanho)
        {
            return ConteudoComCabecalho(ConstantsValues.AssinaturaJpeg, tamanho);
        }

        private static byte[] ConteudoPng(int tamanho)
        {
            return ConteudoComCabecalho(ConstantsValues.AssinaturaPng, tamanho);
        }

        private static byte[] ConteudoComCabecalho(byte[] cabecalho, int tamanho)
        {
            byte[] conteudo = new byte[tamanho];
            Array.Copy(cabecalho, conteudo, cabecalho.Length);
            for (int posicao = cabecalho.Length; posicao < tamanho; posicao++)
            {
                conteudo[posicao] = (byte)posicao;
            }

            return conteudo;
        }

        private static string CaminhoDoArquivo(string raiz, string chave)
        {
            List<string> partes = new List<string> { raiz, ConstantsValues.SubpastaArquivosArmazenamento };
            partes.AddRange(chave.Split(ConstantsValues.CaractereSeparadorChaveArmazenamento));
            return Path.Combine(partes.ToArray());
        }

        private static string CaminhoDoSegredo(string raiz)
        {
            return Path.Combine(raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura);
        }

        private static byte[] LerSegredo(string raiz)
        {
            return File.ReadAllBytes(CaminhoDoSegredo(raiz));
        }

        private static void GravarArquivoDireto(string raiz, string chave, byte[] conteudo)
        {
            string caminho = CaminhoDoArquivo(raiz, chave);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            File.WriteAllBytes(caminho, conteudo);
        }

        private static long UnixDe(DateTimeOffset instante)
        {
            return instante.ToUnixTimeSeconds();
        }

        private static string Texto(long valor)
        {
            return valor.ToString(CultureInfo.InvariantCulture);
        }

        private static string Par(string nome, string valor)
        {
            return nome + "=" + Uri.EscapeDataString(valor);
        }

        private static string Rota(params string[] pares)
        {
            return "/" + ConstantsValues.RotaLeituraArquivoArmazenado + "?" + string.Join("&", pares);
        }

        private static string LinkDe(string chave, long exp, string sig)
        {
            return Rota(Par(ConstantsValues.ParametroChaveLink, chave), Par(ConstantsValues.ParametroExpiracaoLink, Texto(exp)), Par(ParametroSinal, sig));
        }

        private static string LinkAleatorio(string chave, long exp)
        {
            byte[] segredoAleatorio = RandomNumberGenerator.GetBytes(TamanhoSegredo);
            return LinkDe(chave, exp, AssinaturaLinkArmazenamento.Calcular(segredoAleatorio, chave, exp));
        }

        private static string PedidoMalformado(int caso, byte[] segredo, long exp)
        {
            string expTexto = Texto(exp);
            string sig = AssinaturaLinkArmazenamento.Calcular(segredo, ChaveFoto, exp);
            string chaveLonga = new string(ChaveFoto[0], TamanhoChaveAcimaDoLimite);
            switch (caso)
            {
                case CasoExpComZeroAEsquerda:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, "0" + expTexto), Par(ParametroSinal, sig));
                case CasoExpComLetra:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto + "a"), Par(ParametroSinal, sig));
                case CasoExpComOnzeDigitos:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, "1" + expTexto), Par(ParametroSinal, sig));
                case CasoExpComEspacoNoFim:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto + Espaco), Par(ParametroSinal, sig));
                case CasoExpComLF:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto + SufixoLF), Par(ParametroSinal, sig));
                case CasoSigComQuarentaEDois:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig.Substring(0, TamanhoSigCurta)));
                case CasoSigComQuarentaEQuatro:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig + SigSubstituto));
                case CasoSigComCaractereForaDoAlfabeto:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, SinalMais + sig.Substring(1)));
                case CasoSigComLF:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig + SufixoLF));
                case CasoSigComQuarentaEDoisELF:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig.Substring(0, TamanhoSigCurta) + SufixoLF));
                case CasoChaveComDuzentosEUm:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, chaveLonga), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig));
                case CasoExpRepetido:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig));
                case CasoChaveAusente:
                    return Rota(Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig));
                case CasoSigAusente:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto));
                default:
                    return Rota(Par(ConstantsValues.ParametroChaveLink, ChaveFoto), Par(ConstantsValues.ParametroExpiracaoLink, expTexto), Par(ParametroSinal, sig));
            }
        }

        private static string CaminhoDe(string url)
        {
            return new Uri(url, UriKind.Absolute).PathAndQuery;
        }

        private static string QueryDe(string url, string nome)
        {
            string query = new Uri(url, UriKind.Absolute).Query.TrimStart('?');
            foreach (string parte in query.Split('&'))
            {
                if (parte.StartsWith(nome + "=", StringComparison.Ordinal))
                {
                    return Uri.UnescapeDataString(parte.Substring(nome.Length + 1));
                }
            }

            return string.Empty;
        }

        private static string ValorDoCabecalho(HttpResponseMessage resposta, string nome)
        {
            if (resposta.Headers.TryGetValues(nome, out IEnumerable<string>? valoresDaResposta))
            {
                return string.Join(",", valoresDaResposta);
            }

            if (resposta.Content.Headers.TryGetValues(nome, out IEnumerable<string>? valoresDoConteudo))
            {
                return string.Join(",", valoresDoConteudo);
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Servidor de teste do armazenamento em pasta: API real, pasta temporaria propria (apagada quando o dono descarta a fabrica),
    /// relogio falso, log capturado no lugar do ILogService e log do host capturado. Configuracao por AddInMemoryCollection.
    /// </summary>
    public sealed class FabricaArmazenamentoDisco : AutenticacaoWebFactory
    {
        private const string NomeRaiz = "armazenamento";
        private const string NomePastaDados = "dados";
        private const string PrefixoPastaTemporaria = "autovia-d5-";
        private const string FormatoGuid = "N";
        private const string NomeIntegracao = "Armazenamento de teste (D5)";
        private const string UrlBaseIntegracao = "https://armazenamento.exemplo.com";
        private const int VeiculoDeTeste = 1;

        private readonly OpcoesFabricaArmazenamentoDisco _opcoes;
        private readonly bool _donaDaPasta;
        private readonly FakeIntegrationRepository _integracoes = new FakeIntegrationRepository();

        /// <summary>Construtor: sobe a fabrica com as opcoes informadas (padrao = armazenamento em DISCO_LOCAL na pasta temporaria).</summary>
        public FabricaArmazenamentoDisco(OpcoesFabricaArmazenamentoDisco? opcoes = null)
            : base(OpcoesFabricaArmazenamentoDisco.LimiteAlto, null)
        {
            _opcoes = opcoes ?? new OpcoesFabricaArmazenamentoDisco();
            _donaDaPasta = _opcoes.PastaTemporaria == null;
            PastaTemporaria = _opcoes.PastaTemporaria ?? Path.Combine(Path.GetTempPath(), PrefixoPastaTemporaria + Guid.NewGuid().ToString(FormatoGuid));
            Raiz = _opcoes.PastaDadosUsuarioNoTeste
                ? Path.Combine(PastaTemporaria, NomePastaDados, ConstantsValues.PastaAutoViaDadosLocais, ConstantsValues.PastaArmazenamentoPadrao)
                : Path.Combine(PastaTemporaria, NomeRaiz);
            Relogio = new FakeTimeProvider(_opcoes.Inicio ?? DateTimeOffset.UtcNow);
            _integracoes.Add(MontarIntegracaoArmazenamento());
            Fotos.VeiculosAtivos.Add(VeiculoDeTeste);
        }

        /// <summary>Pasta temporaria propria desta fabrica (a raiz do armazenamento fica dentro dela).</summary>
        public string PastaTemporaria { get; }

        /// <summary>Raiz do armazenamento em pasta usada pelo teste (ou a pasta de dados do usuario, quando o teste a informa).</summary>
        public string Raiz { get; }

        /// <summary>Pasta dos arquivos servidos (arquivos dentro da raiz).</summary>
        public string PastaDosArquivos => Path.Combine(Raiz, ConstantsValues.SubpastaArquivosArmazenamento);

        /// <summary>Relogio falso compartilhado por API e teste.</summary>
        public FakeTimeProvider Relogio { get; }

        /// <summary>Log capturado no lugar do ILogService real.</summary>
        public CapturaLogArmazenamentoDisco Log { get; } = new CapturaLogArmazenamentoDisco();

        /// <summary>Log do host (ILogger) capturado, para checar tudo o que o ASP.NET registrou.</summary>
        public CapturaLoggerArmazenamentoDisco LogsDoHost { get; } = new CapturaLoggerArmazenamentoDisco();

        /// <summary>Repositorio de fotos em memoria, com o veiculo de teste ativo.</summary>
        public FakeVeiculoFotoRepository Fotos { get; } = new FakeVeiculoFotoRepository();

        /// <summary>Processador de foto falso: devolve sempre o mesmo conteudo recodificado.</summary>
        public FakeProcessadorFotoService Processador { get; } = new FakeProcessadorFotoService();

        /// <summary>Repositorio de veiculos em memoria: o catalogo le a capa pela navegacao Fotos de cada veiculo semeado.</summary>
        public FakeVeiculoRepository Veiculos { get; } = new FakeVeiculoRepository();

        /// <summary>Provedor S3 falso, usado so quando o teste escolhe S3_COMPATIVEL.</summary>
        public ProvedorS3FalsoArmazenamentoDisco S3 { get; } = new ProvedorS3FalsoArmazenamentoDisco();

        protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
            valores[ConstantsValues.ChaveArmazenamentoDiretorio] = DiretorioConfigurado();
            valores[ConstantsValues.ChaveArmazenamentoUrlPublicaApi] = _opcoes.UrlPublicaApi;
            valores[ConstantsValues.ChaveLimiteLeituraArquivo] = _opcoes.LimiteIp.ToString(CultureInfo.InvariantCulture);
            valores[ConstantsValues.ChaveLimiteLeituraArquivoGlobal] = _opcoes.Teto.ToString(CultureInfo.InvariantCulture);
            valores[ConstantsValues.ChaveExecucaoEmContainer] = _opcoes.EmContainer ? bool.TrueString : bool.FalseString;
            valores[ConstantsValues.ChaveLimiteEnvioFotoVeiculo] = OpcoesFabricaArmazenamentoDisco.LimiteAlto.ToString(CultureInfo.InvariantCulture);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(ConfigurarServicos);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && _donaDaPasta && Directory.Exists(PastaTemporaria))
            {
                Directory.Delete(PastaTemporaria, true);
            }
        }

        private string DiretorioConfigurado()
        {
            if (_opcoes.PastaDadosUsuarioNoTeste)
            {
                return string.Empty;
            }

            return _opcoes.Diretorio ?? Raiz;
        }

        private void ConfigurarServicos(IServiceCollection services)
        {
            services.RemoveAll<IIntegrationRepository>();
            services.AddSingleton<IIntegrationRepository>(_integracoes);

            services.RemoveAll<ILogService>();
            services.AddSingleton<ILogService>(Log);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Relogio);

            services.AddSingleton<ILoggerProvider>(LogsDoHost);

            services.RemoveAll<IVeiculoFotoRepository>();
            services.AddSingleton<IVeiculoFotoRepository>(Fotos);

            services.RemoveAll<IProcessadorFotoService>();
            services.AddSingleton<IProcessadorFotoService>(Processador);

            services.RemoveAll<IVeiculoRepository>();
            services.AddSingleton<IVeiculoRepository>(Veiculos);

            if (_opcoes.SegredoSubstituto != null)
            {
                services.RemoveAll<ISegredoAssinaturaArmazenamento>();
                services.AddSingleton<ISegredoAssinaturaArmazenamento>(_opcoes.SegredoSubstituto);
            }

            if (_opcoes.ProvedorS3Falso)
            {
                services.RemoveAll<IArmazenamentoProvider>();
                services.AddScoped<IArmazenamentoProvider, DiscoArmazenamentoProvider>();
                services.AddSingleton<IArmazenamentoProvider>(S3);
            }

            if (_opcoes.PastaDadosUsuarioNoTeste)
            {
                services.RemoveAll<IRaizArmazenamentoDisco>();
                services.AddSingleton<IRaizArmazenamentoDisco>(new RaizArmazenamentoDisco(
                    Options.Create(new ArmazenamentoDiscoOptions()),
                    false,
                    Path.Combine(PastaTemporaria, NomePastaDados),
                    AppContext.BaseDirectory,
                    Log,
                    true));
            }
        }

        private Integration MontarIntegracaoArmazenamento()
        {
            List<IntegrationSetting> configuracoes = new List<IntegrationSetting>
            {
                new IntegrationSetting { Chave = ConstantsArmazenamento.ChaveProvedor, Valor = _opcoes.Provedor, Protegido = false }
            };

            if (_opcoes.Expiracao != null)
            {
                configuracoes.Add(new IntegrationSetting { Chave = ConstantsArmazenamento.ChaveExpiracao, Valor = _opcoes.Expiracao, Protegido = false });
            }

            return new Integration
            {
                Codigo = ConstantsIntegration.Armazenamento,
                Nome = NomeIntegracao,
                BaseUrl = UrlBaseIntegracao,
                Ativo = true,
                Settings = configuracoes
            };
        }
    }

    /// <summary>Opcoes da fabrica do armazenamento em pasta. Padrao = DISCO_LOCAL, endereco publico em loopback, sem container.</summary>
    public sealed class OpcoesFabricaArmazenamentoDisco
    {
        /// <summary>Limite alto para os testes que nao medem limite (evita 429 proprio).</summary>
        public const int LimiteAlto = 1000;

        private const string UrlPublicaLocal = "http://localhost";

        /// <summary>Pasta temporaria a reutilizar (a fabrica nao a apaga). Nula = cria uma nova e a apaga no fim.</summary>
        public string? PastaTemporaria { get; init; }

        /// <summary>Valor de Armazenamento:Diretorio. Nulo = a raiz do teste; vazio = padrao da maquina.</summary>
        public string? Diretorio { get; init; }

        /// <summary>Valor de Armazenamento:UrlPublicaApi. Vazio = derivado da requisicao (so em loopback).</summary>
        public string UrlPublicaApi { get; init; } = UrlPublicaLocal;

        /// <summary>Indicador de container (DOTNET_RUNNING_IN_CONTAINER) pela configuracao em memoria.</summary>
        public bool EmContainer { get; init; }

        /// <summary>Quando verdadeiro, a raiz vem da pasta de dados do usuario informada ao teste (Windows fora de container).</summary>
        public bool PastaDadosUsuarioNoTeste { get; init; }

        /// <summary>Limite por IP por minuto da rota publica de leitura.</summary>
        public int LimiteIp { get; init; } = LimiteAlto;

        /// <summary>Teto por minuto da rota publica de leitura.</summary>
        public int Teto { get; init; } = LimiteAlto;

        /// <summary>Provedor escolhido na integracao ARMAZENAMENTO (chave 'provedor').</summary>
        public string Provedor { get; init; } = ConstantsArmazenamento.ProvedorDiscoLocal;

        /// <summary>Prazo configurado (url_expiracao_segundos). Nulo = sem a chave (padrao de 900).</summary>
        public string? Expiracao { get; init; }

        /// <summary>Quando verdadeiro, o provedor S3 do teste substitui o real, sob o nome S3_COMPATIVEL.</summary>
        public bool ProvedorS3Falso { get; init; }

        /// <summary>Segredo de assinatura substituto (para provocar falha nao prevista). Nulo = o real.</summary>
        public ISegredoAssinaturaArmazenamento? SegredoSubstituto { get; init; }

        /// <summary>Instante inicial do relogio falso. Nulo = agora (UTC).</summary>
        public DateTimeOffset? Inicio { get; init; }
    }

    /// <summary>ILogService em memoria: separa mensagem de erro, sobrecarga de excecao e informativo, para o teste conferir cada um.</summary>
    public sealed class CapturaLogArmazenamentoDisco : ILogService
    {
        private const string TipoTexto = "texto";
        private const string TipoExcecao = "excecao";
        private const string TipoInformativo = "informativo";

        private readonly object _trava = new object();
        private readonly List<RegistroCapturado> _registros = new List<RegistroCapturado>();

        public void Write(string message, string friendlyMessage, string source)
        {
            Gravar(new RegistroCapturado(TipoTexto, message, friendlyMessage, source));
        }

        public void Write(Exception exception, string friendlyMessage, string source)
        {
            Gravar(new RegistroCapturado(TipoExcecao, exception.ToString(), friendlyMessage, source));
        }

        public void WriteInfo(string message, string source)
        {
            Gravar(new RegistroCapturado(TipoInformativo, message, string.Empty, source));
        }

        /// <summary>Quantos registros de texto tem exatamente esta mensagem.</summary>
        public int Contar(string mensagem)
        {
            lock (_trava)
            {
                return _registros.Count(registro => registro.Tipo == TipoTexto && registro.Mensagem == mensagem);
            }
        }

        /// <summary>Mensagens dos registros de texto, na ordem.</summary>
        public IReadOnlyList<string> Mensagens()
        {
            lock (_trava)
            {
                List<string> mensagens = _registros.Where(registro => registro.Tipo == TipoTexto).Select(registro => registro.Mensagem).ToList();
                return mensagens;
            }
        }

        /// <summary>Textos das sobrecargas de excecao (ToString completo), na ordem.</summary>
        public IReadOnlyList<string> Excecoes()
        {
            lock (_trava)
            {
                List<string> excecoes = _registros.Where(registro => registro.Tipo == TipoExcecao).Select(registro => registro.Mensagem).ToList();
                return excecoes;
            }
        }

        /// <summary>Origem (source) de cada registro, na ordem: o teste confere que nenhuma origem leva chave, assinatura ou caminho.</summary>
        public IReadOnlyList<string> Origens()
        {
            lock (_trava)
            {
                List<string> origens = _registros.Select(registro => registro.Origem).ToList();
                return origens;
            }
        }

        /// <summary>Todo o conteudo registrado (mensagem e mensagem amigavel), para checar vazamento.</summary>
        public IReadOnlyList<string> TodosOsTextos()
        {
            lock (_trava)
            {
                List<string> textos = _registros.Select(registro => registro.Mensagem + " " + registro.Amigavel).ToList();
                return textos;
            }
        }

        private void Gravar(RegistroCapturado registro)
        {
            lock (_trava)
            {
                _registros.Add(registro);
            }
        }

        private sealed record RegistroCapturado(string Tipo, string Mensagem, string Amigavel, string Origem);
    }

    /// <summary>ILoggerProvider em memoria: guarda o que o ASP.NET registra no host, com o nivel de cada registro.</summary>
    public sealed class CapturaLoggerArmazenamentoDisco : ILoggerProvider
    {
        private readonly object _trava = new object();
        private readonly List<RegistroHost> _registros = new List<RegistroHost>();

        public ILogger CreateLogger(string categoryName)
        {
            return new LoggerCapturado(this);
        }

        public void Dispose()
        {
        }

        /// <summary>Todo o texto registrado pelo host, inclusive a excecao completa quando houve uma.</summary>
        public IReadOnlyList<string> Textos()
        {
            lock (_trava)
            {
                List<string> textos = _registros.Select(registro => registro.Texto).ToList();
                return textos;
            }
        }

        /// <summary>Quantos registros de erro (nivel Error ou acima) o host gravou.</summary>
        public int Erros()
        {
            lock (_trava)
            {
                return _registros.Count(registro => registro.Nivel >= LogLevel.Error);
            }
        }

        private void Registrar(LogLevel nivel, string texto)
        {
            lock (_trava)
            {
                _registros.Add(new RegistroHost(nivel, texto));
            }
        }

        private sealed record RegistroHost(LogLevel Nivel, string Texto);

        private sealed class LoggerCapturado : ILogger
        {
            private readonly CapturaLoggerArmazenamentoDisco _provedor;

            public LoggerCapturado(CapturaLoggerArmazenamentoDisco provedor)
            {
                _provedor = provedor;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                string texto = formatter(state, exception);
                if (exception != null)
                {
                    texto = texto + " " + exception.ToString();
                }

                _provedor.Registrar(logLevel, texto);
            }
        }
    }

    /// <summary>Provedor S3 falso: conta as chamadas e devolve URLs de mentira, para mostrar que o gateway escolheu o S3.</summary>
    public sealed class ProvedorS3FalsoArmazenamentoDisco : IArmazenamentoProvider
    {
        private const string UrlBaseS3 = "https://s3.exemplo.com/autovia/";
        private const int ValidadeSimuladaMinutos = 15;

        public string Nome => ConstantsArmazenamento.ProvedorS3Compativel;

        /// <summary>Quantas chamadas o provedor recebeu.</summary>
        public int Chamadas { get; private set; }

        public Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.FromResult(Montar(chave));
        }

        public Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.FromResult<ArquivoArmazenado?>(Montar(chave));
        }

        public Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(ConfiguracaoIntegracao configuracao, string bucket, IReadOnlyCollection<string> chaves, int expiracaoSegundos, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            Dictionary<string, ArquivoArmazenado> urls = new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
            foreach (string chave in chaves)
            {
                urls[chave] = Montar(chave);
            }

            return Task.FromResult<IReadOnlyDictionary<string, ArquivoArmazenado>>(urls);
        }

        private static ArquivoArmazenado Montar(string chave)
        {
            return new ArquivoArmazenado(UrlBaseS3 + chave, DateTimeOffset.UtcNow.AddMinutes(ValidadeSimuladaMinutos));
        }
    }

    /// <summary>Segredo substituto que lanca excecao nao prevista com a mensagem informada (caminho e chave), para provocar o 500 da rota.</summary>
    public sealed class SegredoQueFalhaArmazenamentoDisco : ISegredoAssinaturaArmazenamento
    {
        private readonly string _mensagem;

        public SegredoQueFalhaArmazenamentoDisco(string mensagem)
        {
            _mensagem = mensagem;
        }

        public byte[] ObterOuCriar()
        {
            throw new InvalidOperationException(_mensagem);
        }

        public byte[]? ObterSeExistir()
        {
            throw new InvalidOperationException(_mensagem);
        }
    }
}
