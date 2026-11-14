using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Empresas
{
    /// <summary>
    /// VO de entrada da verificacao publica de locadora (POST /api/empresas/verificar-cadastro). Um campo por
    /// chamada, valor so no corpo. O campo e anulavel de proposito: sem ele no JSON, o validador recusa (400).
    /// </summary>
    public class VerificarCadastroEmpresaVO
    {
        /// <summary>Campo a consultar: CNPJ ou EMAIL (obrigatorio).</summary>
        public CampoVerificacaoEmpresa? Campo { get; set; }

        /// <summary>Valor do campo, com ou sem mascara (CNPJ), ate 200 caracteres no corpo.</summary>
        public string Valor { get; set; } = string.Empty;
    }
}
