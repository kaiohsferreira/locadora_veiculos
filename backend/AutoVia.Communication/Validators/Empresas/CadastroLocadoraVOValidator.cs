using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System.Linq;

namespace AutoVia.Communication.Validators.Empresas
{
    /// <summary>
    /// Validador FluentValidation do cadastro publico de locadora (POST /api/empresas). Cada campo para na
    /// primeira regra que falha. Mensagens vem de ConstantsMessage e limites de ConstantsValues. CNPJ, CPF,
    /// CEP e telefone aceitam mascara (contam so os digitos). Textos sao medidos depois do Trim, como o
    /// servico grava. A senha da tela do responsavel exige letra MAIUSCULA e numero (regra propria, nao a do cliente).
    /// </summary>
    public class CadastroLocadoraVOValidator : AbstractValidator<CadastroLocadoraVO>
    {
        // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400 (ErroCampoInvalido).
        private const char CaractereNulo = '\0';
        private const string NomeCampoRazaoSocial = nameof(CadastroLocadoraVO.RazaoSocial);
        private const string NomeCampoNomeFantasia = nameof(CadastroLocadoraVO.NomeFantasia);
        private const string NomeCampoEmail = nameof(CadastroLocadoraVO.Email);
        private const string NomeCampoLogradouro = nameof(CadastroLocadoraVO.Logradouro);
        private const string NomeCampoNumero = nameof(CadastroLocadoraVO.Numero);
        private const string NomeCampoComplemento = nameof(CadastroLocadoraVO.Complemento);
        private const string NomeCampoBairro = nameof(CadastroLocadoraVO.Bairro);
        private const string NomeCampoCidade = nameof(CadastroLocadoraVO.Cidade);
        private const string NomeCampoResponsavelNome = nameof(CadastroLocadoraVO.ResponsavelNome);
        private const string NomeCampoResponsavelEmail = nameof(CadastroLocadoraVO.ResponsavelEmail);

