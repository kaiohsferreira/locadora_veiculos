using System;
using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.Devolucao
{
    /// <summary>Cobrancas extras lancadas na locacao (GET /api/locacoes/{idLocacao}/cobrancas), sem recalcular.</summary>
    public sealed class ReturnCobrancasLocacaoVO
    {
        public int IdLocacao { get; set; }

        public decimal Total { get; set; }

        public string TotalStr { get; set; } = string.Empty;

        public List<ReturnCobrancaLocacaoVO> Cobrancas { get; set; } = new List<ReturnCobrancaLocacaoVO>();
    }

    /// <summary>Uma cobranca extra da locacao.</summary>
    public sealed class ReturnCobrancaLocacaoVO
    {
        public int IdCobranca { get; set; }

        public int? IdApuracao { get; set; }

        /// <summary>Texto do TipoCobrancaExtra.</summary>
        public string Tipo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string ValorStr { get; set; } = string.Empty;

        public DateOnly DataLancamento { get; set; }

        public string DataLancamentoStr { get; set; } = string.Empty;
    }
}
