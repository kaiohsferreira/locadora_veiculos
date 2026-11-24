using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using System;
using System.Globalization;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Regras puras da caucao na retirada (contrato caucao-retirada v1.1), testadas direto: leitura de Pagamentos:ValorCaucao (so
    /// ponto decimal, cultura invariante, 0 a 99999999.99, ate 2 casas), referencia CAUCAO-RES-{idReserva}, descricao fixa, linha
    /// CAUCAO e a conferencia do retorno (valor e referencia), que reaproveita a regra do PIX.
    /// </summary>
    public class RegrasCaucaoRetiradaTests
    {
        private static readonly DateTime DevolucaoPrevistaDeTeste = new DateTime(2026, 11, 27, 18, 30, 0);
        private static readonly DateTime MomentoDeTeste = new DateTime(2026, 11, 24, 9, 30, 5);

        /// <summary>Textos validos e o valor esperado (ausente, vazio e so espacos valem 0: sem caucao).</summary>
        public static TheoryData<string?, decimal> ValoresValidos => new TheoryData<string?, decimal>
        {
            { "1200.00", 1200.00m },
            { "1200", 1200m },
            { "1200.5", 1200.50m },
            { "0", 0m },
            { "0.00", 0m },
            { "99999999.99", 99999999.99m },
            { " 1200.00 ", 1200.00m },
            { null, 0m },
            { "", 0m },
            { "   ", 0m }
        };

        [Theory]
        [MemberData(nameof(ValoresValidos))]
        public void TentarLerValorCaucao_Valido_DevolveOValor(string? texto, decimal esperado)
        {
            bool valido = RegrasCaucaoRetirada.TentarLerValorCaucao(texto, out decimal valor);

            Assert.True(valido);
            Assert.Equal(esperado, valor);
        }

        /// <summary>Virgula, separador de milhar, sinal, mais de 2 casas, mais de 8 inteiros, expoente, espaco no meio e ponto sem casas: invalidos (a API nao sobe).</summary>
        [Theory]
        [InlineData("1200,00")]
        [InlineData("1.200,00")]
        [InlineData("-1")]
        [InlineData("1200.005")]
        [InlineData("100000000.00")]
        [InlineData("1e3")]
        [InlineData("+1200")]
        [InlineData("1 200.00")]
        [InlineData("1200.")]
        [InlineData(".50")]
        [InlineData("abc")]
        [InlineData("1200.00.00")]
        [InlineData("１２００")]
        public void TentarLerValorCaucao_Invalido_DevolveFalsoEValorZero(string texto)
        {
            bool valido = RegrasCaucaoRetirada.TentarLerValorCaucao(texto, out decimal valor);

            Assert.False(valido);
            Assert.Equal(0m, valor);
        }

        /// <summary>A leitura nao depende da cultura da maquina: com pt-BR ativa, o ponto continua sendo o separador decimal e a virgula continua invalida.</summary>
        [Fact]
        public void TentarLerValorCaucao_ComCulturaPtBr_ContinuaSoComPonto()
        {
            CultureInfo culturaAnterior = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

                bool comPonto = RegrasCaucaoRetirada.TentarLerValorCaucao("1200.00", out decimal valorComPonto);
                bool comVirgula = RegrasCaucaoRetirada.TentarLerValorCaucao("1200,00", out decimal valorComVirgula);

                Assert.True(comPonto);
                Assert.Equal(1200.00m, valorComPonto);
                Assert.False(comVirgula);
                Assert.Equal(0m, valorComVirgula);
            }
            finally
            {
                CultureInfo.CurrentCulture = culturaAnterior;
            }
        }

        [Fact]
        public void ValorCaucaoMaximo_EhOMaiorValorDeNumeric10Com2Casas()
        {
            Assert.Equal(99999999.99m, ConstantsValues.ValorCaucaoMaximo);
            Assert.Equal("Pagamentos:ValorCaucao", ConstantsValues.ChaveValorCaucao);
        }

        [Fact]
        public void ReferenciaDaCaucao_PrefixoMaisIdDaReserva()
        {
            string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(42);

            Assert.Equal("CAUCAO-RES-42", referencia);
        }

        /// <summary>Com o maior id possivel a referencia tem 21 caracteres, cabe nos 64 do gateway e casa o padrao da referencia externa.</summary>
        [Fact]
        public void ReferenciaDaCaucao_ComOMaiorId_CabeNoLimiteECasaOPadraoDoGateway()
        {
            string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(int.MaxValue);

            Assert.Equal("CAUCAO-RES-2147483647", referencia);
            Assert.Equal(21, referencia.Length);
            Assert.True(referencia.Length <= ConstantsCaucao.ReferenciaTamanhoMaximo);
            Assert.Matches(ConstantsCaucao.PadraoReferenciaExterna, referencia);
        }

        [Fact]
        public void DescricaoDaCaucao_TextoFixoDentroDoLimiteDoGateway()
        {
            Assert.Equal("Caução da locação AutoVia", ConstantsValues.CaucaoDescricaoRetirada);
            Assert.False(string.IsNullOrWhiteSpace(ConstantsValues.CaucaoDescricaoRetirada));
            Assert.True(ConstantsValues.CaucaoDescricaoRetirada.Length <= ConstantsCaucao.DescricaoTamanhoMaximo);
        }

        /// <summary>Linha CAUCAO (contrato caucao-retirada, linha_caucao): o id_locacao vem pela navegacao na gravacao (aqui nulo).</summary>
        [Fact]
        public void CriarPagamentoCaucao_ColunasDaLinhaCaucao()
        {
            Pagamento pagamento = RegrasCaucaoRetirada.CriarPagamentoCaucao(1200.00m, "FAKE-CAU-0123456789ab", DevolucaoPrevistaDeTeste, MomentoDeTeste);

            Assert.Equal(TipoPagamento.CAUCAO, pagamento.Tipo);
            Assert.Null(pagamento.IdLocacao);
            Assert.Null(pagamento.IdVenda);
            Assert.Equal(1200.00m, pagamento.Valor);
            Assert.Equal("CARTAO", pagamento.Forma);
            Assert.Equal(StatusPagamento.PENDENTE, pagamento.Status);
            Assert.Equal("FAKE-CAU-0123456789ab", pagamento.IdTransacao);
            Assert.Equal(new DateOnly(2026, 11, 27), pagamento.DataVencimento);
            Assert.Null(pagamento.DataPagamento);
            Assert.Null(pagamento.QrCodeBase64);
            Assert.Null(pagamento.PixCopiaECola);
            Assert.Null(pagamento.PixExpiraEm);
            Assert.Equal(MomentoDeTeste, pagamento.CreatedAt);
            Assert.Equal(MomentoDeTeste, pagamento.UpdatedAt);
            Assert.Null(pagamento.DisabledAt);
        }

        /// <summary>Conferencia do retorno (contrato caucao-retirada, fluxo passo 8): reaproveita RegrasPagamento.MotivoDivergenciaRetornoPix. Valor igual (mesmo com outra escala) e referencia igual nao divergem.</summary>
        [Fact]
        public void ConferenciaDoRetorno_ValorEReferenciaIguais_NaoDiverge()
        {
            string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(42);

            string? motivo = RegrasPagamento.MotivoDivergenciaRetornoPix(1200.00m, 1200m, referencia, "CAUCAO-RES-42");

            Assert.Null(motivo);
        }

        [Fact]
        public void ConferenciaDoRetorno_ValorDiferente_DivergePorValor()
        {
            string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(42);

            string? motivo = RegrasPagamento.MotivoDivergenciaRetornoPix(1200.00m, 1199.99m, referencia, referencia);

            Assert.Equal(ConstantsValues.MotivoDivergenciaValor, motivo);
        }

        /// <summary>Referencia de outra reserva, com outra caixa ou ausente: diverge pela referencia.</summary>
        [Theory]
        [InlineData("CAUCAO-RES-43")]
        [InlineData("caucao-res-42")]
        [InlineData(null)]
        public void ConferenciaDoRetorno_ReferenciaDiferente_DivergePorReferencia(string? referenciaDevolvida)
        {
            string referencia = RegrasCaucaoRetirada.ReferenciaDaCaucao(42);

            string? motivo = RegrasPagamento.MotivoDivergenciaRetornoPix(1200.00m, 1200.00m, referencia, referenciaDevolvida);

            Assert.Equal(ConstantsValues.MotivoDivergenciaReferencia, motivo);
        }

        /// <summary>Decisao 4: sem caucao exigida, qualquer cargo abre, e tambem sem cargo nem locadora no contexto (RF-17 como hoje).</summary>
        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public void CargoPodeAbrirComCaucao_SemCaucaoExigida_TodosOsCargos(CargoFuncionario cargo)
        {
            Assert.True(RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(false, 1, cargo));
        }

        [Fact]
        public void CargoPodeAbrirComCaucao_SemCaucaoExigida_SemCargoNemLocadora()
        {
            Assert.True(RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(false, 0, null));
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public void CargoPodeAbrirComCaucao_ComCaucaoExigida_AtendenteEAdministradorDaLocadora(CargoFuncionario cargo)
        {
            Assert.True(RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(true, 1, cargo));
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public void CargoPodeAbrirComCaucao_ComCaucaoExigida_OutrosCargosNao(CargoFuncionario cargo)
        {
            Assert.False(RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(true, 1, cargo));
        }

        /// <summary>Com caucao exigida, sem locadora no contexto (0) ou sem cargo, nega mesmo para ATENDENTE e ADMINISTRADOR.</summary>
        [Theory]
        [InlineData(0, CargoFuncionario.ATENDENTE)]
        [InlineData(0, CargoFuncionario.ADMINISTRADOR)]
        [InlineData(1, null)]
        public void CargoPodeAbrirComCaucao_ComCaucaoExigida_SemLocadoraOuSemCargo_Nega(int idEmpresa, CargoFuncionario? cargo)
        {
            Assert.False(RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(true, idEmpresa, cargo));
        }
    }
}
