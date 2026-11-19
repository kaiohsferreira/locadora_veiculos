using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da manutencao (P8-BE-01 e P8-BE-02). Sem banco e sem log. Hoje traz so a leitura do tipo pelo nome,
    /// com a mesma regra da troca de status: nome exato depois do Trim, em comparacao ordinal, sem aceitar numero.
    /// </summary>
    public static class RegrasManutencao
    {
        /// <summary>
        /// Converte o texto no TipoManutencao pelo NOME exato, depois do Trim e em comparacao ordinal. Numero, minusculas,
        /// caractere nulo e texto vazio nao convertem.
        /// </summary>
        public static bool TentarLerTipoPorNome(string? texto, out TipoManutencao tipo)
        {
            tipo = TipoManutencao.PREVENTIVA;

            if (texto == null)
            {
                return false;
            }

            string nome = texto.Trim();

            foreach (TipoManutencao candidato in Enum.GetValues<TipoManutencao>())
            {
                if (string.Equals(candidato.ToString(), nome, StringComparison.Ordinal))
                {
                    tipo = candidato;

                    return true;
                }
            }

            return false;
        }
    }
}
