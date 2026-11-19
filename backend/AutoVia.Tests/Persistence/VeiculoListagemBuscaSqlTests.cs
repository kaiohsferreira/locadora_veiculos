using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Busca da listagem de veiculos (VeiculoRepository.ConsultaFrotaPaginada) traduzida pelo EF sem banco (ToQueryString).
    /// Prova: sem busca, o SQL e o mesmo do commit a30e759 (antes da busca) e nao compara texto da busca; com busca, placa,
    /// chassi, modelo e marca ligados por OR dentro de um grupo que entra por AND com os demais filtros e com o filtro de
    /// empresa; placa sem texto util (so hifen) nao entra no grupo; % e _ da busca saem escapados (texto literal).
    /// NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class VeiculoListagemBuscaSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdCategoriaDoTeste = 7;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string OrdenacaoDaConsulta = "ORDER BY";
        private const string TermoDeBusca = "corolla";
        private const string TermoDeBuscaComPlaca = "ABC-1";
        private const string TermoPlacaNormalizado = "ABC1";
        private const string TermoSoComHifen = "--";
        private const string TermoComCuringa = "a%_b";
        private const string TermoComCuringaEscapado = @"%a\%\_b%";
        private const string TermoComCuringaSemEscape = "%a%_b%";
        private const string OperadorOu = " OR ";
        private const string OperadorE = " AND ";
        private const int QuantidadeDeOuSemPlaca = 2;
        private const int QuantidadeDeOuComPlaca = 3;
        private const string QuebraDeLinha = "\n";

        /// <summary>
        /// SQL da consulta sem busca (ConsultaFrotaPaginada com a busca nula), gerado no commit a30e759, antes da busca.
        /// Qualquer mudanca no SQL sem busca quebra SemBusca_IgualAoSqlDoCommitAnterior.
        /// </summary>
        private static readonly string[] LinhasSqlSemBuscaAnterior = new string[]
        {
            "-- @ef_filter__p2='True'",
            "-- @ef_filter__IdEmpresaContexto='4242'",
            "-- @p1='20'",
            "-- @p='0'",
            "SELECT v0.id_veiculo, v0.ano_fabricacao, v0.ano_modelo, v0.chassi, v0.cor, v0.created_at, v0.data_aquisicao, v0.disabled_at, v0.fornecedor, v0.id_categoria, v0.id_empresa, v0.km_atual, v0.marca, v0.modelo, v0.numero_nota_fiscal_compra, v0.placa, v0.renavam, v0.status, v0.updated_at, v0.valor_diaria, v0.valor_venda, c.id_categoria, c.created_at, c.descricao, c.disabled_at, c.nome, c.updated_at, c.valor_diaria_base, e.id_empresa, e.ativo, e.bairro, e.cep, e.cidade, e.cnpj, e.complemento, e.created_at, e.disabled_at, e.email, e.logradouro, e.nome_fantasia, e.numero, e.operadora, e.razao_social, e.telefone, e.uf, e.updated_at, s0.id_veiculo_foto, s0.chave_miniatura, s0.chave_objeto, s0.created_at, s0.disabled_at, s0.id_funcionario_envio, s0.id_funcionario_remocao, s0.id_veiculo, s0.ordem, s0.updated_at, s0.id_veiculo0",
            "FROM (",
            "    SELECT v.id_veiculo, v.ano_fabricacao, v.ano_modelo, v.chassi, v.cor, v.created_at, v.data_aquisicao, v.disabled_at, v.fornecedor, v.id_categoria, v.id_empresa, v.km_atual, v.marca, v.modelo, v.numero_nota_fiscal_compra, v.placa, v.renavam, v.status, v.updated_at, v.valor_diaria, v.valor_venda",
            "    FROM veiculo AS v",
            "    WHERE @ef_filter__p2 AND v.id_empresa = @ef_filter__IdEmpresaContexto AND v.disabled_at IS NULL",
            "    ORDER BY v.placa, v.id_veiculo",
            "    LIMIT @p1 OFFSET @p",
            ") AS v0",
            "INNER JOIN categoria AS c ON v0.id_categoria = c.id_categoria",
            "INNER JOIN empresa AS e ON v0.id_empresa = e.id_empresa",
            "LEFT JOIN (",
            "    SELECT s.id_veiculo_foto, s.chave_miniatura, s.chave_objeto, s.created_at, s.disabled_at, s.id_funcionario_envio, s.id_funcionario_remocao, s.id_veiculo, s.ordem, s.updated_at, s.id_veiculo0",
            "    FROM (",
            "        SELECT v1.id_veiculo_foto, v1.chave_miniatura, v1.chave_objeto, v1.created_at, v1.disabled_at, v1.id_funcionario_envio, v1.id_funcionario_remocao, v1.id_veiculo, v1.ordem, v1.updated_at, v3.id_veiculo AS id_veiculo0, ROW_NUMBER() OVER(PARTITION BY v1.id_veiculo ORDER BY v1.ordem, v1.id_veiculo_foto) AS row",
            "        FROM veiculo_foto AS v1",
            "        INNER JOIN (",
            "            SELECT v2.id_veiculo, v2.id_empresa",
            "            FROM veiculo AS v2",
            "            WHERE @ef_filter__p2 AND v2.id_empresa = @ef_filter__IdEmpresaContexto",
            "        ) AS v3 ON v1.id_veiculo = v3.id_veiculo",
            "        WHERE @ef_filter__p2 AND v3.id_empresa = @ef_filter__IdEmpresaContexto AND v1.disabled_at IS NULL",
            "    ) AS s",
            "    WHERE s.row <= 1",
            ") AS s0 ON v0.id_veiculo = s0.id_veiculo",
            "ORDER BY v0.placa, v0.id_veiculo, c.id_categoria, e.id_empresa, s0.id_veiculo, s0.ordem, s0.id_veiculo_foto"
        };

        private static readonly string SqlSemBuscaAnterior = string.Join(QuebraDeLinha, LinhasSqlSemBuscaAnterior);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Qualquer funcao de minusculas aplicada a uma coluna da veiculo (o EF gera lower(...) em minusculas).</summary>
        private static readonly Regex FuncaoDeMinusculasNaVeiculo = new Regex(@"lower\(\s*\w+\.\w+\s*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Inicio do grupo da busca: parentese que abre logo antes da comparacao de minusculas do chassi.</summary>
        private static readonly Regex GrupoDaBusca = new Regex(@"\(\s*lower\(\w+\.chassi\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>
        /// Trecho do SQL do WHERE da consulta principal, ate antes do ORDER BY (que vem logo depois do WHERE).
        /// Assim o ORDER BY v.placa nao entra no trecho e a comparacao de placa so aparece se estiver no WHERE.
        /// </summary>
        private static string TrechoDoWhere(string sql)
        {
            int inicioDoWhere = sql.IndexOf("WHERE", StringComparison.Ordinal);

            Assert.True(inicioDoWhere >= 0, "A consulta deveria ter WHERE: " + sql);

            int inicioDaOrdenacao = sql.IndexOf(OrdenacaoDaConsulta, inicioDoWhere, StringComparison.Ordinal);

            Assert.True(inicioDaOrdenacao > inicioDoWhere, "A consulta deveria ter ORDER BY depois do WHERE: " + sql);

            return sql.Substring(inicioDoWhere, inicioDaOrdenacao - inicioDoWhere);
        }

        /// <summary>
        /// Grupo da busca dentro do WHERE: do parentese que abre antes de lower(chassi) ate o parentese que fecha
        /// (contando os parenteses aninhados). Falha o teste se o grupo nao existir ou nao fechar.
        /// </summary>
        private static string GrupoDaBuscaNoWhere(string where)
        {
            Match inicio = GrupoDaBusca.Match(where);

            Assert.True(inicio.Success, "O WHERE deveria ter o grupo da busca: " + where);

            int profundidade = 0;

            for (int indice = inicio.Index; indice < where.Length; indice++)
            {
                char caractere = where[indice];

                if (caractere == '(')
                {
                    profundidade++;
                }
                else if (caractere == ')')
                {
                    profundidade--;

                    if (profundidade == 0)
                    {
                        string grupo = where.Substring(inicio.Index, indice - inicio.Index + 1);

                        return grupo;
                    }
                }
            }

            Assert.Fail("O grupo da busca nao fecha o parentese: " + where);

            return string.Empty;
        }

        private static int QuantidadeDeOu(string where)
        {
            int quantidade = Regex.Matches(where, Regex.Escape(OperadorOu)).Count;

            return quantidade;
        }

        [Fact]
        public void SemBusca_NaoHaComparacaoDeTextoDaBusca()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, null, 0, 20).ToQueryString();

            Assert.False(FuncaoDeMinusculasNaVeiculo.IsMatch(sql), "Sem busca, o SQL nao pode comparar chassi, modelo ou marca: " + sql);
            Assert.DoesNotContain(OperadorOu, TrechoDoWhere(sql));
        }

        [Fact]
        public void SemBusca_IgualAoSqlDoCommitAnterior()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, null, 0, 20).ToQueryString();

            Assert.Equal(SqlSemBuscaAnterior, sql.ReplaceLineEndings(QuebraDeLinha));
        }

        [Fact]
        public void BuscaVazia_GeraOMesmoSqlDeSemBusca()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string semBusca = repositorio.ConsultaFrotaPaginada(null, null, null, null, 0, 20).ToQueryString();
            string comBuscaVazia = repositorio.ConsultaFrotaPaginada(null, null, null, string.Empty, 0, 20).ToQueryString();

            Assert.Equal(semBusca, comBuscaVazia);
        }

        /// <summary>
        /// "corolla" tem letras, entao o termo tambem vira termo de placa (COROLLA): sao quatro termos ligados por OR
        /// (chassi, modelo, marca e placa). A contagem 3 de OR e a de quatro termos, e a placa aparece na comparacao v.placa.
        /// </summary>
        [Fact]
        public void ComBuscaComLetras_QuatroTermosLigadosPorOr_IncluiAPlaca()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, TermoDeBusca, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            string grupo = GrupoDaBuscaNoWhere(where);
            Assert.Matches(@"(?i)lower\(v\.chassi\)", grupo);
            Assert.Matches(@"(?i)lower\(v\.modelo\)", grupo);
            Assert.Matches(@"(?i)lower\(v\.marca\)", grupo);
            Assert.Matches(@"(?i)v\.placa", grupo);
            Assert.Equal(QuantidadeDeOuComPlaca, QuantidadeDeOu(grupo));
        }

        [Fact]
        public void ComBusca_ComPlaca_IncluiAPlacaNoGrupoDeOr()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, TermoDeBuscaComPlaca, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            string grupo = GrupoDaBuscaNoWhere(where);
            Assert.Equal(QuantidadeDeOuComPlaca, QuantidadeDeOu(grupo));
            Assert.Matches(@"(?i)v\.placa", grupo);
            Assert.Contains(TermoPlacaNormalizado, sql);
            Assert.DoesNotContain(TermoDeBuscaComPlaca, sql);
        }

        [Fact]
        public void ComBuscaSoComHifen_NaoAdicionaComparacaoDePlaca()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, TermoSoComHifen, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            string grupo = GrupoDaBuscaNoWhere(where);
            Assert.Equal(QuantidadeDeOuSemPlaca, QuantidadeDeOu(grupo));
            Assert.Matches(@"(?i)lower\(v\.chassi\)", grupo);
            Assert.False(Regex.IsMatch(where, @"(?i)v\.placa"), "Sem placa no termo, o WHERE nao pode comparar a placa: " + where);
        }

        /// <summary>
        /// % e _ da busca viram texto literal: o EF escapa com barra invertida no parametro LIKE (padrao do PostgreSQL).
        /// O texto cru com curingas nao aparece no SQL.
        /// </summary>
        [Fact]
        public void ComBuscaComPorcentoEUnderline_TextoEscapadoNoParametro()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, TermoComCuringa, 0, 20).ToQueryString();

            Assert.Contains(TermoComCuringaEscapado, sql);
            Assert.DoesNotContain(TermoComCuringaSemEscape, sql);
        }

        /// <summary>
        /// Busca com status e categoria: (a) os predicados de status e de id_categoria estao no WHERE; (b) o grupo da busca
        /// esta entre parenteses, sem OR fora dele, e ligado por AND aos demais filtros; (c) o parametro do filtro de empresa
        /// continua no WHERE.
        /// </summary>
        [Fact]
        public void ComBusca_MantemOFiltroDeEmpresaEOsOutrosFiltrosPorAnd()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(IdCategoriaDoTeste, StatusVeiculo.DISPONIVEL, null, TermoDeBusca, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.Matches(@"(?i)v\.status\s*=", where);
            Assert.Matches(@"(?i)v\.id_categoria\s*=", where);
            Assert.Contains(ParametroEmpresa, where);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);

            string grupo = GrupoDaBuscaNoWhere(where);
            int posicaoDoGrupo = where.IndexOf(grupo, StringComparison.Ordinal);
            string antesDoGrupo = where.Substring(0, posicaoDoGrupo).TrimEnd();
            string depoisDoGrupo = where.Substring(posicaoDoGrupo + grupo.Length).Trim();
            string restoDoWhere = where.Remove(posicaoDoGrupo, grupo.Length);

            Assert.Equal(QuantidadeDeOuComPlaca, QuantidadeDeOu(grupo));
            Assert.DoesNotContain(OperadorOu, restoDoWhere);
            Assert.EndsWith(OperadorE.Trim(), antesDoGrupo);
            Assert.True(depoisDoGrupo.Length == 0 || depoisDoGrupo.StartsWith(OperadorE.Trim(), StringComparison.Ordinal), "Depois do grupo da busca so pode vir AND: " + where);
        }
    }
}
