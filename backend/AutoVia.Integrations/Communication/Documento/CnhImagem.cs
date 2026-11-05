namespace AutoVia.Integrations.Communication.Documento
{
    /// <summary>
    /// Imagem da CNH enviada ao gateway de leitura. O conteudo nunca vai para log
    /// nem e persistido (LGPD): so e lido para validar e entregar ao provedor.
    /// </summary>
    public class CnhImagem
    {
        /// <summary>Bytes da imagem: obrigatorio, nao vazio, maximo 4 MB.</summary>
        public byte[] Conteudo { get; set; } = Array.Empty<byte>();

        /// <summary>Tipo MIME da imagem: image/jpeg ou image/png.</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Nome do arquivo enviado: obrigatorio. Nunca vai para log.</summary>
        public string NomeArquivo { get; set; } = string.Empty;
    }
}
