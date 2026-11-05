using AutoVia.Api.Extensions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do FiltroVeiculoVOValidator: regras da paginacao (herdadas pelo Include), categoria,
    /// status e placa quando informados, e a resolucao do validador pela injecao de dependencia
    /// (o filtro de acao precisa encontrar IValidator&lt;FiltroVeiculoVO&gt;).
    /// </summary>
    public class FiltroVeiculoVOValidatorTests
    {
        [Fact]
        public void FiltroPadrao_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO();
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PaginaZero_FalhaComMensagemDePagina()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Pagina = 0 };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroPaginaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void TamanhoPaginaZero_FalhaComMensagemDeTamanho()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { TamanhoPagina = 0 };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo);

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void TamanhoPaginaCentoEUm_FalhaComMensagemDeTamanho()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { TamanhoPagina = ConstantsValues.TamanhoPaginaMaximo + 1 };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo);

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IdCategoriaNaoPositivo_FalhaComMensagemDeCategoria(int idCategoria)
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { IdCategoria = idCategoria };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroVeiculoFiltroCategoriaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void StatusForaDoEnum_FalhaComMensagemComValoresAceitos()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Status = (StatusVeiculo)99 };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, string.Join(", ", Enum.GetNames<StatusVeiculo>()));

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void StatusValido_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Status = StatusVeiculo.LOCADO };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PlacaComNoveCaracteres_FalhaComMensagemDePlaca()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Placa = "ABC1D2345" };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoFiltroPlacaTamanho, ConstantsValues.TamanhoMaximoBuscaPlacaVeiculo);

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void PlacaVazia_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Placa = string.Empty };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PlacaComOitoCaracteresComHifen_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Placa = "ABC-1D23" };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void AddValidation_ResolveOValidadorDoFiltroEValidaAPaginacao()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddValidation();
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope escopo = provider.CreateScope();
            IValidator<FiltroVeiculoVO> validator = escopo.ServiceProvider.GetRequiredService<IValidator<FiltroVeiculoVO>>();
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Pagina = 0 };

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroPaginaInvalida, ObterMensagens(resultado));
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();

            return mensagens;
        }
    }
}
