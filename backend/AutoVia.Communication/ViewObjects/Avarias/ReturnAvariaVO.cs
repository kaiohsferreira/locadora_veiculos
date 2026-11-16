using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Avarias
{
    /// <summary>Dados publicos de uma avaria e a URL temporaria da foto.</summary>
    public sealed class ReturnAvariaVO
    {
        public int IdAvaria { get; set; }
        public int IdVistoria { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public GravidadeAvaria Gravidade { get; set; }
        public decimal? ValorEstimado { get; set; }
        public FotoAvariaVO? Foto { get; set; }
    }
}
