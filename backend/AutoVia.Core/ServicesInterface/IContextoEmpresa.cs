using AutoVia.Communication.Enums;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Empresa (locadora) e cargo do funcionario autenticado na requisicao. Preenchido pela validacao
    /// do token com os valores do BANCO (nunca da claim), e lido pelo filtro global "Empresa" do
    /// AutoViaDbContext e pelas politicas de autorizacao. IdEmpresa = 0 significa sem empresa: o
    /// filtro devolve nada (fail-closed).
    /// </summary>
    public interface IContextoEmpresa
    {
        /// <summary>Id da empresa do funcionario autenticado; 0 quando nao ha contexto.</summary>
        int IdEmpresa { get; }

        /// <summary>Id do funcionario autenticado; 0 quando nao ha contexto.</summary>
        int IdFuncionario { get; }

        /// <summary>Cargo do funcionario autenticado; nulo quando nao ha contexto.</summary>
        CargoFuncionario? Cargo { get; }

        /// <summary>Verdadeiro quando a empresa do funcionario e a operadora da plataforma (empresa inicial).</summary>
        bool EmpresaOperadora { get; }

        /// <summary>Verdadeiro para ADMINISTRADOR ou FINANCEIRO: unico ponto do corte de valores financeiros.</summary>
        bool PodeVerValores { get; }

        /// <summary>Define o contexto da requisicao com os valores lidos do banco. Chamado uma vez, na validacao do token.</summary>
        void Definir(int idEmpresa, int idFuncionario, CargoFuncionario cargo, bool empresaOperadora);
    }
}
