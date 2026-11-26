using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Contratos;
using AutoVia.Communication.ViewObjects.Contratos;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System.Text;

namespace AutoVia.Tests.Contratos
{
    /// <summary>
    /// Regras puras do contrato digital da locacao (contrato-locacao v5), sem banco: RP-06, locacao em aberto, aceites, hash,
    /// montagem do documento (O-12, renumeracao, mascaras) e formato do nome e da assinatura. Os textos aprovados sao conferidos
    /// contra as constantes, para que nenhum texto mude sem decisao do Kaio.
    /// </summary>
    public sealed class RegrasContratoLocacaoTests
    {
        private static readonly DateTime Retirada = new DateTime(2026, 11, 20, 10, 0, 0);
        private static readonly DateTime DevolucaoPrevista = new DateTime(2026, 11, 22, 0, 0, 0);

        private static string PngBase64(int tamanhoBytes)
        {
            byte[] bytes = new byte[tamanhoBytes];
            byte[] cabecalho = ConstantsValues.ContratoAssinaturaCabecalhoPng;
            Array.Copy(cabecalho, bytes, cabecalho.Length);

            return Convert.ToBase64String(bytes);
        }

        private static Empresa CriarEmpresa()
        {
            return new Empresa
            {
                RazaoSocial = "Locadora Teste Ltda", NomeFantasia = "Locadora Teste", Cnpj = "12345678000199", Telefone = "3133334444",
                Email = "contato@teste.com", Cep = "30100000", Logradouro = "Rua A", Numero = "10", Bairro = "Centro",
                Cidade = "Belo Horizonte", Uf = "MG"
            };
        }

        private static Cliente CriarCliente()
        {
            return new Cliente { Nome = "Maria Souza", Cpf = "12345678909", CnhNumero = "12345678900", CnhValidade = new DateOnly(2030, 1, 1) };
        }

        private static Veiculo CriarVeiculo()
        {
            return new Veiculo
            {
                IdVeiculo = 1, IdEmpresa = 1, IdCategoria = 1, Marca = "Fiat", Modelo = "Toro", Placa = "ABC1D23",
                Chassi = "9BD000000000001", Renavam = "12345678901", AnoFabricacao = 2025, AnoModelo = 2026, Cor = "Branca"
            };
        }

        private static Locacao CriarLocacao(decimal valorCaucao, decimal valorDiaria)
        {
            return new Locacao
            {
                IdLocacao = 10, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = Retirada, DataDevolucaoPrevista = DevolucaoPrevista,
                ValorDiaria = valorDiaria, ValorCaucao = valorCaucao, KmSaida = 100, Veiculo = CriarVeiculo(), Cliente = CriarCliente()
            };
        }

        private static Vistoria CriarEntrada(TipoVistoria tipo = TipoVistoria.ENTRADA)
        {
            return new Vistoria { IdVistoria = 1, IdLocacao = 10, IdFuncionario = 20, Tipo = tipo, DataHora = Retirada, KmRegistrado = 100, NivelCombustivel = 0.75m };
        }

        private static ConfiguracaoCobrancaExtra CriarConfiguracao()
        {
            return new ConfiguracaoCobrancaExtra
            {
                IdConfiguracao = 1, IdEmpresa = 1, IdCategoria = 1, Versao = 3, BaseFranquia = ConstantsMessage.CobrancasExtrasPorDiaria,
                FranquiaKm = 200, ValorKmExcedente = 1.5m, ValorTanqueCompleto = 300m, ToleranciaAtrasoMinutos = 30,
                UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora, FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional,
                ValorUnidadeAtraso = 20m, BaseAvarias = "ESTIMADO"
            };
        }

