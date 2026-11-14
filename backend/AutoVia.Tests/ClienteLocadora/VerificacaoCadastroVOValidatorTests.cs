using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation.TestHelper;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Validador da verificacao do cadastro pela locadora (T-BE-06): a decisao e obrigatoria e so aceita APROVADO ou
    /// RECUSADO (PENDENTE e ausencia recusam), a recusa exige justificativa com texto e a justificativa tem teto de 500
    /// caracteres medidos depois do Trim. Aprovacao ignora a justificativa e nao a exige.
    /// </summary>
    public class VerificacaoCadastroVOValidatorTests
    {
        private static readonly VerificacaoCadastroVOValidator Validador = new VerificacaoCadastroVOValidator();

        [Fact]
        public void DecisaoAusente_EhRecusadaComAMensagemDeDecisao()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = null };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Decisao)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoDecisaoObrigatoria);
        }

        [Fact]
        public void DecisaoPendente_NaoEhDecisao()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.PENDENTE };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Decisao)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoDecisaoObrigatoria);
        }

        [Fact]
        public void RecusaSemJustificativa_Falha()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = null };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Justificativa)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoJustificativaObrigatoria);
        }

        [Fact]
        public void RecusaComJustificativaSoDeEspacos_Falha()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = "   " };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Justificativa)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoJustificativaObrigatoria);
        }

        [Fact]
        public void RecusaComJustificativaValida_Passa()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = "Documento ilegível" };

            Validador.TestValidate(vo).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Aprovacao_NaoExigeJustificativa()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.APROVADO, Justificativa = null };

            Validador.TestValidate(vo).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void JustificativaComQuinhentosCaracteres_Passa()
        {
            string justificativa = new string('a', ConstantsValues.TamanhoMaximoJustificativaRecusa);
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = justificativa };

            Validador.TestValidate(vo).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void JustificativaAcimaDeQuinhentosCaracteres_Falha()
        {
            string justificativa = new string('a', ConstantsValues.TamanhoMaximoJustificativaRecusa + 1);
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = justificativa };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Justificativa)
                .WithErrorMessage(string.Format(ConstantsMessage.ErroVerificacaoJustificativaTamanho, ConstantsValues.TamanhoMaximoJustificativaRecusa));
        }

        /// <summary>Caractere NUL na justificativa da recusa: 400 com a mensagem de constante (o PostgreSQL recusaria e daria 500).</summary>
        [Fact]
        public void RecusaComCaractereNul_FalhaComMensagemDeConstante()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = "Documento\0ruim" };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Justificativa)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoJustificativaInvalida);
        }

        /// <summary>NUL no meio ou no fim tambem e recusado, mesmo com a justificativa dentro do limite.</summary>
        [Fact]
        public void NulNoFinalDaJustificativa_TambemFalha()
        {
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = "motivo\u0000" };

            Validador.TestValidate(vo).ShouldHaveValidationErrorFor(x => x.Justificativa)
                .WithErrorMessage(ConstantsMessage.ErroVerificacaoJustificativaInvalida);
        }

        /// <summary>O teto mede depois do Trim: espacos nas bordas nao contam.</summary>
        [Fact]
        public void JustificativaComEspacosNasBordas_MedeOTamanhoAparado()
        {
            string justificativa = "  " + new string('a', ConstantsValues.TamanhoMaximoJustificativaRecusa) + "  ";
            VerificacaoCadastroVO vo = new VerificacaoCadastroVO { Decisao = StatusCadastroCliente.RECUSADO, Justificativa = justificativa };

            Validador.TestValidate(vo).ShouldNotHaveAnyValidationErrors();
        }
    }
}
