using System.IO;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Guarda POR METODO dos quatro repositorios desta entrega (ReservaRepository, VeiculoRepository.Disponibilidade,
    /// VeiculoRepository.Catalogo e LocacaoRepository). Cada ocorrencia dos simbolos de bypass da lista de
    /// IsolamentoEmpresaGuardasTests (filtro desligado, SQL bruto, busca por chave, conexao crua) e atribuida ao metodo
    /// que a contem. O teste falha se um simbolo aparecer em metodo fora da lista fechada, se um metodo listado perder
    /// o simbolo, ou se a quantidade dentro de um metodo mudar. A contagem por arquivo continua em IsolamentoEmpresaGuardasTests.
    /// </summary>
    public class GuardaPorMetodoRepositoriosTests
    {
        private const string NomeSolucao = "AutoVia.sln";
        private const string CaminhoReservaRepository = "AutoVia.Infrastructure/Repositories/ReservaRepository.cs";
        private const string CaminhoDisponibilidade = "AutoVia.Infrastructure/Repositories/VeiculoRepository.Disponibilidade.cs";
        private const string CaminhoCatalogo = "AutoVia.Infrastructure/Repositories/VeiculoRepository.Catalogo.cs";
        private const string CaminhoLocacaoRepository = "AutoVia.Infrastructure/Repositories/LocacaoRepository.cs";
        private const string CaminhoPropostaCompraRepository = "AutoVia.Infrastructure/Repositories/PropostaCompraRepository.cs";
        private const string SeparadorMetodoSimbolo = "|";
        private const string PadraoDeclaracaoDeMetodo = @"^        (public|private)\s[^=]*?\s(\w+)\(";

        private static readonly Regex DeclaracaoDeMetodo = new Regex(PadraoDeclaracaoDeMetodo, RegexOptions.CultureInvariant);

        /// <summary>
        /// Lista fechada por arquivo: cada par "metodo|simbolo" aparece uma vez por ocorrencia permitida. Qualquer outro
        /// metodo, simbolo ou quantidade reprova. Motivos: ReservaRepository (quatro consultas nomeadas fora do filtro Empresa
        /// e a trava do veiculo, que tem SQL bruto); VeiculoRepository.Disponibilidade e VeiculoRepository.Catalogo (leituras
        /// publicas entre locadoras, sempre com locadora ativa); LocacaoRepository (trava de linha pela conexao crua, so no
        /// metodo que faz a trava); PropostaCompraRepository (propostas do proprio cliente, com a posse pelo cliente do contexto).
        /// </summary>
        private static readonly Dictionary<string, string[]> OcorrenciasEsperadas = new Dictionary<string, string[]>
        {
            [CaminhoReservaRepository] = new string[]
            {
                "ConsultaVeiculoParaReserva" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(",
                "ConsultaReservasDoVeiculo" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(",
                "ConsultaReservasDoClienteLogado" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(",
                "ConsultaTravaDoVeiculo" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(",
                "ConsultaTravaDoVeiculo" + SeparadorMetodoSimbolo + "FromSql"
            },
            [CaminhoDisponibilidade] = new string[]
            {
                "ConsultaDisponiveis" + SeparadorMetodoSimbolo + "SemEscopoEmpresa("
            },
            [CaminhoCatalogo] = new string[]
            {
                "ConsultaCatalogo" + SeparadorMetodoSimbolo + "SemEscopoEmpresa("
            },
            [CaminhoLocacaoRepository] = new string[]
            {
                "LockActiveRowAsync" + SeparadorMetodoSimbolo + "GetDbConnection("
            },
            [CaminhoPropostaCompraRepository] = new string[]
            {
                "ConsultaPropostasDoClienteLogado" + SeparadorMetodoSimbolo + "SemEscopoEmpresa("
            }
        };

        /// <summary>Sobe a partir da pasta do teste ate achar o AutoVia.sln (raiz do backend).</summary>
        private static string RaizDoBackend()
        {
            DirectoryInfo? pasta = new DirectoryInfo(AppContext.BaseDirectory);

            while (pasta != null && !File.Exists(Path.Combine(pasta.FullName, NomeSolucao)))
            {
                pasta = pasta.Parent;
            }

            Assert.NotNull(pasta);

            return pasta!.FullName;
        }

        /// <summary>
        /// Pares "metodo|simbolo", um por ocorrencia, na ordem do arquivo. O metodo e o ultimo cabecalho de metodo com indentacao
        /// de membro (8 espacos) que aparece antes da linha; ocorrencia antes de qualquer metodo fica com metodo vazio e reprova.
        /// </summary>
        internal static List<string> OcorrenciasPorMetodo(string conteudo)
        {
            List<string> ocorrencias = new List<string>();
            string metodoAtual = string.Empty;

            foreach (string linha in conteudo.Split('\n'))
            {
                Match declaracao = DeclaracaoDeMetodo.Match(linha);

                if (declaracao.Success)
                {
                    metodoAtual = declaracao.Groups[2].Value;
                }

                foreach (string simbolo in IsolamentoEmpresaGuardasTests.SimbolosDeBypass)
                {
                    int quantidade = Regex.Matches(linha, Regex.Escape(simbolo)).Count;

                    for (int indice = 0; indice < quantidade; indice++)
                    {
                        ocorrencias.Add(metodoAtual + SeparadorMetodoSimbolo + simbolo);
                    }
                }
            }

            return ocorrencias;
        }

        [Theory]
        [InlineData(CaminhoReservaRepository)]
        [InlineData(CaminhoDisponibilidade)]
        [InlineData(CaminhoCatalogo)]
        [InlineData(CaminhoLocacaoRepository)]
        [InlineData(CaminhoPropostaCompraRepository)]
        public void Repositorio_SimbolosDeBypassSoNosMetodosNomeadosNaQuantidadeExata(string caminhoRelativo)
        {
            string conteudo = File.ReadAllText(Path.Combine(RaizDoBackend(), caminhoRelativo.Replace('/', Path.DirectorySeparatorChar)));

            List<string> encontrados = OcorrenciasPorMetodo(conteudo);
            List<string> esperados = new List<string>(OcorrenciasEsperadas[caminhoRelativo]);

            encontrados.Sort(StringComparer.Ordinal);
            esperados.Sort(StringComparer.Ordinal);

            Assert.Equal(esperados, encontrados);
        }

        [Fact]
        public void ContagemPorMetodo_SimboloEmOutroMetodo_ApareceComOMetodoDeCadaOcorrencia()
        {
            string conteudo = string.Join("\n",
                "        public IQueryable<Veiculo> ConsultaPermitida(int idVeiculo)",
                "        {",
                "            return _db.Veiculos.SemEscopoEmpresa();",
                "        }",
                "        public IQueryable<Veiculo> ConsultaSemPermissao(int idVeiculo)",
                "        {",
                "            return _db.Veiculos.SemEscopoEmpresa().Where(x => x.Id == idVeiculo);",
                "        }");

            List<string> achados = OcorrenciasPorMetodo(conteudo);

            Assert.Equal(
                new List<string> { "ConsultaPermitida" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(", "ConsultaSemPermissao" + SeparadorMetodoSimbolo + "SemEscopoEmpresa(" },
                achados);
        }

        [Fact]
        public void ContagemPorMetodo_DoisSimbolosNoMesmoMetodo_SaoDoisPares()
        {
            string conteudo = string.Join("\n",
                "        private async Task<bool> LockActiveRowAsync(string comandoSql, int id)",
                "        {",
                "            DbConnection conexao = _db.Database.GetDbConnection();",
                "            return await _db.Veiculos.FromSqlRaw(comandoSql, id).AnyAsync();",
                "        }");

            List<string> achados = OcorrenciasPorMetodo(conteudo);

            Assert.Equal(
                new List<string> { "LockActiveRowAsync" + SeparadorMetodoSimbolo + "FromSql", "LockActiveRowAsync" + SeparadorMetodoSimbolo + "GetDbConnection(" },
                achados.OrderBy(par => par, StringComparer.Ordinal).ToList());
        }
    }
}
