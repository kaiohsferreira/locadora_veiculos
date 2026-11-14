using AutoVia.Core.Entities.PgSql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Metodos de cadastro do FakeVeiculoRepository, com as mesmas regras do VeiculoRepository
    /// real: ignoram desativados, comparam placa, chassi e renavam com Trim e sem diferenciar
    /// maiusculas, ignoram o id informado e nao alteram Status, CreatedAt nem DisabledAt na edicao.
    /// </summary>
    public partial class FakeVeiculoRepository
    {
        /// <summary>Quando true, UpdateAsync devolve false sem alterar nada (simula id nao encontrado no banco).</summary>
        public bool ForcarUpdateFalse { get; set; }

        /// <summary>Quando nao for null, FindByIdAsync lanca esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoBuscarPorId { get; set; }

        /// <summary>Quando nao for null, ExistsByPlacaAsync, ExistsByChassiAsync e ExistsByRenavamAsync lancam esta excecao (falha na leitura).</summary>
        public Exception? ExcecaoAoVerificarDuplicidade { get; set; }

        /// <summary>Quando nao for null, InsertAsync e UpdateAsync lancam esta excecao (falha na gravacao).</summary>
        public Exception? ExcecaoNaGravacao { get; set; }

        /// <summary>Ultima entidade recebida por InsertAsync (a mesma referencia), para conferir a navegacao Categoria.</summary>
        public Veiculo? UltimoVeiculoInserido { get; private set; }

        /// <summary>Locadoras que a consulta por id devolve na navegacao Empresa (como o Include do banco). A gravacao nao as preenche.</summary>
        private readonly Dictionary<int, Empresa> _empresasDoBanco = new Dictionary<int, Empresa>();

        /// <summary>Categorias que a consulta por id devolve na navegacao Categoria (como o Include do banco). A gravacao nao as preenche.</summary>
        private readonly Dictionary<int, Categoria> _categoriasDoBanco = new Dictionary<int, Categoria>();

        /// <summary>Cadastra a locadora que a consulta por id passa a devolver na navegacao Empresa.</summary>
        public void RegistrarEmpresa(Empresa empresa)
        {
            _empresasDoBanco[empresa.IdEmpresa] = empresa;
        }

        /// <summary>Cadastra a categoria que a consulta por id passa a devolver na navegacao Categoria.</summary>
        public void RegistrarCategoria(Categoria categoria)
        {
            _categoriasDoBanco[categoria.IdCategoria] = categoria;
        }

        /// <summary>Simula o Include da consulta por id: preenche Categoria e Empresa quando a copia ainda nao as tem.</summary>
        private void CarregarNavegacoes(Veiculo veiculo)
        {
            if (veiculo.Categoria == null && _categoriasDoBanco.TryGetValue(veiculo.IdCategoria, out Categoria? categoria))
            {
                veiculo.Categoria = categoria;
            }

            if (veiculo.Empresa == null && _empresasDoBanco.TryGetValue(veiculo.IdEmpresa, out Empresa? empresa))
            {
                veiculo.Empresa = empresa;
            }
        }

        /// <summary>Busca um veiculo ativo pelo id; devolve copia (como AsNoTracking) ou null.</summary>
        public Task<Veiculo?> FindByIdAsync(int idVeiculo)
        {
            if (ExcecaoAoBuscarPorId != null)
            {
                throw ExcecaoAoBuscarPorId;
            }

            Veiculo? encontrado = _veiculos.FirstOrDefault(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);
            Veiculo? copia = null;

            if (encontrado != null)
            {
                copia = Copiar(encontrado);
                CarregarNavegacoes(copia);
            }

            return Task.FromResult(copia);
        }

        /// <summary>Verifica se existe veiculo ativo com a mesma placa, ignorando o id informado.</summary>
        public Task<bool> ExistsByPlacaAsync(string placa, int? idVeiculoIgnorado)
        {
            bool existe = ExisteAtivoComValor(x => x.Placa, placa, idVeiculoIgnorado);

            return Task.FromResult(existe);
        }

        /// <summary>Verifica se existe veiculo ativo com o mesmo chassi, ignorando o id informado.</summary>
        public Task<bool> ExistsByChassiAsync(string chassi, int? idVeiculoIgnorado)
        {
            bool existe = ExisteAtivoComValor(x => x.Chassi, chassi, idVeiculoIgnorado);

            return Task.FromResult(existe);
        }

        /// <summary>Verifica se existe veiculo ativo com o mesmo renavam, ignorando o id informado.</summary>
        public Task<bool> ExistsByRenavamAsync(string renavam, int? idVeiculoIgnorado)
        {
            bool existe = ExisteAtivoComValor(x => x.Renavam, renavam, idVeiculoIgnorado);

            return Task.FromResult(existe);
        }

        /// <summary>Grava um novo veiculo com id gerado, CreatedAt = UpdatedAt = mesmo DateTime.Now e DisabledAt nulo.</summary>
        public Task<Veiculo> InsertAsync(Veiculo veiculo)
        {
            if (ExcecaoNaGravacao != null)
            {
                throw ExcecaoNaGravacao;
            }

            UltimoVeiculoInserido = veiculo;

            DateTime agora = DateTime.Now;

            veiculo.IdVeiculo = _proximoId;
            _proximoId++;
            veiculo.CreatedAt = agora;
            veiculo.UpdatedAt = agora;
            veiculo.DisabledAt = null;

            _veiculos.Add(Copiar(veiculo));

            return Task.FromResult(veiculo);
        }

        /// <summary>Atualiza os campos do cadastro do veiculo ativo; Status, CreatedAt e DisabledAt ficam. False se nao achar.</summary>
        public Task<bool> UpdateAsync(Veiculo veiculo)
        {
            if (ExcecaoNaGravacao != null)
            {
                throw ExcecaoNaGravacao;
            }

            if (ForcarUpdateFalse)
            {
                return Task.FromResult(false);
            }

            Veiculo? salvo = _veiculos.FirstOrDefault(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null);

            if (salvo == null)
            {
                return Task.FromResult(false);
            }

            salvo.IdCategoria = veiculo.IdCategoria;
            salvo.Placa = veiculo.Placa;
            salvo.Chassi = veiculo.Chassi;
            salvo.Renavam = veiculo.Renavam;
            salvo.Marca = veiculo.Marca;
            salvo.Modelo = veiculo.Modelo;
            salvo.AnoFabricacao = veiculo.AnoFabricacao;
            salvo.AnoModelo = veiculo.AnoModelo;
            salvo.Cor = veiculo.Cor;
            salvo.KmAtual = veiculo.KmAtual;
            salvo.ValorDiaria = veiculo.ValorDiaria;
            salvo.ValorVenda = veiculo.ValorVenda;
            salvo.DataAquisicao = veiculo.DataAquisicao;
            salvo.UpdatedAt = DateTime.Now;

            return Task.FromResult(true);
        }

        /// <summary>Procura um veiculo ativo, diferente do id ignorado, com o mesmo valor (Trim e sem diferenciar maiusculas).</summary>
        private bool ExisteAtivoComValor(Func<Veiculo, string> campo, string valor, int? idVeiculoIgnorado)
        {
            if (ExcecaoAoVerificarDuplicidade != null)
            {
                throw ExcecaoAoVerificarDuplicidade;
            }

            string valorComparacao = valor.Trim().ToUpper();

            bool existe = _veiculos.Any(x =>
                x.DisabledAt == null &&
                (!idVeiculoIgnorado.HasValue || x.IdVeiculo != idVeiculoIgnorado.Value) &&
                campo(x).Trim().ToUpper() == valorComparacao);

            return existe;
        }
    }
}
