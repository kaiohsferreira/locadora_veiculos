using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// IClienteDocumentoRepository em memoria, com as mesmas regras do repositorio real: ativos filtrados por DisabledAt nulo,
    /// a contagem do teto inclui desativados, e a substituicao desativa os ativos do mesmo cliente e tipo antes de inserir o novo.
    /// </summary>
    public sealed class FakeClienteDocumentoRepository : IClienteDocumentoRepository
    {
        private int _proximoId = 1;

        /// <summary>Clientes ativos conhecidos pelo repositorio (id). Cliente fora deste conjunto e tratado como inexistente ou desativado.</summary>
        public HashSet<int> ClientesAtivos { get; } = new HashSet<int>();

        /// <summary>Todos os documentos gravados (ativos e desativados), na ordem de gravacao.</summary>
        public List<ClienteDocumento> Armazenados { get; } = new List<ClienteDocumento>();

        /// <summary>Quando nao for nulo, SubstituirAsync lanca esta excecao antes de gravar (simula falha do banco).</summary>
        public Exception? ExcecaoAoSubstituir { get; set; }

        /// <summary>Quantas vezes SubstituirAsync foi chamado (inclusive as que falharam).</summary>
        public int SubstituicoesChamadas { get; private set; }

        /// <summary>Ultima data passada ao teto de envios.</summary>
        public DateTime? UltimoDesdeConsultado { get; private set; }

        /// <summary>Quantas vezes FindAtivosDoClienteAsync foi chamado (teste: a retirada de outra locadora nao le os documentos).</summary>
        public int ConsultasAtivosDoCliente { get; private set; }

        /// <summary>Semeia um documento (ativo ou desativado) sem passar pelo servico. Id zero recebe um novo id.</summary>
        public ClienteDocumento Semear(ClienteDocumento documento)
        {
            if (documento.IdClienteDocumento == 0)
            {
                documento.IdClienteDocumento = _proximoId;
            }

            if (documento.IdClienteDocumento >= _proximoId)
            {
                _proximoId = documento.IdClienteDocumento + 1;
            }

            Armazenados.Add(Copiar(documento));

            return documento;
        }

        public Task<bool> ClienteAtivoAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ClientesAtivos.Contains(idCliente));
        }

        public Task<int> CountEnviadosDesdeWithDisabledsAsync(int idCliente, DateTime desde, CancellationToken cancellationToken = default)
        {
            UltimoDesdeConsultado = desde;

            int total = Armazenados.Count(x => x.IdCliente == idCliente && x.CreatedAt >= desde);

            return Task.FromResult(total);
        }

        public Task<List<ClienteDocumento>> FindAtivosDoClienteAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            ConsultasAtivosDoCliente++;

            List<ClienteDocumento> ativos = Armazenados
                .Where(x => x.IdCliente == idCliente && x.DisabledAt == null)
                .OrderBy(x => x.IdClienteDocumento)
                .Select(Copiar)
                .ToList();

            return Task.FromResult(ativos);
        }

        public Task<ResultadoSubstituicaoClienteDocumento> SubstituirAsync(ClienteDocumento novo, CancellationToken cancellationToken = default)
        {
            SubstituicoesChamadas++;

            if (ExcecaoAoSubstituir != null)
            {
                throw ExcecaoAoSubstituir;
            }

            List<DocumentoClienteDesativado> desativados = new List<DocumentoClienteDesativado>();
            foreach (ClienteDocumento ativo in Armazenados.Where(x => x.IdCliente == novo.IdCliente && x.Tipo == novo.Tipo && x.DisabledAt == null))
            {
                desativados.Add(new DocumentoClienteDesativado(ativo.IdClienteDocumento, ativo.ChaveObjeto));
                ativo.Disable();
            }

            DateTime agora = DateTime.Now;
            novo.IdClienteDocumento = _proximoId;
            _proximoId++;
            novo.CreatedAt = agora;
            novo.UpdatedAt = agora;
            novo.DisabledAt = null;

            Armazenados.Add(Copiar(novo));

            return Task.FromResult(new ResultadoSubstituicaoClienteDocumento(novo, desativados));
        }

        /// <summary>Copia os campos do documento, para que o chamador nao altere o armazenamento sem passar pelo repositorio.</summary>
        private static ClienteDocumento Copiar(ClienteDocumento origem)
        {
            ClienteDocumento copia = new ClienteDocumento
            {
                IdClienteDocumento = origem.IdClienteDocumento,
                IdCliente = origem.IdCliente,
                Tipo = origem.Tipo,
                ChaveObjeto = origem.ChaveObjeto,
                NomeArquivo = origem.NomeArquivo,
                TipoConteudo = origem.TipoConteudo,
                TamanhoBytes = origem.TamanhoBytes,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }
    }
}
