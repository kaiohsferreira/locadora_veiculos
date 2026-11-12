using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de entrada do pedido de recuperacao de senha (POST /api/auth/recuperacao-senha). Estrutura
    /// plana. O perfil e anulavel de proposito: sem ele no JSON, o validador recusa (400).
    /// </summary>
    public class SolicitarRecuperacaoSenhaVO
    {
        /// <summary>Perfil da conta: CLIENTE ou FUNCIONARIO (obrigatorio).</summary>
        public PerfilUsuario? Perfil { get; set; }

        /// <summary>CPF com ou sem mascara; apos remover os nao digitos, 11 digitos.</summary>
        public string Cpf { get; set; } = string.Empty;
    }
}
