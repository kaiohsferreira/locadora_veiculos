using AutoVia.Communication.Enums;
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
    /// Persistencia do vinculo cliente x locadora (T-BE-06) sobre o AutoViaDbContext. As consultas usam o DbSet
    /// ClientesLocadoras, que ja traz o filtro "Empresa" (locadora do contexto OU cliente do contexto); nao ha bypass
    /// aqui. Leituras sem rastreamento e so de linhas ativas (DisabledAt nulo). Nenhum metodo remove a linha: a
    /// exclusao e logica (Disable). As consultas sao metodos que devolvem IQueryable, para o teste conferir pelo
    /// ToQueryString que o EF traduz tudo para SQL (sem avaliacao no cliente).
    /// </summary>
    public class ClienteLocadoraRepository : IClienteLocadoraRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ClienteLocadoraRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>
        /// Guarda do INSERT do pedido PENDENTE: o SaveChanges so confirma sem transacao aberta. Com transacao, o 409 que
        /// vem depois desfaria o PENDENTE e o cliente nunca entraria na fila. Falha rapido em vez de gravar errado.
        /// </summary>
        public static void ValidarSemTransacaoAberta(bool transacaoAberta)
        {
            if (transacaoAberta)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroVinculoDentroDeTransacao);
            }
        }

        /// <summary>Busca a locadora ativa pelo id: DisabledAt nulo e Ativo verdadeiro. A locadora e global (sem filtro de contexto).</summary>
        public async Task<Empresa?> FindEmpresaAtivaAsync(int idEmpresa)
        {
            try
            {
                Empresa? empresa = await _db.Empresas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.DisabledAt == null && x.Ativo);

                return empresa;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verdadeiro quando o cliente existe e esta ativo (DisabledAt nulo). A tabela cliente nao tem filtro de contexto.</summary>
        public async Task<bool> ClienteAtivoAsync(int idCliente)
        {
            try
            {
                bool ativo = await _db.Clientes
                    .AsNoTracking()
                    .AnyAsync(x => x.IdCliente == idCliente && x.DisabledAt == null);

                return ativo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Vinculo ativo de menor id do par cliente x locadora, sem rastreamento.</summary>
        public async Task<ClienteLocadora?> FindAtivoMaisAntigoAsync(int idCliente, int idEmpresa)
        {
            try
            {
                ClienteLocadora? vinculo = await ConsultaVinculoAtivoMaisAntigo(idCliente, idEmpresa)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return vinculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Grava um vinculo sempre PENDENTE, sem justificativa e sem verificador. Id gerado; datas com DateTime.Now.</summary>
        public async Task<ClienteLocadora> InsertAsync(ClienteLocadora vinculo)
        {
            try
            {
                ValidarSemTransacaoAberta(_db.Database.CurrentTransaction != null);

                DateTime agora = DateTime.Now;

                vinculo.IdClienteLocadora = 0;
                vinculo.Status = StatusCadastroCliente.PENDENTE;
                vinculo.JustificativaRecusa = null;
                vinculo.IdFuncionarioVerificador = null;
                // DataSolicitacao nao e definida aqui: vem do servico (IRelogio), para ser testavel com relogio fixo.
                vinculo.DataVerificacao = null;
                vinculo.CreatedAt = agora;
                vinculo.UpdatedAt = agora;
                vinculo.DisabledAt = null;

                _db.ClientesLocadoras.Add(vinculo);
                await _db.SaveChangesAsync();

                return vinculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Exclusao logica do vinculo (Disable: DisabledAt e UpdatedAt). Nunca remove a linha.</summary>
        public async Task<bool> DesativarAsync(int idClienteLocadora)
        {
            try
            {
                ClienteLocadora? salvo = await _db.ClientesLocadoras
                    .FirstOrDefaultAsync(x => x.IdClienteLocadora == idClienteLocadora && x.DisabledAt == null);

                if (salvo == null)
                {
                    return false;
                }

                salvo.Disable();
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Grava a decisao em todas as linhas ativas do par (regra da corrida do contrato). Recusa guarda a justificativa;
        /// aprovacao grava justificativa nula. Verificador e data da decisao vem do servico.
        /// </summary>
        public async Task<bool> DecidirParAsync(int idCliente, int idEmpresa, StatusCadastroCliente status, string? justificativa, int idFuncionarioVerificador, DateTime dataVerificacao)
        {
            try
            {
                List<ClienteLocadora> ativos = await ConsultaVinculosAtivosDoPar(idCliente, idEmpresa).ToListAsync();

                if (ativos.Count == 0)
                {
                    return false;
                }

                DateTime agora = DateTime.Now;
                string? justificativaGravada = status == StatusCadastroCliente.RECUSADO ? justificativa : null;

                foreach (ClienteLocadora vinculo in ativos)
                {
                    vinculo.Status = status;
                    vinculo.JustificativaRecusa = justificativaGravada;
                    vinculo.IdFuncionarioVerificador = idFuncionarioVerificador;
                    vinculo.DataVerificacao = dataVerificacao;
                    vinculo.UpdatedAt = agora;
                }

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroDecidirCadastroCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Conta os vinculos ativos do cliente: uma linha por locadora (a de menor id de cada par).</summary>
        public async Task<int> CountMinhasAsync(int idCliente)
        {
            try
            {
                int total = await ConsultaMinhas(idCliente).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocadoras, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista os vinculos ativos do cliente, uma linha por locadora, com a locadora, ordenados e paginados.</summary>
        public async Task<List<ClienteLocadora>> FindMinhasAsync(int idCliente, int deslocamento, int quantidade)
        {
            try
            {
                List<ClienteLocadora> vinculos = await ConsultaMinhasPaginada(idCliente, deslocamento, quantidade).ToListAsync();

                return vinculos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocadoras, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Conta as locadoras distintas com pedido do cliente desde a data (teto de pedidos de aprovacao). Conta linhas
        /// ativas e desativadas, uma vez por locadora. Consulta pelo DbSet filtrado, sem bypass.
        /// </summary>
        public async Task<int> CountEmpresasSolicitadasDesdeAsync(int idCliente, DateTime desde)
        {
            try
            {
                int total = await ConsultaEmpresasSolicitadasDesde(idCliente, desde).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVinculoClienteLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Locadoras distintas (id_empresa) com linha do cliente com data de solicitacao igual ou posterior a data. Nao filtra
        /// desativadas: a linha desativada tambem conta no teto. A consulta e so de forma (teste de SQL), sem avaliar no cliente.
        /// </summary>
        public IQueryable<int> ConsultaEmpresasSolicitadasDesde(int idCliente, DateTime desde)
        {
            IQueryable<int> consulta = _db.ClientesLocadoras
                .AsNoTracking()
                .Where(x => x.IdCliente == idCliente && x.DataSolicitacao >= desde)
                .Select(x => x.IdEmpresa)
                .Distinct();

            return consulta;
        }

        /// <summary>Vinculos ativos do par cliente x locadora (todas as linhas ativas, na ordem do banco). Base da decisao e da busca da linha que vale.</summary>
        public IQueryable<ClienteLocadora> ConsultaVinculosAtivosDoPar(int idCliente, int idEmpresa)
        {
            IQueryable<ClienteLocadora> consulta = _db.ClientesLocadoras
                .Where(x => x.DisabledAt == null && x.IdCliente == idCliente && x.IdEmpresa == idEmpresa);

            return consulta;
        }

        /// <summary>Linha que vale do par: a ativa de menor id.</summary>
        public IQueryable<ClienteLocadora> ConsultaVinculoAtivoMaisAntigo(int idCliente, int idEmpresa)
        {
            IQueryable<ClienteLocadora> consulta = ConsultaVinculosAtivosDoPar(idCliente, idEmpresa)
                .OrderBy(x => x.IdClienteLocadora);

            return consulta;
        }

        /// <summary>
        /// Consulta base de "minhas locadoras": vinculos ativos do cliente cuja linha e a de menor id ativa do par
        /// (cliente, locadora). O filtro "Empresa" continua valendo nas duas consultas (linha e subconsulta).
        /// </summary>
        public IQueryable<ClienteLocadora> ConsultaMinhas(int idCliente)
        {
            IQueryable<ClienteLocadora> consulta = _db.ClientesLocadoras
                .AsNoTracking()
                .Where(x => x.DisabledAt == null
                    && x.IdCliente == idCliente
                    && x.IdClienteLocadora == _db.ClientesLocadoras
                        .Where(y => y.DisabledAt == null && y.IdCliente == x.IdCliente && y.IdEmpresa == x.IdEmpresa)
                        .Min(y => y.IdClienteLocadora));

            return consulta;
        }

        /// <summary>Consulta final de "minhas locadoras": a base, com a locadora, ordenada por data e id e paginada.</summary>
        public IQueryable<ClienteLocadora> ConsultaMinhasPaginada(int idCliente, int deslocamento, int quantidade)
        {
            IQueryable<ClienteLocadora> consulta = ConsultaMinhas(idCliente)
                .Include(x => x.Empresa)
                .OrderBy(x => x.DataSolicitacao)
                .ThenBy(x => x.IdClienteLocadora)
                .Skip(deslocamento)
                .Take(quantidade);

            return consulta;
        }

        /// <summary>Conta os cadastros da fila de verificacao com os filtros (uma linha por cliente, a que vale do par).</summary>
        public async Task<int> CountFilaAsync(StatusCadastroCliente? status, string? busca, string? digitosCpf)
        {
            try
            {
                int total = await ConsultaFila(status, busca, digitosCpf).CountAsync();

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Pagina da fila de verificacao, com o cliente, ordenada pela data do pedido e pelo id do cliente.</summary>
        public async Task<List<ClienteLocadora>> FindFilaAsync(StatusCadastroCliente? status, string? busca, string? digitosCpf, int deslocamento, int quantidade)
        {
            try
            {
                List<ClienteLocadora> pagina = await ConsultaFilaPaginada(status, busca, digitosCpf, deslocamento, quantidade).ToListAsync();

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: a pagina da fila (ConsultaFila) com o cliente anexado, ordenada pela data do pedido e pelo id do
        /// cliente, com deslocamento e quantidade por parametro. So monta a consulta (teste de SQL por ToQueryString); quem
        /// executa e FindFilaAsync.
        /// </summary>
        public IQueryable<ClienteLocadora> ConsultaFilaPaginada(StatusCadastroCliente? status, string? busca, string? digitosCpf, int deslocamento, int quantidade)
        {
            try
            {
                IQueryable<ClienteLocadora> consulta = ConsultaFila(status, busca, digitosCpf)
                    .Include(x => x.Cliente)
                    .OrderBy(x => x.DataSolicitacao)
                    .ThenBy(x => x.IdCliente)
                    .Skip(deslocamento)
                    .Take(quantidade);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Linha que vale do par cliente x locadora, com o cliente anexado; nulo se o cliente estiver desativado ou sem vinculo ativo.</summary>
        public async Task<ClienteLocadora?> FindVinculoComClienteAsync(int idCliente, int idEmpresa)
        {
            try
            {
                ClienteLocadora? vinculo = await ConsultaVinculoAtivoMaisAntigo(idCliente, idEmpresa)
                    .Where(x => x.Cliente.DisabledAt == null)
                    .Include(x => x.Cliente)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return vinculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroObterVerificacaoCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta da fila de verificacao: vinculos ativos de clientes ativos, na linha que vale do par (a ativa de menor id do
        /// par cliente x locadora), com filtro de status e busca. A busca casa nome e e-mail (contem, sem diferenciar maiusculas)
        /// e o CPF so quando ha digitos; o EF escapa % e _ no Contains. O filtro "Empresa" vale na consulta e na subconsulta.
        /// So monta a consulta (teste de SQL por ToQueryString); quem executa e CountFilaAsync ou FindFilaAsync.
        /// </summary>
        public IQueryable<ClienteLocadora> ConsultaFila(StatusCadastroCliente? status, string? busca, string? digitosCpf)
        {
            try
            {
                IQueryable<ClienteLocadora> consulta = _db.ClientesLocadoras
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null
                        && x.Cliente.DisabledAt == null
                        && x.IdClienteLocadora == _db.ClientesLocadoras
                            .Where(y => y.DisabledAt == null && y.IdCliente == x.IdCliente && y.IdEmpresa == x.IdEmpresa)
                            .Min(y => y.IdClienteLocadora));

                if (status.HasValue)
                {
                    StatusCadastroCliente statusFiltro = status.Value;
                    consulta = consulta.Where(x => x.Status == statusFiltro);
                }

                if (!string.IsNullOrEmpty(busca))
                {
                    string termoBusca = busca.ToLowerInvariant();
                    bool temDigitos = !string.IsNullOrEmpty(digitosCpf);
                    string termoDigitos = digitosCpf ?? string.Empty;

                    consulta = consulta.Where(x => x.Cliente.Nome.ToLower().Contains(termoBusca)
                        || x.Cliente.Email.ToLower().Contains(termoBusca)
                        || (temDigitos && x.Cliente.Cpf.Contains(termoDigitos)));
                }

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }
    }
}
