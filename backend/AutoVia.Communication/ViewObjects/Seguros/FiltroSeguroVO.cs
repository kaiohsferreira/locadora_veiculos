using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Seguros
{
    /// <summary>
    /// Filtros da listagem de apolices (GET /api/seguros). Herda pagina e tamanhoPagina de
    /// FiltroPaginacaoVO. Os filtros sao opcionais e combinam com AND.
    /// Validado por FiltroSeguroVOValidator.
    /// </summary>
    public class FiltroSeguroVO : FiltroPaginacaoVO
    {
        /// <summary>Id do veiculo (maior que zero). Opcional.</summary>
        public int? IdVeiculo { get; set; }

        /// <summary>Quando true, traz so as apolices que vencem de hoje ate 60 dias. Padrao false.</summary>
        public bool ApenasAVencer { get; set; }
    }
}
