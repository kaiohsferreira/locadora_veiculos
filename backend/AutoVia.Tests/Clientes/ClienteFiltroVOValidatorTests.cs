using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Testes do ClienteFiltroVOValidator: as regras de paginacao herdadas (Include) e o
    /// status, que so e conferido quando informado.
    /// </summary>
    public class ClienteFiltroVOValidatorTests
    {
        [Fact]
        public void FiltroSemStatusEPaginaValida_PassaNaValidacao()
        {
            ClienteFiltroVO filtro = new ClienteFiltroVO { Pagina = 1, TamanhoPagina = 20, Status = null };

            ValidationResult resultado = new ClienteFiltroVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void StatusValido_PassaNaValidacao()
        {
            ClienteFiltroVO filtro = new ClienteFiltroVO { Status = StatusCadastroCliente.APROVADO };

            ValidationResult resultado = new ClienteFiltroVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void StatusForaDoEnum_FalhaComMensagemDeStatus()
        {
            ClienteFiltroVO filtro = new ClienteFiltroVO { Status = (StatusCadastroCliente)99 };

            ValidationResult resultado = new ClienteFiltroVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteStatusInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void PaginaZero_FalhaPelasRegrasHerdadasDePaginacao()
        {
            ClienteFiltroVO filtro = new ClienteFiltroVO { Pagina = 0, TamanhoPagina = 20 };

            ValidationResult resultado = new ClienteFiltroVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroPaginaInvalida, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void TamanhoPaginaAcimaDoMaximo_FalhaPelasRegrasHerdadasDePaginacao()
        {
            ClienteFiltroVO filtro = new ClienteFiltroVO { Pagina = 1, TamanhoPagina = ConstantsValues.TamanhoPaginaMaximo + 1 };
            string mensagem = string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo);

            ValidationResult resultado = new ClienteFiltroVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagem, resultado.Errors.Select(erro => erro.ErrorMessage));
        }
    }
}
