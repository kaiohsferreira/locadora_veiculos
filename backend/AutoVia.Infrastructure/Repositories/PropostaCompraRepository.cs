using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Propostas de compra (P6-BE-02). A tabela tem o filtro "Empresa" pelo Veiculo: a locadora so ve propostas dos proprios
    /// veiculos. O cliente nao tem contexto de locadora, por isso a leitura da propria proposta e a unica consulta que desliga
    /// o filtro (ConsultaPropostasDoClienteLogado, na lista fechada da guarda de isolamento), com a posse pelo cliente do
    /// contexto, igual a ConsultaReservasDoClienteLogado. Todo catch registra no log e relanca.
    /// </summary>
    public class PropostaCompraRepository : IPropostaCompraRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public PropostaCompraRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Grava a proposta nova e devolve com o id gerado. As navegacoes nao sao gravadas.</summary>
        public async Task<PropostaCompra> InsertAsync(PropostaCompra proposta)
        {
            try
            {
                proposta.IdPropostaCompra = 0;
                proposta.CreatedAt = DateTime.Now;
                proposta.UpdatedAt = DateTime.Now;
                proposta.DisabledAt = null;

                await _db.PropostasCompra.AddAsync(proposta);
                await _db.SaveChangesAsync();

                return proposta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRegistrarProposta, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca a proposta ativa do cliente pelo id, com o veiculo, sem rastreamento.</summary>
        public async Task<PropostaCompra?> FindDoClienteByIdAsync(int idPropostaCompra, int idCliente)
        {
            try
            {
                PropostaCompra? proposta = await ConsultaPropostasDoClienteLogado(idCliente)
                    .AsNoTracking()
                    .Where(x => x.IdPropostaCompra == idPropostaCompra)
                    .FirstOrDefaultAsync();

                return proposta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarProposta, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta nomeada: propostas nao desativadas do cliente pedido, com o veiculo. A posse e a propria consulta: vale so
        /// com contexto de cliente positivo, igual ao id pedido e igual ao dono. Sem essa igualdade, nada volta.
        /// </summary>
        public IQueryable<PropostaCompra> ConsultaPropostasDoClienteLogado(int idCliente)
        {
            try
            {
                int idClienteContexto = _db.IdClienteContexto;
                bool posseDoCliente = idClienteContexto > 0 && idClienteContexto == idCliente;

                IQueryable<PropostaCompra> consulta = _db.PropostasCompra
                    .SemEscopoEmpresa()
                    .Where(x => posseDoCliente && x.IdCliente == idCliente && x.DisabledAt == null)
                    .Include(x => x.Veiculo);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarProposta, GetType().ToString());
                throw;
            }
        }
    }
}
