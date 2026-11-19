using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// Regras puras dos documentos do cliente e as constantes e o padrao do teto. Tipo pelo nome exato, chave do objeto (formato,
    /// extensao por tipo do conteudo, passa em ChaveSegura, sem CPF), nome limpo (pasta, controle, 121 caracteres, vazio),
    /// documento ativo de maior id por tipo, e os numeros do contrato (20 envios, 24 horas, 10 MB com a folga multipart).
    /// </summary>
    public sealed class RegrasClienteDocumentoTests
    {
        private const int IdClienteDoTeste = 7;
        private const string CpfDoTeste = "52998224725";
        private const int TamanhoCorpoEsperado = 10551296;
        private const int TamanhoArquivoEsperado = 10485760;
        private const int LimiteNomeArquivo = 120;
        private const string TipoConteudoForaDaTabela = "text/plain";
        private static readonly Regex PadraoChave = new Regex(@"^clientes/7/documentos/[0-9a-f]{32}\.(pdf|jpg|png)$", RegexOptions.CultureInvariant);

        [Theory]
        [InlineData("CNH", TipoDocumentoCliente.CNH)]
        [InlineData("  CNH  ", TipoDocumentoCliente.CNH)]
        [InlineData("COMPROVANTE_RESIDENCIA", TipoDocumentoCliente.COMPROVANTE_RESIDENCIA)]
        [InlineData(" COMPROVANTE_RESIDENCIA\t", TipoDocumentoCliente.COMPROVANTE_RESIDENCIA)]
        public void TipoPeloNomeExato_AceitaComTrim(string nome, TipoDocumentoCliente esperado)
        {
            bool lido = RegrasClienteDocumento.TentarLerTipoPorNome(nome, out TipoDocumentoCliente tipo);

            Assert.True(lido);
            Assert.Equal(esperado, tipo);
            Assert.True(RegrasClienteDocumento.TipoPermitido(nome));
        }

        [Theory]
        [InlineData("cnh")]
        [InlineData("Cnh")]
        [InlineData("comprovante_residencia")]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("COMPROVANTE")]
        [InlineData("")]
        [InlineData("   ")]
        public void TipoFora_DoNomeExato_NaoEAceito(string nome)
        {
            bool lido = RegrasClienteDocumento.TentarLerTipoPorNome(nome, out TipoDocumentoCliente tipo);

            Assert.False(lido);
            Assert.Equal(default(TipoDocumentoCliente), tipo);
        }

        [Fact]
        public void TipoNulo_NaoEAceito()
        {
            Assert.False(RegrasClienteDocumento.TentarLerTipoPorNome(null, out TipoDocumentoCliente tipo));
            Assert.Equal(default(TipoDocumentoCliente), tipo);
            Assert.False(RegrasClienteDocumento.TipoPermitido(null));
        }

        [Theory]
        [InlineData(ConstantsValues.TipoConteudoPdf, "pdf")]
        [InlineData(ConstantsValues.TipoConteudoJpeg, "jpg")]
        [InlineData(ConstantsValues.TipoConteudoPng, "png")]
        public void ChaveDoObjeto_TemFormatoEExtensaoDoTipoDoConteudo_EPassaEmChaveSegura(string tipoConteudo, string extensao)
        {
            string chave = RegrasClienteDocumento.MontarChaveObjeto(IdClienteDoTeste, Guid.Parse("0f8fad5b-d9cb-469f-a165-8c45a4f10123"), tipoConteudo);

            Assert.Equal("clientes/7/documentos/0f8fad5bd9cb469fa1658c45a4f10123." + extensao, chave);
            Assert.Matches(PadraoChave, chave);
            Assert.True(RegrasArmazenamentoDisco.ChaveSegura(chave));
            Assert.DoesNotContain(CpfDoTeste, chave, StringComparison.Ordinal);
        }

        [Fact]
        public void ChaveDoObjeto_NaoLevaNomeDoClienteNemNomeOriginal()
        {
            string chave = RegrasClienteDocumento.MontarChaveObjeto(IdClienteDoTeste, Guid.NewGuid(), ConstantsValues.TipoConteudoPdf);

            Assert.DoesNotContain("Maria", chave, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cnh", chave, StringComparison.OrdinalIgnoreCase);
            Assert.Matches(PadraoChave, chave);
        }

        [Fact]
        public void ChaveDoObjeto_TipoDeConteudoForaDaTabela_EErroDeProgramacao()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RegrasClienteDocumento.MontarChaveObjeto(IdClienteDoTeste, Guid.NewGuid(), TipoConteudoForaDaTabela));
        }

        [Fact]
        public void NomeLimpo_FicaSoOArquivoSemPasta()
        {
            Assert.Equal("cnh.pdf", RegrasClienteDocumento.LimparNomeArquivo("C:\\Users\\Maria\\Documentos\\cnh.pdf"));
            Assert.Equal("cnh.pdf", RegrasClienteDocumento.LimparNomeArquivo("../../etc/cnh.pdf"));
            Assert.Equal("cnh.pdf", RegrasClienteDocumento.LimparNomeArquivo("pasta/sub\\cnh.pdf"));
        }

        [Fact]
        public void NomeLimpo_TiraCaracteresDeControleEAparaOsEspacos()
        {
            Assert.Equal("contaluz.pdf", RegrasClienteDocumento.LimparNomeArquivo("  conta\u0000luz\r\n.pdf  "));
        }

        [Fact]
        public void NomeLimpo_CortaEmCentoEVinteCaracteres()
        {
            string longo = new string('a', LimiteNomeArquivo + 1);

            string limpo = RegrasClienteDocumento.LimparNomeArquivo(longo);

            Assert.Equal(LimiteNomeArquivo, limpo.Length);
        }

        [Fact]
        public void NomeLimpo_TiraCaracteresDeFormatoComOInversorDeTexto()
        {
            Assert.Equal("contagnp.pdf", RegrasClienteDocumento.LimparNomeArquivo("conta\u202Egnp.pdf"));
        }

        [Fact]
        public void NomeLimpo_NaoCortaUmParSubstitutoNoMeio()
        {
            string nome = new string('a', LimiteNomeArquivo - 1) + "\uD83D\uDE00.pdf";

            string limpo = RegrasClienteDocumento.LimparNomeArquivo(nome);

            Assert.Equal(new string('a', LimiteNomeArquivo - 1), limpo);
            Assert.False(char.IsHighSurrogate(limpo[limpo.Length - 1]));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\u0001\u0002")]
        [InlineData("\u202E\u200B")]
        [InlineData("C:\\pasta\\")]
        public void NomeLimpo_SemNadaSobrando_UsaONomeFixo(string? nome)
        {
            Assert.Equal(ConstantsValues.NomeArquivoDocumentoClientePadrao, RegrasClienteDocumento.LimparNomeArquivo(nome));
        }

        [Fact]
        public void NomeLimpo_NomeMaiorQueOLimite_FicaComOsPrimeirosCentoEVinte()
        {
            string nome = new string('b', LimiteNomeArquivo) + ".pdf";

            string limpo = RegrasClienteDocumento.LimparNomeArquivo(nome);

            Assert.Equal(new string('b', LimiteNomeArquivo), limpo);
        }

        [Fact]
        public void AtivoDeMaiorId_ValeOMaisNovoDoTipo()
        {
            List<ClienteDocumento> documentos = new List<ClienteDocumento>
            {
                CriarDocumento(3, TipoDocumentoCliente.CNH, null),
                CriarDocumento(8, TipoDocumentoCliente.CNH, null),
                CriarDocumento(5, TipoDocumentoCliente.CNH, null)
            };

            ClienteDocumento? escolhido = RegrasClienteDocumento.EscolherAtivoPorTipo(documentos, TipoDocumentoCliente.CNH);

            Assert.NotNull(escolhido);
            Assert.Equal(8, escolhido!.IdClienteDocumento);
        }

        [Fact]
        public void AtivoDeMaiorId_IgnoraDesativadosEOutroTipo()
        {
            List<ClienteDocumento> documentos = new List<ClienteDocumento>
            {
                CriarDocumento(9, TipoDocumentoCliente.CNH, DateTime.Now),
                CriarDocumento(4, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, null)
            };

            Assert.Null(RegrasClienteDocumento.EscolherAtivoPorTipo(documentos, TipoDocumentoCliente.CNH));
            Assert.Equal(4, RegrasClienteDocumento.EscolherAtivoPorTipo(documentos, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA)!.IdClienteDocumento);
            Assert.Null(RegrasClienteDocumento.EscolherAtivoPorTipo(new List<ClienteDocumento>(), TipoDocumentoCliente.CNH));
        }

        [Fact]
        public void Expiracao_UsaOMesmoFormatoDasFotosEmUtc()
        {
            DateTimeOffset expiraEm = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.FromHours(-3));

            Assert.Equal(RegrasVeiculoFoto.FormatarExpiraEm(expiraEm), RegrasClienteDocumento.FormatarExpiraEm(expiraEm));
            Assert.Equal("2026-11-19T15:00:00.0000000+00:00", RegrasClienteDocumento.FormatarExpiraEm(expiraEm));
        }

        [Fact]
        public void Numeros_DoContrato_EstaoFixados()
        {
            Assert.Equal(TamanhoArquivoEsperado, ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes);
            Assert.Equal(ConstantsArmazenamento.TamanhoMaximoFotoBytes, ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes);
            Assert.Equal(TamanhoCorpoEsperado, ConstantsValues.TamanhoMaximoCorpoDocumentoCliente);
            Assert.Equal(20, ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao);
            Assert.Equal(24, ConstantsValues.JanelaEnviosDocumentoClienteHoras);
            Assert.Equal(20, ConstantsValues.LimiteEnvioDocumentoClientePorMinutoPadrao);
            Assert.Equal(60, ConstantsValues.LimiteListaDocumentoClientePorMinutoPadrao);
            Assert.Equal("AutoViaEnvioDocumentoCliente", ConstantsValues.PoliticaLimiteEnvioDocumentoCliente);
            Assert.Equal("AutoViaListaDocumentoCliente", ConstantsValues.PoliticaLimiteListaDocumentoCliente);
            Assert.Equal("Clientes:LimiteEnvioDocumentoPorMinuto", ConstantsValues.ChaveLimiteEnvioDocumentoCliente);
            Assert.Equal("Clientes:LimiteListaDocumentoPorMinuto", ConstantsValues.ChaveLimiteListaDocumentoCliente);
            Assert.Equal("api/clientes/{idCliente:int}/documentos", ConstantsValues.RotaClienteDocumento);
        }

        [Fact]
        public void LimiteDeEnviosPorCliente_PadraoVinteECaiNoPadraoComZeroOuNegativo()
        {
            ClientesOptions opcoes = new ClientesOptions();
            Assert.Equal(20, opcoes.LimiteEnviosDocumentoPorDia);

            opcoes.LimiteEnviosDocumentoPorDia = 0;
            Assert.Equal(20, opcoes.LimiteEnviosDocumentoPorDia);

            opcoes.LimiteEnviosDocumentoPorDia = -3;
            Assert.Equal(20, opcoes.LimiteEnviosDocumentoPorDia);

            opcoes.LimiteEnviosDocumentoPorDia = 5;
            Assert.Equal(5, opcoes.LimiteEnviosDocumentoPorDia);
        }

        [Fact]
        public void MensagemDoTamanho_TemOLimiteEmMegabytes()
        {
            string mensagem = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroClienteDocumentoTamanho, ConstantsValues.TamanhoMaximoArquivoDocumentoClienteMb);

            Assert.Equal("O arquivo deve ter no máximo 10 MB.", mensagem);
        }

        private static ClienteDocumento CriarDocumento(int id, TipoDocumentoCliente tipo, DateTime? desativadoEm)
        {
            ClienteDocumento documento = new ClienteDocumento
            {
                IdClienteDocumento = id,
                IdCliente = IdClienteDoTeste,
                Tipo = tipo,
                ChaveObjeto = "clientes/7/documentos/" + id + ".pdf",
                NomeArquivo = "cnh.pdf",
                TipoConteudo = ConstantsValues.TipoConteudoPdf,
                TamanhoBytes = 1,
                DisabledAt = desativadoEm
            };

            return documento;
        }
    }
}
