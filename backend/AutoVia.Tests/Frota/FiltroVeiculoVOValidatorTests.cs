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
        /// <summary>Ultima pagina aceita com tamanho 100: (int.MaxValue / 100) + 1 = 21474837 (teto do validador base).</summary>
        private const int LimiteDePaginaComTamanhoCem = 21474837;

        /// <summary>Limite da busca pelo numero do contrato frota-tela11 (backend_busca.validacao): 60 caracteres. Se a constante mudar, o teste avisa.</summary>
        private const int LimiteContratoBuscaVeiculo = 60;

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

        [Fact]
        public void PaginaDoIntAcimaDoTeto_FalhaComMensagemDeDeslocamento()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Pagina = int.MaxValue, TamanhoPagina = ConstantsValues.TamanhoPaginaMaximo };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroPaginaDeslocamentoInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void PaginaNoUltimoValorDoTeto_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Pagina = LimiteDePaginaComTamanhoCem, TamanhoPagina = ConstantsValues.TamanhoPaginaMaximo };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
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
        public void PlacaComCaractereNulo_FalhaComMensagemDeCampoInvalido()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Placa = "ABC\0" };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroCampoInvalido, "Placa");

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
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

        [Fact]
        public void BuscaComSessentaCaracteres_PassaNaValidacao()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = new string('a', ConstantsValues.TamanhoMaximoBuscaVeiculo) };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void TamanhoMaximoDaBusca_EhSessentaPeloContrato()
        {
            Assert.Equal(LimiteContratoBuscaVeiculo, ConstantsValues.TamanhoMaximoBuscaVeiculo);
        }

        /// <summary>Busca so com espacos acima do limite: o contrato manda aparar, e vazio vira sem filtro, entao passa.</summary>
        [Fact]
        public void BuscaSoComEspacosAcimaDoLimite_PassaNaValidacaoAposOTrim()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = new string(' ', LimiteContratoBuscaVeiculo + 1) };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void BuscaComSessentaEUmCaracteres_FalhaComMensagemDeTamanho()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = new string('a', ConstantsValues.TamanhoMaximoBuscaVeiculo + 1) };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoFiltroBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaVeiculo);

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void BuscaComSessentaCaracteresComEspacosNasPontas_PassaNaValidacaoAposOTrim()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = "  " + new string('a', ConstantsValues.TamanhoMaximoBuscaVeiculo) + "  " };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void BuscaComSessentaEUmCaracteresComEspacosNasPontas_FalhaAposOTrim()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = " " + new string('a', ConstantsValues.TamanhoMaximoBuscaVeiculo + 1) + " " };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoFiltroBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaVeiculo);

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void BuscaComCaractereNulo_FalhaComMensagemDeCampoInvalido()
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = "Coro\0lla" };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroCampoInvalido, "Busca");

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        /// <summary>
        /// Substituto alto sem o baixo que o segue. A string e montada no proprio teste: argumento de InlineData
        /// passa pela serializacao do xUnit, que pode trocar o substituto isolado (visto na rodada: os casos falharam sem sentido).
        /// </summary>
        [Fact]
        public void BuscaComSubstitutoAltoSemPar_FalhaComMensagemDeCampoInvalido()
        {
            string busca = "Coro" + '\uD800' + "lla";

            VerificarBuscaRecusadaPorCampoInvalido(busca);
        }

        [Fact]
        public void BuscaComSubstitutoBaixoSemPar_FalhaComMensagemDeCampoInvalido()
        {
            string busca = "Coro" + '\uDE97' + "lla";

            VerificarBuscaRecusadaPorCampoInvalido(busca);
        }

        [Fact]
        public void BuscaComSubstitutoAltoNoFim_FalhaComMensagemDeCampoInvalido()
        {
            string busca = "Coro" + '\uD83D';

            VerificarBuscaRecusadaPorCampoInvalido(busca);
        }

        [Fact]
        public void BuscaComParSubstitutoValido_PassaNaValidacao()
        {
            string busca = "Car" + '\uD83D' + '\uDE97';
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = busca };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        private static void VerificarBuscaRecusadaPorCampoInvalido(string busca)
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = busca };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroCampoInvalido, "Busca");

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BuscaNulaOuVazia_PassaNaValidacao(string? busca)
        {
            FiltroVeiculoVO filtro = new FiltroVeiculoVO { Busca = busca };
            FiltroVeiculoVOValidator validator = new FiltroVeiculoVOValidator();

            ValidationResult resultado = validator.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();

            return mensagens;
        }
    }
}
