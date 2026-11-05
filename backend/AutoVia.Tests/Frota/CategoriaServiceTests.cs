using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do CategoriaService: nome unico entre as ativas, normalizacao de nome e
    /// descricao, categoria desativada tratada como inexistente e paginacao.
    /// Usa o repositorio em memoria (FakeCategoriaRepository) e o log em memoria.
    /// </summary>
    public class CategoriaServiceTests
    {
        [Fact]
        public async Task InsertValido_DevolveIdGeradoEValorFormatado()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "Economico", Descricao = "Carros compactos", ValorDiariaBase = 150m };

            ReturnCategoriaVO retorno = await servico.InsertAsync(vo);

            Assert.True(retorno.IdCategoria > 0);
            Assert.Equal(StaticMethods.FormateToMonetary(150m), retorno.ValorDiariaBaseStr);
        }

        [Fact]
        public async Task InsertComNomeDeAtivaDiferenteSoEmMaiusculasEEspacos_LancaNomeDuplicado()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "SUV", ValorDiariaBase = 200m });
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "  suv ", ValorDiariaBase = 180m };

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNomeDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComNomeDeCategoriaDesativada_EPermitido()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria antiga = new Categoria { Nome = "Minivan", ValorDiariaBase = 120m };
            antiga.Disable();
            repositorio.Adicionar(antiga);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "Minivan", ValorDiariaBase = 130m };

            ReturnCategoriaVO retorno = await servico.InsertAsync(vo);

            Assert.True(retorno.IdCategoria > 0);
            Assert.NotEqual(antiga.IdCategoria, retorno.IdCategoria);
        }

        [Fact]
        public async Task InsertComDescricaoSoEspacos_GravaDescricaoNulaENomeComTrim()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "  Economico  ", Descricao = "   ", ValorDiariaBase = 80m };

            ReturnCategoriaVO retorno = await servico.InsertAsync(vo);

            Assert.Equal("Economico", retorno.Nome);
            Assert.Null(retorno.Descricao);
        }

        [Fact]
        public async Task GetById_IdInexistente_LancaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaService servico = CriarServico(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(99));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task GetById_CategoriaDesativada_LancaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria desativada = new Categoria { Nome = "Antiga", ValorDiariaBase = 90m };
            desativada.Disable();
            repositorio.Adicionar(desativada);
            CategoriaService servico = CriarServico(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(desativada.IdCategoria));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Update_IdInexistente_LancaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 100m };

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(99, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Update_ComNomeDeOutraAtivaDiferenteSoEmCaixa_LancaNomeDuplicado()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "Sedan", ValorDiariaBase = 110m });
            Categoria suv = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "sedan", ValorDiariaBase = 210m };

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.UpdateAsync(suv.IdCategoria, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNomeDuplicado, excecao.Message);
        }

        [Fact]
        public async Task Update_MantendoOProprioNome_AlteraDescricaoEValor()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria suv = new Categoria { Nome = "SUV", Descricao = "Antiga", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", Descricao = "Nova descricao", ValorDiariaBase = 260.5m };

            await servico.UpdateAsync(suv.IdCategoria, vo);
            ReturnCategoriaVO atualizada = await servico.GetByIdAsync(suv.IdCategoria);

            Assert.Equal("Nova descricao", atualizada.Descricao);
            Assert.Equal(260.5m, atualizada.ValorDiariaBase);
        }

        [Fact]
        public async Task GetAll_PaginaSoAtivasOrdenadasPorNome()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "Sedan", ValorDiariaBase = 110m });
            repositorio.Adicionar(new Categoria { Nome = "Economico", ValorDiariaBase = 80m });
            repositorio.Adicionar(new Categoria { Nome = "Minivan", ValorDiariaBase = 120m });
            Categoria antiga = new Categoria { Nome = "Antiga", ValorDiariaBase = 50m };
            antiga.Disable();
            repositorio.Adicionar(antiga);
            CategoriaService servico = CriarServico(repositorio);

            PaginaVO<ReturnCategoriaVO> pagina1 = await servico.GetAllAsync(new FiltroPaginacaoVO { Pagina = 1, TamanhoPagina = 2 });
            List<string> nomesPagina1 = pagina1.Itens.Select(x => x.Nome).ToList();

            PaginaVO<ReturnCategoriaVO> pagina2 = await servico.GetAllAsync(new FiltroPaginacaoVO { Pagina = 2, TamanhoPagina = 2 });
            List<string> nomesPagina2 = pagina2.Itens.Select(x => x.Nome).ToList();

            Assert.Equal(3, pagina1.Total);
            Assert.Equal(2, pagina1.TamanhoPagina);
            Assert.Equal(new List<string> { "Economico", "Minivan" }, nomesPagina1);
            Assert.Equal(3, pagina2.Total);
            Assert.Equal(2, pagina2.Pagina);
            Assert.Equal(new List<string> { "Sedan" }, nomesPagina2);
        }

        [Fact]
        public async Task Update_CategoriaDesativada_LancaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria desativada = new Categoria { Nome = "Antiga", ValorDiariaBase = 90m };
            desativada.Disable();
            repositorio.Adicionar(desativada);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "Nova", ValorDiariaBase = 100m };

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(desativada.IdCategoria, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Update_UsandoNomeDeCategoriaDesativada_EPermitido()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria minivan = new Categoria { Nome = "Minivan", ValorDiariaBase = 120m };
            minivan.Disable();
            repositorio.Adicionar(minivan);
            Categoria suv = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "Minivan", ValorDiariaBase = 210m };

            await servico.UpdateAsync(suv.IdCategoria, vo);
            ReturnCategoriaVO atualizada = await servico.GetByIdAsync(suv.IdCategoria);

            Assert.Equal("Minivan", atualizada.Nome);
        }

        [Fact]
        public async Task Update_GravaNomeComTrimEDescricaoSoEspacosViraNula()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria suv = new Categoria { Nome = "SUV", Descricao = "Antiga", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "  Sedan  ", Descricao = "   ", ValorDiariaBase = 200m };

            await servico.UpdateAsync(suv.IdCategoria, vo);
            ReturnCategoriaVO atualizada = await servico.GetByIdAsync(suv.IdCategoria);

            Assert.Equal("Sedan", atualizada.Nome);
            Assert.Null(atualizada.Descricao);
        }

        [Fact]
        public async Task Update_IdInexistenteComNomeDuplicado_LancaNaoEncontradaAntesDoConflito()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            repositorio.Adicionar(new Categoria { Nome = "SUV", ValorDiariaBase = 200m });
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 180m };

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(99, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Update_RepositorioDevolveFalso_LancaNaoEncontrada()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            Categoria suv = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
            repositorio.Adicionar(suv);
            repositorio.ForcarUpdateFalse = true;
            CategoriaService servico = CriarServico(repositorio);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 210m };

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.UpdateAsync(suv.IdCategoria, vo));

            Assert.Equal(ConstantsMessage.ErroCategoriaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task GetById_FalhaInesperadaDoRepositorio_RelancaMesmaExcecaoERegistraLog()
        {
            FakeCategoriaRepository repositorio = new FakeCategoriaRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            repositorio.ExcecaoAoBuscarPorId = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            CategoriaService servico = CriarServico(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetByIdAsync(1));

            Assert.Same(excecaoSimulada, excecao);
            bool registrou = log.Entries.Any(entrada => entrada.Contains(ConstantsMessage.ErroBuscarCategoria));
            Assert.True(registrou);
        }

        private static CategoriaService CriarServico(FakeCategoriaRepository repositorio, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<CategoriaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            CategoriaService servico = new CategoriaService(repositorio, mapper, log);

            return servico;
        }

        private static CategoriaService CriarServico(FakeCategoriaRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<CategoriaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            CategoriaService servico = new CategoriaService(repositorio, mapper, new FakeLogService());

            return servico;
        }
    }
}
