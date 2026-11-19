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
        /// Quantas fotos ativas tem o veiculo ativo da locadora do contexto, em um unico comando. Nulo quando o veiculo nao existe,
        /// esta desativado ou e de outra locadora (a mesma consulta confere o veiculo e conta as fotos).
        /// </summary>
        public async Task<int?> ContarAtivasDeVeiculoAtivoAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                int? total = await ConsultaContagemAtivasDeVeiculoAtivo(idVeiculo).FirstOrDefaultAsync(cancellationToken);
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Total e maior ordem das fotos ativas do veiculo, em um unico comando e sem trazer as fotos. Sem foto: Total 0 e
        /// MaiorOrdem nulo. Nao exige que o veiculo esteja ativo (so as fotos ativas da locadora do contexto contam): quem confere
        /// o veiculo e ContarAtivasDeVeiculoAtivoAsync, antes.
        /// </summary>
        public async Task<ResumoFotosAtivasVeiculo> ObterResumoAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                ResumoFotosAtivasVeiculo? resumo = await ConsultaResumoAtivasDoVeiculo(idVeiculo).FirstOrDefaultAsync(cancellationToken);
                if (resumo == null)
                {
                    return new ResumoFotosAtivasVeiculo(0, null);
                }

                return resumo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da contagem das fotos ativas do veiculo ativo, em um unico SELECT (subconsulta escalar, sem GROUP BY). Sem linha
        /// quando o veiculo nao e ativo da locadora do contexto. So monta a consulta, sem executar: quem executa e
        /// ContarAtivasDeVeiculoAtivoAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<int?> ConsultaContagemAtivasDeVeiculoAtivo(int idVeiculo)
        {
            try
            {
                IQueryable<int?> consulta = ConsultaVeiculoAtivo(idVeiculo)
                    .AsNoTracking()
                    .Select(veiculo => (int?)_db.VeiculoFotos.Count(foto => foto.IdVeiculo == idVeiculo && foto.DisabledAt == null));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta do resumo das fotos ativas do veiculo (Total e MaiorOrdem), em um unico SELECT com subconsultas escalares e sem
        /// GROUP BY. A ancora e no maximo uma foto ativa do veiculo (Take(1)), com o filtro "Empresa" das fotos; sem foto ativa nao
        /// ha linha e quem executa devolve Total 0 e MaiorOrdem nulo. Nao confere o veiculo ativo. So monta a consulta, sem
        /// executar: quem executa e ObterResumoAtivasAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<ResumoFotosAtivasVeiculo> ConsultaResumoAtivasDoVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<ResumoFotosAtivasVeiculo> consulta = _db.VeiculoFotos
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null)
                    .AsNoTracking()
                    .Take(1)
                    .Select(ancora => new ResumoFotosAtivasVeiculo(
                        _db.VeiculoFotos.Count(foto => foto.IdVeiculo == idVeiculo && foto.DisabledAt == null),
                        _db.VeiculoFotos
                            .Where(foto => foto.IdVeiculo == idVeiculo && foto.DisabledAt == null)
                            .Max(foto => (int?)foto.Ordem)));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarFotoVeiculo, GetType().ToString());
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
