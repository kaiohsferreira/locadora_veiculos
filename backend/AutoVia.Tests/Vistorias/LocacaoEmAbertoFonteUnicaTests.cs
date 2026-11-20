using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;

namespace AutoVia.Tests.Vistorias;

/// <summary>
/// Fonte unica da locacao em aberto (item 3 da juncao com a branch_s2): a aceitacao da vistoria tem de ser exatamente o conjunto
/// de RegrasStatusVeiculo.StatusLocacoesEmAberto(), para todo valor de StatusLocacao.
/// </summary>
public class LocacaoEmAbertoFonteUnicaTests
{
    public static TheoryData<StatusLocacao> TodosOsStatusDeLocacao()
    {
        TheoryData<StatusLocacao> dados = new TheoryData<StatusLocacao>();

        foreach (StatusLocacao status in Enum.GetValues<StatusLocacao>())
        {
            dados.Add(status);
        }

        return dados;
    }

    [Theory]
    [MemberData(nameof(TodosOsStatusDeLocacao))]
    public void LocacaoAceitaVistoriaEIgualAoConjuntoEmAbertoParaCadaStatus(StatusLocacao status)
    {
        StatusLocacao[] emAberto = RegrasStatusVeiculo.StatusLocacoesEmAberto();

        bool esperado = emAberto.Contains(status);

        Assert.Equal(esperado, RegrasVistoria.LocacaoAceitaVistoria(status));
    }
}
