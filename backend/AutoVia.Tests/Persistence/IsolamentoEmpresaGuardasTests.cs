using AutoVia.Api.Requests.Avaria;
using AutoVia.Api.Requests.VeiculoFoto;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.ViewObjects.Frota;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Guardas que impedem o esquecimento do isolamento por locadora (multi-locadora, T-BE-03b):
    /// (1) nenhum VO de ENTRADA (AutoVia.Communication.ViewObjects) e nenhum formulario de ENTRADA (AutoVia.Api.Requests)
    /// tem IdEmpresa (a empresa vem do contexto, nunca do corpo);
    /// (2) o bypass do filtro e as formas de SQL e de busca que o contornam (IgnoreQueryFilters, SemEscopoEmpresa,
    /// FromSql, ExecuteSql, SqlQuery, ExecuteUpdate, ExecuteDelete, GetDbConnection, FindAsync e Find) so aparecem na lista
    /// fechada abaixo, com a QUANTIDADE de ocorrencias por arquivo. Um segundo bypass no mesmo arquivo reprova o teste ate a
    /// lista ser atualizada de proposito. Arquivo novo fora da lista tambem reprova. Por METODO, nos nove arquivos de repositorio cobertos
    /// (ReservaRepository, VeiculoRepository.Disponibilidade, VeiculoRepository.Catalogo, LocacaoRepository,
    /// VeiculoRepository.Status, ClienteDocumentoRepository, ManutencaoRepository, PagamentoRepository e ClienteLocadoraRepository), a guarda
    /// esta em GuardaPorMetodoRepositoriosTests, que usa esta mesma lista de simbolos. VistoriaRepository e CobrancasExtrasRepository
    /// nao tem guarda por metodo: so a contagem por arquivo, nesta classe.
    /// </summary>
    public class IsolamentoEmpresaGuardasTests
    {
        private const string PropriedadeEmpresa = "IdEmpresa";
        private const string PrefixoVOsDeSaida = "Return";
        private const string NamespaceVOs = "AutoVia.Communication.ViewObjects";
        private const string NamespaceFormularios = "AutoVia.Api.Requests";
        private const string SufixoVO = "VO";
        private const char SeparadorCaminho = '/';
        private const string PastaObj = "obj";
        private const string PastaBin = "bin";
        private const string ExtensaoCSharp = "*.cs";
        private const string NomeSolucao = "AutoVia.sln";

        /// <summary>Simbolos que desligam o filtro "Empresa" ou contornam o LINQ (SQL bruto, atualizacao e exclusao em massa, busca por chave, conexao crua).</summary>
        internal static readonly string[] SimbolosDeBypass = new string[]
        {
            "IgnoreQueryFilters(",
            "SemEscopoEmpresa(",
            "FromSql",
            "ExecuteSql",
            "SqlQuery",
            "ExecuteUpdate",
            "ExecuteDelete",
            "GetDbConnection(",
            ".FindAsync(",
            ".Find("
        };

        /// <summary>Pastas de producao varridas pela guarda.</summary>
        private static readonly string[] PastasDeProducao = new string[]
        {
            "AutoVia.Api", "AutoVia.Core", "AutoVia.Communication", "AutoVia.Helpers", "AutoVia.Infrastructure", "AutoVia.Integrations"
        };

        /// <summary>VOs que nao sao de entrada mas terminam em VO (saida ou dado interno). Lista fechada.</summary>
        private static readonly string[] VOsInternos = new string[] { nameof(SessaoSituacaoVO) };

        /// <summary>
        /// Lista fechada: arquivo de producao -> quantidade exata de ocorrencias dos simbolos de bypass. Arquivo fora
        /// da lista ou com outra quantidade reprova. Motivos: EscopoEmpresa (definicao do filtro e do bypass nomeado);
        /// AutenticacaoRepository (login, sessao e administrador inicial, antes de haver contexto); RedefinicaoSenhaRepository
        /// (titular do link, sem contexto); VeiculoRepository.Cadastro (unicidade de placa, chassi e renavam na plataforma);
        /// EmpresaRepository (CPF na plataforma inteira: do responsavel no cadastro publico de locadora, sem contexto, e do
        /// funcionario novo na equipe, P10-BE-03, que reaproveita o mesmo metodo; a equipe nao cria bypass proprio);
        /// ReservaRepository (quatro consultas nomeadas fora do filtro Empresa: reservas do proprio cliente, com a posse pelo
        /// cliente do contexto; reservas do veiculo, para o conflito de periodo; veiculo de locadora ativa; e a trava unica
        /// FOR UPDATE do veiculo, cujo SQL bruto conta mais uma ocorrencia; SQL provado em ReservaConsultasSqlTests);
        /// LocacaoRepository (trava de linha FOR UPDATE, obtida pela conexao crua do contexto (metodo GetDbConnection, sem
        /// parenteses) na abertura da locacao; so trava a linha, e as leituras da locacao passam pelo filtro Empresa);
        /// VeiculoRepository.Disponibilidade e VeiculoRepository.Catalogo (leituras publicas entre locadoras: busca de
        /// disponiveis e catalogo de seminovos, um bypass nomeado por arquivo, sempre restrito a locadoras ativas, com o SQL
        /// provado em VeiculoPublicoSqlTests); VeiculoRepository.Status (trava FOR UPDATE da troca manual de status, com id_empresa
        /// por parametro e o filtro ligado; SQL provado em VeiculoStatusConsultasSqlTests); ManutencaoRepository (trava FOR UPDATE
        /// da abertura e do encerramento da ordem, o mesmo SQL da troca de status, com id_empresa por parametro e o filtro
        /// ligado; SQL provado em ManutencaoConsultasSqlTests). PagamentoRepository (quatro ocorrencias: a consulta base do cliente,
        /// com a posse pelo cliente do contexto, e a trava FOR UPDATE da baixa, com SQL bruto e o filtro ligado; mais a trava
        /// FOR UPDATE do cliente para o PIX (item P7-BE-02, posse dentro do SQL, sem filtro de locadora); SQL provado em
        /// PagamentoConsultasSqlTests). VistoriaRepository (trava FOR UPDATE da locacao na vistoria de saida, obtida pela
        /// conexao crua do contexto (GetDbConnection), com consulta sob o filtro Empresa antes e depois da trava; so trava a linha).
        /// CobrancasExtrasRepository (conexao crua do contexto (GetDbConnection) apenas para os locks FOR UPDATE parametrizados
        /// (empresa e locacao), com o escopo validado antes da trava).
        /// ClienteLocadoraRepository (trava FOR UPDATE da linha do cliente no teto de pedidos, item 6 de 20/11; SQL provado em
        /// ClienteLocadoraConsultasSqlTests; a tabela cliente nao tem filtro Empresa, e as demais consultas do repositorio seguem pelo DbSet).
        /// </summary>
        private static readonly Dictionary<string, int> OcorrenciasPermitidas = new Dictionary<string, int>
        {
            ["AutoVia.Infrastructure/Persistence/EscopoEmpresa.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/AutenticacaoRepository.cs"] = 3,
            ["AutoVia.Infrastructure/Repositories/EmpresaRepository.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/LocacaoRepository.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/ManutencaoRepository.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/PagamentoRepository.cs"] = 4,
            // Trava por locacao, precedida e seguida de consulta sob o filtro Empresa.
            ["AutoVia.Infrastructure/Repositories/VistoriaRepository.cs"] = 1,
            // RF-23: uma conexao crua compartilhada apenas pelos locks parametrizados, com escopo validado antes.
            ["AutoVia.Infrastructure/Repositories/CobrancasExtrasRepository.cs"] = 1,
            // Trava do cliente no teto de pedidos de aprovacao (item 6, 20/11): FOR UPDATE da linha do cliente, com o id por parametro.
            ["AutoVia.Infrastructure/Repositories/ClienteLocadoraRepository.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/RedefinicaoSenhaRepository.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/ReservaRepository.cs"] = 5,
            ["AutoVia.Infrastructure/Repositories/VeiculoRepository.Cadastro.cs"] = 3,
            ["AutoVia.Infrastructure/Repositories/VeiculoRepository.Catalogo.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/VeiculoRepository.Disponibilidade.cs"] = 1,
            ["AutoVia.Infrastructure/Repositories/VeiculoRepository.Status.cs"] = 1
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

        /// <summary>Conta as ocorrencias de todos os simbolos de bypass no texto (cada ocorrencia conta uma vez).</summary>
        internal static int ContarBypass(string conteudo)
        {
            int total = 0;

            foreach (string simbolo in SimbolosDeBypass)
            {
                total += Regex.Matches(conteudo, Regex.Escape(simbolo)).Count;
            }

            return total;
        }

        /// <summary>Varre os arquivos de producao e devolve, por arquivo (caminho relativo ao backend, com /), a quantidade de bypass.</summary>
        private static Dictionary<string, int> ContarPorArquivo()
        {
            string raiz = RaizDoBackend();
            Dictionary<string, int> contagem = new Dictionary<string, int>();

            foreach (string pasta in PastasDeProducao)
            {
                string caminhoPasta = Path.Combine(raiz, pasta);

                foreach (string arquivo in Directory.EnumerateFiles(caminhoPasta, ExtensaoCSharp, SearchOption.AllDirectories))
                {
                    if (arquivo.Contains($"{Path.DirectorySeparatorChar}{PastaObj}{Path.DirectorySeparatorChar}") || arquivo.Contains($"{Path.DirectorySeparatorChar}{PastaBin}{Path.DirectorySeparatorChar}"))
                    {
                        continue;
                    }

                    string conteudo = File.ReadAllText(arquivo);
                    int ocorrencias = ContarBypass(conteudo);

                    if (ocorrencias > 0)
                    {
                        string relativo = Path.GetRelativePath(raiz, arquivo).Replace(Path.DirectorySeparatorChar, SeparadorCaminho);
                        contagem[relativo] = ocorrencias;
                    }
                }
            }

            return contagem;
        }

        /// <summary>Formata um mapa de contagem em linhas "arquivo: quantidade", ordenadas, para a mensagem de falha.</summary>
        private static List<string> Linhas(Dictionary<string, int> mapa)
        {
            return mapa.OrderBy(par => par.Key, StringComparer.Ordinal)
                .Select(par => $"{par.Key}: {par.Value}")
                .ToList();
        }

        [Fact]
        public void NenhumVODeEntrada_TemPropriedadeIdEmpresa()
        {
            List<Type> voDeEntrada = typeof(VeiculoVO).Assembly.GetTypes()
                .Where(tipo => tipo.IsClass
                    && tipo.Namespace != null
                    && tipo.Namespace.StartsWith(NamespaceVOs, StringComparison.Ordinal)
                    && tipo.Name.EndsWith(SufixoVO, StringComparison.Ordinal)
                    && !tipo.Name.StartsWith(PrefixoVOsDeSaida, StringComparison.Ordinal)
                    && !VOsInternos.Contains(tipo.Name))
                .ToList();

            Assert.Contains(typeof(VeiculoVO), voDeEntrada);
            Assert.Contains(typeof(LoginVO), voDeEntrada);

            AssertSemPropriedadeEmpresa(voDeEntrada);
        }

        /// <summary>
        /// Formularios de entrada (multipart, AutoVia.Api.Requests) tambem nao podem carregar IdEmpresa. A varredura
        /// precisa achar os dois formularios de avaria; sem eles a lista fica vazia e a guarda nao prova nada.
        /// </summary>
        [Fact]
        public void NenhumFormularioDeEntrada_TemPropriedadeIdEmpresa()
        {
            List<Type> formularios = typeof(CriarAvariaRequest).Assembly.GetTypes()
                .Where(tipo => tipo.IsClass
                    && tipo.Namespace != null
                    && tipo.Namespace.StartsWith(NamespaceFormularios, StringComparison.Ordinal))
                .ToList();

            Assert.Contains(typeof(CriarAvariaRequest), formularios);
            Assert.Contains(typeof(RetryAvariaFotoRequest), formularios);
            Assert.Contains(typeof(EnviarFotoVeiculoRequest), formularios);
            Assert.Contains(typeof(AutoVia.Api.Requests.ClienteDocumento.EnviarDocumentoClienteRequest), formularios);

            AssertSemPropriedadeEmpresa(formularios);
        }

        private static void AssertSemPropriedadeEmpresa(List<Type> tipos)
        {
            List<string> comEmpresa = tipos
                .Where(tipo => tipo.GetProperty(PropriedadeEmpresa, BindingFlags.Public | BindingFlags.Instance) != null)
                .Select(tipo => tipo.FullName!)
                .ToList();

            Assert.Empty(comEmpresa);
        }

        [Fact]
        public void BypassesEFormasDeContornar_SoAparecemNaListaFechadaComAQuantidadeExata()
        {
            Dictionary<string, int> encontrados = ContarPorArquivo();

            Assert.Equal(Linhas(OcorrenciasPermitidas), Linhas(encontrados));
        }

        [Fact]
        public void ContagemDeBypass_DoisBypassesNoMesmoArquivo_SaoDoisEReprovamAListaDeUm()
        {
            string conteudoComDoisBypasses = "db.Veiculos.IgnoreQueryFilters([\"Empresa\"]); db.Veiculos.SemEscopoEmpresa().ToList();";

            int contagem = ContarBypass(conteudoComDoisBypasses);

            Assert.Equal(2, contagem);
            Assert.NotEqual(OcorrenciasPermitidas["AutoVia.Infrastructure/Persistence/EscopoEmpresa.cs"], contagem);
        }

        [Fact]
        public void ContagemDeBypass_SqlBrutoEBuscaPorChave_SaoContados()
        {
            string conteudo = "db.Database.ExecuteSqlRaw(x); db.Veiculos.FromSqlRaw(y); db.Veiculos.Find(1); db.Veiculos.FindAsync(2);";

            int contagem = ContarBypass(conteudo);

            Assert.Equal(4, contagem);
        }
    }
}
