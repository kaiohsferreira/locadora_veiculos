using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.ViewObjects.Frota;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Serviços públicos de cadastro de locadoras, verificação de CNPJ/e-mail e listagem de cidades.
    /// </summary>
    public interface IEmpresaService
    {
        /// <summary>Lista as cidades com locadoras ativas.</summary>
        Task<List<ReturnCidadeLocadoraVO>> GetCidadesAsync();

        /// <summary>Cadastra a locadora e o responsavel (409 na ordem CNPJ, e-mail, CPF); devolve o retorno sem senha nem token.</summary>
        Task<ReturnEmpresaVO> InsertAsync(CadastroLocadoraVO cadastroVO);

        /// <summary>Informa se ja existe empresa ativa com o CNPJ ou o e-mail informado. Devolve so a existencia.</summary>
        Task<ReturnVerificacaoCadastroEmpresaVO> VerificarCadastroAsync(VerificarCadastroEmpresaVO verificarCadastroVO);
    }
}
