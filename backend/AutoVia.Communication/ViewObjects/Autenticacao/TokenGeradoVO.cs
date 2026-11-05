namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO interno da emissao do token: o JWT gerado e o instante de expiracao.
    /// Nao e exposto pela API; o AutenticacaoService o converte em ReturnLoginVO.
    /// </summary>
    public class TokenGeradoVO
    {
        /// <summary>Token JWT assinado (HS256).</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Instante em que o token expira.</summary>
        public DateTime ExpiraEm { get; set; }
    }
}
