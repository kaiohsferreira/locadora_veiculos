using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// Situacao da conta lida pela sessao (VO interno de ISessaoService.ObterSituacaoAsync; nao vai
    /// para a API). Para funcionario, Ativo exige conta ativa e empresa ativa; para cliente, a regra da v2.
    /// </summary>
    public class SessaoSituacaoVO
    {
        /// <summary>Conta utilizavel: funcionario ativo com empresa ativa, ou cliente ativo com senha.</summary>
        public bool Ativo { get; set; }

        /// <summary>Empresa do funcionario; nulo para cliente ou quando a conta nao existe.</summary>
        public int? IdEmpresa { get; set; }

        /// <summary>Cargo do funcionario; nulo para cliente ou quando a conta nao existe.</summary>
        public CargoFuncionario? Cargo { get; set; }

        /// <summary>Verdadeiro quando a empresa do funcionario esta ativa (Ativo e DisabledAt nulo).</summary>
        public bool EmpresaAtiva { get; set; }

        /// <summary>Verdadeiro quando a empresa do funcionario e a operadora da plataforma.</summary>
        public bool EmpresaOperadora { get; set; }
    }
}
