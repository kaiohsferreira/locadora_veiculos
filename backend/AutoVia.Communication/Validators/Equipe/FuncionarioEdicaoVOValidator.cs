using AutoVia.Communication.ViewObjects.Equipe;

namespace AutoVia.Communication.Validators.Equipe
{
    /// <summary>
    /// Validador FluentValidation da edicao de funcionario (PUT /api/equipe/{id}): nome, matricula, e-mail, data de
    /// admissao e cargo. Sem CPF e sem senha, porque nenhum dos dois e editavel aqui.
    /// </summary>
    public class FuncionarioEdicaoVOValidator : DadosFuncionarioValidatorBase<FuncionarioEdicaoVO>
    {
        public FuncionarioEdicaoVOValidator()
        {
            RegrasDeNome(x => x.Nome);
            RegrasDeMatricula(x => x.Matricula);
            RegrasDeEmail(x => x.Email);
            RegrasDeDataAdmissao(x => x.DataAdmissao);
            RegrasDeCargo(x => x.Cargo);
        }
    }
}
