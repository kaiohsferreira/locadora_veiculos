using AutoVia.Infrastructure.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace AutoVia.Tests.Devolucao;

/// <summary>
/// Texto das duas travas do encerramento (LocacaoRepository.Encerramento): so o id da locacao como parametro, filtro de
/// disabled_at nulo, FOR UPDATE no fim e, nos pagamentos, ordem por id (ordem estavel de bloqueio). Prova a forma das constantes
/// lendo o campo privado por reflexao, sem banco. NAO prova o bloqueio no PostgreSQL (roteiro manual).
/// </summary>
public sealed class LocacaoEncerramentoTravasSqlTests
{
    private const string NomeTravaLocacao = "SqlBloquearLocacaoEncerramento";
    private const string NomeTravaPagamentos = "SqlBloquearPagamentosEncerramento";
    private const string ParametroDaTrava = "@id";

    private static string LerConstante(string nome)
    {
        FieldInfo? campo = typeof(LocacaoRepository).GetField(nome, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(campo);

        string? sql = (string?)campo!.GetValue(null);
        Assert.NotNull(sql);

        return sql!;
    }

    private static List<string> ParametrosDo(string sql)
    {
        List<string> parametros = Regex.Matches(sql, @"@\w+").Select(x => x.Value).Distinct().ToList();

        return parametros;
    }

    [Fact]
    public void TravaDaLocacao_SoPeloIdDaLocacaoAtivaEComForUpdate()
    {
        string sql = LerConstante(NomeTravaLocacao);

        Assert.Contains("FROM locacao", sql);
        Assert.Contains("id_locacao = @id", sql);
        Assert.Contains("disabled_at IS NULL", sql);
        Assert.EndsWith("FOR UPDATE", sql);
        Assert.Equal(new List<string> { ParametroDaTrava }, ParametrosDo(sql));
    }

    [Fact]
    public void TravaDosPagamentos_TodasAsLinhasAtivasDaLocacaoEmOrdemEComForUpdate()
    {
        string sql = LerConstante(NomeTravaPagamentos);

        Assert.Contains("FROM pagamento", sql);
        Assert.Contains("id_locacao = @id", sql);
        Assert.Contains("disabled_at IS NULL", sql);
        Assert.Contains("ORDER BY id_pagamento", sql);
        Assert.Contains("FOR UPDATE", sql);
        Assert.Equal(new List<string> { ParametroDaTrava }, ParametrosDo(sql));
    }
}
