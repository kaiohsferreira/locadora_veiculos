using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio do contrato da locacao (contrato_locacao). As consultas usam o DbSet, portanto o filtro "Empresa" pela Locacao
    /// vale em todas, e filtram DisabledAt nulo. Nao ha bypass de filtro nem SQL bruto neste arquivo.
    /// </summary>
    public class ContratoLocacaoRepository : IContratoLocacaoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ContratoLocacaoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<Locacao?> FindLocacaoParaContratoAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await _db.Locacoes.AsNoTracking()
                    .Include(x => x.Cliente)
                    .Include(x => x.Veiculo)
                        .ThenInclude(x => x.Empresa)
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null)
                    .FirstOrDefaultAsync();

                return locacao;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRepositorioContratoLocacao, GetType().ToString());
                throw;
            }
        }

        public async Task<ContratoLocacao?> FindAtivoByLocacaoAsync(int idLocacao)
        {
            try
            {
                ContratoLocacao? contrato = await _db.ContratosLocacao.AsNoTracking()
                    .Include(x => x.Funcionario)
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null)
                    .FirstOrDefaultAsync();

                return contrato;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRepositorioContratoLocacao, GetType().ToString());
                throw;
            }
        }

        public async Task<ContratoLocacao> InsertAsync(ContratoLocacao contrato)
        {
            try
            {
                DateTime agora = DateTime.Now;
                contrato.IdContratoLocacao = 0;
                contrato.CreatedAt = agora;
                contrato.UpdatedAt = agora;
                contrato.DisabledAt = null;

                _db.ContratosLocacao.Add(contrato);
                await _db.SaveChangesAsync();

                return contrato;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarContratoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
