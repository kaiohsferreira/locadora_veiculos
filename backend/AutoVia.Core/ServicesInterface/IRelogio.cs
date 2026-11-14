using System;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Relogio do servidor, isolado para os testes controlarem a data (visao geral da locadora). A implementacao de producao
    /// devolve DateTime.Now (regra R5: nunca UtcNow, as colunas sao timestamp sem fuso).
    /// </summary>
    public interface IRelogio
    {
        /// <summary>Data e hora local atual do servidor.</summary>
        DateTime Agora { get; }
    }
}
