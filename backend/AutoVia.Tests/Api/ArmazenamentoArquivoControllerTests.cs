using AutoVia.Api.ActionResults;
using AutoVia.Api.Controllers;
using AutoVia.Api.Extensions;
using AutoVia.Api.Requests.Armazenamento;
using AutoVia.Api.Validators;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Controller da leitura publica de arquivo com servico falso e validador, teto e registro reais (sem banco, sem subir a API).
    /// Cobre o formato do link (400 com um registro), If-None-Match, o teto (429 com registro agregado), o cancelamento (um
    /// registro informativo) e a falha nao prevista (um registro so, com o nome do tipo, e 500 sem relancar).
    /// </summary>
    public sealed class ArmazenamentoArquivoControllerTests
    {
        private const string ChaveValida = "veiculos/1/fotos/abc.jpg";
        private const string ExpValida = "1700000000";
        private const int LimiteTetoAlto = 1000;
        private const int LimiteTetoUm = 1;
        private const int JanelaTetoMinutos = 1;
        private const string CabecalhoIfNoneMatch = "If-None-Match";
        private const string TextoCaminhoNaFalha = "/dados/armazenamento/arquivos/";
        private const string ConsultaVazia = "?";

        private static readonly string SigValida = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres);

        /// <summary>Servico de leitura falso: grava os argumentos recebidos e devolve o resultado ou lanca a falha configurada.</summary>
        private sealed class ServicoLeituraFalso : IArmazenamentoLeituraService
        {
            public Exception? Falha { get; set; }

            public ArquivoParaLeitura? Resultado { get; set; }

            public int Chamadas { get; private set; }

            public string? UltimaChave { get; private set; }

            public long UltimoExp { get; private set; }

            public string? UltimaAssinatura { get; private set; }

            public IReadOnlyCollection<string>? UltimosEtags { get; private set; }

            public Task<ArquivoParaLeitura> AbrirAsync(string chave, long expiraEmUnix, string assinatura, IReadOnlyCollection<string> etagsDoCliente, CancellationToken cancellationToken = default)
            {
                Chamadas++;
                UltimaChave = chave;
                UltimoExp = expiraEmUnix;
                UltimaAssinatura = assinatura;
                UltimosEtags = etagsDoCliente;

                if (Falha != null)
                {
                    return Task.FromException<ArquivoParaLeitura>(Falha);
                }

                return Task.FromResult(Resultado ?? new ArquivoParaLeitura(new byte[] { 1, 2, 3 }, ConstantsArmazenamento.MimeJpeg, "1-2", false));
            }
        }

        /// <summary>ILogService que separa as sobrecargas: texto, excecao (nao pode ser usada pelo codigo novo) e informativo.</summary>
        private sealed class LogCapturado : ILogService
        {
            public List<string> Mensagens { get; } = new List<string>();

            public List<string> Amigaveis { get; } = new List<string>();

            public List<Exception> Excecoes { get; } = new List<Exception>();

            public List<string> Informativos { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                Mensagens.Add(message);
                Amigaveis.Add(friendlyMessage);
            }

            public void Write(Exception exception, string friendlyMessage, string source)
            {
                Excecoes.Add(exception);
            }

            public void WriteInfo(string message, string source)
            {
                Informativos.Add(message);
            }
        }

        /// <summary>Monta a consulta crua do link com os valores escapados (quando informados).</summary>
        private static string MontarConsulta(string? chave, string? exp, string? sig)
        {
            List<string> partes = new List<string>();

            if (chave != null)
            {
                partes.Add(ConstantsValues.ParametroChaveLink + "=" + Uri.EscapeDataString(chave));
            }

            if (exp != null)
            {
                partes.Add(ConstantsValues.ParametroExpiracaoLink + "=" + Uri.EscapeDataString(exp));
            }

            if (sig != null)
            {
                partes.Add(ConstantsValues.ParametroAssinaturaLink + "=" + Uri.EscapeDataString(sig));
            }

            return "?" + string.Join("&", partes);
        }

        private static ArmazenamentoArquivoController CriarController(ServicoLeituraFalso servico, LogCapturado log, string consulta, int limiteTeto)
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Request.QueryString = new QueryString(consulta);
            contexto.Response.Body = new MemoryStream();

            ArmazenamentoArquivoController controller = new ArmazenamentoArquivoController(
                servico,
                new LerArquivoArmazenadoRequestValidator(),
                new TetoLeituraArquivo(limiteTeto, TimeSpan.FromMinutes(JanelaTetoMinutos)),
                new RegistroRecusasLimiteLeitura(log, new FakeTimeProvider(DateTimeOffset.UtcNow)),
                log);
            controller.ControllerContext = new ControllerContext { HttpContext = contexto };

            return controller;
        }

        /// <summary>Le o corpo que a acao escreveu (o MemoryStream de CriarController).</summary>
        private static string LerCorpo(HttpContext contexto)
        {
            contexto.Response.Body.Seek(0, SeekOrigin.Begin);
            using StreamReader leitor = new StreamReader(contexto.Response.Body, Encoding.UTF8, false, 1024, true);
            return leitor.ReadToEnd();
        }

        [Fact]
        public async Task LinkValido_ChamaOServicoComOsValoresEDevolveOResultadoDeLeitura()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);

            IActionResult resultado = await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.IsType<LeituraArquivoArmazenadoResult>(resultado);
            Assert.Equal(1, servico.Chamadas);
            Assert.Equal(ChaveValida, servico.UltimaChave);
            Assert.Equal(long.Parse(ExpValida, CultureInfo.InvariantCulture), servico.UltimoExp);
            Assert.Equal(SigValida, servico.UltimaAssinatura);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task ChaveAusente_E400ComUmRegistroEOServicoNaoEChamado()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(null, ExpValida, SigValida), LimiteTetoAlto);

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Ler(null, ExpValida, SigValida));

            Assert.All(excecao.Errors, erro => Assert.Equal(ConstantsMessage.ErroArmazenamentoLinkInvalido, erro.ErrorMessage));
            Assert.Equal(0, servico.Chamadas);
            Assert.Single(log.Mensagens);
            Assert.Equal(ConstantsMessage.LogArmazenamentoLeituraLinkMalformado, log.Mensagens[0]);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoLinkInvalido, log.Amigaveis[0]);
            Assert.Empty(log.Excecoes);
        }

        [Theory]
        [InlineData("01700000000")]
        [InlineData("17000000000")]
        [InlineData("1700000000\n")]
        [InlineData(" 1700000000")]
        [InlineData("1700000000 ")]
        [InlineData("1700000000\r\n")]
        [InlineData("170000000a")]
        [InlineData("0")]
        [InlineData("")]
        public async Task ExpMalformado_E400ComUmRegistroSemChamarOServico(string exp)
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, exp, SigValida), LimiteTetoAlto);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Ler(ChaveValida, exp, SigValida));

            Assert.Equal(0, servico.Chamadas);
            Assert.Single(log.Mensagens);
            Assert.Equal(ConstantsMessage.LogArmazenamentoLeituraLinkMalformado, log.Mensagens[0]);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(44)]
        public async Task SigComTamanhoErrado_E400(int tamanho)
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            string sig = new string('A', tamanho);
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, sig), LimiteTetoAlto);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Ler(ChaveValida, ExpValida, sig));

            Assert.Equal(0, servico.Chamadas);
            Assert.Single(log.Mensagens);
        }

        [Fact]
        public async Task SigComCaractereForaDoAlfabeto_E400()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            string sig = "+" + new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 1);
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, sig), LimiteTetoAlto);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Ler(ChaveValida, ExpValida, sig));

            Assert.Equal(0, servico.Chamadas);
        }

        [Fact]
        public async Task QuebraDeLinhaFinalNoExpOuNaSig_E400()
        {
            string sigComLf = SigValida + "\n";
            string sigQuarentaEDoisMaisLf = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 1) + "\n";
            string expComLf = ExpValida + "\n";

            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();

            ArmazenamentoArquivoController primeiro = CriarController(servico, log, MontarConsulta(ChaveValida, expComLf, SigValida), LimiteTetoAlto);
            ArmazenamentoArquivoController segundo = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, sigComLf), LimiteTetoAlto);
            ArmazenamentoArquivoController terceiro = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, sigQuarentaEDoisMaisLf), LimiteTetoAlto);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => primeiro.Ler(ChaveValida, expComLf, SigValida));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => segundo.Ler(ChaveValida, ExpValida, sigComLf));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => terceiro.Ler(ChaveValida, ExpValida, sigQuarentaEDoisMaisLf));

            Assert.Equal(0, servico.Chamadas);
            Assert.Equal(3, log.Mensagens.Count);
        }

        [Fact]
        public async Task ChaveComQuebraDeLinhaFinal_PassaOValidadorEChegaAoServico()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            string chaveComLf = ChaveValida + "\n";
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(chaveComLf, ExpValida, SigValida), LimiteTetoAlto);

            IActionResult resultado = await controller.Ler(chaveComLf, ExpValida, SigValida);

            Assert.IsType<LeituraArquivoArmazenadoResult>(resultado);
            Assert.Equal(1, servico.Chamadas);
            Assert.Equal(chaveComLf, servico.UltimaChave);
        }

        [Fact]
        public async Task ParametroRepetido_E400ComUmRegistro()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            string consulta = "?chave=a&chave=b&" + ConstantsValues.ParametroExpiracaoLink + "=" + ExpValida + "&" + ConstantsValues.ParametroAssinaturaLink + "=" + SigValida;
            ArmazenamentoArquivoController controller = CriarController(servico, log, consulta, LimiteTetoAlto);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => controller.Ler("a", ExpValida, SigValida));

            Assert.Equal(0, servico.Chamadas);
            Assert.Single(log.Mensagens);
        }

        [Fact]
        public async Task IfNoneMatch_ChegaAoServicoSemAspasESemPrefixoW()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);
            controller.HttpContext.Request.Headers[CabecalhoIfNoneMatch] = "W/\"abc-def\", \"123-456\"";

            await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.Equal(new[] { "abc-def", "123-456" }, servico.UltimosEtags);
        }

        [Fact]
        public async Task IfNoneMatchCoringa_E_IfNoneMatchIlegivel_ViramListaVazia()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);
            controller.HttpContext.Request.Headers[CabecalhoIfNoneMatch] = "*";

            await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.Empty(servico.UltimosEtags!);

            ServicoLeituraFalso outroServico = new ServicoLeituraFalso();
            ArmazenamentoArquivoController outro = CriarController(outroServico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);
            outro.HttpContext.Request.Headers[CabecalhoIfNoneMatch] = "abc-sem-aspas";

            await outro.Ler(ChaveValida, ExpValida, SigValida);

            Assert.Empty(outroServico.UltimosEtags!);
        }

        [Fact]
        public async Task TetoEsgotado_E429ComRegistroAgregadoEServicoNaoEChamadoDeNovo()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso();
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoUm);

            await controller.Ler(ChaveValida, ExpValida, SigValida);
            await Assert.ThrowsAsync<LimiteExcedidoException>(() => controller.Ler(ChaveValida, ExpValida, SigValida));

            Assert.Equal(1, servico.Chamadas);
            Assert.Single(log.Mensagens);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota, 1), log.Mensagens[0]);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, log.Amigaveis[0]);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task CancelamentoDuranteALeitura_DevolveVazioComUmRegistroInformativoESemErro()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            ServicoLeituraFalso servico = new ServicoLeituraFalso { Falha = new OperationCanceledException(cancelamento.Token) };
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);
            controller.HttpContext.RequestAborted = cancelamento.Token;

            IActionResult resultado = await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.IsType<EmptyResult>(resultado);
            Assert.Single(log.Informativos);
            Assert.Equal(ConstantsMessage.LogArmazenamentoLeituraCancelada, log.Informativos[0]);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task FalhaNaoPrevista_RegistraUmaVezSoComONomeDoTipoERespondeOs500SemRelancar()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso { Falha = new InvalidOperationException(TextoCaminhoNaFalha + ChaveValida) };
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);

            IActionResult resultado = await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.IsType<EmptyResult>(resultado);
            Assert.Equal(StatusCodes.Status500InternalServerError, controller.HttpContext.Response.StatusCode);
            Assert.Single(log.Mensagens);
            Assert.Equal(nameof(InvalidOperationException), log.Mensagens[0]);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoAbrirArquivo, log.Amigaveis[0]);
            Assert.Empty(log.Excecoes);
            Assert.DoesNotContain(ChaveValida, LerCorpo(controller.HttpContext));
            Assert.DoesNotContain(TextoCaminhoNaFalha, LerCorpo(controller.HttpContext));
        }

        [Fact]
        public async Task FalhaJaRegistradaPeloServico_RespondeOs500SemRegistroNenhum()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso { Falha = new ArmazenamentoLeituraException(nameof(InvalidOperationException)) };
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);

            IActionResult resultado = await controller.Ler(ChaveValida, ExpValida, SigValida);

            Assert.IsType<EmptyResult>(resultado);
            Assert.Equal(StatusCodes.Status500InternalServerError, controller.HttpContext.Response.StatusCode);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Informativos);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task RecusaDoServico_RelancaSemRegistrarNoController()
        {
            ServicoLeituraFalso servico = new ServicoLeituraFalso { Falha = new NotFoundException(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado) };
            LogCapturado log = new LogCapturado();
            ArmazenamentoArquivoController controller = CriarController(servico, log, MontarConsulta(ChaveValida, ExpValida, SigValida), LimiteTetoAlto);

            await Assert.ThrowsAsync<NotFoundException>(() => controller.Ler(ChaveValida, ExpValida, SigValida));

            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }
    }
}
