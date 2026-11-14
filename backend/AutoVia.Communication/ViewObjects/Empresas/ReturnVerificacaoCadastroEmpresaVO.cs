namespace AutoVia.Communication.ViewObjects.Empresas
{
    /// <summary>
    /// VO de saida da verificacao publica de locadora: so a existencia. Nao traz o campo, o valor, o id, o nome
    /// nem a razao social da empresa encontrada.
    /// </summary>
    public class ReturnVerificacaoCadastroEmpresaVO
    {
        /// <summary>Verdadeiro se existe empresa ativa com o valor informado no campo consultado.</summary>
        public bool Existe { get; set; }
    }
}
