using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Persistencia dos documentos do cliente (cliente_documento) sobre o AutoViaDbContext. Sem filtro "Empresa" (a tabela e do
    /// cliente, da plataforma): o servico so passa o id que veio da sessao. Sem busca por chave e sem SQL bruto (guardas de teste).
    /// Exclusao logica pelo Disable(), nunca Remove(). Todo catch registra a constante da operacao e relanca a excecao.
    /// </summary>
    public sealed class ClienteDocumentoRepository : IClienteDocumentoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ClienteDocumentoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Verdadeiro quando o cliente existe e nao esta desativado.</summary>
        public async Task<bool> ClienteAtivoAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                bool ativo = await _db.Clientes
                    .AsNoTracking()
                    .AnyAsync(x => x.IdCliente == idCliente && x.DisabledAt == null, cancellationToken);

                return ativo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Quantos documentos o cliente enviou desde a data, ativos ou desativados, em um unico COUNT.</summary>
        public async Task<int> CountEnviadosDesdeWithDisabledsAsync(int idCliente, DateTime desde, CancellationToken cancellationToken = default)
        {
            try
            {
                int total = await ConsultaEnviosDesdeWithDisableds(idCliente, desde).CountAsync(cancellationToken);

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Documentos ativos do cliente, sem rastreamento, por id.</summary>
        public async Task<List<ClienteDocumento>> FindAtivosDoClienteAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ClienteDocumento> documentos = await ConsultaAtivosDoCliente(idCliente)
                    .AsNoTracking()
                    .OrderBy(x => x.IdClienteDocumento)
                    .ToListAsync(cancellationToken);

                return documentos;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Desativa os ativos do mesmo cliente e tipo (Disable, com rastreamento) e insere o documento novo, com um unico
        /// SaveChangesAsync. CreatedAt e UpdatedAt do documento novo vem do DateTime.Now deste repositorio.
        /// </summary>
        public async Task<ResultadoSubstituicaoClienteDocumento> SubstituirAsync(ClienteDocumento novo, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ClienteDocumento> ativos = await ConsultaAtivosDoClienteETipo(novo.IdCliente, novo.Tipo).ToListAsync(cancellationToken);

                List<DocumentoClienteDesativado> desativados = new List<DocumentoClienteDesativado>(ativos.Count);
                foreach (ClienteDocumento ativo in ativos)
                {
                    desativados.Add(new DocumentoClienteDesativado(ativo.IdClienteDocumento, ativo.ChaveObjeto));
                    ativo.Disable();
                }

                DateTime agora = DateTime.Now;
                novo.IdClienteDocumento = 0;
                novo.CreatedAt = agora;
                novo.UpdatedAt = agora;
                novo.DisabledAt = null;

                _db.ClientesDocumentos.Add(novo);
                await _db.SaveChangesAsync(cancellationToken);

                return new ResultadoSubstituicaoClienteDocumento(novo, desativados);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGravarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta dos envios do cliente desde a data, ativos OU desativados (o teto conta os substituidos). So monta a consulta,
        /// sem executar: quem executa e CountEnviadosDesdeWithDisabledsAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<ClienteDocumento> ConsultaEnviosDesdeWithDisableds(int idCliente, DateTime desde)
        {
            try
            {
                IQueryable<ClienteDocumento> consulta = _db.ClientesDocumentos
                    .Where(x => x.IdCliente == idCliente && x.CreatedAt >= desde);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta dos documentos ativos do cliente. So monta a consulta, sem executar: quem executa e FindAtivosDoClienteAsync.
        /// Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<ClienteDocumento> ConsultaAtivosDoCliente(int idCliente)
        {
            try
            {
                IQueryable<ClienteDocumento> consulta = _db.ClientesDocumentos
                    .Where(x => x.IdCliente == idCliente && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta dos documentos ativos do cliente de um tipo, com rastreamento (a substituicao os desativa). So monta a consulta,
        /// sem executar: quem executa e SubstituirAsync. Publica para o teste de SQL por ToQueryString.
        /// </summary>
        public IQueryable<ClienteDocumento> ConsultaAtivosDoClienteETipo(int idCliente, TipoDocumentoCliente tipo)
        {
            try
            {
                IQueryable<ClienteDocumento> consulta = _db.ClientesDocumentos
                    .Where(x => x.IdCliente == idCliente && x.Tipo == tipo && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarClienteDocumento, GetType().ToString());
                throw;
            }
        }
    }
}
