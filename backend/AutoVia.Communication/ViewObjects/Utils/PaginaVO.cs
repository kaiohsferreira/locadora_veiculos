namespace AutoVia.Communication.ViewObjects.Utils
{
    /// <summary>
    /// Resposta padrao de toda listagem paginada (Prompt 3):
    /// { itens, total, pagina, tamanhoPagina }.
    /// </summary>
    public class PaginaVO<T>
    {
        public PaginaVO()
        {
        }

        public PaginaVO(List<T> itens, int total, int pagina, int tamanhoPagina)
        {
            Itens = itens;
            Total = total;
            Pagina = pagina;
            TamanhoPagina = tamanhoPagina;
        }

        /// <summary>Itens da pagina atual.</summary>
        public List<T> Itens { get; set; } = new List<T>();

        /// <summary>Total de registros que atendem ao filtro (todas as paginas).</summary>
        public int Total { get; set; }

        /// <summary>Numero da pagina, base 1.</summary>
        public int Pagina { get; set; }

        /// <summary>Quantidade de itens por pagina.</summary>
        public int TamanhoPagina { get; set; }
    }
}
