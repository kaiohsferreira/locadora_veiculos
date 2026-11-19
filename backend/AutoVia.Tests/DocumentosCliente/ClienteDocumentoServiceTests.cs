using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// Servico de documentos do cliente com repositorio e armazenamento em memoria, relogio fixo e log em memoria. Cobre o
    /// dono (401 e 404 antes do armazenamento), o cliente ativo, o teto de 24 horas (429 sem gravar), o tipo pelo conteudo,
    /// os mesmos bytes no armazenamento, a substituicao com remocao do antigo, a falha de gravacao, a falha ao remover o
    /// antigo, a listagem e o que nunca vai para o log.
    /// </summary>
    public sealed class ClienteDocumentoServiceTests
    {
        private const int IdCliente = 1;
        private const int IdOutroCliente = 2;
        private const string NomeCnh = "cnh-frente-verso.pdf";
        private const string NomeComprovante = "conta-de-luz.jpg";
        private const int TamanhoCorpoPdf = 64;
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 19, 10, 0, 0);
        private static readonly Regex PadraoChave = new Regex(@"^clientes/1/documentos/[0-9a-f]{32}\.(pdf|jpg|png)$", RegexOptions.CultureInvariant);

        private sealed class Cenario
        {
            public Cenario(ClienteDocumentoService servico, FakeClienteDocumentoRepository repositorio, FakeArmazenamentoGateway armazenamento, FakeLogService log, ClientesOptions opcoes)
            {
                Servico = servico;
                Repositorio = repositorio;
                Armazenamento = armazenamento;
                Log = log;
                Opcoes = opcoes;
            }

            public ClienteDocumentoService Servico { get; }

            public FakeClienteDocumentoRepository Repositorio { get; }

            public FakeArmazenamentoGateway Armazenamento { get; }

            public FakeLogService Log { get; }

            public ClientesOptions Opcoes { get; }
        }

        private static Cenario CriarCenario()
        {
            FakeClienteDocumentoRepository repositorio = new FakeClienteDocumentoRepository();
            repositorio.ClientesAtivos.Add(IdCliente);
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            ClientesOptions opcoes = new ClientesOptions();
            ClienteDocumentoService servico = new ClienteDocumentoService(repositorio, armazenamento, new FakeRelogio(AgoraFixo), opcoes, log);

            return new Cenario(servico, repositorio, armazenamento, log, opcoes);
        }

        private static byte[] Pdf()
        {
            byte[] conteudo = new byte[TamanhoCorpoPdf];
            ConstantsValues.AssinaturaPdf.CopyTo(conteudo, 0);

            return conteudo;
        }

        private static byte[] Jpeg()
        {
            byte[] conteudo = new byte[TamanhoCorpoPdf];
            byte[] assinaturaJpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            assinaturaJpeg.CopyTo(conteudo, 0);

            return conteudo;
        }

        private static async Task<ReturnClienteDocumentoVO> EnviarAsync(Cenario cenario, byte[] conteudo, string tipo, string nome, int idRota = IdCliente, int idSessao = IdCliente)
        {
            return await cenario.Servico.EnviarAsync(idRota, idSessao, tipo, conteudo, nome);
        }

        private static ClienteDocumento SemearAtivo(FakeClienteDocumentoRepository repositorio, int id, TipoDocumentoCliente tipo, string chave, DateTime criadoEm)
        {
            ClienteDocumento documento = new ClienteDocumento
            {
                IdClienteDocumento = id,
                IdCliente = IdCliente,
                Tipo = tipo,
                ChaveObjeto = chave,
                NomeArquivo = NomeCnh,
                TipoConteudo = ConstantsValues.TipoConteudoPdf,
                TamanhoBytes = TamanhoCorpoPdf,
                CreatedAt = criadoEm,
                UpdatedAt = criadoEm
            };

            return repositorio.Semear(documento);
        }

        // --- Dono e cliente ativo: antes de qualquer armazenamento ---

        [Fact]
        public async Task SemIdNaSessao_Da401SemArmazenamento()
        {
            Cenario cenario = CriarCenario();

            await Assert.ThrowsAsync<UnauthorizedException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh, IdCliente, 0));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.SubstituicoesChamadas);
        }

        [Fact]
        public async Task IdDeOutroClienteNaRota_Da404SemChamarArmazenamentoNemGravacao()
        {
            Cenario cenario = CriarCenario();

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh, IdOutroCliente, IdCliente));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Armazenamento.AssinaturaLoteCount);
            Assert.Equal(0, cenario.Repositorio.SubstituicoesChamadas);
        }

        [Fact]
        public async Task RotaZero_Da404()
        {
            Cenario cenario = CriarCenario();

            await Assert.ThrowsAsync<NotFoundException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh, 0, IdCliente));
        }

        [Fact]
        public async Task ClienteDesativadoOuInexistente_Da404SemArmazenamento()
        {
            Cenario cenario = CriarCenario();
            cenario.Repositorio.ClientesAtivos.Clear();

            await Assert.ThrowsAsync<NotFoundException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
        }

        // --- Teto de envios em 24 horas ---

        [Fact]
        public async Task TetoDeVinteEnviosEmVinteQuatroHoras_Da429SemGravar()
        {
            Cenario cenario = CriarCenario();
            for (int i = 0; i < ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao; i++)
            {
                SemearAtivo(cenario.Repositorio, i + 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/" + i + ".pdf", AgoraFixo.AddHours(-1));
            }

            LimiteExcedidoException excecao = await Assert.ThrowsAsync<LimiteExcedidoException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));

            Assert.Equal(ConstantsMessage.ErroLimiteEnviosDocumento, excecao.Message);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.SubstituicoesChamadas);
        }

        [Fact]
        public async Task EnviosComMaisDeVinteQuatroHoras_NaoContamNoTeto()
        {
            Cenario cenario = CriarCenario();
            for (int i = 0; i < ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao; i++)
            {
                SemearAtivo(cenario.Repositorio, i + 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/" + i + ".pdf", AgoraFixo.AddHours(-25));
            }

            ReturnClienteDocumentoVO retorno = await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            Assert.NotNull(retorno);
            Assert.Equal(AgoraFixo.AddHours(-ConstantsValues.JanelaEnviosDocumentoClienteHoras), cenario.Repositorio.UltimoDesdeConsultado);
        }

        [Fact]
        public async Task TetoContaDesativadosTambem()
        {
            Cenario cenario = CriarCenario();
            ClienteDocumento desativado = SemearAtivo(cenario.Repositorio, 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/x.pdf", AgoraFixo.AddHours(-1));
            desativado.Disable();
            for (int i = 1; i < ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao; i++)
            {
                SemearAtivo(cenario.Repositorio, i + 1, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, "clientes/1/documentos/" + i + ".pdf", AgoraFixo.AddHours(-1));
            }

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));
        }

        [Fact]
        public async Task DezenoveEnviosNaJanela_AindaAceita()
        {
            Cenario cenario = CriarCenario();
            for (int i = 0; i < ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao - 1; i++)
            {
                SemearAtivo(cenario.Repositorio, i + 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/" + i + ".pdf", AgoraFixo.AddHours(-1));
            }

            ReturnClienteDocumentoVO retorno = await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            Assert.NotNull(retorno);
            Assert.Equal(1, cenario.Armazenamento.UploadCount);
        }

        [Fact]
        public async Task TetoVemAntesDaChecagemDoConteudo_Da429NaoDa400()
        {
            Cenario cenario = CriarCenario();
            for (int i = 0; i < ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao; i++)
            {
                SemearAtivo(cenario.Repositorio, i + 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/" + i + ".pdf", AgoraFixo.AddHours(-1));
            }
            byte[] exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0 };

            await Assert.ThrowsAsync<LimiteExcedidoException>(() => EnviarAsync(cenario, exe, "CNH", NomeCnh));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
        }

        // --- Envio certo ---

        [Fact]
        public async Task EnvioDePdf_DevolveOVoCompletoEGuardaOsMesmosBytes()
        {
            Cenario cenario = CriarCenario();
            byte[] original = Pdf();

            ReturnClienteDocumentoVO retorno = await EnviarAsync(cenario, original, "CNH", NomeCnh);

            Assert.Equal("CNH", retorno.Tipo);
            Assert.Equal(NomeCnh, retorno.NomeArquivo);
            Assert.Equal(ConstantsValues.TipoConteudoPdf, retorno.TipoConteudo);
            Assert.Equal(TamanhoCorpoPdf, retorno.TamanhoBytes);
            Assert.False(string.IsNullOrEmpty(retorno.DataEnvio));
            Assert.NotNull(retorno.Url);
            Assert.NotNull(retorno.ExpiraEm);
            Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+\+00:00$", RegexOptions.CultureInvariant), retorno.ExpiraEm!);

            Assert.Single(cenario.Armazenamento.UploadKeys);
            string chave = cenario.Armazenamento.UploadKeys[0];
            Assert.Matches(PadraoChave, chave);
            Assert.EndsWith(".pdf", chave, StringComparison.Ordinal);
            Assert.True(RegrasArmazenamentoDisco.ChaveSegura(chave));
            Assert.Equal(ConstantsValues.TipoConteudoPdf, cenario.Armazenamento.UploadContentTypes[0]);
            Assert.Equal(original, cenario.Armazenamento.UploadConteudos[0]);
        }

        [Fact]
        public async Task TipoDoConteudoDecideAExtensaoEOTipoNoArmazenamento_NaoOQueFoiDeclarado()
        {
            Cenario cenario = CriarCenario();

            ReturnClienteDocumentoVO retorno = await EnviarAsync(cenario, Jpeg(), "COMPROVANTE_RESIDENCIA", NomeComprovante);

            Assert.Equal("COMPROVANTE_RESIDENCIA", retorno.Tipo);
            Assert.Equal(ConstantsValues.TipoConteudoJpeg, retorno.TipoConteudo);
            Assert.EndsWith(".jpg", cenario.Armazenamento.UploadKeys[0], StringComparison.Ordinal);
            Assert.Equal(ConstantsValues.TipoConteudoJpeg, cenario.Armazenamento.UploadContentTypes[0]);
        }

        [Fact]
        public async Task NomeGuardadoEhOLimpo_NaChaveNemNoLog()
        {
            Cenario cenario = CriarCenario();

            ReturnClienteDocumentoVO retorno = await EnviarAsync(cenario, Pdf(), "CNH", "C:\\Users\\Maria\\" + NomeCnh);

            Assert.Equal(NomeCnh, retorno.NomeArquivo);
            Assert.DoesNotContain("Maria", cenario.Armazenamento.UploadKeys[0], StringComparison.Ordinal);
            Assert.DoesNotContain(NomeCnh, cenario.Armazenamento.UploadKeys[0], StringComparison.Ordinal);
            Assert.Empty(cenario.Log.Snapshot());
        }

        [Fact]
        public async Task ConteudoNaoAceito_Da400ComoValidationSemArmazenamento()
        {
            Cenario cenario = CriarCenario();
            byte[] exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0 };

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => EnviarAsync(cenario, exe, "CNH", NomeCnh));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
        }

        [Fact]
        public async Task TipoInvalidoNoServico_Da400ComoValidationSemArmazenamento()
        {
            Cenario cenario = CriarCenario();

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => EnviarAsync(cenario, Pdf(), "cnh", NomeCnh));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
        }

        // --- Substituicao ---

        [Fact]
        public async Task SubstituicaoDesativaAAntigaERemoveOArquivoDela()
        {
            Cenario cenario = CriarCenario();
            ReturnClienteDocumentoVO primeiro = await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);
            string chavePrimeiro = cenario.Armazenamento.UploadKeys[0];

            ReturnClienteDocumentoVO segundo = await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            Assert.NotEqual(primeiro.IdClienteDocumento, segundo.IdClienteDocumento);
            Assert.Contains(chavePrimeiro, cenario.Armazenamento.RemoveKeys);
            ClienteDocumento antigo = cenario.Repositorio.Armazenados.Single(x => x.IdClienteDocumento == primeiro.IdClienteDocumento);
            Assert.NotNull(antigo.DisabledAt);
            Assert.Single(cenario.Repositorio.Armazenados, x => x.DisabledAt == null);
        }

        [Fact]
        public async Task SubstituicaoDeUmTipoNaoMexeNoOutroTipo()
        {
            Cenario cenario = CriarCenario();
            await EnviarAsync(cenario, Pdf(), "COMPROVANTE_RESIDENCIA", NomeComprovante);

            await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            Assert.Equal(2, cenario.Repositorio.Armazenados.Count(x => x.DisabledAt == null));
            Assert.Empty(cenario.Armazenamento.RemoveKeys);
        }

        [Fact]
        public async Task FalhaAoRemoverOAntigo_NaoDerrubaOEnvio_SoRegistraNoLogSemChave()
        {
            Cenario cenario = CriarCenario();
            await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);
            cenario.Armazenamento.FalharRemocao = true;

            ReturnClienteDocumentoVO segundo = await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            Assert.NotNull(segundo);
            List<string> entradas = cenario.Log.Snapshot();
            const int idDocumentoAntigo = 1;
            string mensagemEsperada = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroRemoverArquivoAntigoClienteDocumento, idDocumentoAntigo);
            Assert.Contains(entradas, entrada => entrada.Contains(mensagemEsperada, StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains("clientes/", StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains(NomeCnh, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDeGravacao_RemoveOArquivoNovoERelanca()
        {
            Cenario cenario = CriarCenario();
            cenario.Repositorio.ExcecaoAoSubstituir = new InvalidOperationException("falha simulada do banco");

            await Assert.ThrowsAsync<InvalidOperationException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));

            string chave = cenario.Armazenamento.UploadKeys[0];
            Assert.Contains(chave, cenario.Armazenamento.RemoveKeys);
            Assert.Empty(cenario.Repositorio.Armazenados);
            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroEnviarClienteDocumento, StringComparison.Ordinal));
            Assert.DoesNotContain(cenario.Log.Snapshot(), entrada => entrada.Contains(chave, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDeGravacaoEFalhaAoRemoverONovo_AindaRelancaAExcecaoOriginal()
        {
            Cenario cenario = CriarCenario();
            cenario.Repositorio.ExcecaoAoSubstituir = new InvalidOperationException("falha simulada do banco");
            cenario.Armazenamento.FalharRemocao = true;

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));

            Assert.Equal("falha simulada do banco", excecao.Message);
            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroRemoverArquivoNovoClienteDocumento, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDoArmazenamentoNoEnvio_NaoGravaNemRemove_ERelancaAIntegracao()
        {
            Cenario cenario = CriarCenario();
            cenario.Armazenamento.FalharEnvioIntegracao = true;

            await Assert.ThrowsAsync<IntegrationException>(() => EnviarAsync(cenario, Pdf(), "CNH", NomeCnh));

            Assert.Equal(0, cenario.Repositorio.SubstituicoesChamadas);
            Assert.Empty(cenario.Repositorio.Armazenados);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroEnviarClienteDocumento, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDeGravacaoComTokenCancelado_CompensacaoRodaSemCancelamento()
        {
            Cenario cenario = CriarCenario();
            cenario.Repositorio.ExcecaoAoSubstituir = new InvalidOperationException("falha simulada do banco");
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.EnviarAsync(IdCliente, IdCliente, "CNH", Pdf(), NomeCnh, cancelamento.Token));

            Assert.Single(cenario.Armazenamento.RemoveKeys);
            Assert.Equal(CancellationToken.None, cenario.Armazenamento.UltimoTokenRemocao);
        }

        [Fact]
        public async Task RemocaoDoAntigoComTokenDaRequisicao_UsaCancellationTokenNone()
        {
            Cenario cenario = CriarCenario();
            await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            ReturnClienteDocumentoVO segundo = await cenario.Servico.EnviarAsync(IdCliente, IdCliente, "CNH", Pdf(), NomeCnh, cancelamento.Token);

            Assert.NotNull(segundo);
            Assert.Single(cenario.Armazenamento.RemoveKeys);
            Assert.Equal(CancellationToken.None, cenario.Armazenamento.UltimoTokenRemocao);
        }

        // --- Listagem ---

        [Fact]
        public async Task ListaVazia_DevolveVaziaSemChamarOArmazenamento()
        {
            Cenario cenario = CriarCenario();

            List<ReturnClienteDocumentoVO> lista = await cenario.Servico.ListarAsync(IdCliente, IdCliente);

            Assert.Empty(lista);
            Assert.Equal(0, cenario.Armazenamento.AssinaturaLoteCount);
        }

        [Fact]
        public async Task ListaComUmDocumentoDeCadaTipo_CnhAntesDoComprovante()
        {
            Cenario cenario = CriarCenario();
            await EnviarAsync(cenario, Pdf(), "COMPROVANTE_RESIDENCIA", NomeComprovante);
            await EnviarAsync(cenario, Pdf(), "CNH", NomeCnh);

            List<ReturnClienteDocumentoVO> lista = await cenario.Servico.ListarAsync(IdCliente, IdCliente);

            Assert.Equal(2, lista.Count);
            Assert.Equal("CNH", lista[0].Tipo);
            Assert.Equal("COMPROVANTE_RESIDENCIA", lista[1].Tipo);
            Assert.All(lista, item => Assert.NotNull(item.Url));
        }

        [Fact]
        public async Task DoisAtivosDoMesmoTipo_ValeODeMaiorId()
        {
            Cenario cenario = CriarCenario();
            SemearAtivo(cenario.Repositorio, 5, TipoDocumentoCliente.CNH, "clientes/1/documentos/a.pdf", AgoraFixo.AddHours(-2));
            SemearAtivo(cenario.Repositorio, 9, TipoDocumentoCliente.CNH, "clientes/1/documentos/b.pdf", AgoraFixo.AddHours(-1));

            List<ReturnClienteDocumentoVO> lista = await cenario.Servico.ListarAsync(IdCliente, IdCliente);

            ReturnClienteDocumentoVO item = Assert.Single(lista);
            Assert.Equal(9, item.IdClienteDocumento);
        }

        [Fact]
        public async Task ItemSemLinkAssinado_NaoDerrubaALista()
        {
            Cenario cenario = CriarCenario();
            SemearAtivo(cenario.Repositorio, 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/sumiu.pdf", AgoraFixo.AddHours(-1));
            SemearAtivo(cenario.Repositorio, 2, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, "clientes/1/documentos/ok.pdf", AgoraFixo.AddHours(-1));
            cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add("clientes/1/documentos/sumiu.pdf");

            List<ReturnClienteDocumentoVO> lista = await cenario.Servico.ListarAsync(IdCliente, IdCliente);

            Assert.Equal(2, lista.Count);
            Assert.Null(lista[0].Url);
            Assert.Null(lista[0].ExpiraEm);
            Assert.NotNull(lista[1].Url);
            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroListarClienteDocumento, StringComparison.Ordinal));
        }

        [Fact]
        public async Task FalhaDaAssinaturaInteira_SobeComoIntegracao()
        {
            Cenario cenario = CriarCenario();
            SemearAtivo(cenario.Repositorio, 1, TipoDocumentoCliente.CNH, "clientes/1/documentos/a.pdf", AgoraFixo.AddHours(-1));
            cenario.Armazenamento.FalharAssinatura = true;

            await Assert.ThrowsAsync<AutoVia.Communication.CustomExceptions.IntegrationException>(() => cenario.Servico.ListarAsync(IdCliente, IdCliente));
        }

        [Fact]
        public async Task Listagem_DoOutroCliente_Da404()
        {
            Cenario cenario = CriarCenario();

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.ListarAsync(IdOutroCliente, IdCliente));
        }
    }
}