        private static List<string> TitulosDoDocumento(ContratoDocumentoVO documento)
        {
            return documento.Clausulas.Select(x => x.Titulo).ToList();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void TemVistoriaEntradaAtiva_SoEntradaAtivaDaLocacao(bool esperado, bool entradaAtiva)
        {
            Vistoria? entrada = entradaAtiva ? CriarEntrada() : null;

            Assert.Equal(esperado, RegrasContratoLocacao.TemVistoriaEntradaAtiva(entrada, 10));
        }

        [Fact]
        public void TemVistoriaEntradaAtiva_VistoriaDeSaidaNaoConta()
        {
            Assert.False(RegrasContratoLocacao.TemVistoriaEntradaAtiva(CriarEntrada(TipoVistoria.SAIDA), 10));
        }

        [Fact]
        public void TemVistoriaEntradaAtiva_DesativadaOuDeOutraLocacaoNaoConta()
        {
            Vistoria desativada = CriarEntrada();
            desativada.DisabledAt = DateTime.Now;

            Assert.False(RegrasContratoLocacao.TemVistoriaEntradaAtiva(desativada, 10));
            Assert.False(RegrasContratoLocacao.TemVistoriaEntradaAtiva(CriarEntrada(), 99));
        }

        [Theory]
        [InlineData(StatusLocacao.EM_ANDAMENTO, true)]
        [InlineData(StatusLocacao.ATRASADA, true)]
        [InlineData(StatusLocacao.FINALIZADA, false)]
        [InlineData(StatusLocacao.CANCELADA, false)]
        public void LocacaoEmAberto_SoEmAndamentoOuAtrasada(StatusLocacao status, bool esperado)
        {
            Assert.Equal(esperado, RegrasContratoLocacao.LocacaoEmAberto(status));
        }

        [Fact]
        public void ErroAceites_CondicoesGeraisSempreExigidas()
        {
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, RegrasContratoLocacao.ErroAceites(false, true, false));
            Assert.Null(RegrasContratoLocacao.ErroAceites(true, null, false));
        }

