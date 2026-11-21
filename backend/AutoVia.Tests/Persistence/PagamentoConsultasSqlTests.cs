using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas das cobrancas traduzidas pelo EF sem banco (ToQueryString). Prova: a consulta do cliente NAO traz o parametro
    /// do filtro de locadora, nem nas navegacoes da posse (locacao e venda); a posse e pelo id do cliente do contexto, com
    /// WHERE FALSE para outro cliente e sem contexto; disabled_at nulo na cobranca e na origem; a trava da baixa tem FOR UPDATE
    /// com o id como parametro e a empresa do contexto DENTRO do SQL (EXISTS sobre locacao ou venda e veiculo) alem do filtro
    /// de locadora; a listagem da locadora tem o filtro com a empresa do contexto; a ordem e a pagina (LIMIT e OFFSET) das
    /// duas listas; e os predicados das abas, dos totais e da listagem traduzem para SQL (ToQueryString lanca se nao traduzir).
    /// NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class PagamentoConsultasSqlTests
    {
        private const int IdClienteDoTeste = 7;
        private const int IdClienteAlheio = 8;
        private const int IdPagamentoDoTeste = 12;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string PosseNegadaNoSql = "WHERE FALSE";
        private const string ColunaDeIdDoClienteNoSql = @"id_cliente = @(\w+)";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string TravaNoSql = @"id_pagamento = @(\w+) AND disabled_at IS NULL AND \(EXISTS .*v\.id_empresa = @(\w+)\).*FOR UPDATE";
        private const string ColunaDeTravaNoSql = "FOR UPDATE";
        private const string ColunaQrCodeNoSql = "qr_code_base64";
        private const string TravaDoClienteNoSql = @"id_pagamento = @(\w+) AND disabled_at IS NULL AND \(EXISTS \(SELECT 1 FROM locacao l WHERE l\.id_locacao = pagamento\.id_locacao AND l\.id_cliente = @(\w+) AND l\.disabled_at IS NULL\) OR EXISTS \(SELECT 1 FROM venda vd WHERE vd\.id_venda = pagamento\.id_venda AND vd\.id_cliente = @(\w+) AND vd\.disabled_at IS NULL\)\) FOR UPDATE";
        private const string NaoRastreiaNoExpression = "AsNoTracking";
        private const string LinhaInteiraNoSql = "SELECT * FROM pagamento";
        private const string OrdemDaListaDaLocadoraNoSql = @"ORDER BY \S+\.data_vencimento, \S+\.id_pagamento\b";
        private const string OrdemDaListaDoClienteNoSql = @"ORDER BY \S+\.data_vencimento DESC, \S+\.id_pagamento DESC\b";
        private const string PaginaNoSql = @"LIMIT @(\w+) OFFSET @(\w+)";
        private const int DeslocamentoDoTeste = 20;
        private const int TamanhoDaPaginaDoTeste = 10;
        private static readonly DateOnly HojeDoTeste = new DateOnly(2026, 11, 19);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeCliente(int idCliente)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.DefinirCliente(idCliente);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.FINANCEIRO, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>Valor do parametro no cabecalho do ToQueryString (o filtro e os argumentos aparecem como "-- @nome='valor'").</summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @?" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        [Fact]
        public void ConsultaMeusPagamentos_NaoTemParametroDoFiltroDeLocadora_NemNasNavegacoesDaPosse()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(FiltroEmpresa, sql);
        }

        [Fact]
        public void ConsultaMeusPagamentos_PosseDoClienteNasDuasOrigens_LocacaoEVenda()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString();

            MatchCollection colunas = Regex.Matches(sql, ColunaDeIdDoClienteNoSql);
            Assert.True(colunas.Count >= 2, $"A posse deveria comparar id_cliente na locacao e na venda. SQL: {sql}");
            Assert.Equal(IdClienteDoTeste.ToString(), ValorDoParametro(sql, colunas[0].Groups[1].Value));
            Assert.DoesNotContain(PosseNegadaNoSql, sql);
        }

        [Fact]
        public void ConsultaMeusPagamentos_OutroCliente_WhereFalse()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteAlheio).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotContain(FiltroEmpresa, sql);
        }

        [Fact]
        public void ConsultaMeusPagamentos_SemClienteNoContexto_WhereFalse()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotContain(FiltroEmpresa, sql);
        }

        [Fact]
        public void ConsultaMeusPagamentos_DisabledAtNoPagamentoENaOrigem()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString();

            Assert.True(Regex.Matches(sql, Regex.Escape(DisabledAtNulo)).Count >= 3, $"Deveria filtrar disabled_at na cobranca, na locacao e na venda. SQL: {sql}");
        }

        [Fact]
        public void ConsultaMeusPagamentos_TrazOVeiculoParaADescricao()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString();

            Assert.Contains("veiculo", sql);
        }

        [Fact]
        public void ConsultaPagamentosDaLocadora_TemOFiltroDeLocadoraComAEmpresaDoContexto()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPagamentosDaLocadora().ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaTravaPagamento_ForUpdateComOIdEFiltroDeLocadoraLigado()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaTravaPagamento(IdPagamentoDoTeste).ToQueryString();

            Assert.Contains(ColunaDeTravaNoSql, sql);
            Match trava = Regex.Match(sql, TravaNoSql);
            Assert.True(trava.Success, $"A trava deveria ter FOR UPDATE e disabled_at nulo. SQL: {sql}");
            Assert.Equal(IdPagamentoDoTeste.ToString(), ValorDoParametro(sql, trava.Groups[1].Value));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, trava.Groups[2].Value));
            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void ConsultaTravaPagamentoDoCliente_ForUpdateComIdClienteSemFiltroDeLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaTravaPagamentoDoCliente(IdClienteDoTeste, IdPagamentoDoTeste).ToQueryString();

            Assert.Contains(ColunaDeTravaNoSql, sql);
            Assert.DoesNotContain(FiltroEmpresa, sql);
            Match trava = Regex.Match(sql, TravaDoClienteNoSql);
            Assert.True(trava.Success, $"A trava do cliente deveria ter a posse nas duas origens, disabled_at e FOR UPDATE. SQL: {sql}");
            Assert.Equal(IdPagamentoDoTeste.ToString(), ValorDoParametro(sql, trava.Groups[1].Value));
            Assert.Equal(IdClienteDoTeste.ToString(), ValorDoParametro(sql, trava.Groups[2].Value));
            Assert.Equal(IdClienteDoTeste.ToString(), ValorDoParametro(sql, trava.Groups[3].Value));
        }

        [Fact]
        public void ConsultaTravaParaPixSemRastreio_NaoRastreiaA_TravaDoClienteEADaLocadoraRastreiaParaGravar()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string leituraDaTrava = repositorio.ConsultaTravaParaPixSemRastreio(IdClienteDoTeste, IdPagamentoDoTeste).Expression.ToString();
            string gravacaoDoCliente = repositorio.ConsultaTravaPagamentoDoCliente(IdClienteDoTeste, IdPagamentoDoTeste).Expression.ToString();
            string travaDaBaixa = repositorio.ConsultaTravaPagamento(IdPagamentoDoTeste).Expression.ToString();

            Assert.Contains(NaoRastreiaNoExpression, leituraDaTrava);
            Assert.DoesNotContain(NaoRastreiaNoExpression, gravacaoDoCliente);
            Assert.DoesNotContain(NaoRastreiaNoExpression, travaDaBaixa);
        }

        [Fact]
        public void ConsultaCobrancaPixDaLocadora_TemOFiltroDeLocadoraEAOrigemAtiva()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCobrancaPixDaLocadora(IdPagamentoDoTeste).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
            Assert.Contains(DisabledAtNulo, sql);
            Assert.DoesNotContain(ColunaDeTravaNoSql, sql);
        }

        [Fact]
        public void ListagensDaLocadoraEDoCliente_NaoLeemOQrCodeBase64()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sqlLocadora = repositorio.ConsultaPaginaDaLocadora(repositorio.ConsultaPagamentosDaLocadora(), DeslocamentoDoTeste, TamanhoDaPaginaDoTeste).ToQueryString();
            Assert.DoesNotContain(ColunaQrCodeNoSql, sqlLocadora);

            using AutoViaDbContext dbCliente = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorioCliente = new PagamentoRepository(dbCliente, new FakeLogService());
            string sqlCliente = repositorioCliente.ConsultaPaginaDoCliente(repositorioCliente.ConsultaMeusPagamentos(IdClienteDoTeste), DeslocamentoDoTeste, TamanhoDaPaginaDoTeste).ToQueryString();
            Assert.DoesNotContain(ColunaQrCodeNoSql, sqlCliente);
        }

        [Fact]
        public void LeiturasDeUmaCobranca_TrazemOQrCodeBase64()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            using AutoViaDbContext dbCliente = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorioCliente = new PagamentoRepository(dbCliente, new FakeLogService());

            Assert.Contains(ColunaQrCodeNoSql, repositorioCliente.ConsultaMeusPagamentos(IdClienteDoTeste).ToQueryString());
            Assert.Contains(ColunaQrCodeNoSql, repositorio.ConsultaCobrancaPixDaLocadora(IdPagamentoDoTeste).ToQueryString());
            Assert.Contains(ColunaQrCodeNoSql, repositorio.ConsultaTravaPagamento(IdPagamentoDoTeste).ToQueryString());
            // A trava do cliente (sem filtro de locadora) le a linha inteira com SELECT *: o QR vem junto, sem o nome da coluna no SQL.
            Assert.Contains(LinhaInteiraNoSql, repositorioCliente.ConsultaTravaPagamentoDoCliente(IdClienteDoTeste, IdPagamentoDoTeste).ToQueryString());
        }

        [Fact]
        public void ConsultaLocacaoDaCobranca_E_VendaDaCobranca_TemFiltroDeLocadoraLigado()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sqlLocacao = repositorio.ConsultaLocacaoDaCobranca(IdPagamentoDoTeste).ToQueryString();
            string sqlVenda = repositorio.ConsultaVendaDaCobranca(IdPagamentoDoTeste).ToQueryString();

            Assert.Contains(FiltroEmpresa, sqlLocacao);
            Assert.Contains(FiltroEmpresa, sqlVenda);
            Assert.Contains(DisabledAtNulo, sqlLocacao);
            Assert.Contains(DisabledAtNulo, sqlVenda);
        }

        [Fact]
        public void ConsultaPaginaDaLocadora_OrdemDeVencimentoEIdCrescenteEPagina()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPaginaDaLocadora(repositorio.ConsultaPagamentosDaLocadora(), DeslocamentoDoTeste, TamanhoDaPaginaDoTeste).ToQueryString();

            Assert.Matches(OrdemDaListaDaLocadoraNoSql, sql);
            Assert.DoesNotMatch(@"ORDER BY \S+\.data_vencimento DESC", sql);
            Match pagina = Regex.Match(sql, PaginaNoSql);
            Assert.True(pagina.Success, $"A lista deveria ter LIMIT e OFFSET. SQL: {sql}");
            Assert.Equal(TamanhoDaPaginaDoTeste.ToString(), ValorDoParametro(sql, pagina.Groups[1].Value));
            Assert.Equal(DeslocamentoDoTeste.ToString(), ValorDoParametro(sql, pagina.Groups[2].Value));
        }

        [Fact]
        public void ConsultaPaginaDoCliente_OrdemDeVencimentoEIdDecrescenteEPagina()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPaginaDoCliente(repositorio.ConsultaMeusPagamentos(IdClienteDoTeste), DeslocamentoDoTeste, TamanhoDaPaginaDoTeste).ToQueryString();

            Assert.Matches(OrdemDaListaDoClienteNoSql, sql);
            Match pagina = Regex.Match(sql, PaginaNoSql);
            Assert.True(pagina.Success, $"A lista do cliente deveria ter LIMIT e OFFSET. SQL: {sql}");
            Assert.Equal(TamanhoDaPaginaDoTeste.ToString(), ValorDoParametro(sql, pagina.Groups[1].Value));
            Assert.Equal(DeslocamentoDoTeste.ToString(), ValorDoParametro(sql, pagina.Groups[2].Value));
        }

        [Fact]
        public void PredicadosDaAbaNaConsultaDoCliente_TraduzemParaSqlComAPosseMantida()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());
            string[] abas = { ConstantsValues.AbaTodos, ConstantsValues.AbaPendentes, ConstantsValues.AbaPagos };
            string[] periodos = { ConstantsValues.PeriodoDozeMeses, ConstantsValues.PeriodoAnoAtual, ConstantsValues.PeriodoSeisMeses, ConstantsValues.PeriodoTudo };

            foreach (string aba in abas)
            {
                foreach (string periodo in periodos)
                {
                    DateOnly? inicio = RegrasPagamento.InicioDoPeriodo(periodo, HojeDoTeste);

                    string sql = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).Where(RegrasPagamento.PredicadoDaAba(aba, inicio)).ToQueryString();

                    Assert.True(Regex.Matches(sql, ColunaDeIdDoClienteNoSql).Count >= 2, $"A posse deveria continuar no SQL da aba {aba}. SQL: {sql}");
                    Assert.DoesNotContain(PosseNegadaNoSql, sql);
                    if (aba != ConstantsValues.AbaPendentes && inicio.HasValue)
                    {
                        Assert.True(Regex.IsMatch(sql, @"data_vencimento >= @\w+"), $"O limite do periodo deveria estar no SQL da aba {aba}. SQL: {sql}");
                    }
                }
            }
        }

        [Fact]
        public void PredicadosDaAbaTodosEPagos_ComparamOVencimentoComOInicioDoPeriodo()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());
            DateOnly? inicio = RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoSeisMeses, HojeDoTeste);

            string sqlTodos = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaTodos, inicio)).ToQueryString();
            string sqlPagos = repositorio.ConsultaMeusPagamentos(IdClienteDoTeste).Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaPagos, inicio)).ToQueryString();

            Assert.Matches(@"data_vencimento >= @\w+", sqlTodos);
            Assert.Matches(@"data_vencimento >= @\w+", sqlPagos);
            Assert.Contains("status", sqlPagos);
        }

        [Fact]
        public void PredicadosDosTotaisEDaListagem_TraduzemParaSqlNaConsultaDaLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sqlPendente = repositorio.ConsultaPagamentosDaLocadora().Where(RegrasPagamento.PredicadoPendente()).ToQueryString();
            Assert.Contains(FiltroEmpresa, sqlPendente);
            Assert.True(sqlPendente.Contains("tipo <> 'CAUCAO'"), $"A pendencia deveria excluir a caucao no SQL. SQL: {sqlPendente}");

            string sqlPago = repositorio.ConsultaPagamentosDaLocadora().Where(RegrasPagamento.PredicadoPagoNoPeriodo(null)).ToQueryString();
            Assert.Contains(FiltroEmpresa, sqlPago);

            string sqlQuitado = repositorio.ConsultaPagamentosDaLocadora().Where(RegrasPagamento.PredicadoQuitadoNoAno(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))).ToQueryString();
            Assert.Contains("data_pagamento IS NOT NULL", sqlQuitado);
            Assert.Matches(@"data_pagamento >= @\w+", sqlQuitado);
            Assert.Matches(@"data_pagamento < @\w+", sqlQuitado);

            string sqlListagem = repositorio.ConsultaPagamentosDaLocadora().Where(RegrasPagamento.PredicadoDaListagem(StatusPagamento.PENDENTE, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))).ToQueryString();
            Assert.Matches(@"data_vencimento >= @\w+", sqlListagem);
            Assert.Matches(@"data_vencimento <= @\w+", sqlListagem);
        }
    }
}
