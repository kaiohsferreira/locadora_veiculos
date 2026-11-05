using AutoVia.Communication.Enums;

namespace AutoVia.Integrations.Communication.Documento
{
    /// <summary>
    /// Resultado da leitura da CNH. O OCR apenas sugere: RequerConferenciaManual
    /// indica que a equipe deve conferir os dados antes de aprovar (RF-10).
    /// Campo nao lido vem null e conta confianca 0 em ConfiancaGeral.
    /// </summary>
    public class CnhLeituraResultado
    {
        /// <summary>Numero da CNH somente com digitos. Null se nao foi lido.</summary>
        public string? NumeroCnh { get; set; }

        /// <summary>Confianca 0..1 do numero (0 quando o campo esta ausente).</summary>
        public decimal ConfiancaNumero { get; set; }

        /// <summary>Categoria A, B, AB, C, D ou E. Null se nao for uma categoria valida.</summary>
        public CategoriaCnh? Categoria { get; set; }

        /// <summary>Confianca 0..1 da categoria (0 quando o campo esta ausente).</summary>
        public decimal ConfiancaCategoria { get; set; }

        /// <summary>Data de validade. Null se nao foi lida ou nao e uma data valida.</summary>
        public DateOnly? Validade { get; set; }

        /// <summary>Confianca 0..1 da validade (0 quando o campo esta ausente).</summary>
        public decimal ConfiancaValidade { get; set; }

        /// <summary>Menor das tres confiancas. Campo ausente conta 0.</summary>
        public decimal ConfiancaGeral { get; set; }

        /// <summary>Confianca minima configurada em confianca_minima.</summary>
        public decimal ConfiancaMinima { get; set; }

        /// <summary>
        /// true quando ConfiancaGeral e menor que o minimo, quando algum campo esta ausente
        /// ou quando o numero nao tem 11 digitos. Igual ao minimo nao exige conferencia.
        /// </summary>
        public bool RequerConferenciaManual { get; set; }

        /// <summary>Modelo de OCR configurado em modelo_ocr.</summary>
        public string ModeloOcr { get; set; } = string.Empty;

        /// <summary>Nome do provedor que fez a leitura (ex.: FAKE).</summary>
        public string Provedor { get; set; } = string.Empty;
    }
}
