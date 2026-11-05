namespace AutoVia.Integrations.Communication.Documento
{
    /// <summary>
    /// Campos crus devolvidos por um provedor de OCR, antes da normalizacao.
    /// Uso interno entre o provedor e o gateway. Campo nao lido vem null.
    /// </summary>
    public class CnhOcrBruto
    {
        /// <summary>Numero da CNH como veio do provedor (pode ter pontos e espacos).</summary>
        public string? Numero { get; set; }

        /// <summary>Confianca 0..1 do numero.</summary>
        public decimal ConfiancaNumero { get; set; }

        /// <summary>Categoria como veio do provedor (ex.: "ab", " B ").</summary>
        public string? Categoria { get; set; }

        /// <summary>Confianca 0..1 da categoria.</summary>
        public decimal ConfiancaCategoria { get; set; }

        /// <summary>Validade como veio do provedor (dd/MM/yyyy, yyyy-MM-dd ou dd/MM/yy).</summary>
        public string? Validade { get; set; }

        /// <summary>Confianca 0..1 da validade.</summary>
        public decimal ConfiancaValidade { get; set; }
    }
}
