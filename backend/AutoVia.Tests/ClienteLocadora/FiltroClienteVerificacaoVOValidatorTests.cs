using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation.TestHelper;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Validador do filtro da fila de verificacao (P2-BE-05): status so por nome exato (numero, minuscula e texto vazio
    /// recusados), busca ate 60 caracteres depois de Trim, busca so com espacos e aceita, caractere nulo e substituto UTF-16
    /// isolado recusados, par valido de caractere fora do plano basico aceito.
    /// </summary>
    public class FiltroClienteVerificacaoVOValidatorTests
    {
        private const string StatusPendente = "PENDENTE";
        private const string StatusMinusculo = "pendente";
        private const string StatusNumero = "1";
        private const string CaractereNuloNoMeio = "ma\0ria";
        private const string ParValido = "ma😀ria";

        private static readonly FiltroClienteVerificacaoVOValidator Validador = new FiltroClienteVerificacaoVOValidator();

        private static string Texto(int tamanho)
        {
            return new string('a', tamanho);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(StatusPendente)]
        [InlineData("APROVADO")]
        [InlineData("RECUSADO")]
        public void StatusValidoOuAusente_NaoEhRecusado(string? status)
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Status = status };

            Validador.TestValidate(filtro).ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData(StatusMinusculo)]
        [InlineData(StatusNumero)]
        [InlineData("")]
        [InlineData("TODOS")]
        public void StatusForaDoNomeExato_EhRecusadoComAMensagemDeStatus(string status)
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Status = status };

            Validador.TestValidate(filtro).ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage(ConstantsMessage.ErroClienteFilaStatusInvalido);
        }

        [Fact]
        public void BuscaCom60CaracteresDepoisDoTrim_EhAceita()
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = "  " + Texto(ConstantsValues.TamanhoMaximoBuscaClienteVerificacao) + "  " };

            Validador.TestValidate(filtro).ShouldNotHaveValidationErrorFor(x => x.Busca);
        }

        [Fact]
        public void BuscaCom61Caracteres_EhRecusadaComOLimite()
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = Texto(ConstantsValues.TamanhoMaximoBuscaClienteVerificacao + 1) };

            Validador.TestValidate(filtro).ShouldHaveValidationErrorFor(x => x.Busca)
                .WithErrorMessage(string.Format(ConstantsMessage.ErroClienteFilaBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaClienteVerificacao));
        }

        [Fact]
        public void BuscaSoComEspacos_EhAceitaComoSemBusca()
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = "         " };

            Validador.TestValidate(filtro).ShouldNotHaveValidationErrorFor(x => x.Busca);
        }

        [Fact]
        public void BuscaComCaractereNulo_EhRecusada()
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = CaractereNuloNoMeio };

            Validador.TestValidate(filtro).ShouldHaveValidationErrorFor(x => x.Busca)
                .WithErrorMessage(string.Format(ConstantsMessage.ErroCampoInvalido, "Busca"));
        }

        /// <summary>Substituto isolado montado em tempo de execucao: o xUnit troca substituto solto por U+FFFD ao serializar InlineData.</summary>
        [Fact]
        public void BuscaComSubstitutoAltoIsolado_EhRecusada()
        {
            string busca = "ma" + new string((char)0xD83D, 1) + "ria";
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = busca };

            Validador.TestValidate(filtro).ShouldHaveValidationErrorFor(x => x.Busca)
                .WithErrorMessage(string.Format(ConstantsMessage.ErroCampoInvalido, "Busca"));
        }

        [Fact]
        public void BuscaComSubstitutoBaixoIsolado_EhRecusada()
        {
            string busca = "ma" + new string((char)0xDE00, 1) + "ria";
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = busca };

            Validador.TestValidate(filtro).ShouldHaveValidationErrorFor(x => x.Busca)
                .WithErrorMessage(string.Format(ConstantsMessage.ErroCampoInvalido, "Busca"));
        }

        [Fact]
        public void BuscaComParValidoDeSubstitutos_EhAceita()
        {
            FiltroClienteVerificacaoVO filtro = new FiltroClienteVerificacaoVO { Busca = ParValido };

            Validador.TestValidate(filtro).ShouldNotHaveValidationErrorFor(x => x.Busca);
        }
    }
}
