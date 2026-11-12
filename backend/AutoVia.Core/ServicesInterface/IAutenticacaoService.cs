using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;

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

        /// <summary>Abre a sessao de um cliente ja identificado por outro meio (login Google, BE-6), com a mesma regra de validade do login por senha. Lanca UnauthorizedException se a conta nao tem senha cadastrada.</summary>
        ReturnLoginVO EmitirSessaoCliente(Cliente cliente, bool lembrar);
    }
}
