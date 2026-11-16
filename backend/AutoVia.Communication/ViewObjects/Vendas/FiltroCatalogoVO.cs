using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Vendas
{
    /// <summary>
    /// Filtros do catalogo de seminovos (GET /api/veiculos/a-venda). Herda pagina e
    /// tamanhoPagina de FiltroPaginacaoVO. Todos os filtros sao opcionais e combinam com AND.
    /// Validado por FiltroCatalogoVOValidator.
    /// </summary>
    public class FiltroCatalogoVO : FiltroPaginacaoVO
    {
        /// <summary>Busca parcial pela marca, sem diferenciar maiusculas. Opcional.</summary>
        public string? Marca { get; set; }

        /// <summary>Preco minimo de venda, em reais (inclusive). Opcional.</summary>
        public decimal? PrecoMinimo { get; set; }

        /// <summary>Preco maximo de venda, em reais (inclusive). Opcional.</summary>
        public decimal? PrecoMaximo { get; set; }

        /// <summary>Ano do modelo minimo (inclusive). Opcional.</summary>
        public int? AnoMinimo { get; set; }

        /// <summary>Ano do modelo maximo (inclusive). Opcional.</summary>
        public int? AnoMaximo { get; set; }

        /// <summary>Quilometragem maxima (inclusive). Opcional.</summary>
        public int? KmMaximo { get; set; }
    }
}
