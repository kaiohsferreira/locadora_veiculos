using AutoVia.Communication.ViewObjects.Empresas;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Regras do cadastro publico de locadora (P10-BE-01): cria a empresa com o responsavel ADMINISTRADOR e
    /// verifica publicamente se um CNPJ ou e-mail ja esta cadastrado.
    /// </summary>
    public interface IEmpresaService
    {
        /// <summary>Cadastra a locadora e o responsavel (409 na ordem CNPJ, e-mail, CPF); devolve o retorno sem senha nem token.</summary>
        Task<ReturnEmpresaVO> InsertAsync(CadastroLocadoraVO cadastroVO);

        /// <summary>Informa se ja existe empresa ativa com o CNPJ ou o e-mail informado. Devolve so a existencia.</summary>
        Task<ReturnVerificacaoCadastroEmpresaVO> VerificarCadastroAsync(VerificarCadastroEmpresaVO verificarCadastroVO);
    }
}
