using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// VO de saida de um funcionario da equipe (GET, POST e lista). O CPF sai mascarado no servidor; senha e hash nunca
    /// saem. A data sai em AAAA-MM-DD (DateOnly) e em DD/MM/AAAA (DataAdmissaoStr).
    /// </summary>
    public class ReturnFuncionarioVO
    {
        /// <summary>Id do funcionario.</summary>
        public int IdFuncionario { get; set; }

        /// <summary>Nome completo.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>CPF mascarado no formato 341.***.***-12.</summary>
        public string CpfMascarado { get; set; } = string.Empty;

        /// <summary>Matricula na locadora.</summary>
        public string Matricula { get; set; } = string.Empty;

        /// <summary>E-mail institucional.</summary>
        public string? Email { get; set; }

        /// <summary>Cargo atribuido.</summary>
        public CargoFuncionario Cargo { get; set; }

        /// <summary>Falso quando o acesso foi desativado (o registro continua na equipe).</summary>
        public bool Ativo { get; set; }

        /// <summary>Data de admissao (AAAA-MM-DD).</summary>
        public DateOnly DataAdmissao { get; set; }

        /// <summary>Data de admissao em DD/MM/AAAA, formatada pelo StaticMethods no Profile.</summary>
        public string DataAdmissaoStr { get; set; } = string.Empty;

        /// <summary>Verdadeiro quando o funcionario e o proprio usuario autenticado.</summary>
        public bool EhVoce { get; set; }
    }
}
