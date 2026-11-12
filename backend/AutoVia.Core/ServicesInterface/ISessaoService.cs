using AutoVia.Communication.Enums;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Conferencia da sessao a cada requisicao autenticada: a conta precisa estar ativa e a
    /// versao da sessao (claim sv) precisa ser a atual. O resultado fica em cache por usuario.
    /// </summary>
    public interface ISessaoService
    {
        /// <summary>Indica se a sessao do token ainda vale: conta ativa e sv igual a atual. Sem sv, devolve false sem consultar o banco.</summary>
        Task<bool> SessaoValidaAsync(PerfilUsuario perfil, int idUsuario, string? versaoSessao);

        /// <summary>Descarta o resultado em cache do usuario; a proxima verificacao consulta o banco.</summary>
        void Invalidar(PerfilUsuario perfil, int idUsuario);
    }
}
