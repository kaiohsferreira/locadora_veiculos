using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.Validators.Empresas;
using AutoVia.Communication.Validators.Equipe;
using AutoVia.Communication.Validators.Frota;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Validators
{
    /// <summary>
    /// Caractere nulo (U+0000) em texto livre de entrada (item 5 do plano de pendencias, 20/11/2026): o PostgreSQL recusa o texto e
    /// o erro sairia como 500. Cada validador que grava ou pesquisa o texto recusa o nulo com 400, com ErroCampoInvalido e o nome
    /// do campo. Os testes falham antes da regra (o texto com nulo passa) e passam depois. Sem banco e sem servidor.
    /// </summary>
    public class TextoLivreCaractereNuloTests
    {
        private const string TextoComNulo = "Ana\0 Teste";
        private const string TextoSemNulo = "Ana Teste";
        private const string EmailComNulo = "ana.teste@exemplo.com\0";

        [Theory]
        [InlineData(nameof(ClienteVO.Nome))]
        [InlineData(nameof(ClienteVO.Email))]
        [InlineData(nameof(ClienteVO.Logradouro))]
        [InlineData(nameof(ClienteVO.Numero))]
        [InlineData(nameof(ClienteVO.Complemento))]
        [InlineData(nameof(ClienteVO.Bairro))]
        [InlineData(nameof(ClienteVO.Cidade))]
        public void ClienteVOValidator_TextoComNulo_DevolveErroDoCampo(string campo)
        {
            ClienteVO vo = new ClienteVO();
            DefinirTextoCliente(vo, campo, TextoComNulo);

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, campo));
        }

        [Theory]
        [InlineData(nameof(CadastroLocadoraVO.RazaoSocial))]
        [InlineData(nameof(CadastroLocadoraVO.NomeFantasia))]
        [InlineData(nameof(CadastroLocadoraVO.Email))]
        [InlineData(nameof(CadastroLocadoraVO.Logradouro))]
        [InlineData(nameof(CadastroLocadoraVO.Numero))]
        [InlineData(nameof(CadastroLocadoraVO.Complemento))]
        [InlineData(nameof(CadastroLocadoraVO.Bairro))]
        [InlineData(nameof(CadastroLocadoraVO.Cidade))]
        [InlineData(nameof(CadastroLocadoraVO.ResponsavelNome))]
        [InlineData(nameof(CadastroLocadoraVO.ResponsavelEmail))]
        public void CadastroLocadoraVOValidator_TextoComNulo_DevolveErroDoCampo(string campo)
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO();
            DefinirTextoLocadora(vo, campo, TextoComNulo);

            ValidationResult resultado = new CadastroLocadoraVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, campo));
        }

        [Theory]
        [InlineData(nameof(FuncionarioVO.Nome))]
        [InlineData(nameof(FuncionarioVO.Matricula))]
        [InlineData(nameof(FuncionarioVO.Email))]
        public void FuncionarioVOValidator_TextoComNulo_DevolveErroDoCampo(string campo)
        {
            FuncionarioVO vo = new FuncionarioVO();
            DefinirTextoFuncionario(vo, campo, TextoComNulo);

            ValidationResult resultado = new FuncionarioVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, campo));
        }

        [Theory]
        [InlineData(nameof(FuncionarioEdicaoVO.Nome))]
        [InlineData(nameof(FuncionarioEdicaoVO.Matricula))]
        [InlineData(nameof(FuncionarioEdicaoVO.Email))]
        public void FuncionarioEdicaoVOValidator_TextoComNulo_DevolveErroDoCampo(string campo)
        {
            FuncionarioEdicaoVO vo = new FuncionarioEdicaoVO();
            DefinirTextoEdicaoFuncionario(vo, campo, TextoComNulo);

            ValidationResult resultado = new FuncionarioEdicaoVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, campo));
        }

        [Theory]
        [InlineData(nameof(CategoriaVO.Nome))]
        [InlineData(nameof(CategoriaVO.Descricao))]
        public void CategoriaVOValidator_TextoComNulo_DevolveErroDoCampo(string campo)
        {
            CategoriaVO vo = new CategoriaVO();
            DefinirTextoCategoria(vo, campo, TextoComNulo);

            ValidationResult resultado = new CategoriaVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, campo));
        }

        [Fact]
        public void FiltroEquipeVOValidator_BuscaComNulo_DevolveErroDoCampo()
        {
            FiltroEquipeVO filtro = new FiltroEquipeVO { Busca = TextoComNulo };

            ValidationResult resultado = new FiltroEquipeVOValidator().Validate(filtro);

            Assert.True(TemErroDoCampo(resultado, nameof(FiltroEquipeVO.Busca)));
        }

        [Fact]
        public void VerificarCadastroVOValidator_EmailComNulo_DevolveErroDoValor()
        {
            VerificarCadastroVO vo = new VerificarCadastroVO { Campo = CampoVerificacaoCadastro.EMAIL, Valor = EmailComNulo };

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, nameof(VerificarCadastroVO.Valor)));
        }

        [Fact]
        public void VerificarCadastroEmpresaVOValidator_EmailComNulo_DevolveErroDoValor()
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = EmailComNulo };

            ValidationResult resultado = new VerificarCadastroEmpresaVOValidator().Validate(vo);

            Assert.True(TemErroDoCampo(resultado, nameof(VerificarCadastroEmpresaVO.Valor)));
        }

        [Fact]
        public void ClienteVOValidator_TextoSemNulo_NaoDevolveErroDeCaractere()
        {
            ClienteVO vo = new ClienteVO { Nome = TextoSemNulo };

            ValidationResult resultado = new ClienteVOValidator().Validate(vo);

            Assert.False(TemErroDoCampo(resultado, nameof(ClienteVO.Nome)));
        }

        private static bool TemErroDoCampo(ValidationResult resultado, string campo)
        {
            string mensagem = string.Format(ConstantsMessage.ErroCampoInvalido, campo);

            return resultado.Errors.Any(erro => erro.PropertyName == campo && erro.ErrorMessage == mensagem);
        }

        private static void DefinirTextoCliente(ClienteVO vo, string campo, string texto)
        {
            switch (campo)
            {
                case nameof(ClienteVO.Nome):
                    vo.Nome = texto;
                    break;
                case nameof(ClienteVO.Email):
                    vo.Email = texto;
                    break;
                case nameof(ClienteVO.Logradouro):
                    vo.Logradouro = texto;
                    break;
                case nameof(ClienteVO.Numero):
                    vo.Numero = texto;
                    break;
                case nameof(ClienteVO.Complemento):
                    vo.Complemento = texto;
                    break;
                case nameof(ClienteVO.Bairro):
                    vo.Bairro = texto;
                    break;
                case nameof(ClienteVO.Cidade):
                    vo.Cidade = texto;
                    break;
            }
        }

        private static void DefinirTextoLocadora(CadastroLocadoraVO vo, string campo, string texto)
        {
            switch (campo)
            {
                case nameof(CadastroLocadoraVO.RazaoSocial):
                    vo.RazaoSocial = texto;
                    break;
                case nameof(CadastroLocadoraVO.NomeFantasia):
                    vo.NomeFantasia = texto;
                    break;
                case nameof(CadastroLocadoraVO.Email):
                    vo.Email = texto;
                    break;
                case nameof(CadastroLocadoraVO.Logradouro):
                    vo.Logradouro = texto;
                    break;
                case nameof(CadastroLocadoraVO.Numero):
                    vo.Numero = texto;
                    break;
                case nameof(CadastroLocadoraVO.Complemento):
                    vo.Complemento = texto;
                    break;
                case nameof(CadastroLocadoraVO.Bairro):
                    vo.Bairro = texto;
                    break;
                case nameof(CadastroLocadoraVO.Cidade):
                    vo.Cidade = texto;
                    break;
                case nameof(CadastroLocadoraVO.ResponsavelNome):
                    vo.ResponsavelNome = texto;
                    break;
                case nameof(CadastroLocadoraVO.ResponsavelEmail):
                    vo.ResponsavelEmail = texto;
                    break;
            }
        }

        private static void DefinirTextoFuncionario(FuncionarioVO vo, string campo, string texto)
        {
            switch (campo)
            {
                case nameof(FuncionarioVO.Nome):
                    vo.Nome = texto;
                    break;
                case nameof(FuncionarioVO.Matricula):
                    vo.Matricula = texto;
                    break;
                case nameof(FuncionarioVO.Email):
                    vo.Email = texto;
                    break;
            }
        }

        private static void DefinirTextoEdicaoFuncionario(FuncionarioEdicaoVO vo, string campo, string texto)
        {
            switch (campo)
            {
                case nameof(FuncionarioEdicaoVO.Nome):
                    vo.Nome = texto;
                    break;
                case nameof(FuncionarioEdicaoVO.Matricula):
                    vo.Matricula = texto;
                    break;
                case nameof(FuncionarioEdicaoVO.Email):
                    vo.Email = texto;
                    break;
            }
        }

        private static void DefinirTextoCategoria(CategoriaVO vo, string campo, string texto)
        {
            switch (campo)
            {
                case nameof(CategoriaVO.Nome):
                    vo.Nome = texto;
                    break;
                case nameof(CategoriaVO.Descricao):
                    vo.Descricao = texto;
                    break;
            }
        }
    }
}
