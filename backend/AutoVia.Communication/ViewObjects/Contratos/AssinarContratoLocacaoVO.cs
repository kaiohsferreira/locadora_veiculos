namespace AutoVia.Communication.ViewObjects.Contratos
{
    /// <summary>
    /// Corpo do POST de assinatura do contrato (flat). O id da locacao vem da rota; o servidor define datas, o funcionario
    /// que coleta, os valores e o documento. Nao ha IdEmpresa, CPF nem dado de cartao neste VO.
    /// </summary>
    public sealed class AssinarContratoLocacaoVO
    {
        /// <summary>Nome de quem assina, como digitado na tela (obrigatorio, ate 100 caracteres, sem caractere de controle).</summary>
        public string NomeAssinante { get; set; } = string.Empty;

        /// <summary>Aceite das condicoes gerais: obrigatorio e deve ser verdadeiro.</summary>
        public bool AceiteCondicoesGerais { get; set; }

        /// <summary>Aceite da politica de combustivel: exigido pelo servico somente quando a clausula 5 consta do documento.</summary>
        public bool? AceitePoliticaCombustivel { get; set; }

        /// <summary>Imagem da assinatura em PNG, codificada em base64 (ate 65.536 bytes decodificados).</summary>
        public string AssinaturaPngBase64 { get; set; } = string.Empty;
    }
}
