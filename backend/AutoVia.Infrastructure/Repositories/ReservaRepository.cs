using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Reservas. Ordem global de travas (decisao D-10): veiculo, depois reserva; funcionario nunca e travado. Consultas
    /// entre locadoras so nos metodos nomeados desta classe (lista fechada da guarda de isolamento). Todo catch registra
    /// no log e relanca, inclusive 404 e 409 lancados dentro da transacao (decisao D-9).
    /// </summary>
    public class ReservaRepository : IReservaRepository
    {
        private const string SqlBloquearVeiculoAtivo = "SELECT * FROM veiculo WHERE id_veiculo = {0} AND disabled_at IS NULL FOR UPDATE";
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ReservaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Le o veiculo ativo de locadora ativa para a pre-checagem da reserva. Sem trava.</summary>
        public async Task<Veiculo?> FindVeiculoParaReservaAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await ConsultaVeiculoParaReserva(idVeiculo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculoReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cria a reserva em transacao: trava o veiculo, reconfere a locadora ativa, o status do veiculo e o conflito de
        /// periodo, insere a reserva e grava o veiculo como RESERVADO.
        /// </summary>
        public async Task<Reserva> InsertAtomicallyAsync(Reserva reserva)
        {
            try
            {
                reserva.IdReserva = 0;
                reserva.CreatedAt = DateTime.Now;
                reserva.UpdatedAt = DateTime.Now;
                reserva.DisabledAt = null;

                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();
                try
                {
                    Veiculo? veiculo = await BloquearVeiculoAtivoAsync(reserva.IdVeiculo);

                    if (veiculo == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                    }

                    bool locadoraAtiva = await ConsultaVeiculoParaReserva(reserva.IdVeiculo).AnyAsync();

                    if (!locadoraAtiva)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                    }

                    if (!RegrasReserva.VeiculoAceitaReserva(veiculo.Status))
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaVeiculoIndisponivel);
                    }

                    bool conflito = await ConsultaReservasDoVeiculo(reserva.IdVeiculo)
                        .AnyAsync(RegrasReserva.SobrepoePeriodo(reserva.DataRetiradaPrevista, reserva.DataDevolucaoPrevista));

                    if (conflito)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaConflitoPeriodo);
                    }

                    _db.Reservas.Add(reserva);
                    veiculo.Status = StatusVeiculo.RESERVADO;
                    veiculo.UpdatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();
                    return reserva;
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Reserva do cliente (posse pela consulta nomeada), com veiculo, categoria e locadora. Desativadas ficam fora.</summary>
        public async Task<Reserva?> FindByIdAsync(int idReserva, int idCliente)
        {
            try
            {
                Reserva? reserva = await ConsultaReservasDoClienteLogadoComCapa(idCliente)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdReserva == idReserva);
                return reserva;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Total de reservas do cliente com o mesmo filtro da listagem (situacao, busca). Usado no contador e no teto.</summary>
        public async Task<int> CountAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca)
        {
            try
            {
                IQueryable<Reserva> consulta = AplicarFiltrosDaListagem(ConsultaReservasDoClienteLogado(idCliente), status, idReservaBusca, textoBusca);
                int total = await consulta.CountAsync();
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagina de reservas do cliente. Ordem por retirada prevista e id, crescente (ATIVAS) ou decrescente.</summary>
        public async Task<List<Reserva>> FindAllAsync(int idCliente, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca, bool ordemCrescente, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<Reserva> consulta = AplicarFiltrosDaListagem(ConsultaReservasDoClienteLogadoComCapa(idCliente), status, idReservaBusca, textoBusca);
                IQueryable<Reserva> ordenada = ordemCrescente
                    ? consulta.OrderBy(x => x.DataRetiradaPrevista).ThenBy(x => x.IdReserva)
                    : consulta.OrderByDescending(x => x.DataRetiradaPrevista).ThenByDescending(x => x.IdReserva);

                List<Reserva> reservas = await ordenada
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();
                return reservas;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cancela a reserva do cliente em transacao: le a reserva para descobrir o veiculo, trava o veiculo, rele a reserva
        /// e grava CANCELADA. O veiculo volta a DISPONIVEL ou fica RESERVADO, conforme outra reserva ativa; sem veiculo
        /// ativo, nao e tocado. Ja cancelada e idempotente; CONCLUIDA responde 409.
        /// </summary>
        public async Task CancelAtomicallyAsync(int idReserva, int idCliente)
        {
            try
            {
                IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();
                try
                {
                    Reserva? preliminar = await ConsultaReservasDoClienteLogado(idCliente)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.IdReserva == idReserva);

                    if (preliminar == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                    }

                    Veiculo? veiculo = await BloquearVeiculoAtivoAsync(preliminar.IdVeiculo);

                    Reserva? reserva = await ConsultaReservasDoClienteLogado(idCliente)
                        .FirstOrDefaultAsync(x => x.IdReserva == idReserva);

                    if (reserva == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                    }

                    if (reserva.Status == StatusReserva.CANCELADA)
                    {
                        await transacao.CommitAsync();
                        return;
                    }

                    if (reserva.Status == StatusReserva.CONCLUIDA)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaConcluidaNaoCancelavel);
                    }

                    reserva.Status = StatusReserva.CANCELADA;
                    reserva.UpdatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();

                    if (veiculo != null)
                    {
                        bool outraReservaAtiva = await ConsultaReservasDoVeiculo(reserva.IdVeiculo).AnyAsync();
                        StatusVeiculo statusAtual = veiculo.Status;
                        StatusVeiculo statusCalculado = RegrasReserva.StatusDoVeiculoAposCancelamento(statusAtual, outraReservaAtiva);

                        if (statusCalculado != statusAtual)
                        {
                            veiculo.Status = statusCalculado;
                            veiculo.UpdatedAt = DateTime.Now;
                            await _db.SaveChangesAsync();
                        }
                    }

                    await transacao.CommitAsync();
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: veiculo ativo de locadora ativa, sem trava. Usada na pre-checagem e na reconferencia da locadora
        /// logo depois da trava. Vale o filtro de empresas ativas; nao ha dado de cliente nesta consulta.
        /// </summary>
        public IQueryable<Veiculo> ConsultaVeiculoParaReserva(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .SemEscopoEmpresa()
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null)
                    .ApenasEmpresasAtivas();

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculoReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: reservas nao desativadas ATIVA ou CONFIRMADA do veiculo, de QUALQUER cliente. Usada no conflito
        /// de periodo e em "outra reserva ativa". Sem posse: o conflito considera todos os clientes.
        /// </summary>
        public IQueryable<Reserva> ConsultaReservasDoVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<Reserva> consulta = _db.Reservas
                    .SemEscopoEmpresa()
                    .Where(x => x.IdVeiculo == idVeiculo
                        && x.DisabledAt == null
                        && (x.Status == StatusReserva.ATIVA || x.Status == StatusReserva.CONFIRMADA));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: reservas nao desativadas do cliente pedido, com veiculo, categoria e locadora. A posse e a propria
        /// consulta: vale so com contexto de cliente positivo, igual ao id pedido e igual ao dono. Sem essa igualdade, nada volta.
        /// </summary>
        public IQueryable<Reserva> ConsultaReservasDoClienteLogado(int idCliente)
        {
            try
            {
                int idClienteContexto = _db.IdClienteContexto;
                bool posseDoCliente = idClienteContexto > 0 && idClienteContexto == idCliente;

                IQueryable<Reserva> consulta = _db.Reservas
                    .SemEscopoEmpresa()
                    .Where(x => posseDoCliente && x.IdCliente == idCliente && x.DisabledAt == null)
                    .Include(x => x.Veiculo).ThenInclude(x => x.Categoria)
                    .Include(x => x.Veiculo).ThenInclude(x => x.Empresa);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: a mesma consulta do cliente, com a capa do veiculo (primeira foto ativa) por Include da colecao de
        /// fotos. Usada so na leitura por id e na listagem; contagem e cancelamento seguem sem a capa. Sem bypass: parte de
        /// ConsultaReservasDoClienteLogado, que ja faz a posse pelo cliente do contexto.
        /// </summary>
        public IQueryable<Reserva> ConsultaReservasDoClienteLogadoComCapa(int idCliente)
        {
            try
            {
                IQueryable<Reserva> consulta = ConsultaReservasDoClienteLogado(idCliente)
                    .Include(x => x.Veiculo).ThenInclude(x => x.Fotos.Where(foto => foto.DisabledAt == null).OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto).Take(ConstantsValues.QuantidadeFotoCapaPorVeiculo));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: trava do veiculo (FOR UPDATE), sem filtro de empresa, para criar e cancelar. Unica consulta
        /// com SQL bruto deste repositorio. Nao confere a locadora: quem cria reconfere a locadora ativa logo depois da trava.
        /// </summary>
        public IQueryable<Veiculo> ConsultaTravaDoVeiculo(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .FromSqlRaw(SqlBloquearVeiculoAtivo, idVeiculo)
                    .SemEscopoEmpresa();

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculoReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Trava o veiculo pela consulta nomeada da trava (ordem global de travas: veiculo primeiro).</summary>
        private async Task<Veiculo?> BloquearVeiculoAtivoAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await ConsultaTravaDoVeiculo(idVeiculo).FirstOrDefaultAsync();
                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVeiculoReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Aplica situacao (status) e busca (codigo OU modelo) sobre a consulta-base do cliente, sem paginacao. A contagem recebe a consulta sem a capa.</summary>
        private IQueryable<Reserva> AplicarFiltrosDaListagem(IQueryable<Reserva> consultaBase, IReadOnlyCollection<StatusReserva>? status, int? idReservaBusca, string? textoBusca)
        {
            try
            {
                IQueryable<Reserva> consulta = consultaBase.AsNoTracking();

                if (status != null)
                {
                    StatusReserva[] statusFiltro = status.ToArray();
                    consulta = consulta.Where(x => statusFiltro.Contains(x.Status));
                }

                bool temCodigo = idReservaBusca.HasValue;
                bool temTexto = textoBusca != null;

                if (temCodigo || temTexto)
                {
                    int codigoBusca = idReservaBusca ?? 0;
                    string termoBusca = textoBusca ?? string.Empty;
                    consulta = consulta.Where(x => (temCodigo && x.IdReserva == codigoBusca)
                        || (temTexto && x.Veiculo.Modelo.ToLower().Contains(termoBusca)));
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }
    }
}
