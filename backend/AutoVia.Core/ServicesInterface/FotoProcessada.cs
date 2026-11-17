namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Resultado do processamento de uma foto: bytes da versao gravada (JPEG, sem EXIF), tipo do conteudo e as dimensoes dessa versao.
    /// Nao e VO e nao vai para AutoVia.Communication.
    /// </summary>
    /// <param name="Conteudo">Bytes do JPEG recodificado.</param>
    /// <param name="ContentType">Sempre image/jpeg.</param>
    /// <param name="Largura">Largura, em pixels, da imagem gravada.</param>
    /// <param name="Altura">Altura, em pixels, da imagem gravada.</param>
    public sealed record FotoProcessada(byte[] Conteudo, string ContentType, int Largura, int Altura);
}
