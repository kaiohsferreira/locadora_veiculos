using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de entrada da verificacao de cadastro (POST publico). Estrutura plana, com um campo
    /// por chamada. O campo e anulavel de proposito: sem ele no JSON, o validador recusa (400).
    /// O valor vai sempre no corpo, nunca na URL. A validacao e feita pelo VerificarCadastroVOValidator.
    /// </summary>
    public class VerificarCadastroVO
    {
        /// <summary>Campo a consultar: CPF, EMAIL ou CNH (obrigatorio).</summary>
        public CampoVerificacaoCadastro? Campo { get; set; }

        /// <summary>Valor do campo, com ou sem mascara (CPF e CNH), ate 200 caracteres no corpo.</summary>
        public string Valor { get; set; } = string.Empty;
    }
}
