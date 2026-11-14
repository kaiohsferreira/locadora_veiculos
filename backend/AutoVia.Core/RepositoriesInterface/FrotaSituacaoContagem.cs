using AutoVia.Communication.Enums;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Resultado agrupado do banco: quantos veiculos (DisabledAt nulo, da locadora do contexto) estao em uma situacao.
    /// Nao e entidade: e so a linha do GROUP BY por status.
    /// </summary>
    public class FrotaSituacaoContagem
    {
        /// <summary>Situacao do veiculo (coluna status).</summary>
        public StatusVeiculo Situacao { get; set; }

        /// <summary>Quantidade de veiculos nesta situacao.</summary>
        public int Quantidade { get; set; }
    }
}
