using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>Persistencia das avarias com filtro de exclusao logica.</summary>
    public sealed class AvariaRepository : IAvariaRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public AvariaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<Vistoria?> FindAtivaComLocacaoAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            try
            {
                Vistoria? vistoria = await ConsultaVistoriaAtivaComLocacao(idVistoria).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                return vistoria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            try
            {
                Avaria? avaria = await ConsultaAvariaAtiva(idVistoria, idAvaria).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                return avaria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default)
        {
            try
            {
                DateTime agora = DateTime.Now;
                avaria.CreatedAt = agora;
                avaria.UpdatedAt = agora;
                avaria.DisabledAt = null;
                _db.Avarias.Add(avaria);
                await _db.SaveChangesAsync(cancellationToken);
                return avaria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da vistoria ativa (nao desativada) com o filtro "Empresa" da locadora do contexto. So monta a consulta, sem executar:
        /// a leitura fica em FindAtivaComLocacaoAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Vistoria> ConsultaVistoriaAtiva(int idVistoria)
        {
            try
            {
                IQueryable<Vistoria> consulta = _db.Vistorias
                    .Where(x => x.IdVistoria == idVistoria && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da vistoria ativa com a locacao carregada (Include), descartando locacao desativada. Mesmo filtro "Empresa" de
        /// ConsultaVistoriaAtiva. So monta a consulta, sem executar: a leitura fica em FindAtivaComLocacaoAsync.
        /// </summary>
        public IQueryable<Vistoria> ConsultaVistoriaAtivaComLocacao(int idVistoria)
        {
            try
            {
                IQueryable<Vistoria> consulta = ConsultaVistoriaAtiva(idVistoria)
                    .Include(x => x.Locacao)
                    .Where(x => x.Locacao.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da avaria ativa (nao desativada) da vistoria informada, com o filtro "Empresa" pela vistoria. So monta a consulta, sem executar:
        /// a leitura fica em FindAtivaAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Avaria> ConsultaAvariaAtiva(int idVistoria, int idAvaria)
        {
            try
            {
                IQueryable<Avaria> consulta = _db.Avarias
                    .Where(x => x.IdVistoria == idVistoria && x.IdAvaria == idAvaria && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }
    }
}
