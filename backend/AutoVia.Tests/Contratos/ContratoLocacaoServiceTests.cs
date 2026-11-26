using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Contratos;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Contratos
{
    /// <summary>
    /// Servico do contrato da locacao com repositorios em memoria (sem banco): ordem das checagens (O-6), leitura pura (RN-4),
    /// copia guardada que nao remonta (RN-10 e documento_guardado), hash, O-12, renumeracao, concorrencia por regra (RN-3) e log
    /// sem dados pessoais.
    /// </summary>
    public sealed class ContratoLocacaoServiceTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 26, 15, 0, 0);
        private static readonly DateTime Retirada = new DateTime(2026, 11, 20, 10, 0, 0);
        private static readonly DateTime DevolucaoPrevista = new DateTime(2026, 11, 22, 0, 0, 0);
        private const int IdLocacaoDoTeste = 10;
        private const int IdFuncionarioDoTeste = 20;
        private const string NomeAssinanteDoTeste = "Maria Souza";
        private const string CpfDoTeste = "12345678909";

        /// <summary>Cenario com uma locacao da locadora 1, vistoria de entrada, configuracao da categoria e o funcionario que coleta.</summary>
        private sealed class Cenario
        {
            public Cenario()
            {
                IContextoEmpresa contexto = ContextoEmpresaDeTeste.Criar(1, CargoFuncionario.ATENDENTE);
                Contratos = new FakeContratoLocacaoRepository(() => 1);
                VistoriasRepo = new FakeVistoriaRepository();
                Cobrancas = new FakeCobrancasExtrasRepository(contexto);
                Relogio = new FakeRelogio(Agora);
                Log = new FakeLogService();
                Servico = new ContratoLocacaoService(Contratos, VistoriasRepo, Cobrancas, Relogio, Log);

                Contratos.Locacoes.Add(CriarLocacao(500m, 199.90m, 1));
                VistoriasRepo.Vistorias.Add(CriarEntrada());
                Contratos.Funcionarios.Add(new Funcionario { IdFuncionario = IdFuncionarioDoTeste, Nome = "Ana Souza", Ativo = true, IdEmpresa = 1 });
                Cobrancas.Configuracoes.Add(CriarConfiguracao());
            }

            public FakeContratoLocacaoRepository Contratos { get; }
            public FakeVistoriaRepository VistoriasRepo { get; }
            public FakeCobrancasExtrasRepository Cobrancas { get; }
            public FakeRelogio Relogio { get; }
            public FakeLogService Log { get; }
            public ContratoLocacaoService Servico { get; }

            public Locacao Locacao => Contratos.Locacoes[0];

            public Task<ReturnContratoLocacaoVO> AssinarAsync() => Servico.AssinarAsync(IdLocacaoDoTeste, IdFuncionarioDoTeste, CorpoValido());
        }

        private static AssinarContratoLocacaoVO CorpoValido()
        {
            return new AssinarContratoLocacaoVO
            {
                NomeAssinante = NomeAssinanteDoTeste,
                AceiteCondicoesGerais = true,
                AceitePoliticaCombustivel = true,
                AssinaturaPngBase64 = Convert.ToBase64String(ConstantsValues.ContratoAssinaturaCabecalhoPng)
            };
        }

        private static Locacao CriarLocacao(decimal valorCaucao, decimal valorDiaria, int idEmpresa)
        {
            return new Locacao
            {
                IdLocacao = IdLocacaoDoTeste, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = Retirada,
                DataDevolucaoPrevista = DevolucaoPrevista, ValorDiaria = valorDiaria, ValorCaucao = valorCaucao, KmSaida = 100,
                Veiculo = new Veiculo
                {
                    IdVeiculo = 1, IdEmpresa = idEmpresa, IdCategoria = 1, Marca = "Fiat", Modelo = "Toro", Placa = "ABC1D23",
                    Chassi = "9BD000000000001", Renavam = "12345678901", AnoFabricacao = 2025, AnoModelo = 2026, Cor = "Branca",
                    Empresa = new Empresa
                    {
                        IdEmpresa = idEmpresa, RazaoSocial = "Locadora Teste Ltda", NomeFantasia = "Locadora Teste", Cnpj = "12345678000199",
                        Telefone = "3133334444", Email = "contato@teste.com", Cep = "30100000", Logradouro = "Rua A", Numero = "10",
                        Bairro = "Centro", Cidade = "Belo Horizonte", Uf = "MG"
                    }
                },
                Cliente = new Cliente { Nome = NomeAssinanteDoTeste, Cpf = CpfDoTeste, CnhNumero = "12345678900", CnhValidade = new DateOnly(2030, 1, 1) }
            };
        }

        private static Vistoria CriarEntrada()
        {
            return new Vistoria
            {
                IdVistoria = 1, IdLocacao = IdLocacaoDoTeste, IdFuncionario = IdFuncionarioDoTeste, Tipo = TipoVistoria.ENTRADA,
                DataHora = Retirada, KmRegistrado = 100, NivelCombustivel = 0.75m
            };
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

        private static string HashDoTexto(string texto)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
        }

        private static string Serializar(ContratoDocumentoVO documento)
        {
            return JsonSerializer.Serialize(documento, ConstantsValues.ContratoOpcoesJson);
        }

        [Fact]
        public async Task Obter_SemVistoriaDeEntrada_Recusa409ComRP06SemGravar()
        {
            Cenario cenario = new Cenario();
            cenario.VistoriasRepo.Vistorias.Clear();

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ObterAsync(IdLocacaoDoTeste));

            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, erro.Message);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Obter_SemVistoriaELocacaoFinalizada_DaRP06AntesDeNaoAberta()
        {
            Cenario cenario = new Cenario();
            cenario.VistoriasRepo.Vistorias.Clear();
            cenario.Locacao.Status = StatusLocacao.FINALIZADA;

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ObterAsync(IdLocacaoDoTeste));

            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, erro.Message);
        }

        [Fact]
        public async Task Obter_ComVistoriaELocacaoFinalizada_Recusa409NaoAberta()
        {
            Cenario cenario = new Cenario();
            cenario.Locacao.Status = StatusLocacao.FINALIZADA;

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.ObterAsync(IdLocacaoDoTeste));

            Assert.Equal(ConstantsMessage.ErroContratoLocacaoNaoAberta, erro.Message);
        }

        [Fact]
        public async Task Obter_PendenteMontaODocumentoSemGravarNada()
        {
            Cenario cenario = new Cenario();

            ReturnContratoLocacaoVO retorno = await cenario.Servico.ObterAsync(IdLocacaoDoTeste);

            Assert.Equal(ConstantsValues.ContratoSituacaoPendente, retorno.Situacao);
            Assert.Null(retorno.HashSha256);
            Assert.Null(retorno.Assinatura);
            Assert.Equal(8, retorno.Documento.Clausulas.Count);
            Assert.Equal(0, cenario.Contratos.Insercoes);
            Assert.Empty(cenario.Contratos.Contratos);
        }

        [Fact]
        public async Task Assinar_GravaOSnapshotEOHashDoTextoExato()
        {
            Cenario cenario = new Cenario();

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            Assert.Equal(ConstantsValues.ContratoSituacaoAssinado, retorno.Situacao);
            ContratoLocacao gravado = Assert.Single(cenario.Contratos.Contratos);
            Assert.Equal(HashDoTexto(gravado.DocumentoJson), gravado.HashSha256);
            Assert.Equal(64, gravado.HashSha256.Length);
            Assert.Equal(gravado.HashSha256, retorno.HashSha256);
            Assert.Equal(IdFuncionarioDoTeste, gravado.IdFuncionario);
            Assert.Equal(Agora, gravado.AssinadoEm);
        }

        [Fact]
        public async Task Assinar_DevolveAAssinaturaComOColetorDoCadastro()
        {
            Cenario cenario = new Cenario();

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            Assert.NotNull(retorno.Assinatura);
            Assert.Equal(NomeAssinanteDoTeste, retorno.Assinatura!.NomeAssinante);
            Assert.Equal(IdFuncionarioDoTeste, retorno.Assinatura.IdFuncionarioColetor);
            Assert.Equal("Ana Souza", retorno.Assinatura.NomeFuncionarioColetor);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(Agora), retorno.Assinatura.AssinadoEmStr);
            Assert.True(retorno.Assinatura.AceitePoliticaCombustivel);
        }

        [Fact]
        public async Task Obter_DepoisDeAssinar_DevolveOSnapshotSemRemontarComOsDadosVivos()
        {
            Cenario cenario = new Cenario();
            ReturnContratoLocacaoVO assinado = await cenario.AssinarAsync();
            string documentoAssinado = Serializar(assinado.Documento);

            cenario.Locacao.KmSaida = 9999;
            cenario.Locacao.ValorDiaria = 999m;
            cenario.Locacao.Cliente.Nome = "Outro Nome";
            cenario.Locacao.DataDevolucaoPrevista = new DateTime(2027, 1, 1);
            cenario.VistoriasRepo.Vistorias[0].KmRegistrado = 5000;

            ReturnContratoLocacaoVO depois = await cenario.Servico.ObterAsync(IdLocacaoDoTeste);

            Assert.Equal(ConstantsValues.ContratoSituacaoAssinado, depois.Situacao);
            Assert.Equal(documentoAssinado, Serializar(depois.Documento));
            Assert.Equal(assinado.HashSha256, depois.HashSha256);
            Assert.Single(cenario.Contratos.Contratos);
        }

        [Fact]
        public async Task Assinar_DuasVezes_SegundaRecusa409JaAssinadoSemGravarDeNovo()
        {
            Cenario cenario = new Cenario();
            await cenario.AssinarAsync();

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            Assert.Equal(ConstantsMessage.ErroContratoJaAssinado, erro.Message);
            Assert.Equal(1, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_JaAssinadoVemAntesDeRP06EDeNaoAberta()
        {
            Cenario cenario = new Cenario();
            await cenario.AssinarAsync();
            cenario.VistoriasRepo.Vistorias.Clear();
            cenario.Locacao.Status = StatusLocacao.FINALIZADA;

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            Assert.Equal(ConstantsMessage.ErroContratoJaAssinado, erro.Message);
            Assert.Equal(1, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_SemVistoriaELocacaoFinalizada_DaRP06AntesDeNaoAberta()
        {
            Cenario cenario = new Cenario();
            cenario.VistoriasRepo.Vistorias.Clear();
            cenario.Locacao.Status = StatusLocacao.FINALIZADA;

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, erro.Message);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_ComVistoriaELocacaoFinalizadaOuCancelada_Recusa409NaoAberta()
        {
            Cenario cenario = new Cenario();
            cenario.Locacao.Status = StatusLocacao.FINALIZADA;

            BusinessException erroFinalizada = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            cenario.Locacao.Status = StatusLocacao.CANCELADA;
            BusinessException erroCancelada = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            Assert.Equal(ConstantsMessage.ErroContratoLocacaoNaoAberta, erroFinalizada.Message);
            Assert.Equal(ConstantsMessage.ErroContratoLocacaoNaoAberta, erroCancelada.Message);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_LocacaoAtrasada_Permite()
        {
            Cenario cenario = new Cenario();
            cenario.Locacao.Status = StatusLocacao.ATRASADA;

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            Assert.Equal(ConstantsValues.ContratoSituacaoAssinado, retorno.Situacao);
            Assert.Equal(1, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_SoVistoriaDeSaida_DaRP06SemGravar()
        {
            Cenario cenario = new Cenario();
            cenario.VistoriasRepo.Vistorias.Clear();
            cenario.VistoriasRepo.Vistorias.Add(new Vistoria
            {
                IdVistoria = 2, IdLocacao = IdLocacaoDoTeste, IdFuncionario = IdFuncionarioDoTeste, Tipo = TipoVistoria.SAIDA,
                DataHora = Agora, KmRegistrado = 120, NivelCombustivel = 0.5m
            });

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => cenario.AssinarAsync());

            Assert.Equal(ConstantsMessage.ErroContratoSemVistoriaEntrada, erro.Message);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_LocacaoDeOutraLocadora_Recusa404()
        {
            Cenario cenario = new Cenario();
            cenario.Locacao.Veiculo.IdEmpresa = 2;

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => cenario.AssinarAsync());
            NotFoundException erroLeitura = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.ObterAsync(IdLocacaoDoTeste));

            Assert.Equal(ConstantsMessage.ErroLocacaoNaoEncontrada, erro.Message);
            Assert.Equal(ConstantsMessage.ErroLocacaoNaoEncontrada, erroLeitura.Message);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_ComClausula5SemAceiteDeCombustivel_Recusa400ComOTextoAprovado()
        {
            Cenario cenario = new Cenario();
            AssinarContratoLocacaoVO corpo = CorpoValido();
            corpo.AceitePoliticaCombustivel = null;

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() =>
                cenario.Servico.AssinarAsync(IdLocacaoDoTeste, IdFuncionarioDoTeste, corpo));

            Assert.Equal(ConstantsMessage.ErroContratoAceiteObrigatorio, erro.Errors.First().ErrorMessage);
            Assert.Equal(0, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_SemConfiguracaoDaCategoria_AceiteDeCombustivelNaoEPedido()
        {
            Cenario cenario = new Cenario();
            cenario.Cobrancas.Configuracoes.Clear();
            AssinarContratoLocacaoVO corpo = CorpoValido();
            corpo.AceitePoliticaCombustivel = null;

            ReturnContratoLocacaoVO retorno = await cenario.Servico.AssinarAsync(IdLocacaoDoTeste, IdFuncionarioDoTeste, corpo);

            Assert.Null(retorno.Documento.TabelaCobrancas);
            Assert.Null(retorno.Assinatura!.AceitePoliticaCombustivel);
            Assert.False(Assert.Single(cenario.Contratos.Contratos).AceitePoliticaCombustivel);
            Assert.Equal(4, retorno.Documento.Clausulas.Count);
        }

        [Fact]
        public async Task Assinar_CaucaoZero_TiraAClausula4ERenumera()
        {
            Cenario cenario = new Cenario();
            cenario.Locacao.ValorCaucao = 0m;

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, retorno.Documento.Clausulas.Select(x => x.Numero).ToArray());
            Assert.Equal(ConstantsMessage.ContratoTituloClausulaNatureza, retorno.Documento.Clausulas[6].Titulo);
        }

        [Fact]
        public async Task Assinar_DuasChamadasConcorrentes_SoUmaGrava()
        {
            Cenario cenario = new Cenario();
            Task<ReturnContratoLocacaoVO> primeira = cenario.AssinarAsync();
            Task<ReturnContratoLocacaoVO> segunda = cenario.AssinarAsync();

            int sucessos = 0;
            List<BusinessException> recusas = new List<BusinessException>();
            foreach (Task<ReturnContratoLocacaoVO> tentativa in new[] { primeira, segunda })
            {
                try
                {
                    await tentativa;
                    sucessos++;
                }
                catch (BusinessException erro)
                {
                    recusas.Add(erro);
                }
            }

            Assert.Equal(1, sucessos);
            BusinessException recusada = Assert.Single(recusas);
            Assert.Equal(ConstantsMessage.ErroContratoJaAssinado, recusada.Message);
            Assert.Equal(1, cenario.Contratos.Insercoes);
        }

        [Fact]
        public async Task Assinar_FalhaDeBanco_RegistraLogERelanca()
        {
            Cenario cenario = new Cenario();
            cenario.Contratos.FalharAoInserir = new InvalidOperationException("falha de teste");

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.AssinarAsync());

            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.StartsWith(ConstantsMessage.ErroAssinarContratoLocacao, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Log_NaoRegistraNomeCpfImagemNemDocumento()
        {
            Cenario cenario = new Cenario();
            cenario.Contratos.FalharAoInserir = new InvalidOperationException("falha de teste");
            AssinarContratoLocacaoVO corpo = CorpoValido();

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.AssinarAsync(IdLocacaoDoTeste, IdFuncionarioDoTeste, corpo));

            foreach (string entrada in cenario.Log.Snapshot())
            {
                Assert.DoesNotContain(NomeAssinanteDoTeste, entrada);
                Assert.DoesNotContain(CpfDoTeste, entrada);
                Assert.DoesNotContain(corpo.AssinaturaPngBase64, entrada);
                Assert.DoesNotContain("documento", entrada, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task Assinar_DocumentoGuardadoTemOsValoresPrevistosO12()
        {
            Cenario cenario = new Cenario();

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            ContratoDocumentoVO guardado = JsonSerializer.Deserialize<ContratoDocumentoVO>(Assert.Single(cenario.Contratos.Contratos).DocumentoJson, ConstantsValues.ContratoOpcoesJson)!;
            Assert.Equal(2, guardado.Locacao.DiariasPrevistas);
            Assert.Equal(StaticMethods.FormateToMonetary(399.80m), guardado.Locacao.ValorTotalPrevistoStr);
            Assert.Equal(StaticMethods.FormateToMonetary(399.80m), retorno.Documento.Locacao.ValorTotalPrevistoStr);
        }

        [Fact]
        public async Task Assinar_ValorTotalPrevistoGuardadoNaoMudaComADiaria_O12()
        {
            Cenario cenario = new Cenario();
            ReturnContratoLocacaoVO assinado = await cenario.AssinarAsync();

            cenario.Locacao.ValorDiaria = 1000m;
            ReturnContratoLocacaoVO depois = await cenario.Servico.ObterAsync(IdLocacaoDoTeste);

            Assert.Equal(assinado.Documento.Locacao.ValorTotalPrevistoStr, depois.Documento.Locacao.ValorTotalPrevistoStr);
            Assert.Equal(assinado.Documento.Locacao.DiariasPrevistas, depois.Documento.Locacao.DiariasPrevistas);
        }

        [Fact]
        public async Task Assinar_ComCpfMascaradoNoDocumento_NuncaOCompleto()
        {
            Cenario cenario = new Cenario();

            ReturnContratoLocacaoVO retorno = await cenario.AssinarAsync();

            string texto = Serializar(retorno.Documento);
            Assert.DoesNotContain(CpfDoTeste, texto);
            Assert.Contains(StaticMethods.MascararCpf(CpfDoTeste), texto);
        }
    }
}
