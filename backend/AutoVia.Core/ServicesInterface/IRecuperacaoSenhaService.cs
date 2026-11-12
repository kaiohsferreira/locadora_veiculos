using AutoVia.Communication.ViewObjects.Autenticacao;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Recuperacao de senha por link de uso unico (N1, RF-49). O pedido responde sempre igual, sem revelar
    /// se a conta existe; a redefinicao troca a senha, desativa o link e derruba as sessoes anteriores.
    /// </summary>
    public interface IRecuperacaoSenhaService
    {
        /// <summary>Gera e enfileira o link para a conta elegivel. Devolve sempre a mesma mensagem neutra.</summary>
        Task<ReturnRecuperacaoSolicitadaVO> SolicitarAsync(SolicitarRecuperacaoSenhaVO vo);

        /// <summary>Redefine a senha com o link. Link invalido ou titular inativo lanca BusinessException (409).</summary>
        Task RedefinirAsync(RedefinirSenhaVO vo);
    }
}
