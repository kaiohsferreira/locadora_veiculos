using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Reservas
{
    /// <summary>
    /// Guardas de codigo-fonte do ReservaRepository: (1) todo catch tipado (404 e 409 dentro da transacao) registra no log
    /// antes de relancar (decisao D-9); (2) todo metodo publico termina em catch (Exception ex) com _logService.Write e throw;
    /// (3) o insert da reserva zera o id, preenche CreatedAt e UpdatedAt e deixa DisabledAt nulo. A guarda (3) prova so que o
    /// codigo atribui esses campos; o valor gravado no PostgreSQL nao e provado aqui (roteiro manual).
    /// Os simbolos de bypass do filtro "Empresa" e o SQL direto, por metodo, estao em GuardaPorMetodoRepositoriosTests.
    /// </summary>
    public class GuardasDeCodigoDaReservaTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string CaminhoRepositorio = "AutoVia.Infrastructure/Repositories/ReservaRepository.cs";
        private const string PadraoDeclaracaoMetodo = @"^        (public|private)\s[^=]*?\s(\w+)\(";
        private const string PadraoCatchTipadoComLog = @"catch \((BusinessException|NotFoundException) \w+\)\s*\{\s*_logService\.Write\(";
        private const string PadraoCatchTipado = @"catch \((BusinessException|NotFoundException) ";
        private const string PadraoCatchGenericoComLogEThrow = @"catch \(Exception ex\)\s*\{\s*_logService\.Write\(ex, ConstantsMessage\.\w+, GetType\(\)\.ToString\(\)\);\s*throw;\s*\}\s*\}\s*$";
        private const string InicioDeMetodoPublico = "        public ";
        private const string InicioDeDocumentacao = "///";
        private const string AtribuicaoIdZero = "reserva.IdReserva = 0;";
        private const string AtribuicaoCreatedAt = "reserva.CreatedAt = DateTime.Now;";
        private const string AtribuicaoUpdatedAt = "reserva.UpdatedAt = DateTime.Now;";
        private const string AtribuicaoDisabledAtNulo = "reserva.DisabledAt = null;";
        private const string NomeMetodoInsert = "InsertAtomicallyAsync";

        /// <summary>Le o arquivo do repositorio subindo a partir da pasta do teste ate achar o AutoVia.sln.</summary>
        private static string LerRepositorio()
        {
            DirectoryInfo? pasta = new DirectoryInfo(AppContext.BaseDirectory);

            while (pasta != null && !File.Exists(Path.Combine(pasta.FullName, NomeSolucao)))
            {
                pasta = pasta.Parent;
            }

            Assert.NotNull(pasta);

            return File.ReadAllText(Path.Combine(pasta!.FullName, CaminhoRepositorio.Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>Corpo de cada metodo publico (nome, texto), sem as linhas de documentacao, do cabecalho ate o proximo metodo.</summary>
        private static Dictionary<string, string> CorposDosMetodosPublicos(string conteudo)
        {
            Dictionary<string, string> corpos = new Dictionary<string, string>(StringComparer.Ordinal);
            string nomeAtual = string.Empty;
            List<string> linhasAtuais = new List<string>();

            foreach (string linha in conteudo.Split('\n'))
            {
                string semQuebra = linha.TrimEnd('\r');

                if (semQuebra.TrimStart().StartsWith(InicioDeDocumentacao, StringComparison.Ordinal))
                {
                    continue;
                }

                Match declaracao = Regex.Match(semQuebra, PadraoDeclaracaoMetodo);

                if (declaracao.Success)
                {
                    if (nomeAtual.Length > 0 && linhasAtuais.Count > 0)
                    {
                        corpos[nomeAtual] = string.Join("\n", linhasAtuais);
                    }

                    nomeAtual = semQuebra.StartsWith(InicioDeMetodoPublico, StringComparison.Ordinal) ? declaracao.Groups[2].Value : string.Empty;
                    linhasAtuais = new List<string>();
                }

                linhasAtuais.Add(semQuebra);
            }

            if (nomeAtual.Length > 0)
            {
                corpos[nomeAtual] = string.Join("\n", linhasAtuais);
            }

            return corpos;
        }

        [Fact]
        public void Repositorio_CatchTipadoSempreRegistraNoLogAntesDeRelancar()
        {
            string conteudo = LerRepositorio();

            int totalTipados = Regex.Matches(conteudo, PadraoCatchTipado).Count;
            int tipadosComLog = Regex.Matches(conteudo, PadraoCatchTipadoComLog).Count;

            Assert.Equal(totalTipados, tipadosComLog);
        }

        [Fact]
        public void Repositorio_CadaMetodoPublicoTerminaEmCatchExceptionComLogEThrow()
        {
            string conteudo = LerRepositorio();
            Dictionary<string, string> corpos = CorposDosMetodosPublicos(conteudo);

            Assert.NotEmpty(corpos);
            foreach (KeyValuePair<string, string> metodo in corpos)
            {
                Assert.True(Regex.IsMatch(metodo.Value.TrimEnd(), PadraoCatchGenericoComLogEThrow), $"Metodo publico sem catch (Exception ex) com log e throw: {metodo.Key}");
            }
        }

        [Fact]
        public void Insert_ZeraOIdPreencheAuditoriaEDeixaDisabledAtNulo()
        {
            string conteudo = LerRepositorio();
            Dictionary<string, string> corpos = CorposDosMetodosPublicos(conteudo);

            Assert.True(corpos.ContainsKey(NomeMetodoInsert), "InsertAtomicallyAsync nao encontrado no ReservaRepository.");
            string corpoInsert = corpos[NomeMetodoInsert];

            Assert.Contains(AtribuicaoIdZero, corpoInsert);
            Assert.Contains(AtribuicaoCreatedAt, corpoInsert);
            Assert.Contains(AtribuicaoUpdatedAt, corpoInsert);
            Assert.Contains(AtribuicaoDisabledAtNulo, corpoInsert);
        }
    }
}
