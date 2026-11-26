using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.Devolucao
{
    /// <summary>
    /// Resposta do encerramento (devolucao-encerramento v4, ReturnEncerramentoLocacaoVO). Igual na primeira chamada e na
    /// repeticao. Nunca traz id_transacao, token, final do cartao, e-mail ou CPF.
    /// </summary>
    public sealed class ReturnEncerramentoLocacaoVO
    {
        public ReturnLocacaoEncerradaVO Locacao { get; set; } = new ReturnLocacaoEncerradaVO();

        public ReturnVeiculoEncerradoVO Veiculo { get; set; } = new ReturnVeiculoEncerradoVO();

        public ReturnCobrancasEncerramentoVO Cobrancas { get; set; } = new ReturnCobrancasEncerramentoVO();

        public ReturnCaucaoEncerramentoVO Caucao { get; set; } = new ReturnCaucaoEncerramentoVO();

        /// <summary>Textos de ConstantsMessage ja aprovados; vazia na repeticao.</summary>
        public List<string> Avisos { get; set; } = new List<string>();
    }

    /// <summary>Locacao depois do encerramento.</summary>
    public sealed class ReturnLocacaoEncerradaVO
    {
        public int IdLocacao { get; set; }

        /// <summary>Texto do status: FINALIZADA.</summary>
        public string Status { get; set; } = string.Empty;

        public int KmSaida { get; set; }

        public int KmChegada { get; set; }

        public string DataDevolucaoEfetiva { get; set; } = string.Empty;

        public string DataDevolucaoEfetivaStr { get; set; } = string.Empty;

        public int DiariasPrevistas { get; set; }

        public decimal ValorDiaria { get; set; }

        public string ValorDiariaStr { get; set; } = string.Empty;

        public decimal ValorTotal { get; set; }

        public string ValorTotalStr { get; set; } = string.Empty;
    }

    /// <summary>Veiculo depois do encerramento.</summary>
    public sealed class ReturnVeiculoEncerradoVO
    {
        public int IdVeiculo { get; set; }

        /// <summary>Texto do status: DISPONIVEL ou RESERVADO.</summary>
        public string Status { get; set; } = string.Empty;

        public int KmAtual { get; set; }
    }

    /// <summary>Cobrancas do encerramento: extras da apuracao e diaria lancada.</summary>
    public sealed class ReturnCobrancasEncerramentoVO
    {
        public decimal TotalExtras { get; set; }

        public string TotalExtrasStr { get; set; } = string.Empty;

        public decimal TotalDiarias { get; set; }

        public string TotalDiariasStr { get; set; } = string.Empty;

        public int? IdPagamentoCobrancaExtra { get; set; }

        public int? IdPagamentoDiaria { get; set; }
    }

    /// <summary>Caucao depois do encerramento.</summary>
    public sealed class ReturnCaucaoEncerramentoVO
    {
        /// <summary>Texto de AcaoCaucaoDevolucao.</summary>
        public string Acao { get; set; } = string.Empty;

        public decimal ValorReservado { get; set; }

        public string ValorReservadoStr { get; set; } = string.Empty;

        public decimal ValorRetido { get; set; }

        public string ValorRetidoStr { get; set; } = string.Empty;

        public decimal ValorLiberado { get; set; }

        public string ValorLiberadoStr { get; set; } = string.Empty;

        /// <summary>Status gravado do pagamento CAUCAO: PAGO, CANCELADO ou PENDENTE; nulo sem caucao.</summary>
        public string? StatusPagamento { get; set; }

        public bool TratarAMao { get; set; }
    }
}
