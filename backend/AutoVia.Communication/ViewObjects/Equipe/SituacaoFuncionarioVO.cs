namespace AutoVia.Communication.ViewObjects.Equipe
{
    /// <summary>
    /// VO de entrada da situacao do funcionario (PATCH /api/equipe/{id}/situacao). Ativo = false desativa o acesso;
    /// true reativa. Nao e exclusao. Anulavel para o validador responder 400 quando o campo e omitido.
    /// </summary>
    public class SituacaoFuncionarioVO
    {
        /// <summary>Nova situacao do acesso (obrigatoria): true ativo, false desativado.</summary>
        public bool? Ativo { get; set; }
    }
}
