using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
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
        public async Task InsertDevolveDadosDaLocadoraEDaCategoriaMesmoSemNavegacaoNaGravacao()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Empresa locadora = new Empresa { IdEmpresa = 7, NomeFantasia = "Locadora Sul", Cidade = "Curitiba", Uf = "PR" };
            veiculos.RegistrarEmpresa(locadora);
            veiculos.RegistrarCategoria(suv);
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService(), ContextoEmpresaDeTeste.Criar(7));
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Veiculo? gravado = veiculos.UltimoVeiculoInserido;
            Assert.NotNull(gravado);
            Assert.Null(gravado.Empresa);
            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Equal("Locadora Sul", retorno.LocadoraNomeFantasia);
            Assert.Equal("Curitiba", retorno.LocadoraCidade);
            Assert.Equal("PR", retorno.LocadoraUf);
        }

        [Fact]
        public async Task Insert_GravaAEmpresaDoContextoNuncaADoCorpo()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService(), ContextoEmpresaDeTeste.Criar(7));
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            await servico.InsertAsync(vo);

            Veiculo gravado = Assert.Single(veiculos.Veiculos);
            Assert.Equal(7, gravado.IdEmpresa);
            Assert.Equal(StatusVeiculo.DISPONIVEL, gravado.Status);
        }

        [Fact]
        public async Task Insert_SemContextoDeEmpresa_RecusaAntesDeGravarComSessaoInvalida()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService(), ContextoEmpresaDeTeste.Vazio());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroSessaoInvalida, excecao.Message);
            Assert.Empty(veiculos.Veiculos);
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
        public async Task InsertGravaFornecedorENotaFiscalECamposVoltamNoRetornoEnaConsulta()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Fornecedor = "  Locadora Atlas Motors ";
            vo.NumeroNotaFiscalCompra = " 000.123-4 ";

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);
            ReturnVeiculoVO consultado = await servico.GetByIdAsync(retorno.IdVeiculo);

            Assert.Equal("Locadora Atlas Motors", retorno.Fornecedor);
            Assert.Equal("000.123-4", retorno.NumeroNotaFiscalCompra);
            Assert.Equal("Locadora Atlas Motors", consultado.Fornecedor);
            Assert.Equal("000.123-4", consultado.NumeroNotaFiscalCompra);
        }

        [Fact]
        public async Task InsertComFornecedorENotaSoComEspacos_GravaNulos()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Fornecedor = "   ";
            vo.NumeroNotaFiscalCompra = string.Empty;

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);

            Assert.Null(retorno.Fornecedor);
            Assert.Null(retorno.NumeroNotaFiscalCompra);
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
        public async Task UpdateSobrescreveFornecedorENotaFiscalEVazioLimpa()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO criacao = CriarVOValido(suv.IdCategoria);
            criacao.Fornecedor = "Fornecedor antigo";
            criacao.NumeroNotaFiscalCompra = "NF-1";
            ReturnVeiculoVO criado = await servico.InsertAsync(criacao);

            VeiculoVO troca = CriarVOValido(suv.IdCategoria);
            troca.Fornecedor = "Fornecedor novo";
            troca.NumeroNotaFiscalCompra = "NF-2";
            await servico.UpdateAsync(criado.IdVeiculo, troca);
            ReturnVeiculoVO trocado = await servico.GetByIdAsync(criado.IdVeiculo);

            VeiculoVO limpeza = CriarVOValido(suv.IdCategoria);
            limpeza.Fornecedor = "  ";
            limpeza.NumeroNotaFiscalCompra = null;
            await servico.UpdateAsync(criado.IdVeiculo, limpeza);
            ReturnVeiculoVO limpo = await servico.GetByIdAsync(criado.IdVeiculo);

            Assert.Equal("Fornecedor novo", trocado.Fornecedor);
            Assert.Equal("NF-2", trocado.NumeroNotaFiscalCompra);
            Assert.Null(limpo.Fornecedor);
            Assert.Null(limpo.NumeroNotaFiscalCompra);
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

        [Fact]
        public async Task UpdateComRenavamDeOutroVeiculoAtivo_LancaRenavamDuplicado()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            Veiculo outro = CriarVeiculo(veiculos, suv, "XYZ9A87", "1HGCM82633A004352", "11111111111");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);
            vo.Placa = "KLM2B34";
            vo.Chassi = "2HGCM82633A004352";
            vo.Renavam = "12345678901";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.UpdateAsync(outro.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoRenavamDuplicado, excecao.Message);
        }

        [Fact]
        public async Task UpdateComCategoriaDesativada_LancaNaoEncontradaDaCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            Categoria desativada = new Categoria { Nome = "Antiga", ValorDiariaBase = 90m };
            desativada.Disable();
            categorias.Adicionar(desativada);
            Veiculo veiculo = CriarVeiculo(veiculos, suv, "ABC1D23", "9BWZZZ377VT004251", "12345678901");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(desativada.IdCategoria);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(veiculo.IdVeiculo, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task InsertGravaCreatedAtIgualAoUpdatedAt()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            ReturnVeiculoVO retorno = await servico.InsertAsync(vo);
            Veiculo? gravado = await veiculos.FindByIdAsync(retorno.IdVeiculo);

            Assert.NotNull(gravado);
            Assert.Equal(gravado.CreatedAt, gravado.UpdatedAt);
        }

        [Fact]
        public async Task InsertPassaAoRepositorioEntidadeSemNavegacaoDeCategoria()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService());
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            await servico.InsertAsync(vo);

            Assert.NotNull(veiculos.UltimoVeiculoInserido);
            Assert.Null(veiculos.UltimoVeiculoInserido.Categoria);
        }

        [Fact]
        public async Task FalhaInesperadaNaGravacaoDoInsert_RelancaMesmaExcecaoERegistraLog()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            veiculos.ExcecaoNaGravacao = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, categorias, log);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.InsertAsync(vo));

            Assert.Same(excecaoSimulada, excecao);
            bool registrou = log.Entries.Any(entrada => entrada.Contains(ConstantsMessage.ErroSalvarVeiculo));
            Assert.True(registrou);
        }

        [Fact]
        public async Task FalhaNaLeituraDeDuplicidade_RelancaMesmaExcecaoERegistraErroDeBusca()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            veiculos.ExcecaoAoVerificarDuplicidade = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            VeiculoService servico = CriarServico(veiculos, categorias, log);
            VeiculoVO vo = CriarVOValido(suv.IdCategoria);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.InsertAsync(vo));

            Assert.Same(excecaoSimulada, excecao);
            bool registrouBusca = log.Entries.Any(entrada => entrada.Contains(ConstantsMessage.ErroBuscarVeiculo));
            Assert.True(registrouBusca);
        }

        /// <summary>POST de veiculo: a resposta traz fotoCapa e erroFotoCapa nulos e o armazenamento nunca e chamado (o veiculo novo nao tem foto).</summary>
        [Fact]
        public async Task InsertValido_NaoPreencheACapaNemChamaOArmazenamento()
        {
            FakeVeiculoRepository veiculos = new FakeVeiculoRepository();
            FakeCategoriaRepository categorias = new FakeCategoriaRepository();
            Categoria suv = CriarCategoria(categorias, "SUV");
            veiculos.FotosAoGravar = new List<VeiculoFoto>
            {
                new VeiculoFoto { IdVeiculoFoto = 1, Ordem = 1, ChaveObjeto = "veiculos/1/fotos/1.jpg", IdFuncionarioEnvio = 1 }
            };
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            VeiculoService servico = CriarServico(veiculos, categorias, new FakeLogService(), null, gateway);

            ReturnVeiculoVO retorno = await servico.InsertAsync(CriarVOValido(suv.IdCategoria));

            Assert.Single(veiculos.Veiculos.Single(x => x.IdVeiculo == retorno.IdVeiculo).Fotos);
            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(0, gateway.AssinaturaLoteCount);
            Assert.Equal(0, gateway.HeadCount);
        }

        private static VeiculoService CriarServico(FakeVeiculoRepository veiculos, FakeCategoriaRepository categorias, FakeLogService log, IContextoEmpresa? contexto = null, FakeArmazenamentoGateway? gateway = null)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<VeiculoProfile>();
                cfg.AddProfile<VeiculoCadastroProfile>();
            }, NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            VeiculoService servico = new VeiculoService(veiculos, categorias, mapper, log, contexto ?? ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(gateway ?? new FakeArmazenamentoGateway(), log));

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
