using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Acesso a dados das apolices (P9-BE-01). Leituras sem rastreamento e so ativas;
    /// a contagem e a pagina usam o mesmo ConsultaSeguros, para o total bater com a pagina.
    /// Exclusao so logica (nao ha Remove). Todo metodo loga e relanca a excecao.
    /// </summary>
    public class SeguroRepository : ISeguroRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public SeguroRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Lista as apolices ativas que atendem aos filtros, ordenadas por fim da vigencia e id, na pagina pedida, com o veiculo.</summary>
        public async Task<List<Seguro>> FindAllAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte, int deslocamento, int quantidade)
        {
            try
            {
                List<Seguro> seguros = await ConsultaSeguros(idVeiculo, fimVigenciaDe, fimVigenciaAte)
                    .Include(x => x.Veiculo)
                    .OrderBy(x => x.DataFim)
                    .ThenBy(x => x.IdSeguro)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return seguros;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarSeguros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta as apolices ativas que atendem aos mesmos filtros de FindAllAsync.</summary>
        public async Task<int> CountAsync(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte)
        {
            try
            {
                int total = await ConsultaSeguros(idVeiculo, fimVigenciaDe, fimVigenciaAte).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarSeguros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca uma apolice ativa pelo id, com o veiculo (AsNoTracking); null se nao existir.</summary>
        public async Task<Seguro?> FindByIdAsync(int idSeguro)
        {
            try
            {
                Seguro? seguro = await ConsultaSeguroPorId(idSeguro).FirstOrDefaultAsync();

                return seguro;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica se existe apolice ativa com o mesmo numero (Trim, sem diferenciar maiusculas), ignorando o id informado.</summary>
        public async Task<bool> ExistsByNumeroApoliceAsync(string numeroApolice, int? idSeguroIgnorado)
        {
            try
            {
                bool existe = await ConsultaSegurosComNumero(numeroApolice, idSeguroIgnorado)
                    .AnyAsync();

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava uma nova apolice: id gerado, CreatedAt = UpdatedAt = mesmo DateTime.Now, DisabledAt nulo.</summary>
        public async Task<Seguro> InsertAsync(Seguro seguro)
        {
            try
            {
                DateTime agora = DateTime.Now;

                seguro.IdSeguro = 0;
                seguro.CreatedAt = agora;
                seguro.UpdatedAt = agora;
                seguro.DisabledAt = null;

                _db.Seguros.Add(seguro);
                await _db.SaveChangesAsync();

                return seguro;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Atualiza os dados da apolice ativa (busca rastreada). CreatedAt e DisabledAt ficam
        /// como estao; UpdatedAt = DateTime.Now. Devolve false se nao achar.
        /// </summary>
        public async Task<bool> UpdateAsync(Seguro seguro)
        {
            try
            {
                Seguro? seguroSalvo = await _db.Seguros
                    .FirstOrDefaultAsync(x => x.IdSeguro == seguro.IdSeguro && x.DisabledAt == null);

                if (seguroSalvo == null)
                {
                    return false;
                }

                seguroSalvo.IdVeiculo = seguro.IdVeiculo;
                seguroSalvo.Seguradora = seguro.Seguradora;
                seguroSalvo.NumeroApolice = seguro.NumeroApolice;
                seguroSalvo.DataInicio = seguro.DataInicio;
                seguroSalvo.DataFim = seguro.DataFim;
                seguroSalvo.ValorPremio = seguro.ValorPremio;
                seguroSalvo.Cobertura = seguro.Cobertura;
                seguroSalvo.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da apolice ativa pelo id, sem rastreamento, com o veiculo. Passa pelo filtro "Empresa"
        /// (apolice de veiculo de outra locadora nao aparece). Usada por FindByIdAsync; publica para o teste de SQL.
        /// </summary>
        public IQueryable<Seguro> ConsultaSeguroPorId(int idSeguro)
        {
            try
            {
                IQueryable<Seguro> consulta = _db.Seguros
                    .AsNoTracking()
                    .Include(x => x.Veiculo)
                    .Where(x => x.IdSeguro == idSeguro && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta de duplicidade do numero da apolice: ativas da locadora do contexto (filtro "Empresa"), sem o id
        /// ignorado, com Trim e sem diferenciar maiusculas (D-4: unicidade por locadora). Usada por ExistsByNumeroApoliceAsync.
        /// </summary>
        public IQueryable<Seguro> ConsultaSegurosComNumero(string numeroApolice, int? idSeguroIgnorado)
        {
            try
            {
                IQueryable<Seguro> consulta = _db.Seguros
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idSeguroIgnorado.HasValue)
                {
                    int idIgnorado = idSeguroIgnorado.Value;
                    consulta = consulta.Where(x => x.IdSeguro != idIgnorado);
                }

                string numeroComparacao = numeroApolice.Trim().ToUpper();
                consulta = consulta.Where(x => x.NumeroApolice.Trim().ToUpper() == numeroComparacao);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a consulta das apolices ativas, sem rastreamento, com os filtros informados
        /// combinados por AND. A janela de fim da vigencia chega pronta do servico. Usada por FindAllAsync e CountAsync.
        /// </summary>
        public IQueryable<Seguro> ConsultaSeguros(int? idVeiculo, DateOnly? fimVigenciaDe, DateOnly? fimVigenciaAte)
        {
            try
            {
                IQueryable<Seguro> consulta = _db.Seguros
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null);

                if (idVeiculo.HasValue)
                {
                    int idVeiculoFiltro = idVeiculo.Value;
                    consulta = consulta.Where(x => x.IdVeiculo == idVeiculoFiltro);
                }

                if (fimVigenciaDe.HasValue)
                {
                    DateOnly inicioJanela = fimVigenciaDe.Value;
                    consulta = consulta.Where(x => x.DataFim >= inicioJanela);
                }

                if (fimVigenciaAte.HasValue)
                {
                    DateOnly fimJanela = fimVigenciaAte.Value;
                    consulta = consulta.Where(x => x.DataFim <= fimJanela);
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarSeguros, GetType().ToString());
                throw;
            }
        }
    }
}
