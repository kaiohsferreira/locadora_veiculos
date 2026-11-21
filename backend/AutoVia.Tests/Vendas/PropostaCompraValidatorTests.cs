using AutoVia.Communication.Validators.Vendas;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Linq;
using Xunit;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes dos validadores da simulacao e da proposta de compra: parcelas de 1 a 60 (definicao da Maria Eduarda em
    /// 21/11/2026), entrada nao negativa com ate 2 casas e veiculo positivo na proposta.
    /// </summary>
    public class PropostaCompraValidatorTests
    {
        [Theory]
        [InlineData(1, true)]
        [InlineData(60, true)]
        [InlineData(0, false)]
        [InlineData(61, false)]
        [InlineData(-3, false)]
        public void Simulacao_ParcelasDe1A60(int numeroParcelas, bool valido)
        {
            SimulacaoParcelamentoVOValidator validador = new SimulacaoParcelamentoVOValidator();

            ValidationResult resultado = validador.Validate(new SimulacaoParcelamentoVO { ValorEntrada = 0m, NumeroParcelas = numeroParcelas });

            Assert.Equal(valido, resultado.IsValid);
            if (!valido)
            {
                ValidationFailure erro = Assert.Single(resultado.Errors);
                Assert.Equal(nameof(SimulacaoParcelamentoVO.NumeroParcelas), erro.PropertyName);
                Assert.Equal(string.Format(ConstantsMessage.ErroPropostaParcelasForaDoLimite, 1, 60), erro.ErrorMessage);
            }
        }

        [Fact]
        public void Simulacao_EntradaNegativa_Recusada()
        {
            SimulacaoParcelamentoVOValidator validador = new SimulacaoParcelamentoVOValidator();

            ValidationResult resultado = validador.Validate(new SimulacaoParcelamentoVO { ValorEntrada = -0.01m, NumeroParcelas = 12 });

            ValidationFailure erro = Assert.Single(resultado.Errors);
            Assert.Equal(ConstantsMessage.ErroPropostaEntradaNegativa, erro.ErrorMessage);
        }

        [Fact]
        public void Simulacao_EntradaComTresCasas_Recusada()
        {
            SimulacaoParcelamentoVOValidator validador = new SimulacaoParcelamentoVOValidator();

            ValidationResult resultado = validador.Validate(new SimulacaoParcelamentoVO { ValorEntrada = 1000.005m, NumeroParcelas = 12 });

            ValidationFailure erro = Assert.Single(resultado.Errors);
            Assert.Equal(ConstantsMessage.ErroPropostaEntradaPrecisao, erro.ErrorMessage);
        }

        [Fact]
        public void Proposta_ValidaComOsMesmosLimites()
        {
            PropostaCompraVOValidator validador = new PropostaCompraVOValidator();

            ValidationResult valida = validador.Validate(new PropostaCompraVO { IdVeiculo = 4, ValorEntrada = 20000m, NumeroParcelas = 60 });
            ValidationResult invalida = validador.Validate(new PropostaCompraVO { IdVeiculo = 0, ValorEntrada = -1m, NumeroParcelas = 61 });

            Assert.True(valida.IsValid);
            Assert.Equal(3, invalida.Errors.Count);
            Assert.Contains(invalida.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPropostaVeiculoInvalido);
            Assert.Contains(invalida.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPropostaEntradaNegativa);
            Assert.Contains(invalida.Errors.Select(erro => erro.PropertyName), nome => nome == nameof(PropostaCompraVO.NumeroParcelas));
        }
    }
}
