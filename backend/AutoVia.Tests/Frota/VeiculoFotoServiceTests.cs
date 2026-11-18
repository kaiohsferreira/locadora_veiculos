using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Tests.Fakes;
using FluentValidation;
using System.Globalization;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Servico de fotos do veiculo com os fakes do repositorio, do armazenamento, do processador e do log. Prova a ordem das
    /// checagens (404 sem banco, sessao, veiculo sob o filtro, teto), a compensacao em cada saida depois do envio, o 502 sem
    /// linha gravada, a remocao nas duas ordens de falha, o isolamento por veiculo e por locadora e a lista com uma assinatura em lote.
    /// </summary>
    public sealed class VeiculoFotoServiceTests
    {
        private const int IdVeiculo = 7;
        private const int IdVeiculoDeOutraLocadora = 9;
        private const int IdVeiculoZero = 0;
        private const int IdVeiculoNegativo = -1;
        private const int IdFotoZero = 0;
        private const int IdFuncionario = 3;
        private const int IdEmpresa = 1;
        private const int IdFotoPrimeira = 1;
        private const int IdFotoSegunda = 2;
        private const int IdFotoTerceira = 3;
        private const int IdFotoQuarta = 4;
        private const int IdFotoDeOutroVeiculo = 5;
        private const string PadraoChaveObjeto = @"^veiculos/7/fotos/[0-9a-f]{32}\.jpg$";
        private static readonly byte[] JpegValido = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        /// <summary>Marca presente em toda URL assinada que o FakeArmazenamentoGateway devolve (https://storage.example/...).</summary>
        private const string MarcaUrlAssinadaDeTeste = "storage.example";

        /// <summary>Cenario com os fakes ligados ao servico. Por padrao o veiculo 7 e ativo na locadora 1 e o funcionario e o 3.</summary>
        private sealed class Cenario
        {
            public Cenario(int idFuncionario = IdFuncionario)
            {
                Contexto = CriarContexto(IdEmpresa, idFuncionario);
                Servico = new VeiculoFotoService(Repositorio, Processador, Armazenamento, Contexto, Log);
                Repositorio.VeiculosAtivos.Add(IdVeiculo);
            }

            public FakeVeiculoFotoRepository Repositorio { get; } = new FakeVeiculoFotoRepository();
            public FakeProcessadorFotoService Processador { get; } = new FakeProcessadorFotoService();
            public FakeArmazenamentoGateway Armazenamento { get; } = new FakeArmazenamentoGateway();
            public FakeLogService Log { get; } = new FakeLogService();
            public IContextoEmpresa Contexto { get; }
            public VeiculoFotoService Servico { get; }
        }

        private static IContextoEmpresa CriarContexto(int idEmpresa, int idFuncionario)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            if (idFuncionario > 0)
            {
                contexto.Definir(idEmpresa, idFuncionario, CargoFuncionario.ATENDENTE, false);
            }

            return contexto;
        }

        private static VeiculoFoto CriarFotoAtiva(int idFoto, int ordem, int idVeiculo)
        {
            return new VeiculoFoto
            {
                IdVeiculoFoto = idFoto,
                IdVeiculo = idVeiculo,
                Ordem = ordem,
                ChaveObjeto = "veiculos/" + idVeiculo.ToString(CultureInfo.InvariantCulture) + "/fotos/chave" + idFoto.ToString(CultureInfo.InvariantCulture) + ".jpg",
                IdFuncionarioEnvio = IdFuncionario
            };
        }

        private static void PreencherFotosAtivas(Cenario cenario, int quantidade)
        {
            for (int i = 1; i <= quantidade; i++)
            {
                cenario.Repositorio.Fotos.Add(CriarFotoAtiva(i, i, IdVeiculo));
            }
        }

        private static string MensagemLimite()
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoLimiteAtingido, ConstantsValues.MaximoFotosPorVeiculo);
        }

        /// <summary>Cada item da listagem traz url OU erroFoto, exatamente um deles; a miniatura segue nula nesta fatia.</summary>
        private static void AssertUmDosDoisEstados(ReturnVeiculoFotoVO foto)
        {
            bool temUrl = foto.Url != null;
            bool temErro = foto.ErroFoto != null;
            Assert.NotEqual(temUrl, temErro);
            Assert.Null(foto.UrlMiniatura);
        }

        /// <summary>Verifica que o item sem URL nao leva URL nem prazo e leva o texto do item, comparado pela constante.</summary>
        private static void AssertItemSemUrl(ReturnVeiculoFotoVO foto)
        {
            Assert.Null(foto.Url);
            Assert.Null(foto.ExpiraEm);
            Assert.Equal(ConstantsMessage.ErroVeiculoFotoSemArquivo, foto.ErroFoto);
        }

        [Fact]
        public async Task Enviar_IdVeiculoZeroOuNegativo_Da404SemConsultarOBanco()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.VeiculosAtivos.Add(IdVeiculoZero);
            cenario.Repositorio.VeiculosAtivos.Add(IdVeiculoNegativo);

            NotFoundException zero = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.EnviarAsync(IdVeiculoZero, JpegValido, ConstantsArmazenamento.MimeJpeg));
            NotFoundException negativo = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.EnviarAsync(IdVeiculoNegativo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, zero.Message);
            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, negativo.Message);
            Assert.Equal(0, cenario.Processador.ChamadasCount);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_VeiculoInexistenteOuDeOutraLocadora_Da404SemProcessarNemEnviar()
        {
            Cenario cenario = new Cenario();

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.EnviarAsync(IdVeiculoDeOutraLocadora, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, ex.Message);
            Assert.Equal(0, cenario.Processador.ChamadasCount);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_SemSessao_Da401SemTocarNoArmazenamento()
        {
            Cenario cenario = new Cenario(idFuncionario: 0);

            UnauthorizedException ex = await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroSessaoInvalida, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
        }

        [Fact]
        public async Task Enviar_ComQuatorzeFotosAtivas_GravaADecimaQuinta()
        {
            Cenario cenario = new Cenario();
            PreencherFotosAtivas(cenario, 14);

            ReturnVeiculoFotoVO vo = await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Equal(15, cenario.Repositorio.Fotos.Count);
            Assert.Equal(15, vo.Ordem);
            Assert.Equal(1, cenario.Armazenamento.UploadCount);
        }

        [Fact]
        public async Task Enviar_ComQuinzeFotosAtivas_Da409SemProcessarNemEnviar()
        {
            Cenario cenario = new Cenario();
            PreencherFotosAtivas(cenario, 15);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(MensagemLimite(), ex.Message);
            Assert.Equal(0, cenario.Processador.ChamadasCount);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_ComDezesseisFotosAtivas_Da409()
        {
            Cenario cenario = new Cenario();
            PreencherFotosAtivas(cenario, 16);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(MensagemLimite(), ex.Message);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_SegundaConferenciaReprova_RemoveOObjetoEDa409SemGravar()
        {
            Cenario cenario = new Cenario();
            List<VeiculoFoto> quinzeAtivas = new List<VeiculoFoto>();
            for (int i = 1; i <= 15; i++)
            {
                quinzeAtivas.Add(CriarFotoAtiva(i, i, IdVeiculo));
            }

            cenario.Repositorio.FotosNaProximaLeitura = quinzeAtivas;

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(MensagemLimite(), ex.Message);
            Assert.Equal(1, cenario.Armazenamento.UploadCount);
            Assert.Equal(cenario.Armazenamento.UploadKeys, cenario.Armazenamento.RemoveKeys);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_FalhaDoArmazenamento_Da502SemLinhaNemRemocao()
        {
            Cenario cenario = new Cenario();
            cenario.Armazenamento.FalharEnvioIntegracao = true;

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Repositorio.Fotos);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
            List<string> entradas = cenario.Log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroEnviarFotoVeiculo, entradas[0], StringComparison.Ordinal);
            Assert.All(entradas, entrada => Assert.DoesNotContain(cenario.Armazenamento.UploadKeys[0], entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain("https://", entrada, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Enviar_EnvioValido_SobeOConteudoRecodificadoNaoOOriginal()
        {
            Cenario cenario = new Cenario();

            await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Single(cenario.Armazenamento.UploadConteudos);
            Assert.Equal(FakeProcessadorFotoService.ConteudoRecodificado, cenario.Armazenamento.UploadConteudos[0]);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, cenario.Armazenamento.UploadContentTypes[0]);
        }

        [Fact]
        public async Task Enviar_CancelamentoNaSegundaLeitura_RegistraERemoveORelanca()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.CancelarProximaLeitura = true;

            await Assert.ThrowsAsync<OperationCanceledException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(cenario.Armazenamento.UploadKeys, cenario.Armazenamento.RemoveKeys);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            List<string> entradas = cenario.Log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.StartsWith(ConstantsMessage.ErroEnviarFotoVeiculo, StringComparison.Ordinal));
            Assert.All(entradas, entrada => Assert.DoesNotContain(cenario.Armazenamento.UploadKeys[0], entrada));
        }

        [Fact]
        public async Task Enviar_FalhaDaGravacao_RemoveOObjetoEDeixaSubirAExcecao()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharInsert = true;

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal("Gravacao falhou", ex.Message);
            Assert.Equal(cenario.Armazenamento.UploadKeys, cenario.Armazenamento.RemoveKeys);
            Assert.Empty(cenario.Repositorio.Fotos);
        }

        [Fact]
        public async Task Enviar_FalhaDaSegundaLeitura_RemoveOObjetoEDeixaSubirAExcecao()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharProximaLeitura = true;

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal("Leitura falhou", ex.Message);
            Assert.Equal(cenario.Armazenamento.UploadKeys, cenario.Armazenamento.RemoveKeys);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
        }

        [Fact]
        public async Task Enviar_FalhaDaRemocaoNaCompensacao_SobeAFalhaDoArmazenamento()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharInsert = true;
            cenario.Armazenamento.FalharRemocao = true;

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoRemover, ex.Message);
            Assert.Empty(cenario.Repositorio.Fotos);
        }

        [Fact]
        public async Task Enviar_CompensacaoRecebeCancellationTokenNone_MesmoComOChamadorCancelado()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharInsert = true;
            System.Threading.CancellationTokenSource origem = new System.Threading.CancellationTokenSource();
            origem.Cancel();

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg, origem.Token));

            Assert.Equal(System.Threading.CancellationToken.None, cenario.Armazenamento.UltimoTokenRemocao);
        }

        [Fact]
        public async Task Enviar_RecusaDaImagem_NaoEnviaNemGrava()
        {
            Cenario cenario = new Cenario();
            cenario.Processador.Falha = new ValidationException(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);

            await Assert.ThrowsAsync<ValidationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_FilaDoProcessadorCheia_Da429SemEnviar()
        {
            Cenario cenario = new Cenario();
            cenario.Processador.Falha = new LimiteExcedidoException(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado);

            LimiteExcedidoException ex = await Assert.ThrowsAsync<LimiteExcedidoException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(0, cenario.Repositorio.InsertCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Enviar_EnvioValido_GravaLinhaComChaveDoContratoEAutoriaDoContexto()
        {
            Cenario cenario = new Cenario();

            ReturnVeiculoFotoVO vo = await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Single(cenario.Repositorio.Fotos);
            VeiculoFoto gravada = cenario.Repositorio.Fotos[0];
            Assert.Matches(PadraoChaveObjeto, gravada.ChaveObjeto);
            Assert.Equal(gravada.ChaveObjeto, cenario.Armazenamento.UploadKeys[0]);
            Assert.Equal(IdVeiculo, gravada.IdVeiculo);
            Assert.Equal(1, gravada.Ordem);
            Assert.Equal(IdFuncionario, gravada.IdFuncionarioEnvio);
            Assert.Null(gravada.IdFuncionarioRemocao);
            Assert.Null(gravada.ChaveMiniatura);
            Assert.Equal(gravada.IdVeiculoFoto, vo.IdVeiculoFoto);
            Assert.Equal(1, vo.Ordem);
            Assert.Null(vo.UrlMiniatura);
            Assert.StartsWith("https://", vo.Url);
        }

        [Fact]
        public async Task Enviar_ExpiraEmSaiEmUtcComFuso()
        {
            Cenario cenario = new Cenario();

            ReturnVeiculoFotoVO vo = await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.NotNull(vo.Url);
            Assert.Null(vo.ErroFoto);
            Assert.NotNull(vo.ExpiraEm);
            DateTimeOffset expiraEm = DateTimeOffset.Parse(vo.ExpiraEm, CultureInfo.InvariantCulture, DateTimeStyles.None);
            Assert.Equal(TimeSpan.Zero, expiraEm.Offset);
        }

        [Fact]
        public async Task Enviar_DuasVezesSeguidas_GeraChavesDiferentes()
        {
            Cenario cenario = new Cenario();

            await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);
            await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Equal(2, cenario.Repositorio.Fotos.Count);
            Assert.NotEqual(cenario.Repositorio.Fotos[0].ChaveObjeto, cenario.Repositorio.Fotos[1].ChaveObjeto);
            Assert.Matches(PadraoChaveObjeto, cenario.Repositorio.Fotos[1].ChaveObjeto);
        }

        [Fact]
        public async Task Enviar_OrdemEhAMaiorAtivaMaisUmMesmoComBuraco()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(1, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(2, 2, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(3, 5, IdVeiculo));

            ReturnVeiculoFotoVO vo = await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Equal(6, vo.Ordem);
        }

        [Fact]
        public async Task Enviar_PassaAoProcessadorOTokenQueRecebeu()
        {
            Cenario cenario = new Cenario();
            System.Threading.CancellationTokenSource origem = new System.Threading.CancellationTokenSource();

            await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg, origem.Token);

            Assert.Equal(origem.Token, cenario.Processador.UltimoToken);
        }

        [Fact]
        public async Task Enviar_SucessoFazExatamenteTresIdasAoBanco()
        {
            Cenario cenario = new Cenario();

            await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Equal(1, cenario.Repositorio.ContarAtivasCount);
            Assert.Equal(1, cenario.Repositorio.ResumoCount);
            Assert.Equal(1, cenario.Repositorio.InsertCount);
            Assert.Equal(0, cenario.Repositorio.VeiculoAtivoExisteCount);
            Assert.Equal(0, cenario.Repositorio.CountAtivasCount);
            Assert.Equal(0, cenario.Repositorio.FindAtivasCount);
            Assert.Equal(3, cenario.Repositorio.TotalChamadas);
        }

        [Fact]
        public async Task Enviar_VeiculoInexistenteComTetoCheio_Da404AntesDo409SemProcessar()
        {
            Cenario cenario = new Cenario();
            for (int i = 1; i <= 15; i++)
            {
                cenario.Repositorio.Fotos.Add(CriarFotoAtiva(i, i, IdVeiculoDeOutraLocadora));
            }

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.EnviarAsync(IdVeiculoDeOutraLocadora, JpegValido, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, ex.Message);
            Assert.Equal(0, cenario.Processador.ChamadasCount);
            Assert.Equal(0, cenario.Armazenamento.UploadCount);
            Assert.Equal(1, cenario.Repositorio.ContarAtivasCount);
            Assert.Equal(0, cenario.Repositorio.ResumoCount);
        }

        [Fact]
        public async Task Enviar_FotoDesativadaNaoContaNemEntraNaOrdem()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            VeiculoFoto desativada = CriarFotoAtiva(IdFotoSegunda, 9, IdVeiculo);
            desativada.Disable();
            cenario.Repositorio.Fotos.Add(desativada);

            ReturnVeiculoFotoVO vo = await cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg);

            Assert.Equal(2, vo.Ordem);
        }

        [Fact]
        public async Task Listar_SemFoto_DevolveListaVaziaSemChamarOArmazenamento()
        {
            Cenario cenario = new Cenario();

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Empty(lista);
            Assert.Equal(0, cenario.Armazenamento.AssinaturaLoteCount);
            Assert.Equal(0, cenario.Armazenamento.HeadCount);
        }

        [Fact]
        public async Task Listar_ComTresFotos_DevolveEmOrdemComUmaAssinaturaEmLote()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 3, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoTerceira, 2, IdVeiculo));

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Equal(new int[] { IdFotoPrimeira, IdFotoTerceira, IdFotoSegunda }, lista.Select(foto => foto.IdVeiculoFoto).ToArray());
            Assert.Equal(1, cenario.Armazenamento.AssinaturaLoteCount);
            Assert.Equal(3, cenario.Armazenamento.ChavesAssinadas.Count);
            Assert.Equal(0, cenario.Armazenamento.HeadCount);
            Assert.All(lista, foto => Assert.StartsWith("https://", foto.Url));
        }

        [Fact]
        public async Task Listar_VeiculoDeOutraLocadora_Da404()
        {
            Cenario cenario = new Cenario();

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetAllAsync(IdVeiculoDeOutraLocadora));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.AssinaturaLoteCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Listar_IdVeiculoZero_Da404MesmoComVeiculoZeroAtivoNoFake()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.VeiculosAtivos.Add(IdVeiculoZero);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetAllAsync(IdVeiculoZero));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, ex.Message);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Listar_FalhaDaAssinatura_SobeAIntegrationException()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Armazenamento.FalharAssinatura = true;

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GetAllAsync(IdVeiculo));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoObterUrl, ex.Message);
        }

        [Fact]
        public async Task Listar_ChaveAusenteNoMeio_DevolveOsTresItensNaOrdemComOMeioEmErro()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 2, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoTerceira, 3, IdVeiculo));
            cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add(cenario.Repositorio.Fotos[1].ChaveObjeto);

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Equal(new int[] { IdFotoPrimeira, IdFotoSegunda, IdFotoTerceira }, lista.Select(foto => foto.IdVeiculoFoto).ToArray());
            Assert.NotNull(lista[0].Url);
            Assert.NotNull(lista[0].ExpiraEm);
            Assert.Null(lista[0].ErroFoto);
            AssertItemSemUrl(lista[1]);
            Assert.NotNull(lista[2].Url);
            Assert.NotNull(lista[2].ExpiraEm);
            Assert.Null(lista[2].ErroFoto);
            Assert.All(lista, AssertUmDosDoisEstados);
            Assert.Equal(1, cenario.Armazenamento.AssinaturaLoteCount);
            Assert.Equal(0, cenario.Armazenamento.HeadCount);
        }

        [Fact]
        public async Task Listar_PrimeiraFotoSemArquivo_FicaNaPosicaoZeroComErroESemDerrubarALista()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 2, IdVeiculo));
            cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add(cenario.Repositorio.Fotos[0].ChaveObjeto);

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Equal(2, lista.Count);
            Assert.Equal(IdFotoPrimeira, lista[0].IdVeiculoFoto);
            AssertItemSemUrl(lista[0]);
            Assert.Equal(IdFotoSegunda, lista[1].IdVeiculoFoto);
            Assert.NotNull(lista[1].Url);
            Assert.Null(lista[1].ErroFoto);
            Assert.All(lista, AssertUmDosDoisEstados);
        }

        [Fact]
        public async Task Listar_TodasSemArquivo_DevolveTodasComErroSemExcecao()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 2, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoTerceira, 3, IdVeiculo));
            foreach (VeiculoFoto foto in cenario.Repositorio.Fotos)
            {
                cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add(foto.ChaveObjeto);
            }

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Equal(3, lista.Count);
            Assert.All(lista, AssertItemSemUrl);
            Assert.All(lista, AssertUmDosDoisEstados);
        }

        [Fact]
        public async Task Listar_DuasSemArquivoEmQuatro_GeraUmRegistroComAContagemSemChaveNemUrl()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 2, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoTerceira, 3, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoQuarta, 4, IdVeiculo));
            cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add(cenario.Repositorio.Fotos[0].ChaveObjeto);
            cenario.Armazenamento.ChavesOmitidasNaAssinatura.Add(cenario.Repositorio.Fotos[2].ChaveObjeto);

            await cenario.Servico.GetAllAsync(IdVeiculo);

            string esperado = ConstantsMessage.ErroListarFotosVeiculo + " | " + string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoListaSemUrlAssinada, 2);
            List<string> entradas = cenario.Log.Snapshot();
            Assert.Equal(new List<string> { esperado }, entradas);
            Assert.All(cenario.Repositorio.Fotos, foto => Assert.All(entradas, entrada => Assert.DoesNotContain(foto.ChaveObjeto, entrada)));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }

        [Fact]
        public async Task Listar_TodasComArquivo_NaoGeraNenhumRegistro()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoSegunda, 2, IdVeiculo));

            List<ReturnVeiculoFotoVO> lista = await cenario.Servico.GetAllAsync(IdVeiculo);

            Assert.Equal(2, lista.Count);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Listar_ExcecaoNaoPrevistaNaAssinatura_SobeEGravaORegistroDeListagem()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Armazenamento.LancarExcecaoGenericaAssinatura = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GetAllAsync(IdVeiculo));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroListarFotosVeiculo + " | ", entradas[0]);
        }

        [Fact]
        public async Task Listar_CancelamentoNaAssinatura_SobeEGravaORegistroDeListagem()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            cenario.Armazenamento.LancarCancelamentoAssinatura = true;

            await Assert.ThrowsAsync<OperationCanceledException>(() => cenario.Servico.GetAllAsync(IdVeiculo));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroListarFotosVeiculo + " | ", entradas[0]);
        }

        [Fact]
        public async Task Remover_FotoDoVeiculo_ApagaOObjetoEDesativaComQuemRemoveu()
        {
            Cenario cenario = new Cenario();
            VeiculoFoto foto = CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo);
            cenario.Repositorio.Fotos.Add(foto);

            await cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira);

            Assert.Equal(new List<string> { foto.ChaveObjeto }, cenario.Armazenamento.RemoveKeys);
            Assert.Equal(1, cenario.Repositorio.DesativarCount);
            Assert.NotNull(foto.DisabledAt);
            Assert.Equal(IdFuncionario, foto.IdFuncionarioRemocao);
        }

        [Fact]
        public async Task Remover_FalhaDoArmazenamento_Da502ENadaMuda()
        {
            Cenario cenario = new Cenario();
            VeiculoFoto foto = CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo);
            cenario.Repositorio.Fotos.Add(foto);
            cenario.Armazenamento.FalharRemocao = true;

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoRemover, ex.Message);
            Assert.Equal(0, cenario.Repositorio.DesativarCount);
            Assert.Null(foto.DisabledAt);
            List<string> entradas = cenario.Log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroRemoverFotoVeiculo, entradas[0], StringComparison.Ordinal);
            Assert.All(entradas, entrada => Assert.DoesNotContain(foto.ChaveObjeto, entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain("https://", entrada, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Remover_FalhaDaDesativacaoDepoisDoObjeto_SobeEOObjetoJaSaiu()
        {
            Cenario cenario = new Cenario();
            VeiculoFoto foto = CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo);
            cenario.Repositorio.Fotos.Add(foto);
            cenario.Repositorio.FalharDesativar = true;

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira));

            Assert.Equal("Desativacao falhou", ex.Message);
            Assert.Equal(new List<string> { foto.ChaveObjeto }, cenario.Armazenamento.RemoveKeys);
        }

        [Fact]
        public async Task Remover_Repetido_Da404SemNovaRemocaoNoArmazenamento()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));
            await cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoNaoEncontrada, ex.Message);
            Assert.Equal(1, cenario.Armazenamento.DeleteCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Remover_FotoDeOutroVeiculo_Da404SemApagarNada()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoDeOutroVeiculo, 1, IdVeiculoDeOutraLocadora));

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoDeOutroVeiculo));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoNaoEncontrada, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
            Assert.Equal(0, cenario.Repositorio.DesativarCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Remover_VeiculoDeOutraLocadora_Da404SemLerNemApagar()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculoDeOutraLocadora));

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.RemoverAsync(IdVeiculoDeOutraLocadora, IdFotoPrimeira));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Remover_IdFotoZero_Da404SemApagarNada()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoZero, 1, IdVeiculo));

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoZero));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoNaoEncontrada, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
            Assert.Empty(cenario.Log.Entries);
        }

        [Fact]
        public async Task Remover_SemSessao_Da401SemApagarNada()
        {
            Cenario cenario = new Cenario(idFuncionario: 0);
            cenario.Repositorio.Fotos.Add(CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo));

            UnauthorizedException ex = await Assert.ThrowsAsync<UnauthorizedException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira));

            Assert.Equal(ConstantsMessage.ErroSessaoInvalida, ex.Message);
            Assert.Equal(0, cenario.Armazenamento.DeleteCount);
        }

        [Fact]
        public async Task LogDaFalhaDeEnvio_TemOErroDeEnvioExatoSemChaveNemUrlNemMensagemDoProvedor()
        {
            Cenario cenario = new Cenario();
            cenario.Armazenamento.FalharEnvioIntegracao = true;

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Equal(new List<string> { ConstantsMessage.ErroEnviarFotoVeiculo + " | " + ConstantsMessage.ErroArmazenamentoEnviar }, entradas);
            Assert.All(entradas, entrada => Assert.DoesNotContain(cenario.Armazenamento.UploadKeys[0], entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }

        [Fact]
        public async Task LogDaFalhaDaGravacao_TemOErroDeEnvioSemChaveNemUrl()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharInsert = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.NotEmpty(entradas);
            Assert.All(entradas, entrada => Assert.Equal(ConstantsMessage.ErroEnviarFotoVeiculo + " | Gravacao falhou", entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(cenario.Armazenamento.UploadKeys[0], entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }

        [Fact]
        public async Task LogDaFalhaDaRemocaoNaCompensacao_TemSoErrosDeEnvioERemocaoSemChaveNemUrl()
        {
            Cenario cenario = new Cenario();
            cenario.Repositorio.FalharInsert = true;
            cenario.Armazenamento.FalharRemocao = true;

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.EnviarAsync(IdVeiculo, JpegValido, ConstantsArmazenamento.MimeJpeg));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Contains(ConstantsMessage.ErroEnviarFotoVeiculo + " | " + ConstantsMessage.ErroArmazenamentoRemover, entradas);
            Assert.All(entradas, entrada => Assert.StartsWith(ConstantsMessage.ErroEnviarFotoVeiculo, entrada, StringComparison.Ordinal));
            Assert.All(entradas, entrada => Assert.DoesNotContain(cenario.Armazenamento.UploadKeys[0], entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }

        [Fact]
        public async Task LogDaFalhaDaAssinaturaNaListagem_TemOErroDeListagemExatoSemChaveNemUrl()
        {
            Cenario cenario = new Cenario();
            VeiculoFoto foto = CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo);
            cenario.Repositorio.Fotos.Add(foto);
            cenario.Armazenamento.FalharAssinatura = true;

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GetAllAsync(IdVeiculo));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Equal(new List<string> { ConstantsMessage.ErroListarFotosVeiculo + " | " + ConstantsMessage.ErroArmazenamentoObterUrl }, entradas);
            Assert.All(entradas, entrada => Assert.DoesNotContain(foto.ChaveObjeto, entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }

        [Fact]
        public async Task LogDaFalhaDaRemocao_TemOErroDeRemocaoExatoSemChaveNemUrl()
        {
            Cenario cenario = new Cenario();
            VeiculoFoto foto = CriarFotoAtiva(IdFotoPrimeira, 1, IdVeiculo);
            cenario.Repositorio.Fotos.Add(foto);
            cenario.Armazenamento.FalharRemocao = true;

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.RemoverAsync(IdVeiculo, IdFotoPrimeira));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Equal(new List<string> { ConstantsMessage.ErroRemoverFotoVeiculo + " | " + ConstantsMessage.ErroArmazenamentoRemover }, entradas);
            Assert.All(entradas, entrada => Assert.DoesNotContain(foto.ChaveObjeto, entrada));
            Assert.All(entradas, entrada => Assert.DoesNotContain(MarcaUrlAssinadaDeTeste, entrada));
        }
    }
}
