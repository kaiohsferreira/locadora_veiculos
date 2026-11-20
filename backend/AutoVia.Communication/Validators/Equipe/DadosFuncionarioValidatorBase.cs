using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Linq.Expressions;

namespace AutoVia.Communication.Validators.Equipe
{
    /// <summary>
    /// Regras comuns dos dados do funcionario (nome, matricula, e-mail, data de admissao e cargo), compartilhadas pelo
    /// cadastro e pela edicao. Cada campo para na primeira regra que falha. Textos medidos depois do Trim, como o servico
    /// grava. As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public abstract class DadosFuncionarioValidatorBase<TVO> : AbstractValidator<TVO> where TVO : class
    {
        // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400 (ErroCampoInvalido).
        private const char CaractereNulo = '\0';

        /// <summary>Validador de e-mail do proprio FluentValidation, aplicado ao texto ja aparado.</summary>
        private static readonly InlineValidator<string> ValidadorEmail = CriarValidadorEmail();

        /// <summary>Nome obrigatorio, sem so espacos, ate o limite depois do Trim.</summary>
        protected void RegrasDeNome(Expression<Func<TVO, string>> campo)
        {
            RuleFor(campo)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroEquipeNomeObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoNomeFuncionario))
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, ConstantsValues.RotuloNomeFuncionario, ConstantsValues.TamanhoMaximoNomeFuncionario));

            RuleFor(campo)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeDoCampo(campo)));
        }

        /// <summary>Matricula obrigatoria, sem so espacos, ate o limite depois do Trim.</summary>
        protected void RegrasDeMatricula(Expression<Func<TVO, string>> campo)
        {
            RuleFor(campo)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroEquipeMatriculaObrigatoria)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoMatriculaFuncionario))
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, ConstantsValues.RotuloMatriculaFuncionario, ConstantsValues.TamanhoMaximoMatriculaFuncionario));

            RuleFor(campo)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeDoCampo(campo)));
        }

        /// <summary>E-mail obrigatorio, ate o limite e em formato valido depois do Trim.</summary>
        protected void RegrasDeEmail(Expression<Func<TVO, string>> campo)
        {
            RuleFor(campo)
                .Cascade(CascadeMode.Stop)
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage(ConstantsMessage.ErroEquipeEmailObrigatorio)
                .Must(email => TamanhoAteAposTrim(email, ConstantsValues.TamanhoMaximoEmailFuncionario))
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, ConstantsValues.RotuloEmailFuncionario, ConstantsValues.TamanhoMaximoEmailFuncionario))
                .Must(email => EmailValidoAposTrim(email))
                .WithMessage(ConstantsMessage.ErroEquipeEmailInvalido);

            RuleFor(campo)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeDoCampo(campo)));
        }

        /// <summary>Data de admissao obrigatoria (diferente do valor padrao) e nao posterior a hoje.</summary>
        protected void RegrasDeDataAdmissao(Expression<Func<TVO, DateOnly>> campo)
        {
            RuleFor(campo)
                .Cascade(CascadeMode.Stop)
                .Must(data => data != default(DateOnly))
                .WithMessage(ConstantsMessage.ErroEquipeDataAdmissaoObrigatoria)
                .Must(data => data <= DateOnly.FromDateTime(DateTime.Now))
                .WithMessage(ConstantsMessage.ErroEquipeDataAdmissaoFutura);
        }

        /// <summary>Cargo obrigatorio e um dos valores do enum.</summary>
        protected void RegrasDeCargo(Expression<Func<TVO, CargoFuncionario?>> campo)
        {
            RuleFor(campo)
                .Cascade(CascadeMode.Stop)
                .Must(cargo => cargo.HasValue)
                .WithMessage(ConstantsMessage.ErroEquipeCargoObrigatorio)
                .Must(cargo => cargo.HasValue && Enum.IsDefined(cargo.Value))
                .WithMessage(ConstantsMessage.ErroRequisicaoInvalida);
        }

        /// <summary>Nome do campo (como a propriedade do VO, em PascalCase) a partir da expressao; vai na mensagem ErroCampoInvalido.</summary>
        private static string NomeDoCampo<TCampo>(Expression<Func<TVO, TCampo>> campo)
        {
            MemberExpression membro = (MemberExpression)campo.Body;
            string nome = membro.Member.Name;

            return nome;
        }

        /// <summary>Monta o validador de e-mail uma vez, sobre o proprio texto.</summary>
        private static InlineValidator<string> CriarValidadorEmail()
        {
            InlineValidator<string> validador = new InlineValidator<string>();
            validador.RuleFor(texto => texto).EmailAddress();

            return validador;
        }

        /// <summary>E-mail valido depois de remover as bordas. Nulo ou so espacos nao chega aqui (a regra de obrigatorio para antes).</summary>
        private static bool EmailValidoAposTrim(string? email)
        {
            string aparado = email == null ? string.Empty : email.Trim();
            ValidationResult resultado = ValidadorEmail.Validate(aparado);

            return resultado.IsValid;
        }

        /// <summary>Texto medido sem as bordas: nulo passa; senao o tamanho do Trim nao pode passar do maximo.</summary>
        private static bool TamanhoAteAposTrim(string? valor, int tamanhoMaximo)
        {
            if (valor == null)
            {
                return true;
            }

            return valor.Trim().Length <= tamanhoMaximo;
        }
    }
}