        public CadastroLocadoraVOValidator()
        {
            RuleFor(x => x.RazaoSocial)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraRazaoSocialObrigatoria)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoRazaoSocialLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloRazaoSocialLocadora, ConstantsValues.TamanhoMaximoRazaoSocialLocadora));

            RuleFor(x => x.RazaoSocial)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoRazaoSocial));

            RuleFor(x => x.NomeFantasia)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraNomeFantasiaObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoNomeFantasiaLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloNomeFantasiaLocadora, ConstantsValues.TamanhoMaximoNomeFantasiaLocadora));

            RuleFor(x => x.NomeFantasia)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoNomeFantasia));

            RuleFor(x => x.Cnpj)
                .Cascade(CascadeMode.Stop)
                .Must(cnpj => !string.IsNullOrWhiteSpace(cnpj))
                .WithMessage(ConstantsMessage.ErroLocadoraCnpjObrigatorio)
                .Must(cnpj => StaticMethods.IsCnpjValido(cnpj))
                .WithMessage(ConstantsMessage.ErroLocadoraCnpjInvalido);

            RuleFor(x => x.Telefone)
                .Cascade(CascadeMode.Stop)
                .Must(telefone => !string.IsNullOrWhiteSpace(telefone))
                .WithMessage(ConstantsMessage.ErroLocadoraTelefoneObrigatorio)
                .Must(telefone => QuantidadeDeDigitosEntre(telefone, ConstantsValues.TamanhoMinimoTelefoneLocadora, ConstantsValues.TamanhoMaximoTelefoneLocadora))
                .WithMessage(ConstantsMessage.ErroLocadoraTelefoneInvalido);

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage(ConstantsMessage.ErroLocadoraEmailObrigatorio)
                .Must(email => TamanhoAteAposTrim(email, ConstantsValues.TamanhoMaximoEmailLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloEmailLocadora, ConstantsValues.TamanhoMaximoEmailLocadora))
                .Must(email => EmailValidoAposTrim(email))
                .WithMessage(ConstantsMessage.ErroLocadoraEmailInvalido);

            RuleFor(x => x.Email)
                .Must(email => email == null || !email.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoEmail));

            RuleFor(x => x.Cep)
                .Must(cep => QuantidadeDeDigitosEntre(cep, ConstantsValues.TamanhoCepLocadora, ConstantsValues.TamanhoCepLocadora))
                .WithMessage(ConstantsMessage.ErroLocadoraCepInvalido);

            RuleFor(x => x.Logradouro)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraLogradouroObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoLogradouroLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloLogradouroLocadora, ConstantsValues.TamanhoMaximoLogradouroLocadora));

            RuleFor(x => x.Logradouro)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoLogradouro));

            RuleFor(x => x.Numero)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraNumeroObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoNumeroEnderecoLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloNumeroLocadora, ConstantsValues.TamanhoMaximoNumeroEnderecoLocadora));

            RuleFor(x => x.Numero)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoNumero));

            RuleFor(x => x.Complemento)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoComplementoLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloComplementoLocadora, ConstantsValues.TamanhoMaximoComplementoLocadora));

            RuleFor(x => x.Complemento)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoComplemento));

            RuleFor(x => x.Bairro)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraBairroObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoBairroLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloBairroLocadora, ConstantsValues.TamanhoMaximoBairroLocadora));

            RuleFor(x => x.Bairro)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBairro));

            RuleFor(x => x.Cidade)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraCidadeObrigatoria)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoCidadeLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloCidadeLocadora, ConstantsValues.TamanhoMaximoCidadeLocadora));

            RuleFor(x => x.Cidade)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoCidade));

            RuleFor(x => x.Uf)
                .Must(uf => UfValida(uf))
                .WithMessage(ConstantsMessage.ErroLocadoraUfInvalida);

            RuleFor(x => x.ResponsavelNome)
                .Cascade(CascadeMode.Stop)
                .Must(texto => !string.IsNullOrWhiteSpace(texto))
                .WithMessage(ConstantsMessage.ErroLocadoraResponsavelNomeObrigatorio)
                .Must(texto => TamanhoAteAposTrim(texto, ConstantsValues.TamanhoMaximoResponsavelNome))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloResponsavelNomeLocadora, ConstantsValues.TamanhoMaximoResponsavelNome));

            RuleFor(x => x.ResponsavelNome)
                .Must(texto => texto == null || !texto.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoResponsavelNome));

            RuleFor(x => x.ResponsavelCpf)
                .Cascade(CascadeMode.Stop)
                .Must(cpf => !string.IsNullOrWhiteSpace(cpf))
                .WithMessage(ConstantsMessage.ErroLocadoraResponsavelCpfObrigatorio)
                .Must(cpf => StaticMethods.IsCpfValido(cpf))
                .WithMessage(ConstantsMessage.ErroLocadoraResponsavelCpfInvalido);

            RuleFor(x => x.ResponsavelEmail)
                .Cascade(CascadeMode.Stop)
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage(ConstantsMessage.ErroLocadoraResponsavelEmailObrigatorio)
                .Must(email => TamanhoAteAposTrim(email, ConstantsValues.TamanhoMaximoEmailLocadora))
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloResponsavelEmailLocadora, ConstantsValues.TamanhoMaximoEmailLocadora))
                .Must(email => EmailValidoAposTrim(email))
                .WithMessage(ConstantsMessage.ErroLocadoraResponsavelEmailInvalido);

            RuleFor(x => x.ResponsavelEmail)
                .Must(email => email == null || !email.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoResponsavelEmail));

            RuleFor(x => x.Senha)
                .Cascade(CascadeMode.Stop)
                .Must(senha => !string.IsNullOrWhiteSpace(senha))
                .WithMessage(ConstantsMessage.ErroLocadoraSenhaObrigatoria)
                .Must(senha => senha.Length >= ConstantsValues.TamanhoMinimoSenhaCadastro && senha.Length <= ConstantsValues.TamanhoMaximoSenhaCadastro)
                .WithMessage(string.Format(ConstantsMessage.ErroLocadoraSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro))
                .Must(senha => senha.Any(caractere => char.IsUpper(caractere)) && senha.Any(caractere => char.IsDigit(caractere)))
                .WithMessage(ConstantsMessage.ErroLocadoraSenhaFraca);

            RuleFor(x => x.AceiteTermos)
                .Must(aceite => aceite)
                .WithMessage(ConstantsMessage.ErroLocadoraAceiteTermosObrigatorio);
        }

        /// <summary>Validador de e-mail do proprio FluentValidation (regra EmailAddress), aplicado ao texto ja aparado.</summary>
        private static readonly InlineValidator<string> ValidadorEmail = CriarValidadorEmail();

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

        /// <summary>Conta so os digitos do valor (sem mascara) e confere a faixa informada.</summary>
        private static bool QuantidadeDeDigitosEntre(string? valor, int minimo, int maximo)
        {
            string digitos = StaticMethods.OnlyDigits(valor);

            return digitos.Length >= minimo && digitos.Length <= maximo;
        }

        /// <summary>UF obrigatoria: exatamente 2 letras depois do Trim.</summary>
        private static bool UfValida(string? uf)
        {
            if (string.IsNullOrWhiteSpace(uf))
            {
                return false;
            }

            string sigla = uf.Trim();

            return sigla.Length == ConstantsValues.TamanhoUfLocadora && sigla.All(caractere => char.IsLetter(caractere));
        }
    }
}
