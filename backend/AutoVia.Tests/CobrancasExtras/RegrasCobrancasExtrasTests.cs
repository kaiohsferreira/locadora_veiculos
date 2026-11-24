using AutoVia.Core.Regras;

namespace AutoVia.Tests.CobrancasExtras;

/// <summary>Regra pura da previsao efetiva da devolucao (mudanca B1): o dia combinado na mesma hora da retirada.</summary>
public sealed class RegrasCobrancasExtrasTests
{
    [Fact]
    public void RetiradaAs10ComPrevistaNaMeiaNoiteVaiAsDezNoDiaCombinado()
    {
        DateTime efetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(new DateTime(2026, 11, 18, 10, 0, 0), new DateTime(2026, 11, 20, 0, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), efetiva);
    }

    [Fact]
    public void RetiradaAMeiaNoiteMantemAMeiaNoiteDoDiaCombinado()
    {
        DateTime efetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(new DateTime(2026, 11, 18, 0, 0, 0), new DateTime(2026, 11, 20, 0, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 20, 0, 0, 0), efetiva);
    }

    [Fact]
    public void PrevistaGravadaComHoraTemAHoraDescartada()
    {
        DateTime efetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(new DateTime(2026, 11, 18, 10, 0, 0), new DateTime(2026, 11, 20, 15, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), efetiva);
    }

    [Fact]
    public void RetiradaComSegundosPreservaOsSegundosNaPrevista()
    {
        DateTime efetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(new DateTime(2026, 11, 18, 23, 59, 59), new DateTime(2026, 11, 20, 0, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 20, 23, 59, 59), efetiva);
    }
}
