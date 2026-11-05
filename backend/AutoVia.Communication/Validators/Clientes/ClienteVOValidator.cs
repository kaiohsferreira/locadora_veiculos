using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Clientes
{
    /// <summary>
    /// Validador FluentValidation do VO de cadastro do cliente (POST publico). Cada
    /// campo para na primeira regra que falha. As mensagens vem de ConstantsMessage e
    /// os limites de ConstantsValues. CPF, telefone, CNH e CEP aceitam mascara: a
    /// contagem de digitos e feita sobre StaticMethods.OnlyDigits. Textos opcionais
    /// sao medidos depois do Trim, como o servico grava.
    /// </summary>
    public class ClienteVOValidator : AbstractValidator<ClienteVO>
    {
        public ClienteVOValidator()
        {
            RuleFor(x => x.Nome)
                .Cascade(CascadeMode.Stop)
                .Must(nome => !string.IsNullOrWhiteSpace(nome))
                .WithMessage(ConstantsMessage.ErroClienteNomeObrigatorio)
                .Must(nome => TamanhoAteAposTrim(nome, ConstantsValues.TamanhoMaximoNomeCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloNomeCliente, ConstantsValues.TamanhoMaximoNomeCliente));

            RuleFor(x => x.Cpf)
                .Cascade(CascadeMode.Stop)
                .Must(cpf => !string.IsNullOrWhiteSpace(cpf))
                .WithMessage(ConstantsMessage.ErroClienteCpfObrigatorio)
                .Must(cpf => StaticMethods.IsCpfValido(cpf))
                .WithMessage(ConstantsMessage.ErroClienteCpfInvalido);

            RuleFor(x => x.DataNascimento)
                .Cascade(CascadeMode.Stop)
                .Must(data => data != default(DateOnly))
                .WithMessage(ConstantsMessage.ErroClienteDataNascimentoObrigatoria)
                .Must(data => data <= HojeCliente())
                .WithMessage(ConstantsMessage.ErroClienteDataNascimentoFutura)
                .Must(data => data <= HojeCliente().AddYears(-ConstantsValues.IdadeMinimaCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteMenorDeIdade, ConstantsValues.IdadeMinimaCliente));

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage(ConstantsMessage.ErroClienteEmailObrigatorio)
                .Must(email => TamanhoAteAposTrim(email, ConstantsValues.TamanhoMaximoEmailCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloEmailCliente, ConstantsValues.TamanhoMaximoEmailCliente))
                .EmailAddress()
                .WithMessage(ConstantsMessage.ErroClienteEmailInvalido);

            RuleFor(x => x.Telefone)
                .Cascade(CascadeMode.Stop)
                .Must(telefone => !string.IsNullOrWhiteSpace(telefone))
                .WithMessage(ConstantsMessage.ErroClienteTelefoneObrigatorio)
                .Must(telefone => QuantidadeDeDigitosEntre(telefone, ConstantsValues.TamanhoMinimoTelefoneCliente, ConstantsValues.TamanhoMaximoTelefoneCliente))
                .WithMessage(ConstantsMessage.ErroClienteTelefoneInvalido);

            RuleFor(x => x.CnhNumero)
                .Cascade(CascadeMode.Stop)
                .Must(numero => !string.IsNullOrWhiteSpace(numero))
                .WithMessage(ConstantsMessage.ErroClienteCnhNumeroObrigatorio)
                .Must(numero => QuantidadeDeDigitosEntre(numero, ConstantsValues.TamanhoCnhNumero, ConstantsValues.TamanhoCnhNumero))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCnhNumeroInvalido, ConstantsValues.TamanhoCnhNumero));

            RuleFor(x => x.CnhCategoria)
                .Must(categoria => categoria.HasValue && Enum.IsDefined<CategoriaCnh>(categoria.Value))
                .WithMessage(ConstantsMessage.ErroClienteCnhCategoriaInvalida);

            RuleFor(x => x.CnhValidade)
                .Cascade(CascadeMode.Stop)
                .Must(validade => validade != default(DateOnly))
                .WithMessage(ConstantsMessage.ErroClienteCnhValidadeObrigatoria)
                .Must(validade => validade >= HojeCliente())
                .WithMessage(ConstantsMessage.ErroClienteCnhVencida);

            RuleFor(x => x.Logradouro)
                .Must(logradouro => TamanhoAteAposTrim(logradouro, ConstantsValues.TamanhoMaximoLogradouroCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloLogradouroCliente, ConstantsValues.TamanhoMaximoLogradouroCliente));

            RuleFor(x => x.Numero)
                .Must(numero => TamanhoAteAposTrim(numero, ConstantsValues.TamanhoMaximoNumeroEnderecoCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloNumeroEnderecoCliente, ConstantsValues.TamanhoMaximoNumeroEnderecoCliente));

            RuleFor(x => x.Complemento)
                .Must(complemento => TamanhoAteAposTrim(complemento, ConstantsValues.TamanhoMaximoComplementoCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloComplementoCliente, ConstantsValues.TamanhoMaximoComplementoCliente));

            RuleFor(x => x.Bairro)
                .Must(bairro => TamanhoAteAposTrim(bairro, ConstantsValues.TamanhoMaximoBairroCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloBairroCliente, ConstantsValues.TamanhoMaximoBairroCliente));

            RuleFor(x => x.Cidade)
                .Must(cidade => TamanhoAteAposTrim(cidade, ConstantsValues.TamanhoMaximoCidadeCliente))
                .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloCidadeCliente, ConstantsValues.TamanhoMaximoCidadeCliente));

            RuleFor(x => x.Uf)
                .Must(uf => UfValida(uf))
                .WithMessage(ConstantsMessage.ErroClienteUfInvalida);

            RuleFor(x => x.Cep)
                .Must(cep => CepValido(cep))
                .WithMessage(ConstantsMessage.ErroClienteCepInvalido);

            RuleFor(x => x.Senha)
                .Cascade(CascadeMode.Stop)
                .Must(senha => !string.IsNullOrWhiteSpace(senha))
                .WithMessage(ConstantsMessage.ErroClienteSenhaObrigatoria)
                .Must(senha => senha.Length >= ConstantsValues.TamanhoMinimoSenhaCadastro && senha.Length <= ConstantsValues.TamanhoMaximoSenhaCadastro)
                .WithMessage(string.Format(ConstantsMessage.ErroClienteSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro))
                .Must(senha => senha.Any(caractere => char.IsLetter(caractere)) && senha.Any(caractere => char.IsDigit(caractere)))
                .WithMessage(ConstantsMessage.ErroClienteSenhaFraca);
        }

        /// <summary>Data de hoje no fuso local (DateTime.Now), lida no momento da validacao.</summary>
        private static DateOnly HojeCliente()
        {
            DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);

            return hoje;
        }

        /// <summary>Texto opcional ou obrigatorio medido sem as bordas: nulo passa, senao o tamanho do Trim nao pode passar do maximo.</summary>
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

        /// <summary>UF opcional: vazia ou nula passa; senao sao exatamente 2 letras.</summary>
        private static bool UfValida(string? uf)
        {
            if (string.IsNullOrWhiteSpace(uf))
            {
                return true;
            }

            string sigla = uf.Trim();

            return sigla.Length == ConstantsValues.TamanhoUfCliente && sigla.All(caractere => char.IsLetter(caractere));
        }

        /// <summary>CEP opcional: vazio ou nulo passa; senao sao exatamente 8 digitos apos remover a mascara.</summary>
        private static bool CepValido(string? cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
            {
                return true;
            }

            return QuantidadeDeDigitosEntre(cep, ConstantsValues.TamanhoCepCliente, ConstantsValues.TamanhoCepCliente);
        }
    }
}
