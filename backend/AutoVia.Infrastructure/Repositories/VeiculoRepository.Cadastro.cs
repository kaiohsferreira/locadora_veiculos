using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Cadastro e edicao de veiculos (P1-BE-02). Leituras sem rastreamento e sempre
    /// com DisabledAt nulo; a edicao busca a entidade rastreada e copia so os campos
    /// do cadastro (Status, CreatedAt e DisabledAt nunca mudam aqui).
    /// </summary>
    public partial class VeiculoRepository
    {
        /// <summary>Busca um veiculo ativo pelo id, com a categoria (AsNoTracking).</summary>
        public async Task<Veiculo?> FindByIdAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await _db.Veiculos
                    .AsNoTracking()
                    .Include(x => x.Categoria)
                    .FirstOrDefaultAsync(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);

                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe veiculo ativo com a mesma placa, comparando com Trim e sem diferenciar maiusculas.</summary>
        public async Task<bool> ExistsByPlacaAsync(string placa, int? idVeiculoIgnorado)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idVeiculoIgnorado.HasValue)
                {
                    consulta = consulta.Where(x => x.IdVeiculo != idVeiculoIgnorado.Value);
                }

                bool existe = await consulta
                    .AnyAsync(x => x.Placa.Trim().ToUpper() == placa.Trim().ToUpper());

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe veiculo ativo com o mesmo chassi, comparando com Trim e sem diferenciar maiusculas.</summary>
        public async Task<bool> ExistsByChassiAsync(string chassi, int? idVeiculoIgnorado)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idVeiculoIgnorado.HasValue)
                {
                    consulta = consulta.Where(x => x.IdVeiculo != idVeiculoIgnorado.Value);
                }

                bool existe = await consulta
                    .AnyAsync(x => x.Chassi.Trim().ToUpper() == chassi.Trim().ToUpper());

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe veiculo ativo com o mesmo renavam, comparando com Trim e sem diferenciar maiusculas.</summary>
        public async Task<bool> ExistsByRenavamAsync(string renavam, int? idVeiculoIgnorado)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idVeiculoIgnorado.HasValue)
                {
                    consulta = consulta.Where(x => x.IdVeiculo != idVeiculoIgnorado.Value);
                }

                bool existe = await consulta
                    .AnyAsync(x => x.Renavam.Trim().ToUpper() == renavam.Trim().ToUpper());

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava um novo veiculo: id gerado, CreatedAt = UpdatedAt = mesmo DateTime.Now, DisabledAt nulo.</summary>
        public async Task<Veiculo> InsertAsync(Veiculo veiculo)
        {
            try
            {
                DateTime agora = DateTime.Now;

                veiculo.IdVeiculo = 0;
                veiculo.CreatedAt = agora;
                veiculo.UpdatedAt = agora;
                veiculo.DisabledAt = null;

                _db.Veiculos.Add(veiculo);
                await _db.SaveChangesAsync();

                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Atualiza os campos do cadastro do veiculo ativo. Copia so os campos do contrato;
        /// Status, CreatedAt e DisabledAt ficam como estao. Devolve false se nao achar.
        /// </summary>
        public async Task<bool> UpdateAsync(Veiculo veiculo)
        {
            try
            {
                Veiculo? veiculoSalvo = await _db.Veiculos
                    .FirstOrDefaultAsync(x => x.IdVeiculo == veiculo.IdVeiculo && x.DisabledAt == null);

                if (veiculoSalvo == null)
                {
                    return false;
                }

                veiculoSalvo.IdCategoria = veiculo.IdCategoria;
                veiculoSalvo.Placa = veiculo.Placa;
                veiculoSalvo.Chassi = veiculo.Chassi;
                veiculoSalvo.Renavam = veiculo.Renavam;
                veiculoSalvo.Marca = veiculo.Marca;
                veiculoSalvo.Modelo = veiculo.Modelo;
                veiculoSalvo.AnoFabricacao = veiculo.AnoFabricacao;
                veiculoSalvo.AnoModelo = veiculo.AnoModelo;
                veiculoSalvo.Cor = veiculo.Cor;
                veiculoSalvo.KmAtual = veiculo.KmAtual;
                veiculoSalvo.ValorDiaria = veiculo.ValorDiaria;
                veiculoSalvo.ValorVenda = veiculo.ValorVenda;
                veiculoSalvo.DataAquisicao = veiculo.DataAquisicao;
                veiculoSalvo.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