        [Fact]
        public void ErroAceites_CombustivelSoExigidoQuandoAClausula5Consta()
        {
            Assert.Null(RegrasContratoLocacao.ErroAceites(true, null, false));
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, RegrasContratoLocacao.ErroAceites(true, null, true));
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, RegrasContratoLocacao.ErroAceites(true, false, true));
            Assert.Null(RegrasContratoLocacao.ErroAceites(true, true, true));
        }

        [Fact]
        public void NomeCampoAceiteRecusado_IndicaOCampoDoAceiteQueFalhou()
        {
            Assert.Equal(nameof(AssinarContratoLocacaoVO.AceiteCondicoesGerais), RegrasContratoLocacao.NomeCampoAceiteRecusado(false));
            Assert.Equal(nameof(AssinarContratoLocacaoVO.AceitePoliticaCombustivel), RegrasContratoLocacao.NomeCampoAceiteRecusado(true));
        }

        [Fact]
        public void CalcularHashSha256_VetorConhecido()
        {
            Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", RegrasContratoLocacao.CalcularHashSha256("abc"));
        }

        [Fact]
        public void CalcularHashSha256_TextoComAcentoUsaOsBytesUtf8()
        {
            string texto = "Locação";
            string esperado = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();

            Assert.Equal(esperado, RegrasContratoLocacao.CalcularHashSha256(texto));
            Assert.Equal(64, RegrasContratoLocacao.CalcularHashSha256(texto).Length);
        }

        [Fact]
        public void MontarDocumento_ComCaucaoEConfiguracao_TemAsOitoClausulasNaOrdem()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(500m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), CriarConfiguracao());

            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, documento.Clausulas.Select(x => x.Numero).ToArray());
            Assert.Equal("Caução", documento.Clausulas[3].Titulo);
            Assert.NotNull(documento.TabelaCobrancas);
            Assert.NotNull(documento.Locacao.ValorCaucaoStr);
        }

        [Fact]
        public void MontarDocumento_CaucaoZero_TiraAClausula4ERenumera()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(0m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), CriarConfiguracao());

            Assert.DoesNotContain(ConstantsMessage.ContratoTituloClausulaCaucao, TitulosDoDocumento(documento));
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, documento.Clausulas.Select(x => x.Numero).ToArray());
            Assert.Equal(ConstantsMessage.ContratoTituloClausulaNatureza, documento.Clausulas[6].Titulo);
            Assert.Null(documento.Locacao.ValorCaucaoStr);
        }

        [Fact]
        public void MontarDocumento_SemConfiguracao_TiraAs2_5_6_7ERenumera()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(500m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), null);

            Assert.Null(documento.TabelaCobrancas);
            Assert.Equal(new[] { ConstantsMessage.ContratoTituloClausulaObjeto, ConstantsMessage.ContratoTituloClausulaValores, ConstantsMessage.ContratoTituloClausulaCaucao, ConstantsMessage.ContratoTituloClausulaNatureza },
                TitulosDoDocumento(documento));
            Assert.Equal(new[] { 1, 2, 3, 4 }, documento.Clausulas.Select(x => x.Numero).ToArray());
            Assert.Equal(4, documento.Clausulas[3].Numero);
        }

        [Fact]
        public void MontarDocumento_SemConfiguracaoESemCaucao_FicamTresClausulas()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(0m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), null);

            Assert.Equal(new[] { 1, 2, 3 }, documento.Clausulas.Select(x => x.Numero).ToArray());
        }

        [Fact]
        public void MontarDocumento_DiariasEValorPrevistoPelaRegraDaApuracao_O12()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(500m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), CriarConfiguracao());

            int diariasEsperadas = RegrasDevolucao.CalcularDiarias(Retirada, DevolucaoPrevista);
            Assert.Equal(2, diariasEsperadas);
            Assert.Equal(diariasEsperadas, documento.Locacao.DiariasPrevistas);
            Assert.Equal(StaticMethods.FormateToMonetary(RegrasDevolucao.ValorDaDiaria(2, 199.90m)), documento.Locacao.ValorTotalPrevistoStr);
            Assert.Equal(StaticMethods.FormateToMonetary(399.80m), documento.Locacao.ValorTotalPrevistoStr);
        }

        [Fact]
        public void MontarDocumento_MesmaDataDeRetiradaEDevolucao_DiariaZeroETotalZero()
        {
            Locacao locacao = CriarLocacao(0m, 199.90m);
            locacao.DataDevolucaoPrevista = Retirada.Date;

            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), locacao, CriarCliente(), CriarVeiculo(), CriarEntrada(), null);

            Assert.Equal(0, documento.Locacao.DiariasPrevistas);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), documento.Locacao.ValorTotalPrevistoStr);
        }

        [Fact]
        public void MontarDocumento_DiariaComCentavos_SomaSemPerderCentavo()
        {
            DateTime devolucaoTresDias = new DateTime(2026, 11, 23, 0, 0, 0);
            Locacao locacao = CriarLocacao(0m, 199.90m);
            locacao.DataDevolucaoPrevista = devolucaoTresDias;

            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), locacao, CriarCliente(), CriarVeiculo(), CriarEntrada(), null);

            Assert.Equal(3, documento.Locacao.DiariasPrevistas);
            Assert.Equal(StaticMethods.FormateToMonetary(599.70m), documento.Locacao.ValorTotalPrevistoStr);
        }

        [Fact]
        public void MontarDocumento_MascaraCpfECnhEDatasComoTexto()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(0m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), null);

            Assert.Equal(StaticMethods.MascararCpf("12345678909"), documento.Locatario.CpfMascarado);
            Assert.Equal(StaticMethods.MascararCnh("12345678900"), documento.Locatario.CnhMascarada);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(Retirada), documento.Locacao.DataRetiradaStr);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(DevolucaoPrevista), documento.Locacao.DataDevolucaoPrevistaStr);
            Assert.Equal(0.75m, documento.VistoriaEntrada.NivelCombustivel);
            Assert.Equal(1, documento.VersaoDocumento);
        }

        [Fact]
        public void MontarDocumento_NaoTemCpfCompletoNemDadoDeCartao()
        {
            ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(CriarEmpresa(), CriarLocacao(500m, 199.90m), CriarCliente(),
                CriarVeiculo(), CriarEntrada(), CriarConfiguracao());
            string texto = System.Text.Json.JsonSerializer.Serialize(documento, ConstantsValues.ContratoOpcoesJson);

            Assert.DoesNotContain("12345678909", texto);
            Assert.DoesNotContain("Cartao", texto, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public void ErroNomeAssinante_VazioOuSoEspacos_ExigeONome(string? nome)
        {
            Assert.Equal(ConstantsMessage.ErroContratoNomeAssinante, RegrasFormatoAssinaturaContrato.ErroNomeAssinante(nome));
        }

        [Theory]
        [InlineData("Ana\u0000Silva")]
        [InlineData("Ana\tSilva")]
        public void ErroNomeAssinante_ComCaractereDeControle_Recusa(string nome)
        {
            Assert.Equal(ConstantsMessage.ErroContratoNomeAssinanteInvalido, RegrasFormatoAssinaturaContrato.ErroNomeAssinante(nome));
        }

        [Fact]
        public void ErroNomeAssinante_Limite100Caracteres()
        {
            Assert.Null(RegrasFormatoAssinaturaContrato.ErroNomeAssinante(new string('A', 100)));
            Assert.Equal(ConstantsMessage.ErroContratoNomeAssinanteTamanho, RegrasFormatoAssinaturaContrato.ErroNomeAssinante(new string('A', 101)));
        }

        [Fact]
        public void ErroNomeAssinante_NomeComumEhAceito()
        {
            Assert.Null(RegrasFormatoAssinaturaContrato.ErroNomeAssinante("Ana Silva"));
        }

        [Fact]
        public void ErroAssinaturaPng_VazioEInvalidoEFormato()
        {
            Assert.Equal(ConstantsMessage.ErroContratoAssinaturaObrigatoria, RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(null));
            Assert.Equal(ConstantsMessage.ErroContratoAssinaturaInvalida, RegrasFormatoAssinaturaContrato.ErroAssinaturaPng("abc"));
            Assert.Equal(ConstantsMessage.ErroContratoAssinaturaInvalida, RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(Convert.ToBase64String(new byte[8])));
        }

        [Fact]
        public void ErroAssinaturaPng_PngValidoEhAceito()
        {
            Assert.Null(RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(PngBase64(8)));
        }

        [Fact]
        public void ErroAssinaturaPng_TetoDeBytesEDeCaracteres()
        {
            Assert.Null(RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(PngBase64(65536)));
            Assert.Equal(ConstantsMessage.ErroContratoAssinaturaTamanho, RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(PngBase64(65537)));
            Assert.Equal(ConstantsMessage.ErroContratoAssinaturaTamanho, RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(new string('A', 87385)));
        }

        [Fact]
        public void ValidadorDoVo_RecusaAceiteFalsoComOTextoAprovado()
        {
            AssinarContratoLocacaoVOValidator validador = new AssinarContratoLocacaoVOValidator();
            AssinarContratoLocacaoVO model = new AssinarContratoLocacaoVO
            {
                NomeAssinante = "Maria Souza", AceiteCondicoesGerais = false, AssinaturaPngBase64 = PngBase64(8)
            };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(model);

            Assert.False(resultado.IsValid);
            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, resultado.Errors[0].ErrorMessage);
        }

        [Fact]
        public void ValidadorDoVo_AceitaEntradaCorreta()
        {
            AssinarContratoLocacaoVOValidator validador = new AssinarContratoLocacaoVOValidator();
            AssinarContratoLocacaoVO model = new AssinarContratoLocacaoVO
            {
                NomeAssinante = "Maria Souza", AceiteCondicoesGerais = true, AceitePoliticaCombustivel = null, AssinaturaPngBase64 = PngBase64(8)
            };

            Assert.True(validador.Validate(model).IsValid);
        }

        [Fact]
        public void TextosAprovados_SaoIguaisAoContrato()
        {
            Assert.Equal("A locação não tem vistoria de entrada registrada. Registre a vistoria antes de emitir o contrato (RP-06).", ConstantsMessage.ErroContratoSemVistoriaEntrada);
            Assert.Equal("O contrato só pode ser emitido com a locação em andamento ou atrasada.", ConstantsMessage.ErroContratoLocacaoNaoAberta);
            Assert.Equal("Este contrato já foi assinado.", ConstantsMessage.ErroContratoJaAssinado);
            Assert.Equal("Marque os aceites obrigatórios para assinar o contrato.", ConstantsMessage.ErroContratoAceiteObrigatorio);
            Assert.Equal("O nome do assinante contém caracteres inválidos.", ConstantsMessage.ErroContratoNomeAssinanteInvalido);
            Assert.Equal("A assinatura deve ser uma imagem PNG válida.", ConstantsMessage.ErroContratoAssinaturaInvalida);
        }

        [Fact]
        public void TextosDasClausulasEDasMensagensRestantes_SaoIguaisAoContrato()
        {
            Assert.Equal("Informe o nome de quem assina o contrato.", ConstantsMessage.ErroContratoNomeAssinante);
            Assert.Equal("O nome do assinante deve ter no máximo 100 caracteres.", ConstantsMessage.ErroContratoNomeAssinanteTamanho);
            Assert.Equal("A assinatura é obrigatória.", ConstantsMessage.ErroContratoAssinaturaObrigatoria);
            Assert.Equal("A assinatura excede o tamanho máximo permitido.", ConstantsMessage.ErroContratoAssinaturaTamanho);

            Assert.Equal("Objeto da locação", ConstantsMessage.ContratoTituloClausulaObjeto);
            Assert.Equal("Prazos", ConstantsMessage.ContratoTituloClausulaPrazos);
            Assert.Equal("Valores", ConstantsMessage.ContratoTituloClausulaValores);
            Assert.Equal("Caução", ConstantsMessage.ContratoTituloClausulaCaucao);
            Assert.Equal("Política de combustível", ConstantsMessage.ContratoTituloClausulaCombustivel);
            Assert.Equal("Quilometragem", ConstantsMessage.ContratoTituloClausulaQuilometragem);
            Assert.Equal("Avarias", ConstantsMessage.ContratoTituloClausulaAvarias);
            Assert.Equal("Natureza do registro", ConstantsMessage.ContratoTituloClausulaNatureza);

            Assert.Equal("O locador entrega ao locatário o veículo descrito neste contrato, para uso durante o período da locação, nas condições aqui previstas.", ConstantsMessage.ContratoTextoClausulaObjeto);
            Assert.Equal("A locação começa na data de retirada e tem como data prevista de devolução a indicada neste contrato. A devolução depois do prazo fica sujeita à cobrança de atraso prevista na tabela de cobranças.", ConstantsMessage.ContratoTextoClausulaPrazos);
            Assert.Equal("A diária é a indicada neste contrato, cobrada por dia de locação. Outras cobranças, quando houver, são apuradas na devolução e informadas ao locatário.", ConstantsMessage.ContratoTextoClausulaValores);
            Assert.Equal("Quando há caução, o valor indicado neste contrato é pré-autorizado no cartão do locatário na retirada. Na devolução, a caução é liberada ou retida apenas para cobrir cobranças extras apuradas.", ConstantsMessage.ContratoTextoClausulaCaucao);
            Assert.Equal("O veículo é retirado com o nível de combustível registrado na vistoria de entrada e deve ser devolvido com o mesmo nível. A falta de combustível na devolução é cobrada pelo valor do tanque completo indicado na tabela de cobranças, na proporção da diferença.", ConstantsMessage.ContratoTextoClausulaCombustivel);
            Assert.Equal("A franquia de quilometragem é a indicada neste contrato. Cada quilômetro rodado além da franquia é cobrado pelo valor de km excedente da tabela de cobranças.", ConstantsMessage.ContratoTextoClausulaQuilometragem);
            Assert.Equal("Avarias registradas na vistoria de devolução que não constam na vistoria de entrada são cobradas conforme os valores apurados pela locadora.", ConstantsMessage.ContratoTextoClausulaAvarias);
            Assert.Equal("Este documento é um registro gerado pelo sistema AutoVia. O texto do documento fica guardado com um código de conferência (SHA-256), que permite verificar se ele foi alterado depois da assinatura. Não tem certificação ICP-Brasil.", ConstantsMessage.ContratoTextoClausulaNatureza);
        }

        [Fact]
        public void ConstantesDoContrato_SaoOsDoContratoV5()
        {
            Assert.Equal(87384, ConstantsValues.ContratoAssinaturaBase64TamanhoMaximo);
            Assert.Equal(65536, ConstantsValues.ContratoAssinaturaTamanhoMaximoBytes);
            Assert.Equal("PENDENTE_ASSINATURA", ConstantsValues.ContratoSituacaoPendente);
            Assert.Equal("ASSINADO", ConstantsValues.ContratoSituacaoAssinado);
            Assert.Equal("ContratoLocacao", ConstantsValues.PoliticaContratoLocacao);
        }
    }
}
