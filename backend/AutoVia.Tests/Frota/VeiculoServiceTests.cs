using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do VeiculoService (cadastro e edicao): categoria ativa, unicidade de placa,
    /// chassi e renavam entre os ativos, normalizacao, status DISPONIVEL no cadastro,
    /// preservacao do status e da auditoria na edicao e falhas inesperadas logadas.
    /// Usa os repositorios em memoria, o mapper real e o log em memoria.
    /// </summary>
    public class VeiculoServiceTests
    {
        [Fact]
        public async Task InsertValido_DevolveIdStatusDisponivelEValorFormatado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Assert.True(retorno.IdVeiculo > 0);
            Assert.Equal(StatusVeiculo.DISPONIVEL, retorno.Status);
            Assert.Equal(StaticMethods.FormateToMonetary(150m), retorno.ValorDiariaStr);
            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Equal(suv.IdCategoria, retorno.IdCategoria);
        }

        [Fact]
        public async Task InsertNormalizaPlacaChassiRenavamMarcaModeloECor()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "abc-1d23";
            vo.Chassi = "  9bwzzz377vt004251 ";
            vo.Renavam = " 12345678901 ";
            vo.Marca = "  Fiat ";
            vo.Modelo = " Toro  ";
            vo.Cor = "  Azul ";

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Assert.Equal("ABC1D23", retorno.Placa);
            Assert.Equal("9BWZZZ377VT004251", retorno.Chassi);
            Assert.Equal("12345678901", retorno.Renavam);
            Assert.Equal("Fiat", retorno.Marca);
            Assert.Equal("Toro", retorno.Modelo);
            Assert.Equal("Azul", retorno.Cor);
        }

        [Fact]
        public async Task InsertComCorSoEspacos_GravaCorNula()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Cor = "   ";

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Assert.Null(retorno.Cor);
        }

        [Fact]
        public async Task InsertComCategoriaInexistente_LancaNaoEncontradaDaCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(99);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task InsertComCategoriaDesativada_LancaNaoEncontradaDaCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria desativada = new Categoria { Nome = "Antiga", ValorDiariaBase = 90m };
            desativada.Disable();
            categorias.Adicionar(desativada);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(desativada.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task InsertComPlacaDeAtivoEmOutraGrafia_LancaPlacaDuplicada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "1HGCM82633A004352", "98765432100");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "abc-1d23";
            vo.Chassi = "9BWZZZ377VT004251";
            vo.Renavam = "11111111111";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task InsertComChassiDeAtivoEmOutraGrafia_LancaChassiDuplicado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "98765432100");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "XYZ9A87";
            vo.Chassi = "  9bwzzz377vt004251 ";
            vo.Renavam = "11111111111";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoChassiDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComRenavamDeAtivoEmOutraGrafia_LancaRenavamDuplicado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "XYZ9A87";
            vo.Chassi = "1HGCM82633A004352";
            vo.Renavam = " 12345678901 ";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoRenavamDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComPlacaChassiERenavamTodosIguais_LancaPlacaPrimeiro()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task InsertComChassiERenavamDuplicadosENovaPlaca_LancaChassiAntesDoRenavam()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "XYZ9A87";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoChassiDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComDuplicidadeSoDeVeiculoDesativado_EPermitido()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo antigo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901", desativado: true);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Assert.True(retorno.IdVeiculo > 0);
            Assert.NotEqual(antigo.IdVeiculo, retorno.IdVeiculo);
        }

        [Fact]
        public async Task GetByIdExistente_DevolveVeiculoComNomeDaCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());

            ReturnVeiculoVO retorno = await servico.GetByIdAsync(veiculo.IdVeiculo);

            Assert.Equal(veiculo.IdVeiculo, retorno.IdVeiculo);
            Assert.Equal("ABC1D23", retorno.Placa);
            Assert.Equal("SUV", retorno.CategoriaNome);
        }

        [Fact]
        public async Task GetByIdInexistente_LancaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(99));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task GetByIdDesativado_LancaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901", desativado: true);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(veiculo.IdVeiculo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task GetByIdDeVeiculoDeCategoriaDesativada_AindaDevolve()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria antiga = new Categoria { Nome = "Antiga", ValorDiariaBase = 90m };
            antiga.Disable();
            categorias.Adicionar(antiga);
            Veiculo veiculo = CriarVeiculo(veiculos, antiga, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());

            ReturnVeiculoVO retorno = await servico.GetByIdAsync(veiculo.IdVeiculo);

            Assert.Equal("Antiga", retorno.CategoriaNome);
        }

        [Fact]
        public async Task UpdateMantemStatusLocadoEAtualizaOsDadosDoCadastro()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901", StatusVeiculo.LOCADO);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Marca = "Ford";
            vo.KmAtual = 5000;

            await servico.UpdateAsync(veiculo.IdVeiculo, vo);
            ReturnVeiculoVO atualizado = await servico.GetByIdAsync(veiculo.IdVeiculo);

            Assert.Equal(StatusVeiculo.LOCADO, atualizado.Status);
            Assert.Equal("Ford", atualizado.Marca);
            Assert.Equal(5000, atualizado.KmAtual);
        }

        [Fact]
        public async Task UpdateMantemCreatedAtEAtualizaUpdatedAt()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            DateTime criadoEm = new DateTime(2020, 1, 1, 10, 0, 0);
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = suv.IdCategoria,
                Categoria = suv,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                ValorDiaria = 150m,
                CreatedAt = criadoEm,
                UpdatedAt = criadoEm
            };
            veiculos.Adicionar(veiculo);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            await servico.UpdateAsync(veiculo.IdVeiculo, vo);
            Veiculo? depois = await veiculos.FindByIdAsync(veiculo.IdVeiculo);

            Assert.True(depois != null && depois.CreatedAt == criadoEm);
            Assert.True(depois != null && depois.UpdatedAt > criadoEm);
        }

        [Fact]
        public async Task UpdateComAPropriaPlacaEmOutraGrafia_EOk()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "abc-1d23";

            await servico.UpdateAsync(veiculo.IdVeiculo, vo);
            ReturnVeiculoVO atualizado = await servico.GetByIdAsync(veiculo.IdVeiculo);

            Assert.Equal("ABC1D23", atualizado.Placa);
        }

        [Fact]
        public async Task UpdateComPlacaDeOutroVeiculoAtivo_LancaPlacaDuplicada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            Veiculo outro = CriarVeiculo(veiculos, suv, "XYZ9A87", "1HGCM82633A004352", "11111111111");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "abc1d23";
            vo.Chassi = "2HGCM82633A004352";
            vo.Renavam = "22222222222";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.UpdateAsync(outro.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task UpdateComChassiDeOutroVeiculoAtivo_LancaChassiDuplicado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            Veiculo outro = CriarVeiculo(veiculos, suv, "XYZ9A87", "1HGCM82633A004352", "11111111111");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "KLM2B34";
            vo.Chassi = "9bwzzz377vt004251";
            vo.Renavam = "22222222222";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.UpdateAsync(outro.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoChassiDuplicado, excecao.Message);
        }

        [Fact]
        public async Task UpdateInexistente_LancaNaoEncontradaDoVeiculo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(99, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task UpdateDesativado_LancaNaoEncontradaDoVeiculo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901", desativado: true);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(veiculo.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task UpdateComCategoriaInexistente_LancaNaoEncontradaDaCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(99);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(veiculo.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task UpdateComVeiculoInexistenteECategoriaInvalida_LancaNaoEncontradaDoVeiculoPrimeiro()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(99);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(77, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task UpdateQuandoORepositorioNaoGrava_LancaNaoEncontrada()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            veiculos.ForcarUpdateFalse = true;
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(veiculo.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task GetByIdFalhaInesperadaDoRepositorio_RelancaMesmaExcecaoERegistraLog()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            veiculos.ExcecaoAoBuscarPorId = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, categorias, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetByIdAsync(1));

            Assert.Same(excecaoSimulada, excecao);
            bool registrou = log.Entries.Any(entrada => entrada.Contains(ConstantsMessage.ErroBuscarVeiculo));
            Assert.True(registrou);
        }

        [Fact]
        public async Task InsertFalhaInesperadaDaCategoria_RelancaMesmaExcecaoERegistraLog()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            categorias.ExcecaoAoBuscarPorId = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, categorias, log);
            VeiculoVO vo = CriarVOValido(1);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.InsertAsync(vo));

            Assert.Same(excecaoSimulada, excecao);
            bool registrou = log.Entries.Any(entrada => entrada.Contains(ConstantsMessage.ErroSalvarVeiculo));
            Assert.True(registrou);
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository veiculos, FakeCategoriaRepository categorias, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VeiculoProfile>();
                cfg.AddProfile<VeiculoCadastroProfile>();
            }, NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            VeiculoService servico = new VeiculoService(veiculos, categorias, mapper, log);

            return servico;
        }

        private static Categoria CriarCategoria(FakeCategoriaRepository categorias, string nome)
        {
            Categoria categoria = new Categoria { Nome = nome, ValorDiariaBase = 200m };
            categorias.Adicionar(categoria);

            return categoria;
        }

        private static Veiculo CriarVeiculo(
            FakeVeiculoRepository veiculos,
            Categoria categoria,
            string placa,
            string chassi,
            string renavam,
            StatusVeiculo status = StatusVeiculo.DISPONIVEL,
            bool desativado = false)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = categoria.IdCategoria,
                Categoria = categoria,
                Placa = placa,
                Chassi = chassi,
                Renavam = renavam,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 0,
                ValorDiaria = 150m,
                Status = status
            };

            if (desativado)
            {
                veiculo.Disable();
            }

            veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static VeiculoVO CriarVOValido(int idCategoria)
        {
            VeiculoVO vo = new VeiculoVO
            {
                IdCategoria = idCategoria,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                Cor = "Branco",
                KmAtual = 0,
                ValorDiaria = 150m
            };

            return vo;
        }
    }
}
