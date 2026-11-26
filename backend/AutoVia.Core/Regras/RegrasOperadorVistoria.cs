using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Nome abreviado do operador que registrou a vistoria (N10, item 6 do encerramento; vistoria-saida v4): primeiro nome e
    /// inicial do ultimo sobrenome com ponto ("Rogerio V."); so o primeiro nome quando ha uma palavra; nulo quando o nome e vazio.
    /// Funcao pura: sem banco e sem log.
    /// </summary>
    public static class RegrasOperadorVistoria
    {
        public static string? Abreviar(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return null;
            }

            string[] partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (partes.Length == 1)
            {
                return partes[0];
            }

            string primeiroNome = partes[0];
            char inicialSobrenome = char.ToUpperInvariant(partes[partes.Length - 1][0]);
            string abreviado = $"{primeiroNome} {inicialSobrenome}.";

            return abreviado;
        }
    }
}
