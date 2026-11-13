namespace AutoVia.Communication.ViewObjects.Clientes
{
    /// <summary>
    /// VO de saida da verificacao de cadastro: so a informacao de existencia. Nao traz o campo,
    /// o valor, o id, o status nem o nome do cliente encontrado.
    /// </summary>
    public class ReturnVerificacaoCadastroVO
    {
        /// <summary>Verdadeiro se existe cliente ativo com o valor informado no campo consultado.</summary>
        public bool Existe { get; set; }
    }
}
