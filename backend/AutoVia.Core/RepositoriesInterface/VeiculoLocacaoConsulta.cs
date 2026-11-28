using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Veículo da frota pública e disponibilidade calculada na mesma consulta.</summary>
    public sealed class VeiculoLocacaoConsulta
    {
        public required Veiculo Veiculo { get; init; }
        public bool? DisponivelNoPeriodo { get; init; }
    }
}
