using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Conferencia, pelo SQL gerado (ToQueryString, sem banco), das consultas da visao geral (P11-BE-01 e P11-BE-02). Confere,
    /// para duas locadoras diferentes: o filtro "Empresa" com o id do contexto em cada consulta, a locadora A nunca aparecer
    /// no SQL da B, a condicao de desativado (DisabledAt nulo), e o contexto vazio gerando a guarda falsa (fail-closed).
    /// Tambem confere os status e tipos gravados como texto e os limites de data de cada indicador.
    /// </summary>
    public class PainelRepositorySqlTests
    {
        private const string ParametroDoContexto = "@ef_filter__IdEmpresaContexto";
        private const int EmpresaA = 4242;
        private const int EmpresaB = 7373;
        private const string GuardaFalsa = "='False'";
        private const string GuardaVerdadeira = "='True'";
        private const string DesativadoNulo = "disabled_at IS NULL";
        private const string DataInicioSql = "11/08/2026";
        private const string DataFimExclusivoSql = "11/15/2026";
        private const string DataHojeSql = "11/14/2026";

        private static readonly DateOnly Inicio = new DateOnly(2026, 11, 8);
        private static readonly DateOnly FimExclusivo = new DateOnly(2026, 11, 15);
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 14);
        private static readonly DateTime InicioDia = new DateTime(2026, 11, 14, 0, 0, 0);
        private static readonly DateTime FimDia = new DateTime(2026, 11, 15, 0, 0, 0);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Repositorio do painel sobre um contexto de empresa real (id 0 = sem contexto).</summary>
        private static PainelRepository CriarRepositorio(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 1, CargoFuncionario.ADMINISTRADOR, false);
            }

            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new PainelRepository(db, new FakeLogService());
        }

        /// <summary>Uma consulta por indicador, pelo SQL que o repositorio geraria (a mesma IQueryable que a execucao usa).</summary>
        private static Dictionary<string, Func<PainelRepository, string>> Consultas()
        {
            return new Dictionary<string, Func<PainelRepository, string>>
            {
                ["ReceitaDoPeriodo"] = r => r.ConsultaReceitaDoPeriodo(Inicio, FimExclusivo).ToQueryString(),
                ["AReceber"] = r => r.ConsultaAReceber(Hoje).ToQueryString(),
                ["EmAtraso"] = r => r.ConsultaEmAtraso(Hoje).ToQueryString(),
                ["FrotaPorSituacao"] = r => r.ConsultaFrotaPorSituacao().ToQueryString(),
                ["LocacoesEmAndamento"] = r => r.ConsultaLocacoesEmAndamento().ToQueryString(),
                ["DevolucoesPrevistas"] = r => r.ConsultaDevolucoesPrevistas(InicioDia, FimDia).ToQueryString()
            };
        }

        [Fact]
        public void TodaConsulta_UsaOFiltroDaEmpresaDoContexto()
        {
            PainelRepository repositorio = CriarRepositorio(EmpresaA);

            foreach (KeyValuePair<string, Func<PainelRepository, string>> caso in Consultas())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains($"{ParametroDoContexto}='{EmpresaA}'"), $"{caso.Key}: o filtro deveria usar o id {EmpresaA} do contexto.");
                Assert.True(sql.Contains(GuardaVerdadeira), $"{caso.Key}: o guarda de contexto positivo deveria estar ativo.");
            }
        }

        [Fact]
        public void DuasLocadoras_CadaConsultaSaiSoComOIdDaSuaEmpresa()
        {
            PainelRepository primeira = CriarRepositorio(EmpresaA);
            PainelRepository segunda = CriarRepositorio(EmpresaB);

            foreach (KeyValuePair<string, Func<PainelRepository, string>> caso in Consultas())
            {
                string sqlPrimeira = caso.Value(primeira);
                string sqlSegunda = caso.Value(segunda);

                Assert.Contains($"{ParametroDoContexto}='{EmpresaA}'", sqlPrimeira);
                Assert.DoesNotContain($"{ParametroDoContexto}='{EmpresaB}'", sqlPrimeira);
                Assert.Contains($"{ParametroDoContexto}='{EmpresaB}'", sqlSegunda);
                Assert.DoesNotContain($"{ParametroDoContexto}='{EmpresaA}'", sqlSegunda);
            }
        }

        [Fact]
        public void SemContexto_FailClosed_AGuardaViraFalsaEmTodaConsulta()
        {
            PainelRepository repositorio = CriarRepositorio(0);

            foreach (KeyValuePair<string, Func<PainelRepository, string>> caso in Consultas())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains($"{ParametroDoContexto}='0'"), $"{caso.Key}: sem contexto o id deveria ser 0.");
                Assert.True(sql.Contains(GuardaFalsa), $"{caso.Key}: sem contexto a guarda deveria ser falsa (nenhuma linha).");
            }
        }

        [Fact]
        public void TodaConsulta_SoEnxergaRegistrosAtivos()
        {
            PainelRepository repositorio = CriarRepositorio(EmpresaA);

            foreach (KeyValuePair<string, Func<PainelRepository, string>> caso in Consultas())
            {
                string sql = caso.Value(repositorio);

                Assert.Contains(DesativadoNulo, sql);
            }
        }

        [Fact]
        public void ReceitaDoPeriodo_ExcluiCaucaoEUsaStatusPagoComTexto()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaReceitaDoPeriodo(Inicio, FimExclusivo).ToQueryString();

            Assert.Contains("'PAGO'", sql);
            Assert.Contains("'CAUCAO'", sql);
            Assert.Contains("<>", sql);
        }

        [Fact]
        public void ReceitaDoPeriodo_TemInicioIncluidoEFimExcluido()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaReceitaDoPeriodo(Inicio, FimExclusivo).ToQueryString();

            Assert.Contains("p.data_pagamento >= @inicio", sql);
            Assert.Contains("p.data_pagamento < @fimExclusivo", sql);
            Assert.Contains($"@inicio='{DataInicioSql}'", sql);
            Assert.Contains($"@fimExclusivo='{DataFimExclusivoSql}'", sql);
        }

        [Fact]
        public void AReceber_EhPendenteENaoVencido()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaAReceber(Hoje).ToQueryString();

            Assert.Contains("p.status = 'PENDENTE'", sql);
            Assert.Contains("p.data_vencimento >= @hoje", sql);
            Assert.Contains($"@hoje='{DataHojeSql}'", sql);
        }

        [Fact]
        public void EmAtraso_EhAtrasadoOuPendenteVencido()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaEmAtraso(Hoje).ToQueryString();

            Assert.Contains("'ATRASADO'", sql);
            Assert.Contains("'PENDENTE'", sql);
            Assert.Contains(" < ", sql);
        }

        [Fact]
        public void LocacoesEmAndamento_UsaStatusEmAndamentoComTexto()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaLocacoesEmAndamento().ToQueryString();

            Assert.Contains("'EM_ANDAMENTO'", sql);
        }

        [Fact]
        public void DevolucoesPrevistas_UsaADataDeDevolucaoPrevistaNoDia()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaDevolucoesPrevistas(InicioDia, FimDia).ToQueryString();

            Assert.Contains("data_devolucao_prevista", sql);
            Assert.Contains("'EM_ANDAMENTO'", sql);
        }

        [Fact]
        public void FrotaPorSituacao_AgrupaPorStatusNoBanco()
        {
            string sql = CriarRepositorio(EmpresaA).ConsultaFrotaPorSituacao().ToQueryString();

            Assert.Contains("GROUP BY v.status", sql);
            Assert.Contains("count(*)", sql);
        }
    }
}
