using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Persistencia das fotos do veiculo (veiculo_foto). Sem bypass do filtro "Empresa" e sem SQL direto: toda leitura passa pelo
    /// filtro da entidade VeiculoFoto e pelo filtro do Veiculo. Exclusao logica pelo Disable(), nunca Remove().
    /// </summary>
    public sealed class VeiculoFotoRepository : IVeiculoFotoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public VeiculoFotoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<bool> VeiculoAtivoExisteAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                bool existe = await ConsultaVeiculoAtivo(idVeiculo).AsNoTracking().AnyAsync(cancellationToken);
                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        public async Task<int> CountAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                int total = await ConsultaFotosAtivasDoVeiculo(idVeiculo).AsNoTracking().CountAsync(cancellationToken);
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        public async Task<List<VeiculoFoto>> FindAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                List<VeiculoFoto> fotos = await ConsultaFotosAtivasDoVeiculo(idVeiculo)
                    .AsNoTracking()
                    .OrderBy(x => x.Ordem)
                    .ThenBy(x => x.IdVeiculoFoto)
                    .ToListAsync(cancellationToken);
                return fotos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        public async Task<VeiculoFoto?> FindAtivaAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default)
        {
            try
            {
                VeiculoFoto? foto = await ConsultaFotoAtiva(idVeiculo, idVeiculoFoto).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                return foto;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        public async Task<VeiculoFoto> InsertAsync(VeiculoFoto foto, CancellationToken cancellationToken = default)
        {
            try
            {
                DateTime agora = DateTime.Now;
                foto.CreatedAt = agora;
                foto.UpdatedAt = agora;
                foto.DisabledAt = null;
                _db.VeiculoFotos.Add(foto);
                await _db.SaveChangesAsync(cancellationToken);
                return foto;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGravarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        public async Task<bool> DesativarAsync(int idVeiculo, int idVeiculoFoto, int idFuncionarioRemocao, CancellationToken cancellationToken = default)
        {
            try
            {
                VeiculoFoto? foto = await ConsultaFotoAtiva(idVeiculo, idVeiculoFoto).FirstOrDefaultAsync(cancellationToken);
                if (foto == null)
                {
                    return false;
                }

                foto.Disable();
                foto.IdFuncionarioRemocao = idFuncionarioRemocao;
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGravarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta do veiculo ativo (nao desativado) com o filtro "Empresa" da locadora do contexto. So monta a consulta, sem executar:
        /// quem executa e VeiculoAtivoExisteAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<Veiculo> ConsultaVeiculoAtivo(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta das fotos ativas do veiculo, com o filtro "Empresa" pelo Veiculo. So monta a consulta, sem executar: quem executa
        /// e CountAtivasAsync ou FindAtivasAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<VeiculoFoto> ConsultaFotosAtivasDoVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<VeiculoFoto> consulta = _db.VeiculoFotos
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da foto ativa de um veiculo, com o filtro "Empresa" pelo Veiculo. So monta a consulta, sem executar: quem executa
        /// e FindAtivaAsync ou DesativarAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<VeiculoFoto> ConsultaFotoAtiva(int idVeiculo, int idVeiculoFoto)
        {
            try
            {
                IQueryable<VeiculoFoto> consulta = _db.VeiculoFotos
                    .Where(x => x.IdVeiculo == idVeiculo && x.IdVeiculoFoto == idVeiculoFoto && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
