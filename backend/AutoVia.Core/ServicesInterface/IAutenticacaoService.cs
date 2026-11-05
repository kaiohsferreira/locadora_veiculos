using AutoVia.Communication.ViewObjects.Autenticacao;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Login de clientes e funcionarios. Toda falha de autenticacao sai como
    /// UnauthorizedException com a mesma mensagem, sem revelar qual dado falhou.
    /// </summary>
    public interface IAutenticacaoService
    {
        /// <summary>Autentica o CPF e a senha no perfil informado e devolve o token; lanca UnauthorizedException se recusar.</summary>
        Task<ReturnLoginVO> LoginAsync(LoginVO loginVO);
    }
}
