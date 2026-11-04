using AutoVia.Helpers;

namespace AutoVia.Communication.ViewObjects.Utils
{
    /// <summary>
    /// Parametros de paginacao recebidos por query string em toda listagem
    /// (Prompt 3): pagina base 1, tamanhoPagina padrao 20 e maximo 100.
    /// Os filtros de cada modulo herdam desta classe.
    /// Validado por FiltroPaginacaoVOValidator.
    /// </summary>
    public class FiltroPaginacaoVO
    {
        /// <summary>Numero da pagina, base 1.</summary>
        public int Pagina { get; set; } = ConstantsValues.PaginaPadrao;

        /// <summary>Itens por pagina (1 a 100).</summary>
        public int TamanhoPagina { get; set; } = ConstantsValues.TamanhoPaginaPadrao;

        /// <summary>Quantidade de registros a pular, calculada a partir da pagina.</summary>
        public int Deslocamento => (Pagina - 1) * TamanhoPagina;
    }
}
