using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio da categoria de veiculos: nome unico entre as ativas (409),
    /// categoria inexistente ou desativada (404), normalizacao de nome e descricao.
    /// Usa somente ICategoriaRepository; o validador de entrada roda antes, no filtro.
    /// </summary>
    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public CategoriaService(ICategoriaRepository categoriaRepository, IMapper mapper, ILogService logService)
        {
            _categoriaRepository = categoriaRepository;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>Lista as categorias ativas, paginadas e ordenadas por nome.</summary>
        public async Task<PaginaVO<ReturnCategoriaVO>> GetAllAsync(FiltroPaginacaoVO filtro)
        {
            try
            {
                int total = await _categoriaRepository.CountAsync();
                List<Categoria> categorias = await _categoriaRepository.FindAllAsync(filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnCategoriaVO> itens = _mapper.Map<List<ReturnCategoriaVO>>(categorias);
                PaginaVO<ReturnCategoriaVO> pagina = new PaginaVO<ReturnCategoriaVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

                return pagina;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCategorias, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta uma categoria ativa pelo id; lanca NotFoundException se nao existir.</summary>
        public async Task<ReturnCategoriaVO> GetByIdAsync(int idCategoria)
        {
            try
            {
                Categoria? categoria = await _categoriaRepository.FindByIdAsync(idCategoria);

                if (categoria == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCategoriaNaoEncontrada);
                }

                ReturnCategoriaVO retorno = _mapper.Map<ReturnCategoriaVO>(categoria);

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCategoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cadastra uma nova categoria; lanca BusinessException se o nome ja existe entre as ativas.</summary>
        public async Task<ReturnCategoriaVO> InsertAsync(CategoriaVO categoriaVO)
        {
            try
            {
                string nome = categoriaVO.Nome.Trim();

                bool nomeDuplicado = await _categoriaRepository.ExistsByNomeAsync(nome, null);

                if (nomeDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroCategoriaNomeDuplicado);
                }

                Categoria categoria = _mapper.Map<Categoria>(categoriaVO);
                categoria.Nome = nome;
                categoria.Descricao = NormalizarDescricao(categoriaVO.Descricao);

                Categoria gravada = await _categoriaRepository.InsertAsync(categoria);
                ReturnCategoriaVO retorno = _mapper.Map<ReturnCategoriaVO>(gravada);

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Altera uma categoria ativa; lanca NotFoundException ou BusinessException conforme a regra.</summary>
        public async Task UpdateAsync(int idCategoria, CategoriaVO categoriaVO)
        {
            try
            {
                Categoria? existente = await _categoriaRepository.FindByIdAsync(idCategoria);

                if (existente == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCategoriaNaoEncontrada);
                }

                string nome = categoriaVO.Nome.Trim();

                bool nomeDuplicado = await _categoriaRepository.ExistsByNomeAsync(nome, idCategoria);

                if (nomeDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroCategoriaNomeDuplicado);
                }

                Categoria categoria = _mapper.Map<Categoria>(categoriaVO);
                categoria.IdCategoria = idCategoria;
                categoria.Nome = nome;
                categoria.Descricao = NormalizarDescricao(categoriaVO.Descricao);

                bool atualizou = await _categoriaRepository.UpdateAsync(categoria);

                if (!atualizou)
                {
                    throw new NotFoundException(ConstantsMessage.ErroCategoriaNaoEncontrada);
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Normaliza a descricao: null continua null; senao remove os espacos das pontas;
        /// se ficar vazia, vira null. Funcao pura, sem acesso a dados.
        /// </summary>
        private static string? NormalizarDescricao(string? descricao)
        {
            if (descricao == null)
            {
                return null;
            }

            string descricaoSemEspacos = descricao.Trim();

            if (descricaoSemEspacos.Length == 0)
            {
                return null;
            }

            return descricaoSemEspacos;
        }
    }
}
