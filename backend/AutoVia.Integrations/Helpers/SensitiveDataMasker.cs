namespace AutoVia.Integrations.Helpers
{
    /// <summary>
    /// Mascara valores protegidos antes de qualquer log. Use sempre que for
    /// registrar URL, cabecalho ou corpo de requisicao de integracao.
    /// </summary>
    public static class SensitiveDataMasker
    {
        /// <summary>Substitui cada valor sensivel encontrado no texto por ***.</summary>
        public static string Mask(string? text, IEnumerable<string>? sensitiveValues)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (sensitiveValues == null)
                return text;

            string masked = text;
            foreach (string value in sensitiveValues)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                masked = masked.Replace(value, ConstantsIntegration.Mascara, StringComparison.Ordinal);
            }

            return masked;
        }

        /// <summary>Devolve *** para valor protegido e o proprio valor caso contrario.</summary>
        public static string MaskIfProtected(string value, bool isProtected)
        {
            return isProtected ? ConstantsIntegration.Mascara : value;
        }
    }
}
