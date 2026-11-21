using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Conferencia, pelo SQL gerado (ToQueryString, sem banco), das consultas do painel financeiro (P7-BE-05, RF-35): cada
    /// indicador e cada aba tem o filtro "Empresa" com o id do contexto, a condicao de desativado, a caucao fora (tipo
    /// &lt;&gt; 'CAUCAO') e o eixo de data certo no WHERE (recebido pelo pagamento; o resto pelo vencimento). As listas nao
    /// leem qr_code_base64. Duas locadoras: o SQL de uma nunca traz o id da outra.
    /// </summary>
    public class PainelFinanceiroConsultasSqlTests
    {
        private const string ParametroDoContexto = "@ef_filter__IdEmpresaContexto";
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string GuardaFalsa = "='False'";
        private const string DesativadoNulo = "disabled_at IS NULL";
        private const string TipoSemCaucao = "tipo <> 'CAUCAO'";
        private const string StatusPago = "status = 'PAGO'";
        private const string StatusPendente = "status = 'PENDENTE'";
        private const string StatusAtrasado = "status = 'ATRASADO'";
        private const string DataPagamentoNoWhere = "data_pagamento";
        private const string VencimentoNoWhere = "data_vencimento";
        private const string ColunaQrCodeNoSql = "qr_code_base64";
        private const string Onde = "WHERE";
        private const string OrdemDescendente = "DESC";
        private const string OrdemPorPagamento = "data_pagamento";
        private const int EmpresaA = 4242;
        private const int EmpresaB = 7373;

        private static readonly DateOnly De = new DateOnly(2026, 11, 1);
        private static readonly DateOnly Ate = new DateOnly(2026, 11, 30);
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 21);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Repositorio do painel financeiro sobre um contexto de empresa real (id 0 = sem contexto).</summary>
        private static PainelFinanceiroRepository CriarRepositorio(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 1, CargoFuncionario.ADMINISTRADOR, false);
            }

            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new PainelFinanceiroRepository(db, new FakeLogService());
        }

        private static string ClausulaWhere(string sql)
        {
            int posicao = sql.IndexOf(Onde, StringComparison.Ordinal);

            return posicao >= 0 ? sql.Substring(posicao) : string.Empty;
        }

        /// <summary>Os seis indicadores e as quatro abas, cada um pelo SQL que o repositorio executaria.</summary>
        private static Dictionary<string, Func<PainelFinanceiroRepository, string>> ConsultasDeListaEIndicador()
        {
            Dictionary<string, Func<PainelFinanceiroRepository, string>> consultas = new Dictionary<string, Func<PainelFinanceiroRepository, string>>
            {
                ["Recebido"] = r => r.ConsultaIndicadorRecebido(De, Ate).ToQueryString(),
                ["AReceber"] = r => r.ConsultaIndicadorAReceber(De, Ate, Hoje).ToQueryString(),
                ["EmAtraso"] = r => r.ConsultaIndicadorEmAtraso(De, Ate, Hoje).ToQueryString(),
                ["TicketMedio"] = r => r.ConsultaIndicadorTicketMedio(De, Ate).ToQueryString()
            };

            foreach (string aba in ConstantsValues.AbasTitulosFinanceiros)
            {
                string nomeDaAba = aba;
                consultas["Titulos" + nomeDaAba] = r => r.ConsultaTitulos(nomeDaAba, De, Ate, Hoje).ToQueryString();
                consultas["ContagemTitulos" + nomeDaAba] = r => r.ConsultaTitulosDaAba(nomeDaAba, De, Ate, Hoje).ToQueryString();
            }

            return consultas;
        }

        [Fact]
        public void TodaConsulta_UsaOFiltroDaEmpresaDoContextoEAExcluiDesativados()
        {
            PainelFinanceiroRepository repositorio = CriarRepositorio(EmpresaA);

            foreach (KeyValuePair<string, Func<PainelFinanceiroRepository, string>> caso in ConsultasDeListaEIndicador())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains($"{ParametroDoContexto}='{EmpresaA}'"), $"{caso.Key}: o filtro deveria usar o id {EmpresaA} do contexto. SQL: {sql}");
                Assert.True(sql.Contains(DesativadoNulo), $"{caso.Key}: deveria excluir o desativado. SQL: {sql}");
                Assert.True(sql.Contains(FiltroEmpresa), $"{caso.Key}: sem o filtro Empresa. SQL: {sql}");
            }
        }

        [Fact]
        public void TodaConsulta_ExcluiACaucao()
        {
            PainelFinanceiroRepository repositorio = CriarRepositorio(EmpresaA);

            foreach (KeyValuePair<string, Func<PainelFinanceiroRepository, string>> caso in ConsultasDeListaEIndicador())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains(TipoSemCaucao), $"{caso.Key}: a caucao precisa ficar fora do SQL. SQL: {sql}");
            }
        }

        [Fact]
        public void Recebido_FiltraPeloDataDoPagamentoEPagos()
        {
            string where = ClausulaWhere(CriarRepositorio(EmpresaA).ConsultaIndicadorRecebido(De, Ate).ToQueryString());

            Assert.Contains(StatusPago, where);
            Assert.Contains(DataPagamentoNoWhere, where);
            Assert.DoesNotContain(VencimentoNoWhere, where);
        }

        [Fact]
        public void AReceber_FiltraPelosNaoVencidosEPendentes()
        {
            string where = ClausulaWhere(CriarRepositorio(EmpresaA).ConsultaIndicadorAReceber(De, Ate, Hoje).ToQueryString());

            Assert.Contains(StatusPendente, where);
            Assert.Contains(VencimentoNoWhere, where);
            Assert.DoesNotContain(DataPagamentoNoWhere, where);
        }

        [Fact]
        public void EmAtraso_SomaAtrasadoOuPendenteVencidoPeloVencimento()
        {
            string where = ClausulaWhere(CriarRepositorio(EmpresaA).ConsultaIndicadorEmAtraso(De, Ate, Hoje).ToQueryString());

            Assert.Contains(StatusAtrasado, where);
            Assert.Contains(StatusPendente, where);
            Assert.Contains(VencimentoNoWhere, where);
            Assert.DoesNotContain(DataPagamentoNoWhere, where);
        }

        [Fact]
        public void TicketMedio_SoPagosPeloVencimento()
        {
            string where = ClausulaWhere(CriarRepositorio(EmpresaA).ConsultaIndicadorTicketMedio(De, Ate).ToQueryString());

            Assert.Contains(StatusPago, where);
            Assert.Contains(VencimentoNoWhere, where);
            Assert.DoesNotContain(DataPagamentoNoWhere, where);
        }

        [Fact]
        public void Titulos_NaoLemOQrCodeEmNenhumaAba()
        {
            PainelFinanceiroRepository repositorio = CriarRepositorio(EmpresaA);

            foreach (string aba in ConstantsValues.AbasTitulosFinanceiros)
            {
                string sql = repositorio.ConsultaTitulos(aba, De, Ate, Hoje).ToQueryString();

                Assert.DoesNotContain(ColunaQrCodeNoSql, sql);
            }
        }

        [Fact]
        public void PagosRecentes_OrdenaPeloPagamentoDecrescente()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaTitulos(ConstantsValues.AbaTitulosPagosRecentes, De, Ate, Hoje).ToQueryString();

            Assert.Contains(OrdemPorPagamento, sql);
            Assert.Contains(OrdemDescendente, sql);
            Assert.Contains(StatusPago, sql);
        }

        [Fact]
        public void DuasLocadoras_CadaConsultaSaiSoComOIdDaSuaEmpresa()
        {
            PainelFinanceiroRepository primeira = CriarRepositorio(EmpresaA);
            PainelFinanceiroRepository segunda = CriarRepositorio(EmpresaB);

            foreach (KeyValuePair<string, Func<PainelFinanceiroRepository, string>> caso in ConsultasDeListaEIndicador())
            {
                string sqlPrimeira = caso.Value(primeira);
                string sqlSegunda = caso.Value(segunda);

                Assert.True(sqlPrimeira.Contains($"{ParametroDoContexto}='{EmpresaA}'"), $"{caso.Key}: a locadora A deveria ter o proprio id.");
                Assert.True(sqlSegunda.Contains($"{ParametroDoContexto}='{EmpresaB}'"), $"{caso.Key}: a locadora B deveria ter o proprio id.");
                Assert.False(sqlPrimeira.Contains($"'{EmpresaB}'"), $"{caso.Key}: o SQL da locadora A nao pode trazer o id da B.");
                Assert.False(sqlSegunda.Contains($"'{EmpresaA}'"), $"{caso.Key}: o SQL da locadora B nao pode trazer o id da A.");
            }
        }

        [Fact]
        public void SemContexto_OFiltroFechaTudo()
        {
            PainelFinanceiroRepository repositorio = CriarRepositorio(0);

            string sql = repositorio.ConsultaIndicadorRecebido(De, Ate).ToQueryString();

            Assert.Contains(GuardaFalsa, sql);
        }
    }
}
