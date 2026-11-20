using AutoVia.Communication.Validators.Manutencao;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Validadores da manutencao: a abertura (veiculo, tipo pelo nome exato depois do Trim, limites de oficina e descricao,
    /// caractere nulo) e o encerramento (km obrigatorio e nao negativo, custo nao negativo e com ate duas casas).
    /// </summary>
    public class ManutencaoValidatorsTests
    {
        private const int IdVeiculoDoTeste = 3;
        private const string TipoPreventiva = "PREVENTIVA";
        private const string TipoCorretiva = "CORRETIVA";
        private const string CaractereNulo = "\0";
        private const string MensagemPrecisaoDoCusto = "O custo deve ter no máximo 8 dígitos inteiros e 2 casas decimais.";

        private static ValidationResult ValidarAbertura(AbrirManutencaoVO vo)
        {
            AbrirManutencaoVOValidator validador = new AbrirManutencaoVOValidator();

            return validador.Validate(vo);
        }

        private static ValidationResult ValidarEncerramento(EncerrarManutencaoVO vo)
        {
            EncerrarManutencaoVOValidator validador = new EncerrarManutencaoVOValidator();

            return validador.Validate(vo);
        }

        private static AbrirManutencaoVO AberturaValida()
        {
            return new AbrirManutencaoVO { IdVeiculo = IdVeiculoDoTeste, Tipo = TipoPreventiva, Oficina = "Oficina Centro", Descricao = "Troca de oleo" };
        }

        private static List<string> Mensagens(ValidationResult resultado)
        {
            return resultado.Errors.Select(erro => erro.ErrorMessage).ToList();
        }

        [Fact]
        public void AberturaValida_PassaSemErro()
        {
            ValidationResult resultado = ValidarAbertura(AberturaValida());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void AberturaSemOficinaNemDescricao_PassaSemErro()
        {
            AbrirManutencaoVO vo = new AbrirManutencaoVO { IdVeiculo = IdVeiculoDoTeste, Tipo = TipoCorretiva };

            Assert.True(ValidarAbertura(vo).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void VeiculoZeroOuNegativo_PedeOVeiculo(int idVeiculo)
        {
            AbrirManutencaoVO vo = AberturaValida();
            vo.IdVeiculo = idVeiculo;

            ValidationResult resultado = ValidarAbertura(vo);

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoVeiculoObrigatorio }, Mensagens(resultado));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TipoAusenteVazioOuSoEspacos_PedeOTipo(string? tipo)
        {
            AbrirManutencaoVO vo = AberturaValida();
            vo.Tipo = tipo;

            ValidationResult resultado = ValidarAbertura(vo);

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoTipoObrigatorio }, Mensagens(resultado));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("preventiva")]
        [InlineData("Corretiva")]
        [InlineData("1")]
        [InlineData("PREVENTIVA\0")]
        public void TextoQueNaoEUmNomeExato_RecusaOTipo(string tipo)
        {
            AbrirManutencaoVO vo = AberturaValida();
            vo.Tipo = tipo;

            ValidationResult resultado = ValidarAbertura(vo);

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoTipoInvalido }, Mensagens(resultado));
        }

        [Theory]
        [InlineData(TipoPreventiva)]
        [InlineData(TipoCorretiva)]
        [InlineData("  CORRETIVA  ")]
        public void NomeExatoDepoisDoTrim_EAceito(string tipo)
        {
            AbrirManutencaoVO vo = AberturaValida();
            vo.Tipo = tipo;

            Assert.True(ValidarAbertura(vo).IsValid);
        }

        [Fact]
        public void OficinaComCemCaracteres_EAceitaEComCentoEUm_Recusa()
        {
            AbrirManutencaoVO aceita = AberturaValida();
            aceita.Oficina = new string('a', ConstantsValues.TamanhoMaximoOficinaManutencao);
            AbrirManutencaoVO recusada = AberturaValida();
            recusada.Oficina = new string('a', ConstantsValues.TamanhoMaximoOficinaManutencao + 1);

            Assert.True(ValidarAbertura(aceita).IsValid);
            Assert.Equal(
                new List<string> { string.Format(ConstantsMessage.ErroManutencaoOficinaTamanho, ConstantsValues.TamanhoMaximoOficinaManutencao) },
                Mensagens(ValidarAbertura(recusada)));
        }

        [Fact]
        public void DescricaoComDuzentosCaracteres_EAceitaEComDuzentosEUm_Recusa()
        {
            AbrirManutencaoVO aceita = AberturaValida();
            aceita.Descricao = new string('d', ConstantsValues.TamanhoMaximoDescricaoManutencao);
            AbrirManutencaoVO recusada = AberturaValida();
            recusada.Descricao = new string('d', ConstantsValues.TamanhoMaximoDescricaoManutencao + 1);

            Assert.True(ValidarAbertura(aceita).IsValid);
            Assert.Equal(
                new List<string> { string.Format(ConstantsMessage.ErroManutencaoDescricaoTamanho, ConstantsValues.TamanhoMaximoDescricaoManutencao) },
                Mensagens(ValidarAbertura(recusada)));
        }

        [Fact]
        public void CaractereNuloNaOficinaOuNaDescricao_RecusaComOCampo()
        {
            AbrirManutencaoVO naOficina = AberturaValida();
            naOficina.Oficina = "Ofi" + CaractereNulo + "cina";
            AbrirManutencaoVO naDescricao = AberturaValida();
            naDescricao.Descricao = "Desc" + CaractereNulo;

            Assert.Equal(new List<string> { string.Format(ConstantsMessage.ErroCampoInvalido, "Oficina") }, Mensagens(ValidarAbertura(naOficina)));
            Assert.Equal(new List<string> { string.Format(ConstantsMessage.ErroCampoInvalido, "Descricao") }, Mensagens(ValidarAbertura(naDescricao)));
        }

        [Fact]
        public void EncerramentoValido_PassaSemErro()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 5000, Custo = 350.75m };

            Assert.True(ValidarEncerramento(vo).IsValid);
        }

        [Fact]
        public void EncerramentoSemCustoNemData_PassaSemErro()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 0 };

            Assert.True(ValidarEncerramento(vo).IsValid);
        }

        [Fact]
        public void KmAusente_PedeAQuilometragemNaSaida()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = null };

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoKmObrigatorio }, Mensagens(ValidarEncerramento(vo)));
        }

        [Fact]
        public void KmNegativo_RecusaComKmNegativo()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = -1 };

            Assert.Equal(new List<string> { ConstantsMessage.ErroVeiculoKmNegativo }, Mensagens(ValidarEncerramento(vo)));
        }

        [Fact]
        public void CustoNegativo_RecusaComCustoInvalido()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 1000, Custo = -0.01m };

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoCustoInvalido }, Mensagens(ValidarEncerramento(vo)));
        }

        [Fact]
        public void CustoComTresCasas_RecusaComPrecisao()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 1000, Custo = 1.234m };

            Assert.Equal(new List<string> { MensagemPrecisaoDoCusto }, Mensagens(ValidarEncerramento(vo)));
        }

        [Fact]
        public void CustoComOnzeDigitosInteiros_RecusaComPrecisao()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 1000, Custo = 12345678901m };

            Assert.Equal(new List<string> { MensagemPrecisaoDoCusto }, Mensagens(ValidarEncerramento(vo)));
        }

        [Fact]
        public void CustoComOitoDigitosInteirosEDuasCasas_EAceito()
        {
            EncerrarManutencaoVO vo = new EncerrarManutencaoVO { KmAtual = 1000, Custo = 12345678.90m };

            Assert.True(ValidarEncerramento(vo).IsValid);
        }

        private static ValidationResult ValidarFiltro(FiltroManutencaoVO filtro)
        {
            FiltroManutencaoVOValidator validador = new FiltroManutencaoVOValidator();

            return validador.Validate(filtro);
        }

        private static string MensagemDaFaixaDePagina()
        {
            return string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo);
        }

        [Fact]
        public void FiltroSemNada_PassaSemErro()
        {
            Assert.True(ValidarFiltro(new FiltroManutencaoVO()).IsValid);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(20)]
        [InlineData(100)]
        public void TamanhoDePaginaDeUmACem_EAceito(int tamanho)
        {
            FiltroManutencaoVO filtro = new FiltroManutencaoVO { TamanhoPagina = tamanho };

            Assert.True(ValidarFiltro(filtro).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void TamanhoDePaginaForaDaFaixa_RecusaComAFaixa(int tamanho)
        {
            FiltroManutencaoVO filtro = new FiltroManutencaoVO { TamanhoPagina = tamanho };

            Assert.Equal(new List<string> { MensagemDaFaixaDePagina() }, Mensagens(ValidarFiltro(filtro)));
        }

        [Fact]
        public void PaginaQueEstouraOInt_RecusaPeloDeslocamento()
        {
            FiltroManutencaoVO filtro = new FiltroManutencaoVO { Pagina = int.MaxValue, TamanhoPagina = 20 };

            Assert.Contains(ConstantsMessage.ErroPaginaDeslocamentoInvalido, Mensagens(ValidarFiltro(filtro)));
        }

        [Fact]
        public void BuscaComSessentaCaracteres_EAceitaEComSessentaEUm_Recusa()
        {
            FiltroManutencaoVO aceita = new FiltroManutencaoVO { Busca = new string('a', ConstantsValues.TamanhoMaximoBuscaManutencao) };
            FiltroManutencaoVO recusada = new FiltroManutencaoVO { Busca = new string('a', ConstantsValues.TamanhoMaximoBuscaManutencao + 1) };

            Assert.True(ValidarFiltro(aceita).IsValid);
            Assert.Equal(
                new List<string> { string.Format(ConstantsMessage.ErroManutencaoBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaManutencao) },
                Mensagens(ValidarFiltro(recusada)));
        }

        [Fact]
        public void BuscaComEspacosNasPontas_ContaSoOTextoAparado()
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Busca = "  " + new string('a', ConstantsValues.TamanhoMaximoBuscaManutencao) + "  " };

            Assert.True(ValidarFiltro(vo).IsValid);
        }

        [Fact]
        public void BuscaComCaractereNulo_RecusaComOCampo()
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Busca = "fi" + CaractereNulo + "at" };

            Assert.Equal(new List<string> { string.Format(ConstantsMessage.ErroCampoInvalido, "Busca") }, Mensagens(ValidarFiltro(vo)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(TipoPreventiva)]
        [InlineData("  CORRETIVA  ")]
        public void TipoAusenteVazioOuNomeExato_EAceito(string? tipo)
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Tipo = tipo };

            Assert.True(ValidarFiltro(vo).IsValid);
        }

        [Theory]
        [InlineData("preventiva")]
        [InlineData("1")]
        [InlineData("XYZ")]
        public void TipoQueNaoEUmNome_RecusaComOTipoInvalido(string tipo)
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Tipo = tipo };

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoTipoInvalido }, Mensagens(ValidarFiltro(vo)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABERTA")]
        [InlineData("ENCERRADA")]
        public void SituacaoAusenteOuNomeExato_EAceita(string? situacao)
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Situacao = situacao };

            Assert.True(ValidarFiltro(vo).IsValid);
        }

        [Theory]
        [InlineData("aberta")]
        [InlineData("Encerrada")]
        [InlineData("XYZ")]
        public void SituacaoFora_RecusaComASituacaoInvalida(string situacao)
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { Situacao = situacao };

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoSituacaoInvalida }, Mensagens(ValidarFiltro(vo)));
        }

        [Fact]
        public void IdVeiculoAusenteOuPositivo_EAceito()
        {
            Assert.True(ValidarFiltro(new FiltroManutencaoVO { IdVeiculo = null }).IsValid);
            Assert.True(ValidarFiltro(new FiltroManutencaoVO { IdVeiculo = 1 }).IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-4)]
        public void IdVeiculoZeroOuNegativo_RecusaComOFiltroInvalido(int idVeiculo)
        {
            FiltroManutencaoVO vo = new FiltroManutencaoVO { IdVeiculo = idVeiculo };

            Assert.Equal(new List<string> { ConstantsMessage.ErroManutencaoVeiculoFiltroInvalido }, Mensagens(ValidarFiltro(vo)));
        }
    }
}
