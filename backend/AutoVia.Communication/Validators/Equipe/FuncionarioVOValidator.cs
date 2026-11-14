using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Helpers;
using FluentValidation;
using System.Linq;

namespace AutoVia.Communication.Validators.Equipe
{
    /// <summary>
    /// Validador FluentValidation do cadastro de funcionario (POST /api/equipe): dados comuns, CPF com digito verificador
    /// e senha inicial (8 a 64 caracteres, com maiuscula e numero). A senha nao e aparada: vale como foi digitada.
    /// </summary>
    public class FuncionarioVOValidator : DadosFuncionarioValidatorBase<FuncionarioVO>
    {
        public FuncionarioVOValidator()
        {
            RegrasDeNome(x => x.Nome);

            RuleFor(x => x.Cpf)
                .Cascade(CascadeMode.Stop)
                .Must(cpf => !string.IsNullOrWhiteSpace(cpf))
                .WithMessage(ConstantsMessage.ErroEquipeCpfObrigatorio)
                .Must(cpf => StaticMethods.IsCpfValido(cpf))
                .WithMessage(ConstantsMessage.ErroEquipeCpfInvalido);

            RegrasDeMatricula(x => x.Matricula);
            RegrasDeEmail(x => x.Email);
            RegrasDeDataAdmissao(x => x.DataAdmissao);
            RegrasDeCargo(x => x.Cargo);

            RuleFor(x => x.SenhaInicial)
                .Cascade(CascadeMode.Stop)
                .Must(senha => !string.IsNullOrWhiteSpace(senha))
                .WithMessage(ConstantsMessage.ErroEquipeSenhaInicialObrigatoria)
                .Must(senha => senha.Length >= ConstantsValues.TamanhoMinimoSenhaCadastro && senha.Length <= ConstantsValues.TamanhoMaximoSenhaCadastro)
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeSenhaInicialTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro))
                .Must(senha => senha.Any(caractere => char.IsUpper(caractere)) && senha.Any(caractere => char.IsDigit(caractere)))
                .WithMessage(ConstantsMessage.ErroEquipeSenhaInicialFraca);
        }
    }
}
