using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio em memoria da proposta de compra, com as mesmas regras do PropostaCompraRepository real: insert com id
    /// gerado e datas de auditoria; busca so da proposta ativa do proprio cliente.
    /// </summary>
    public class FakePropostaCompraRepository : IPropostaCompraRepository
    {
        private readonly List<PropostaCompra> _propostas = new List<PropostaCompra>();
        private int _proximoId = 1;

        /// <summary>Propostas gravadas, na ordem de insercao (para conferir o que foi salvo).</summary>
        public IReadOnlyList<PropostaCompra> Propostas
        {
            get
            {
                return _propostas;
            }
        }

        public Task<PropostaCompra> InsertAsync(PropostaCompra proposta)
        {
            proposta.IdPropostaCompra = _proximoId;
            _proximoId++;
            proposta.CreatedAt = DateTime.Now;
            proposta.UpdatedAt = proposta.CreatedAt;
            proposta.DisabledAt = null;
            _propostas.Add(proposta);

            return Task.FromResult(proposta);
        }

        public Task<PropostaCompra?> FindDoClienteByIdAsync(int idPropostaCompra, int idCliente)
        {
            PropostaCompra? proposta = _propostas.FirstOrDefault(x => x.IdPropostaCompra == idPropostaCompra
                && x.IdCliente == idCliente
                && x.DisabledAt == null);

            return Task.FromResult(proposta);
        }
    }
}
