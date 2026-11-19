using AutoVia.Communication.Validators.Pagamento;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Validadores das entradas de cobranca (400 antes do servico): origem unica, tipo pelo nome, caucao recusada pela rota,
    /// tipo x origem, valor (maior que zero e NUMERIC(12,2)), vencimento, forma da baixa e os filtros das listagens.
    /// </summary>
    public class PagamentoValidatorsTests
    {
        private static LancarPagamentoVO LancamentoValido()
        {
            return new LancarPagamentoVO
            {
                IdLocacao = 10,
                Tipo = "DIARIA",
                Valor = 395.60m,
                DataVencimento = new DateOnly(2026, 11, 25)
            };
        }

        private static bool Contem(ValidationResult resultado, string mensagem)
        {
            return resultado.Errors.Any(erro => erro.ErrorMessage == mensagem);
        }

        [Fact]
        public void Lancar_ValidoPorLocacaoPassa()
        {
            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(LancamentoValido());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Lancar_ValidoPorVendaPassa()
        {
            LancarPagamentoVO entrada = new LancarPagamentoVO { IdVenda = 4, Tipo = "VENDA", Valor = 100m, DataVencimento = new DateOnly(2026, 11, 25) };

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Lancar_OrigemDupla_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.IdVenda = 4;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.False(resultado.IsValid);
            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoOrigemDupla));
        }

        [Fact]
        public void Lancar_OrigemAusente_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.IdLocacao = null;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.False(resultado.IsValid);
            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoOrigemObrigatoria));
        }

        [Fact]
        public void Lancar_OrigemComIdNaoPositivo_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.IdLocacao = 0;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoOrigemObrigatoria));
        }

        [Fact]
        public void Lancar_CaucaoPelaRota_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Tipo = "CAUCAO";

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.False(resultado.IsValid);
            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoCaucaoPorRota));
        }

        [Fact]
        public void Lancar_TipoAusenteOuEmBranco_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Tipo = "   ";

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoTipoObrigatorio));
        }

        [Theory]
        [InlineData("diaria")]
        [InlineData("1")]
        [InlineData("OUTRO")]
        public void Lancar_TipoForaDaListaOuMinusculo_Recusa(string tipo)
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Tipo = tipo;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoTipoInvalido));
        }

        [Fact]
        public void Lancar_TipoComEspacosNasPontas_Aceita()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Tipo = "  DIARIA  ";

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Lancar_DiariaComVenda_Recusa()
        {
            LancarPagamentoVO entrada = new LancarPagamentoVO { IdVenda = 4, Tipo = "DIARIA", Valor = 100m, DataVencimento = new DateOnly(2026, 11, 25) };

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoTipoExigeLocacao));
        }

        [Fact]
        public void Lancar_CobrancaExtraComVenda_Recusa()
        {
            LancarPagamentoVO entrada = new LancarPagamentoVO { IdVenda = 4, Tipo = "COBRANCA_EXTRA", Valor = 100m, DataVencimento = new DateOnly(2026, 11, 25) };

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoTipoExigeLocacao));
        }

        [Fact]
        public void Lancar_VendaComLocacao_Recusa()
        {
            LancarPagamentoVO entrada = new LancarPagamentoVO { IdLocacao = 10, Tipo = "VENDA", Valor = 100m, DataVencimento = new DateOnly(2026, 11, 25) };

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoTipoExigeVenda));
        }

        [Fact]
        public void Lancar_VendaComVenda_Aceita()
        {
            LancarPagamentoVO entrada = new LancarPagamentoVO { IdVenda = 4, Tipo = "VENDA", Valor = 100m, DataVencimento = new DateOnly(2026, 11, 25) };

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Lancar_ValorZeroOuNegativo_Recusa(decimal valor)
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Valor = valor;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoValorInvalido));
        }

        [Fact]
        public void Lancar_ValorComTresCasas_RecusaComPrecisao()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Valor = 1.001m;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, string.Format(ConstantsMessage.ErroPagamentoValorPrecisao, ConstantsValues.DigitosInteirosValorPagamento, ConstantsValues.EscalaValorPagamento)));
        }

        [Fact]
        public void Lancar_ValorComOnzeDigitosInteiros_RecusaComPrecisao()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Valor = 10000000000m;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, string.Format(ConstantsMessage.ErroPagamentoValorPrecisao, ConstantsValues.DigitosInteirosValorPagamento, ConstantsValues.EscalaValorPagamento)));
        }

        [Fact]
        public void Lancar_ValorNoLimiteDeDezDigitosEDuasCasas_Aceita()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.Valor = 9999999999.99m;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Lancar_VencimentoAusente_Recusa()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.DataVencimento = null;

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoVencimentoObrigatorio));
        }

        [Fact]
        public void Lancar_VencimentoPassadoEhAceito()
        {
            LancarPagamentoVO entrada = LancamentoValido();
            entrada.DataVencimento = new DateOnly(2020, 1, 1);

            ValidationResult resultado = new LancarPagamentoVOValidator().Validate(entrada);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("PIX")]
        [InlineData("CARTAO")]
        [InlineData("BOLETO")]
        [InlineData("DINHEIRO")]
        [InlineData("TRANSFERENCIA")]
        public void Baixa_FormasDaListaFechadaPassam(string forma)
        {
            ValidationResult resultado = new BaixarPagamentoVOValidator().Validate(new BaixarPagamentoVO { Forma = forma });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Baixa_FormaComEspacosNasPontasPassa()
        {
            ValidationResult resultado = new BaixarPagamentoVOValidator().Validate(new BaixarPagamentoVO { Forma = " PIX " });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Baixa_FormaAusenteRecusa()
        {
            ValidationResult resultado = new BaixarPagamentoVOValidator().Validate(new BaixarPagamentoVO { Forma = null });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoFormaObrigatoria));
        }

        [Theory]
        [InlineData("pix")]
        [InlineData("CHEQUE")]
        [InlineData("1")]
        public void Baixa_FormaInvalidaOuMinuscula_Recusa(string forma)
        {
            ValidationResult resultado = new BaixarPagamentoVOValidator().Validate(new BaixarPagamentoVO { Forma = forma });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoFormaInvalida));
        }

        [Fact]
        public void FiltroPagamento_StatusExatoEIntervaloCertos_Passa()
        {
            FiltroPagamentoVO filtro = new FiltroPagamentoVO { Status = "ATRASADO", VencimentoDe = new DateOnly(2026, 1, 1), VencimentoAte = new DateOnly(2026, 2, 1) };

            ValidationResult resultado = new FiltroPagamentoVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("pago")]
        [InlineData("CAUCAO")]
        public void FiltroPagamento_StatusForaDaListaOuMinusculo_Recusa(string status)
        {
            ValidationResult resultado = new FiltroPagamentoVOValidator().Validate(new FiltroPagamentoVO { Status = status });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoStatusInvalido));
        }

        [Fact]
        public void FiltroPagamento_IntervaloInvertido_Recusa()
        {
            FiltroPagamentoVO filtro = new FiltroPagamentoVO { VencimentoDe = new DateOnly(2026, 3, 1), VencimentoAte = new DateOnly(2026, 2, 1) };

            ValidationResult resultado = new FiltroPagamentoVOValidator().Validate(filtro);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoVencimentoIntervalo));
        }

        [Fact]
        public void FiltroPagamento_PaginaZeroRecusaPelaPaginacao()
        {
            FiltroPagamentoVO filtro = new FiltroPagamentoVO { Pagina = 0 };

            ValidationResult resultado = new FiltroPagamentoVOValidator().Validate(filtro);

            Assert.True(Contem(resultado, ConstantsMessage.ErroPaginaInvalida));
        }

        [Theory]
        [InlineData("TODOS")]
        [InlineData("PENDENTES")]
        [InlineData("PAGOS")]
        [InlineData(null)]
        public void FiltroMeus_AbaDaListaOuAusenteEhAceita(string? aba)
        {
            ValidationResult resultado = new FiltroMeusPagamentosVOValidator().Validate(new FiltroMeusPagamentosVO { Aba = aba });

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("pendentes")]
        [InlineData("ATRASADOS")]
        public void FiltroMeus_AbaForaDaListaRecusa(string aba)
        {
            ValidationResult resultado = new FiltroMeusPagamentosVOValidator().Validate(new FiltroMeusPagamentosVO { Aba = aba });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoAbaInvalida));
        }

        [Theory]
        [InlineData("DOZE_MESES")]
        [InlineData("ANO_ATUAL")]
        [InlineData("SEIS_MESES")]
        [InlineData("TUDO")]
        [InlineData(null)]
        public void FiltroMeus_PeriodoDaListaOuAusenteEhAceito(string? periodo)
        {
            ValidationResult resultado = new FiltroMeusPagamentosVOValidator().Validate(new FiltroMeusPagamentosVO { Periodo = periodo });

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("ULTIMO_ANO")]
        [InlineData("tudo")]
        public void FiltroMeus_PeriodoForaDaListaRecusa(string periodo)
        {
            ValidationResult resultado = new FiltroMeusPagamentosVOValidator().Validate(new FiltroMeusPagamentosVO { Periodo = periodo });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoPeriodoInvalido));
        }

        [Fact]
        public void FiltroResumo_PeriodoForaDaListaRecusa()
        {
            ValidationResult resultado = new FiltroResumoMeusPagamentosVOValidator().Validate(new FiltroResumoMeusPagamentosVO { Periodo = "MES" });

            Assert.True(Contem(resultado, ConstantsMessage.ErroPagamentoPeriodoInvalido));
        }

        [Fact]
        public void FiltroResumo_PeriodoAusenteEValidoPassam()
        {
            Assert.True(new FiltroResumoMeusPagamentosVOValidator().Validate(new FiltroResumoMeusPagamentosVO()).IsValid);
            Assert.True(new FiltroResumoMeusPagamentosVOValidator().Validate(new FiltroResumoMeusPagamentosVO { Periodo = "ANO_ATUAL" }).IsValid);
        }
    }
}
