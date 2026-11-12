using AutoVia.Helpers;

namespace AutoVia.Core.Options
{
    /// <summary>
    /// Configuracao da conferencia de sessao (secao Autenticacao:Sessao). O resultado da
    /// consulta fica em cache por usuario; CacheSegundos e o tempo maximo para uma troca de
    /// senha ou desativacao valer nas instancias que nao receberam Invalidar.
    /// </summary>
    public class SessaoOptions
    {
        /// <summary>Tempo, em segundos, que o resultado da conferencia de sessao fica em cache (1 a 300).</summary>
        public int CacheSegundos { get; set; } = ConstantsValues.SessaoCacheSegundosPadrao;
    }
}
