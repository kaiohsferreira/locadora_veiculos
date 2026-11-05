using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Testes do ClienteVOValidator: CPF com digito verificador, idade minima, CNH (numero,
    /// categoria e validade), telefone, e-mail, endereco, CEP, UF e a regra da senha.
    /// Os CPFs de teste sao sinteticos e tem digito verificador valido (algoritmo da Receita).
    /// </summary>
    public class ClienteVOValidatorTests
    {
        private const string CpfValido = "123.456.789-09";
        private const string CpfValidoSemMascara = "98765432100";

        [Fact]
        public void VOValido_PassaNaValidacao()
        {
            ClienteVO vo = CriarVOValido();
            ClienteVOValidator validator = new ClienteVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void NomeVazio_FalhaComMensagemObrigatorio()
        {
            ClienteVO vo = CriarVOValido();
            vo.Nome = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteNomeObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void NomeSoComEspacos_FalhaComMensagemObrigatorio()
        {
            ClienteVO vo = CriarVOValido();
            vo.Nome = "     ";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteNomeObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void NomeNoLimite_PassaENomeAcimaDoLimite_FalhaComTamanho()
        {
            ClienteVO noLimite = CriarVOValido();
            noLimite.Nome = new string('a', ConstantsValues.TamanhoMaximoNomeCliente);
            ClienteVO acima = CriarVOValido();
            acima.Nome = new string('a', ConstantsValues.TamanhoMaximoNomeCliente + 1);
            string mensagem = string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloNomeCliente, ConstantsValues.TamanhoMaximoNomeCliente);

            Assert.True(new ClienteVOValidator().Validate(noLimite).IsValid);
            Assert.Contains(mensagem, ObterMensagens(new ClienteVOValidator().Validate(acima)));
        }

        [Fact]
        public void CpfVazio_FalhaComMensagemObrigatorio()
        {
            ClienteVO vo = CriarVOValido();
            vo.Cpf = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfComDigitoVerificadorErrado_FalhaComMensagemInvalido()
        {
            ClienteVO vo = CriarVOValido();
            vo.Cpf = "123.456.789-10";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfComDigitosIguais_FalhaComMensagemInvalido()
        {
            ClienteVO vo = CriarVOValido();
            vo.Cpf = "111.111.111-11";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfComMascaraOuSemMascara_PassaNaValidacao()
        {
            ClienteVO comMascara = CriarVOValido();
            comMascara.Cpf = CpfValido;
            ClienteVO semMascara = CriarVOValido();
            semMascara.Cpf = CpfValidoSemMascara;

            Assert.True(new ClienteVOValidator().Validate(comMascara).IsValid);
            Assert.True(new ClienteVOValidator().Validate(semMascara).IsValid);
        }

        [Fact]
        public void DataNascimentoNaoInformada_FalhaComMensagemObrigatoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.DataNascimento = default(DateOnly);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteDataNascimentoObrigatoria, ObterMensagens(resultado));
        }

        [Fact]
        public void DataNascimentoFutura_FalhaComMensagemFutura()
        {
            ClienteVO vo = CriarVOValido();
            vo.DataNascimento = Hoje().AddDays(1);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteDataNascimentoFutura, ObterMensagens(resultado));
        }

        [Fact]
        public void ClienteComDezesseteAnos_FalhaComMensagemMenorDeIdade()
        {
            ClienteVO vo = CriarVOValido();
            vo.DataNascimento = Hoje().AddYears(-ConstantsValues.IdadeMinimaCliente).AddDays(1);
            string mensagem = string.Format(ConstantsMessage.ErroClienteMenorDeIdade, ConstantsValues.IdadeMinimaCliente);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void ClienteComExatamenteDezoitoAnos_PassaNaValidacao()
        {
            ClienteVO vo = CriarVOValido();
            vo.DataNascimento = Hoje().AddYears(-ConstantsValues.IdadeMinimaCliente);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void EmailVazio_FalhaComMensagemObrigatorio()
        {
            ClienteVO vo = CriarVOValido();
            vo.Email = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteEmailObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void EmailSemArroba_FalhaComMensagemInvalido()
        {
            ClienteVO vo = CriarVOValido();
            vo.Email = "maria.exemplo.com";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteEmailInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void EmailAcimaDeCentoEVinteCaracteres_FalhaComMensagemTamanho()
        {
            ClienteVO vo = CriarVOValido();
            vo.Email = new string('a', ConstantsValues.TamanhoMaximoEmailCliente - 5) + "@x.com";
            string mensagem = string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloEmailCliente, ConstantsValues.TamanhoMaximoEmailCliente);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void TelefoneVazio_FalhaComMensagemObrigatorio()
        {
            ClienteVO vo = CriarVOValido();
            vo.Telefone = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteTelefoneObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void TelefoneComNoveDigitos_FalhaComMensagemInvalido()
        {
            ClienteVO vo = CriarVOValido();
            vo.Telefone = "(11) 9876-543";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteTelefoneInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void TelefoneComDezOuOnzeDigitos_PassaComOuSemMascara()
        {
            ClienteVO fixo = CriarVOValido();
            fixo.Telefone = "1133334444";
            ClienteVO celular = CriarVOValido();
            celular.Telefone = "(11) 98765-4321";

            Assert.True(new ClienteVOValidator().Validate(fixo).IsValid);
            Assert.True(new ClienteVOValidator().Validate(celular).IsValid);
        }

        [Fact]
        public void CnhComDezDigitos_FalhaComMensagemNumeroInvalido()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhNumero = "1234567890";
            string mensagem = string.Format(ConstantsMessage.ErroClienteCnhNumeroInvalido, ConstantsValues.TamanhoCnhNumero);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhVazia_FalhaComMensagemObrigatoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhNumero = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCnhNumeroObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhComOnzeDigitosEMascara_PassaNaValidacao()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhNumero = "123-456-789-01";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void CnhCategoriaForaDoEnum_FalhaComMensagemCategoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhCategoria = (CategoriaCnh)99;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCnhCategoriaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhCategoriaAusente_FalhaComMensagemCategoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhCategoria = null;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteCnhCategoriaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhValidadeNaoInformada_FalhaComMensagemObrigatoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhValidade = default(DateOnly);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCnhValidadeObrigatoria, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhVencida_FalhaComMensagemVencida()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhValidade = Hoje().AddDays(-1);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCnhVencida, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhValidaHoje_PassaNaValidacao()
        {
            ClienteVO vo = CriarVOValido();
            vo.CnhValidade = Hoje();

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void UfComUmaLetra_FalhaComMensagemUf()
        {
            ClienteVO vo = CriarVOValido();
            vo.Uf = "S";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteUfInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void UfComDigito_FalhaComMensagemUf()
        {
            ClienteVO vo = CriarVOValido();
            vo.Uf = "S1";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteUfInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void UfNulaOuVazia_PassaNaValidacao()
        {
            ClienteVO nula = CriarVOValido();
            nula.Uf = null;
            ClienteVO vazia = CriarVOValido();
            vazia.Uf = "  ";

            Assert.True(new ClienteVOValidator().Validate(nula).IsValid);
            Assert.True(new ClienteVOValidator().Validate(vazia).IsValid);
        }

        [Fact]
        public void CepComQuatroDigitos_FalhaComMensagemCep()
        {
            ClienteVO vo = CriarVOValido();
            vo.Cep = "1234";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCepInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CepComMascaraOuNulo_PassaNaValidacao()
        {
            ClienteVO comMascara = CriarVOValido();
            comMascara.Cep = "01234-567";
            ClienteVO nulo = CriarVOValido();
            nulo.Cep = null;

            Assert.True(new ClienteVOValidator().Validate(comMascara).IsValid);
            Assert.True(new ClienteVOValidator().Validate(nulo).IsValid);
        }

        [Fact]
        public void SenhaVazia_FalhaComMensagemObrigatoria()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = string.Empty;

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteSenhaObrigatoria, ObterMensagens(resultado));
        }

        [Fact]
        public void SenhaComSeteCaracteres_FalhaComMensagemTamanho()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = "abc1234";
            string mensagem = string.Format(ConstantsMessage.ErroClienteSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void SenhaComSessentaECincoCaracteres_FalhaComMensagemTamanho()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = new string('a', ConstantsValues.TamanhoMaximoSenhaCadastro - 1) + "1" + "a";
            string mensagem = string.Format(ConstantsMessage.ErroClienteSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void SenhaComSessentaECaracteres_PassaNaValidacao()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = string.Concat(Enumerable.Repeat("a1", ConstantsValues.TamanhoMaximoSenhaCadastro / 2));

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void SenhaSoComLetras_FalhaComMensagemFraca()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = "abcdefgh";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteSenhaFraca, ObterMensagens(resultado));
        }

        [Fact]
        public void SenhaSoComNumeros_FalhaComMensagemFraca()
        {
            ClienteVO vo = CriarVOValido();
            vo.Senha = "12345678";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteSenhaFraca, ObterMensagens(resultado));
        }

        [Fact]
        public void LogradouroAcimaDoLimite_FalhaComMensagemTamanho()
        {
            ClienteVO vo = CriarVOValido();
            vo.Logradouro = new string('r', ConstantsValues.TamanhoMaximoLogradouroCliente + 1);
            string mensagem = string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloLogradouroCliente, ConstantsValues.TamanhoMaximoLogradouroCliente);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void NumeroAcimaDoLimite_FalhaComMensagemTamanho()
        {
            ClienteVO vo = CriarVOValido();
            vo.Numero = new string('9', ConstantsValues.TamanhoMaximoNumeroEnderecoCliente + 1);
            string mensagem = string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloNumeroEnderecoCliente, ConstantsValues.TamanhoMaximoNumeroEnderecoCliente);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void ComplementoBairroECidadeAcimaDoLimite_FalhamComSuasMensagens()
        {
            ClienteVO vo = CriarVOValido();
            vo.Complemento = new string('c', ConstantsValues.TamanhoMaximoComplementoCliente + 1);
            vo.Bairro = new string('b', ConstantsValues.TamanhoMaximoBairroCliente + 1);
            vo.Cidade = new string('d', ConstantsValues.TamanhoMaximoCidadeCliente + 1);

            List<string> mensagens = ObterMensagens(new ClienteVOValidator().Validate(vo));

            Assert.Contains(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloComplementoCliente, ConstantsValues.TamanhoMaximoComplementoCliente), mensagens);
            Assert.Contains(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloBairroCliente, ConstantsValues.TamanhoMaximoBairroCliente), mensagens);
            Assert.Contains(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloCidadeCliente, ConstantsValues.TamanhoMaximoCidadeCliente), mensagens);
        }

        [Fact]
        public void TextoOpcionalMedidoDepoisDoTrim_PassaQuandoSoOsEspacosPassamDoLimite()
        {
            ClienteVO vo = CriarVOValido();
            vo.Logradouro = "  " + new string('r', ConstantsValues.TamanhoMaximoLogradouroCliente) + "  ";

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        /// <summary>VO com todos os campos validos, usado como base para cada teste alterar um campo.</summary>
        private static ClienteVO CriarVOValido()
        {
            ClienteVO vo = new ClienteVO
            {
                Nome = "Maria da Silva",
                Cpf = CpfValido,
                DataNascimento = new DateOnly(1990, 5, 20),
                Email = "maria.silva@exemplo.com",
                Telefone = "(11) 98765-4321",
                CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = Hoje().AddYears(3),
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = "Apto 2",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Uf = "SP",
                Cep = "01234-567",
                Senha = "senha1234"
            };

            return vo;
        }

        private static DateOnly Hoje()
        {
            DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);

            return hoje;
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();

            return mensagens;
        }
    }
}
