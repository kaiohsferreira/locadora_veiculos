using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.Validators.Empresas;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Reflection;

namespace AutoVia.Tests.Empresas
{
    /// <summary>
    /// Testes do CadastroLocadoraVOValidator (P10-BE-01): cada campo, seus limites e a regra de senha da tela do
    /// responsavel (maiuscula e numero). Cada campo para na primeira regra que falha. Tambem prova que o VO de
    /// entrada nao tem propriedades de empresa, cargo, flags ou datas (a empresa nunca vem do corpo).
    /// </summary>
    public class CadastroLocadoraVOValidatorTests
    {
        private const string CnpjValido = "11.222.333/0001-81";
        private const string CpfValido = "529.982.247-25";
        private const string SenhaValida = "SenhaForte1";

        private readonly CadastroLocadoraVOValidator _validador = new CadastroLocadoraVOValidator();

        [Fact]
        public void VOValido_PassaSemErros()
        {
            ValidationResult resultado = _validador.Validate(CriarVOValido());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void RazaoSocialVaziaOuTamanhoAcima_DaMensagemPropria()
        {
            CadastroLocadoraVO vazia = CriarVOValido();
            vazia.RazaoSocial = "   ";
            CadastroLocadoraVO longa = CriarVOValido();
            longa.RazaoSocial = new string('A', ConstantsValues.TamanhoMaximoRazaoSocialLocadora + 1);

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraRazaoSocialObrigatoria }, MensagensDoCampo(vazia, nameof(CadastroLocadoraVO.RazaoSocial)));
            Assert.Equal(
                new List<string> { string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloRazaoSocialLocadora, ConstantsValues.TamanhoMaximoRazaoSocialLocadora) },
                MensagensDoCampo(longa, nameof(CadastroLocadoraVO.RazaoSocial)));
        }

        [Fact]
        public void NomeFantasiaVazioOuAcimaDoLimite_DaMensagemPropria()
        {
            CadastroLocadoraVO vazio = CriarVOValido();
            vazio.NomeFantasia = string.Empty;
            CadastroLocadoraVO longo = CriarVOValido();
            longo.NomeFantasia = new string('B', ConstantsValues.TamanhoMaximoNomeFantasiaLocadora + 1);

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraNomeFantasiaObrigatorio }, MensagensDoCampo(vazio, nameof(CadastroLocadoraVO.NomeFantasia)));
            Assert.Single(MensagensDoCampo(longo, nameof(CadastroLocadoraVO.NomeFantasia)));
        }

        [Fact]
        public void CnpjVazio_PedeOCnpj()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Cnpj = string.Empty;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraCnpjObrigatorio }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Cnpj)));
        }

        [Theory]
        [InlineData("11222333000182")]
        [InlineData("00000000000000")]
        [InlineData("11111111111111")]
        [InlineData("1122233300018")]
        public void CnpjInvalido_ZeradoRepetidoOuComDigitoErrado_DaMensagemDeCnpj(string cnpj)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Cnpj = cnpj;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraCnpjInvalido }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Cnpj)));
        }

        [Theory]
        [InlineData("1134567890")]
        [InlineData("(11) 3456-7890")]
        [InlineData("11987654321")]
        public void Telefone_DezOuOnzeDigitos_Aceita(string telefone)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Telefone = telefone;

            Assert.Empty(MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Telefone)));
        }

        [Theory]
        [InlineData("123456789")]
        [InlineData("119876543210")]
        public void Telefone_ForaDaFaixaDeDigitos_Recusa(string telefone)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Telefone = telefone;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraTelefoneInvalido }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Telefone)));
        }

        [Fact]
        public void EmailDaEmpresa_InvalidoOuAcimaDe120_Recusa()
        {
            CadastroLocadoraVO invalido = CriarVOValido();
            invalido.Email = "nao-e-email";
            CadastroLocadoraVO longo = CriarVOValido();
            longo.Email = new string('a', 115) + "@x.com";

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraEmailInvalido }, MensagensDoCampo(invalido, nameof(CadastroLocadoraVO.Email)));
            Assert.Single(MensagensDoCampo(longo, nameof(CadastroLocadoraVO.Email)));
        }

        [Theory]
        [InlineData("0123456")]
        [InlineData("012345678")]
        [InlineData("01234-56")]
        public void Cep_ComOutroTamanhoQueOitoDigitos_Recusa(string cep)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Cep = cep;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraCepInvalido }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Cep)));
        }

        [Theory]
        [InlineData("S")]
        [InlineData("S1")]
        [InlineData("")]
        public void Uf_ComOutroTamanhoOuSemLetras_Recusa(string uf)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Uf = uf;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraUfInvalida }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Uf)));
        }

        [Fact]
        public void Uf_DuasLetrasMinusculas_Aceita()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Uf = "sp";

            Assert.Empty(MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Uf)));
        }

        [Fact]
        public void EnderecoObrigatorio_VazioPedeOCampo_E_AcimaDoLimitePedeTamanho()
        {
            CadastroLocadoraVO semLogradouro = CriarVOValido();
            semLogradouro.Logradouro = string.Empty;
            CadastroLocadoraVO numeroLongo = CriarVOValido();
            numeroLongo.Numero = new string('1', ConstantsValues.TamanhoMaximoNumeroEnderecoLocadora + 1);
            CadastroLocadoraVO semCidade = CriarVOValido();
            semCidade.Cidade = " ";

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraLogradouroObrigatorio }, MensagensDoCampo(semLogradouro, nameof(CadastroLocadoraVO.Logradouro)));
            Assert.Single(MensagensDoCampo(numeroLongo, nameof(CadastroLocadoraVO.Numero)));
            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraCidadeObrigatoria }, MensagensDoCampo(semCidade, nameof(CadastroLocadoraVO.Cidade)));
        }

        [Fact]
        public void Complemento_E_Opcional_MasTemLimite()
        {
            CadastroLocadoraVO nulo = CriarVOValido();
            nulo.Complemento = null;
            CadastroLocadoraVO longo = CriarVOValido();
            longo.Complemento = new string('c', ConstantsValues.TamanhoMaximoComplementoLocadora + 1);

            Assert.True(_validador.Validate(nulo).IsValid);
            Assert.Single(MensagensDoCampo(longo, nameof(CadastroLocadoraVO.Complemento)));
        }

        [Fact]
        public void ResponsavelNomeVazio_PedeONome()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.ResponsavelNome = string.Empty;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraResponsavelNomeObrigatorio }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.ResponsavelNome)));
        }

        [Theory]
        [InlineData("529.982.247-26")]
        [InlineData("111.111.111-11")]
        [InlineData("1234567890")]
        public void ResponsavelCpf_SemDigitoVerificador_Recusa(string cpf)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.ResponsavelCpf = cpf;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraResponsavelCpfInvalido }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.ResponsavelCpf)));
        }

        [Fact]
        public void ResponsavelEmail_InvalidoOuVazio_TemMensagemPropria()
        {
            CadastroLocadoraVO invalido = CriarVOValido();
            invalido.ResponsavelEmail = "ana@";
            CadastroLocadoraVO vazio = CriarVOValido();
            vazio.ResponsavelEmail = string.Empty;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraResponsavelEmailInvalido }, MensagensDoCampo(invalido, nameof(CadastroLocadoraVO.ResponsavelEmail)));
            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraResponsavelEmailObrigatorio }, MensagensDoCampo(vazio, nameof(CadastroLocadoraVO.ResponsavelEmail)));
        }

        [Fact]
        public void Senha_VaziaPedeASenha()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Senha = string.Empty;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraSenhaObrigatoria }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Senha)));
        }

        [Theory]
        [InlineData("Senha1a")]
        [InlineData("SenhaForte1234567890123456789012345678901234567890123456789012345")]
        public void Senha_ForaDaFaixaDe8A64_DaMensagemDeTamanho(string senha)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Senha = senha;

            string esperada = string.Format(ConstantsMessage.ErroLocadoraSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro);

            Assert.Equal(new List<string> { esperada }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Senha)));
        }

        [Theory]
        [InlineData("senhaforte1")]
        [InlineData("SENHAFORTE")]
        [InlineData("SenhaForte")]
        public void Senha_SemLetraMaiusculaOuSemNumero_DaMensagemDeForca(string senha)
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.Senha = senha;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraSenhaFraca }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.Senha)));
        }

        [Fact]
        public void AceiteTermos_Falso_Recusa()
        {
            CadastroLocadoraVO vo = CriarVOValido();
            vo.AceiteTermos = false;

            Assert.Equal(new List<string> { ConstantsMessage.ErroLocadoraAceiteTermosObrigatorio }, MensagensDoCampo(vo, nameof(CadastroLocadoraVO.AceiteTermos)));
        }

        [Fact]
        public void VODeEntrada_NaoTemPropriedadesDeEmpresaCargoFlagsOuDatas()
        {
            List<string> nomes = typeof(CadastroLocadoraVO)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .OrderBy(nome => nome, StringComparer.Ordinal)
                .ToList();

            List<string> esperadas = new List<string>
            {
                nameof(CadastroLocadoraVO.AceiteTermos),
                nameof(CadastroLocadoraVO.Bairro),
                nameof(CadastroLocadoraVO.Cep),
                nameof(CadastroLocadoraVO.Cidade),
                nameof(CadastroLocadoraVO.Cnpj),
                nameof(CadastroLocadoraVO.Complemento),
                nameof(CadastroLocadoraVO.Email),
                nameof(CadastroLocadoraVO.Logradouro),
                nameof(CadastroLocadoraVO.NomeFantasia),
                nameof(CadastroLocadoraVO.Numero),
                nameof(CadastroLocadoraVO.RazaoSocial),
                nameof(CadastroLocadoraVO.ResponsavelCpf),
                nameof(CadastroLocadoraVO.ResponsavelEmail),
                nameof(CadastroLocadoraVO.ResponsavelNome),
                nameof(CadastroLocadoraVO.Senha),
                nameof(CadastroLocadoraVO.Telefone),
                nameof(CadastroLocadoraVO.Uf)
            }
            .OrderBy(nome => nome, StringComparer.Ordinal)
            .ToList();

            Assert.Equal(esperadas, nomes);
        }

        /// <summary>Mensagens de erro de um campo (na ordem das regras), para comparar a primeira regra que falhou.</summary>
        private List<string> MensagensDoCampo(CadastroLocadoraVO vo, string campo)
        {
            ValidationResult resultado = _validador.Validate(vo);

            return resultado.Errors
                .Where(erro => erro.PropertyName == campo)
                .Select(erro => erro.ErrorMessage)
                .ToList();
        }

        private static CadastroLocadoraVO CriarVOValido()
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO
            {
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = CnpjValido,
                Telefone = "(11) 3456-7890",
                Email = "contato@rotasul.exemplo.com",
                Cep = "01234-567",
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = null,
                Bairro = "Centro",
                Cidade = "São Paulo",
                Uf = "SP",
                ResponsavelNome = "Ana Responsável",
                ResponsavelCpf = CpfValido,
                ResponsavelEmail = "ana@rotasul.exemplo.com",
                Senha = SenhaValida,
                AceiteTermos = true
            };

            return vo;
        }
    }
}
