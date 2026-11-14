using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// VO de entrada da edicao de funcionario (PUT /api/equipe/{id}). Sem CPF (imutavel), sem senha, sem id de empresa
    /// e sem situacao: a situacao muda so pelo PATCH de situacao.
    /// </summary>
    public class FuncionarioEdicaoVO
    {
        /// <summary>Nome completo do funcionario (obrigatorio, ate 120 caracteres).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Matricula (obrigatoria, ate 20). Unica dentro da locadora, sem diferenca de maiusculas.</summary>
        public string Matricula { get; set; } = string.Empty;

        /// <summary>E-mail institucional (obrigatorio, valido, ate 120).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Data de admissao (AAAA-MM-DD). Obrigatoria e nao pode ser futura.</summary>
        public DateOnly DataAdmissao { get; set; }

        /// <summary>Cargo atribuido. Anulavel de proposito: sem ele no JSON, o validador responde 400.</summary>
        public CargoFuncionario? Cargo { get; set; }
    }
}
