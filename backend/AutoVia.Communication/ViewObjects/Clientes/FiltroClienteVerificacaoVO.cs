using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// Filtro da fila de verificacao de cadastros da locadora (GET /api/clientes/verificacoes): paginacao herdada de
    /// FiltroPaginacaoVO, status do cadastro na locadora (nome exato, texto) e busca por nome, e-mail ou CPF. Validado por
    /// FiltroClienteVerificacaoVOValidator. O status e texto de proposito: numero e minuscula sao recusados com 400.
    /// </summary>
    public class FiltroClienteVerificacaoVO : FiltroPaginacaoVO
    {
        /// <summary>Status do cadastro na locadora: PENDENTE, APROVADO ou RECUSADO (nome exato). Ausente = todos.</summary>
        public string? Status { get; set; }

        /// <summary>Busca por nome, e-mail ou CPF (ate 60 caracteres depois de Trim). Ausente ou so espacos = sem busca.</summary>
        public string? Busca { get; set; }
    }
}
