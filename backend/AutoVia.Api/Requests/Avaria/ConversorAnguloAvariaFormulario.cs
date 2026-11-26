using AutoVia.Communication.Enums;

namespace AutoVia.Api.Requests.Avaria
{
    /// <summary>
    /// Conversao unica do angulo do formulario da avaria: so os nomes exatos do enum (maiusculas), sem aceitar numero nem variacao.
    /// Vazio significa sem angulo (o campo e opcional).
    /// </summary>
    public static class ConversorAnguloAvariaFormulario
    {
        /// <summary>
        /// Converte o texto do formulario. Nulo ou vazio significa sem angulo. Retorna falso quando o texto nao e um dos quatro nomes do enum.
        /// </summary>
        public static bool TentarConverter(string? texto, out AnguloAvaria? angulo)
        {
            if (string.IsNullOrEmpty(texto))
            {
                angulo = null;
                return true;
            }

            string[] nomes = Enum.GetNames(typeof(AnguloAvaria));
            if (!Array.Exists(nomes, nome => string.Equals(nome, texto, StringComparison.Ordinal)))
            {
                angulo = null;
                return false;
            }

            angulo = Enum.Parse<AnguloAvaria>(texto);
            return true;
        }
    }
}
