using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Vistorias
{
    public class ReturnVistoriaVO
    {
        public int IdVistoria { get; set; }
        public int IdLocacao { get; set; }
        public int IdFuncionario { get; set; }
        public TipoVistoria Tipo { get; set; }
        public string DataHoraStr { get; set; } = string.Empty;
        public int KmRegistrado { get; set; }
        public decimal? NivelCombustivel { get; set; }
        public string? Observacoes { get; set; }
    }
}
