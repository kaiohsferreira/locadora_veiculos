using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ICategoriaRepository em memoria, com as mesmas regras do CategoriaRepository real:
    /// ignora desativadas em todas as buscas, compara o nome com Trim e sem diferenciar
    /// maiusculas, ordena por nome e pagina por deslocamento/quantidade.
    /// Adicionar() serve para os testes semearem dados, inclusive desativados.
    /// </summary>
    public class FakeCategoriaRepository : ICategoriaRepository
    {
        private readonly List<Categoria> _categorias = new List<Categoria>();
        private int _proximoId = 1;

        /// <summary>Quando true, UpdateAsync devolve false sem alterar nada (simula id nao encontrado no banco).</summary>
        public bool ForcarUpdateFalse { get; set; }

        /// <summary>Quando nao for null, FindByIdAsync lanca esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoBuscarPorId { get; set; }

        /// <summary>Semeia uma categoria (ativa ou desativada) sem passar pelo servico. Se o id for 0, gera um.</summary>
        public void Adicionar(Categoria categoria)
        {
            if (categoria.IdCategoria == 0)
            {
                categoria.IdCategoria = _proximoId;
            }

            if (categoria.IdCategoria >= _proximoId)
            {
                _proximoId = categoria.IdCategoria + 1;
            }

            _categorias.Add(Copiar(categoria));
        }

        /// <summary>Lista as categorias ativas ordenadas por nome, na pagina pedida.</summary>
        public Task<List<Categoria>> FindAllAsync(int deslocamento, int quantidade)
        {
            List<Categoria> categorias = _categorias
                .Where(x => x.DisabledAt == null)
                .OrderBy(x => x.Nome)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(categorias);
        }

        /// <summary>Conta as categorias ativas.</summary>
        public Task<int> CountAsync()
        {
            int total = _categorias.Count(x => x.DisabledAt == null);

            return Task.FromResult(total);
        }

        /// <summary>Busca uma categoria ativa pelo id; null se nao existir.</summary>
        public Task<Categoria?> FindByIdAsync(int idCategoria)
        {
            if (ExcecaoAoBuscarPorId != null)
            {
                throw ExcecaoAoBuscarPorId;
            }

            Categoria? encontrada = _categorias.FirstOrDefault(x => x.IdCategoria == idCategoria && x.DisabledAt == null);
            Categoria? copia = null;

            if (encontrada != null)
            {
                copia = Copiar(encontrada);
            }

            return Task.FromResult(copia);
        }

        /// <summary>Verifica se existe categoria ativa com o mesmo nome (Trim, sem diferenciar maiusculas), ignorando o id informado.</summary>
        public Task<bool> ExistsByNomeAsync(string nome, int? idCategoriaIgnorada)
        {
            string nomeComparacao = nome.Trim().ToLower();

            bool existe = _categorias.Any(x =>
                x.DisabledAt == null &&
                (!idCategoriaIgnorada.HasValue || x.IdCategoria != idCategoriaIgnorada.Value) &&
                x.Nome.Trim().ToLower() == nomeComparacao);

            return Task.FromResult(existe);
        }

        /// <summary>Grava uma nova categoria com id gerado, CreatedAt = UpdatedAt = DateTime.Now e DisabledAt = null.</summary>
        public Task<Categoria> InsertAsync(Categoria categoria)
        {
            DateTime agora = DateTime.Now;

            categoria.IdCategoria = _proximoId;
            _proximoId++;
            categoria.CreatedAt = agora;
            categoria.UpdatedAt = agora;
            categoria.DisabledAt = null;

            _categorias.Add(Copiar(categoria));

            return Task.FromResult(categoria);
        }

        /// <summary>Atualiza Nome, Descricao e ValorDiariaBase da categoria ativa; false se nao achar.</summary>
        public Task<bool> UpdateAsync(Categoria categoria)
        {
            if (ForcarUpdateFalse)
            {
                return Task.FromResult(false);
            }

            Categoria? salva = _categorias.FirstOrDefault(x => x.IdCategoria == categoria.IdCategoria && x.DisabledAt == null);

            if (salva == null)
            {
                return Task.FromResult(false);
            }

            salva.Nome = categoria.Nome;
            salva.Descricao = categoria.Descricao;
            salva.ValorDiariaBase = categoria.ValorDiariaBase;
            salva.UpdatedAt = DateTime.Now;

            return Task.FromResult(true);
        }

        /// <summary>Copia os campos da categoria, para que o chamador nao altere o armazenamento sem passar pelo repositorio.</summary>
        private static Categoria Copiar(Categoria origem)
        {
            Categoria copia = new Categoria
            {
                IdCategoria = origem.IdCategoria,
                Nome = origem.Nome,
                Descricao = origem.Descricao,
                ValorDiariaBase = origem.ValorDiariaBase,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }
    }
}
