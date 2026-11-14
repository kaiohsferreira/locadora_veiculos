using AutoVia.Communication.Enums;
using System;

namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// VO de entrada do cadastro de funcionario (POST /api/equipe). Estrutura plana, sem id de empresa: a empresa vem
    /// sempre do contexto do ADMINISTRADOR autenticado. A senha inicial existe so nesta requisicao e nunca volta.
    /// </summary>
    public class FuncionarioVO
    {
        /// <summary>Nome completo do funcionario (obrigatorio, ate 120 caracteres).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>CPF com ou sem mascara; 11 digitos com digito verificador valido. Unico na plataforma inteira.</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Matricula (obrigatoria, ate 20). Unica dentro da locadora, sem diferenca de maiusculas.</summary>
        public string Matricula { get; set; } = string.Empty;

        /// <summary>E-mail institucional (obrigatorio, valido, ate 120).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Data de admissao (AAAA-MM-DD). Obrigatoria e nao pode ser futura.</summary>
        public DateOnly DataAdmissao { get; set; }

        /// <summary>Cargo atribuido. Anulavel de proposito: sem ele no JSON, o validador responde 400.</summary>
        public CargoFuncionario? Cargo { get; set; }

        /// <summary>Senha inicial (8 a 64 caracteres, com maiuscula e numero). Gravada so como hash; nunca volta.</summary>
        public string SenhaInicial { get; set; } = string.Empty;
    }
}
