using System.Text.RegularExpressions;

namespace AutoVia.Tests.Contratos
{
    /// <summary>
    /// Guarda estrutural dos arquivos novos do contrato da locacao: sem tipagem implicita (regra R1), sem remocao fisica (Remove, Find) e sem
    /// bypass do filtro de empresa. Le o codigo-fonte pelo caminho a partir da pasta do backend (AutoVia.sln).
    /// </summary>
    public sealed class ContratoLocacaoEstruturaTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string PadraoVar = @"\bvar\b";
        private const string PadraoRemocaoFisica = @"\.Remove\(|\.RemoveRange\(|\.Find\(|\.FindAsync\(";
        private const string PadraoBypass = @"IgnoreQueryFilters\(|SemEscopoEmpresa\(|FromSql|ExecuteSql|GetDbConnection\(";

        private static readonly string[] ArquivosDoContrato = new string[]
        {
            @"AutoVia.Core\Entities\PgSql\ContratoLocacao.cs",
            @"AutoVia.Core\RepositoriesInterface\IContratoLocacaoRepository.cs",
            @"AutoVia.Core\ServicesInterface\IContratoLocacaoService.cs",
            @"AutoVia.Core\Regras\RegrasContratoLocacao.cs",
            @"AutoVia.Communication\ViewObjects\Contratos\AssinarContratoLocacaoVO.cs",
            @"AutoVia.Communication\ViewObjects\Contratos\ReturnContratoLocacaoVO.cs",
            @"AutoVia.Communication\ViewObjects\Contratos\ContratoDocumentoVO.cs",
            @"AutoVia.Communication\Validators\Contratos\RegrasFormatoAssinaturaContrato.cs",
            @"AutoVia.Communication\Validators\Contratos\AssinarContratoLocacaoVOValidator.cs",
            @"AutoVia.Helpers\ConstantsMessage.ContratoLocacao.cs",
            @"AutoVia.Helpers\ConstantsValues.ContratoLocacao.cs",
            @"AutoVia.Infrastructure\Persistence\Configurations\ContratoLocacaoConfiguration.cs",
            @"AutoVia.Infrastructure\Repositories\ContratoLocacaoRepository.cs",
            @"AutoVia.Infrastructure\Services\ContratoLocacaoService.cs",
            @"AutoVia.Api\Authorization\RequisitoContratoLocacao.cs",
            @"AutoVia.Api\Authorization\AutorizacaoContratoLocacaoHandler.cs",
            @"AutoVia.Api\Controllers\ContratoLocacaoController.cs"
        };

        private static string RaizDoBackend()
        {
            DirectoryInfo? pasta = new DirectoryInfo(AppContext.BaseDirectory);
            while (pasta != null && !File.Exists(Path.Combine(pasta.FullName, NomeSolucao)))
            {
                pasta = pasta.Parent;
            }

            return pasta!.FullName;
        }

        private static string Conteudo(string caminhoRelativo)
        {
            return File.ReadAllText(Path.Combine(RaizDoBackend(), caminhoRelativo));
        }

        [Fact]
        public void ArquivosDoContrato_NaoUsamVar()
        {
            foreach (string arquivo in ArquivosDoContrato)
            {
                Assert.False(Regex.IsMatch(Conteudo(arquivo), PadraoVar), $"{arquivo} usa tipagem implicita (regra R1).");
            }
        }

        [Fact]
        public void ArquivosDoContrato_NaoRemovemFisicamenteNemBurlamOFiltro()
        {
            foreach (string arquivo in ArquivosDoContrato)
            {
                string texto = Conteudo(arquivo);
                Assert.False(Regex.IsMatch(texto, PadraoRemocaoFisica), $"{arquivo} remove ou busca por chave.");
                Assert.False(Regex.IsMatch(texto, PadraoBypass), $"{arquivo} contorna o filtro de empresa.");
            }
        }

        /// <summary>
        /// Regra por bloco (nao por contagem): cada catch (Exception) grava _logService.Write dentro do proprio bloco, ate o
        /// proximo catch ou o proximo membro da classe. Um log em outro metodo do mesmo arquivo nao compensa o catch.
        /// </summary>
        [Fact]
        public void ArquivosDoContrato_CadaCatchGenericoTemLogNoProprioBloco()
        {
            Regex catchQualquer = new Regex(@"catch\s*\(");
            Regex fimDoMembro = new Regex(@"\n\s{4,8}(public|private|protected|internal)\b");

            foreach (string arquivo in ArquivosDoContrato)
            {
                string texto = Conteudo(arquivo);
                foreach (Match catchAtual in Regex.Matches(texto, @"catch\s*\((?<tipo>[\w.]+)(\s+\w+)?\)"))
                {
                    if (catchAtual.Groups["tipo"].Value != "Exception")
                    {
                        continue;
                    }

                    int inicio = catchAtual.Index;
                    int fim = texto.Length;
                    Match proximoCatch = catchQualquer.Match(texto, inicio + 1);
                    if (proximoCatch.Success && proximoCatch.Index < fim)
                    {
                        fim = proximoCatch.Index;
                    }
                    Match proximoMembro = fimDoMembro.Match(texto, inicio + 1);
                    if (proximoMembro.Success && proximoMembro.Index < fim)
                    {
                        fim = proximoMembro.Index;
                    }

                    string bloco = texto.Substring(inicio, fim - inicio);
                    Assert.True(bloco.Contains("_logService.Write(", StringComparison.Ordinal),
                        $"{arquivo} tem catch(Exception) sem log no proprio bloco (posicao {inicio}).");
                }
            }
        }

        private static readonly string[] ArquivosDeCodigoComMensagens = new string[]
        {
            @"AutoVia.Infrastructure\Services\ContratoLocacaoService.cs",
            @"AutoVia.Infrastructure\Repositories\ContratoLocacaoRepository.cs",
            @"AutoVia.Core\Regras\RegrasContratoLocacao.cs",
            @"AutoVia.Communication\Validators\Contratos\RegrasFormatoAssinaturaContrato.cs",
            @"AutoVia.Communication\Validators\Contratos\AssinarContratoLocacaoVOValidator.cs",
            @"AutoVia.Api\Controllers\ContratoLocacaoController.cs",
            @"AutoVia.Api\Authorization\AutorizacaoContratoLocacaoHandler.cs"
        };

        /// <summary>R3: texto de mensagem (com espaco ou letra acentuada) so em ConstantsMessage; comentarios nao contam.</summary>
        [Fact]
        public void ArquivosDeCodigo_NaoTemTextoDeMensagemEntreAspas()
        {
            Regex literal = new Regex(@"""((?:[^""\\\r\n]|\\.)*)""");
            Regex acentuada = new Regex(@"[À-ÿ]");

            foreach (string arquivo in ArquivosDeCodigoComMensagens)
            {
                string codigo = string.Join("\n", Conteudo(arquivo).Split('\n')
                    .Where(linha => !linha.TrimStart().StartsWith("//", StringComparison.Ordinal)));

                foreach (Match trecho in literal.Matches(codigo))
                {
                    string valor = trecho.Groups[1].Value;
                    bool pareceMensagem = (valor.Length >= 12 && valor.Contains(' ')) || acentuada.IsMatch(valor);
                    Assert.False(pareceMensagem, $"{arquivo} tem texto de mensagem fora de ConstantsMessage: \"{valor}\".");
                }
            }
        }
    }
}
