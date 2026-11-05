using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de entrada do login (POST /api/auth/login). Estrutura plana. O perfil e
    /// anulavel de proposito: sem ele no JSON, o validador recusa (400) em vez de
    /// assumir CLIENTE por ser o zero do enum.
    /// </summary>
    public class LoginVO
    {
        /// <summary>Perfil de acesso: CLIENTE ou FUNCIONARIO (obrigatorio).</summary>
        public PerfilUsuario? Perfil { get; set; }

        /// <summary>CPF com ou sem mascara; apos remover os nao digitos, 11 digitos.</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Senha em texto, enviada so nesta requisicao. Nunca e gravada nem registrada.</summary>
        public string Senha { get; set; } = string.Empty;
    }
}
