namespace AutoVia.Communication.ViewObjects.Contratos
{
    /// <summary>
    /// Retorno do contrato da locacao (GET e 201 do POST). Pendente: documento montado na hora, sem hash e sem assinatura.
    /// Assinado: copia guardada (documento e hash da gravacao) e a assinatura.
    /// </summary>
    public sealed class ReturnContratoLocacaoVO
    {
        public int IdLocacao { get; set; }

        /// <summary>PENDENTE_ASSINATURA ou ASSINADO.</summary>
        public string Situacao { get; set; } = string.Empty;

        /// <summary>SHA-256 do documento guardado, em hexadecimal minusculo; nulo enquanto pendente.</summary>
        public string? HashSha256 { get; set; }

        public ContratoDocumentoVO Documento { get; set; } = new ContratoDocumentoVO();

        /// <summary>Assinatura guardada; nula enquanto pendente.</summary>
        public ContratoAssinaturaVO? Assinatura { get; set; }
    }

    /// <summary>Assinatura coletada: nome, aceites, imagem, data e o funcionario que atendeu.</summary>
    public sealed class ContratoAssinaturaVO
    {
        public string NomeAssinante { get; set; } = string.Empty;

        public bool AceiteCondicoesGerais { get; set; }

        /// <summary>Nulo quando a clausula de politica de combustivel nao consta do documento.</summary>
        public bool? AceitePoliticaCombustivel { get; set; }

        public string AssinaturaPngBase64 { get; set; } = string.Empty;

        /// <summary>Data e hora da assinatura, formatadas por StaticMethods (dd/MM/yyyy HH:mm).</summary>
        public string AssinadoEmStr { get; set; } = string.Empty;

        public int IdFuncionarioColetor { get; set; }

        public string NomeFuncionarioColetor { get; set; } = string.Empty;
    }
}
